using System;
using System.Collections;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Entities;
using Server.Dto;
using Server.Dto.Conversations;
using Server.Dto.Groups;
using Server.Dto.Users;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;
using Server.Services.WebSockets;
//TODO: GetAll add direct or groups
namespace Server.Controllers
{
    /*
    No update or delete
    Conversations between Instructor and student can be closed by instructor
    */
    [ApiController]
    [Route("[controller]")]
    public class ConversationController : ControllerBase
    {
        public enum Events
        {
            NewMessage,
            MessageUnsent,
            ConversationOpened,
            ConversationClosed
        }
        public static string EventsCategory { get; } = "Conversation";
        private IQueryable<Conversation> GetPreparedQueryable(bool metadata = false)
        {
            var q = _dbContext.Conversations.AsQueryable();
            q = q.Include(u => u.Members);
            if (!metadata)
            {
                q = q.Include(c => c.Group)
                .ThenInclude(g => g.Members)
                .ThenInclude(gm => gm.User);
                q = q.Include(c => c.Group)
                .ThenInclude(g => g.Section);
            }

            return q;
        }

        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly WebSocketsContext _socketsContext;
        private readonly WebSocketsManager _socketsManager;
        public ConversationController(AppDbContext dbContext, IMapper mapper, WebSocketsManager socketsManager)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _socketsManager = socketsManager;
            _socketsContext = _socketsManager.CreateContext(EventsCategory);
        }
        [LoggedInFilter]
        [HttpGet("Subscribe")]
        public async Task<IActionResult> Subscribe([FromQuery] int? conversationId = null)
        {
            await using var socket = await _socketsManager.CreateSocket(EventsCategory, conversationId).ConfigureAwait(false);
            await socket.Wait().ConfigureAwait(false);
            return Ok();
        }
        /// <summary>
        /// Ids of all conversations logged in user is a member of.
        /// </summary>
        [LoggedInFilter]
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(int[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<int[]>> GetAll([FromBody] GetAllConversationsDto dto)
        {
            var user = this.GetUser()!;
            var conversations = _dbContext.Conversations.AsQueryable();
            if (!dto.IsDirect)
            {
                conversations = conversations.Where(c => c.Group != null && c.Group!.Members!.Any(gm => gm.UserId == user.Id));
            }
            else
            {
                conversations = conversations.Where(c => c.Members.Any(u => u.Id == user.Id));
            }
            var ids = await conversations
            .Select(c => c.Id)
            .Skip(dto.Offset)
            .Take(dto.Count)
            .ToArrayAsync()
            .ConfigureAwait(false);
            return Ok(ids);
        }
        /// <param name="conversationsIds">Ids of the conversations to get.</param>
        /// <param name="metadata">Whether to return ConversationMetadataDto or ConversationDto.</param>
        /// <remarks>
        /// A user can get the conversations he is a member of only.
        /// </remarks>
        /// <response code="404">Ids of the non existing converstaions.</response>
        /// <response code="403">Ids of conversations the user has no access rights to.</response>
        [LoggedInFilter]
        [HttpPost("Get")]
        [ProducesResponseType(typeof(ConversationMetadataDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ConversationDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get([FromBody] int[] conversationsIds, bool metadata = false)
        {
            var conversations = GetPreparedQueryable(metadata);

            var existingConversations = conversations.Where(g => conversationsIds.Contains(g.Id));
            var nonExistingConversations = conversationsIds.Except(existingConversations.Select(g => g.Id)).ToArray();

            if (nonExistingConversations.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following conversations don't exist.",
                       Data = new() { ["NonExistingConversations"] = nonExistingConversations }
                   });
            }

            var user = this.GetUser()!;

            var notMemberConversations = await existingConversations
                .Where(c => c.GroupId != null ?
                c.Group!.Members!.All(gm => gm.UserId != user.Id) :
                c.Members.All(u => u.Id != user.Id))
                .Select(g => g.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);

            if (notMemberConversations.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You are not a member of the following conversations.",
                        Data = new() { ["NotMemberConversations"] = notMemberConversations }
                    });
            }

            if (metadata)
            {
                return Ok(_mapper.Map<ConversationMetadataDto[]>(await existingConversations.ToArrayAsync().ConfigureAwait(false)));
            }
            return Ok(_mapper.Map<ConversationDto[]>(await existingConversations.ToArrayAsync().ConfigureAwait(false)));
        }
        /// <summary>
        /// Creates a DIRECT conversation between logged in user and user sepcified by userId.
        /// </summary>
        /// <param name="userId">Id of the user to create a direct conversation with.</param>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// If there already exists a conversation with the specified user then it would be opened (if closed) and returned.
        /// </remarks>
        /// <response code="404">Ids of the non existing user.</response>
        /// <response code="403">User can't create a conversation with himself.</response>
        [InstructorFilter]
        [HttpPost("Create")]
        [ProducesResponseType(typeof(ConversationMetadataDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConversationMetadataDto>> Create([FromQuery] int userId)
        {
            var caller = this.GetUser()!;
            if (caller.Id == userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You can't start a conversation with yourself.",
                    Data = new() { ["LoggedInUserId"] = userId }
                });
            }
            var target = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            if (target == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                new ErrorDto
                {
                    Description = "There is no user with the following id.",
                    Data = new() { ["UserId"] = userId }
                });
            }
            var conversation = await _dbContext.Conversations
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Members.Any(m => m.Id == caller.Id) && c.Members.Any(m => m.Id == userId))
                .ConfigureAwait(false);
            if (conversation == null)
            {
                var user = await _dbContext.Users.FindAsync(caller.Id).ConfigureAwait(false);
                conversation = new()
                {
                    GroupId = null,
                    Members = new User[] { user, target },
                    IsClosed = false
                };
                await _dbContext.Conversations.AddAsync(conversation).ConfigureAwait(false);
            }
            else if (conversation.IsClosed)
            {
                conversation.IsClosed = false;
            }
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            var conversationDto = _mapper.Map<ConversationMetadataDto>(conversation);
            //Send to all users in this entity
            //Send to this to entity but inovlved users
            var conversationMembers = (conversation.IsDirect ? conversation.Members.Select(m => m.Id) : conversation.Group.Members.Select(m => m.UserId)).ToHashSet();
            await _socketsContext.SendToUsers(conversation.Id, conversationMembers, Events.ConversationOpened, conversationDto);
            await _socketsContext.SendToUsers(conversationMembers, Events.ConversationOpened, conversationDto);

            return CreatedAtAction(nameof(Get), new { conversationsIds = new int[] { conversation.Id }, metadata = true }, conversationDto);
        }
        /// <summary>
        /// Close the DIRECT conversation specified by conversationId.
        /// </summary>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// </remarks>
        /// <param name="conversationId">Ids of the conversations to close.</param>
        /// <response code="204">Conversation is closed successfully.</response>
        /// <response code="404">Id of the non existing conversation.</response>
        /// <response code="403">Only direct conversation can be closed or conversations with students.</response>
        [InstructorFilter]
        [HttpPost("Close")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Close([FromQuery] int conversationId)
        {
            var conversations = GetPreparedQueryable(true);
            var conversation = await conversations.FirstOrDefaultAsync(c => c.Id == conversationId).ConfigureAwait(false);
            if (conversation == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                new ErrorDto
                {
                    Description = "There is no conversation with the following id.",
                    Data = new() { ["ConversationId"] = conversationId }
                });
            }
            if (!conversation.IsDirect)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "Only direct conversations with students can be closed."
                });
            }
            if (conversation.Members!.All(u => u.IsInstructor))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "Only direct conversations with students can be closed."
                });
            }
            conversation.IsClosed = true;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            dynamic notificationData = new ExpandoObject();
            notificationData.ConversationId = conversation.Id;
            var caller = this.GetUser()!;
            var conversationMembers = (conversation.IsDirect ? conversation.Members.Select(m => m.Id) : conversation.Group.Members.Select(m => m.UserId)).ToHashSet();
            await _socketsContext.SendToUsers(conversation.Id, conversationMembers, Events.ConversationClosed, notificationData).ConfigureAwait(false);
            await _socketsContext.SendToUsers(conversationMembers, Events.ConversationClosed, notificationData);
            return NoContent();
        }
    }
}