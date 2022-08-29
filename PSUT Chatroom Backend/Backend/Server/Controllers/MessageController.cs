using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Entities;
using Server.Dto;
using Server.Dto.Messages;
using Server.Services;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;
using Server.Services.WebSockets;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageController : ControllerBase
{
    public static readonly TimeSpan MessageUnsendWindow = TimeSpan.FromMinutes(5);

    private IQueryable<Message> GetPreparedQueryable(bool metadata = false)
    {
        var q = _dbContext.Messages
            .AsQueryable()
            .Include(m => m.Sender);

        if (!metadata)
        {
            q = q.Include(m => m.DeliveryInfo)
                .ThenInclude(i => i.Recipient);
        }
        return q;
    }

    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly MessageFileManager _messageFileManager;
    private readonly SaltBae _saltBae;
    private readonly WebSocketsContext _socketsContext;
    private readonly WebSocketsManager _socketsManager;
    public MessageController(AppDbContext dbContext, IMapper mapper, MessageFileManager messageFileManager, SaltBae saltBae, WebSocketsManager socketsManager)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _messageFileManager = messageFileManager;
        _saltBae = saltBae;
        _socketsManager = socketsManager;
        _socketsContext = _socketsManager.CreateContext(ConversationController.EventsCategory);
    }

    /// <param name="messagesIds">Ids of the messages to get.</param>
    /// <param name="metadata">Whether to return MessageMetadataDto or MessageDto.</param>
    /// <remarks>
    /// A user can get the messages he is member of their conversations only.
    /// </remarks>
    /// <response code="404">Ids of the non existing messages.</response>
    /// <response code="403">Ids of messages caller is not a member of their conversations or he is not their owner in case of metadata = false.</response>
    [LoggedInFilter]
    [HttpPost("Get")]
    [ProducesResponseType(typeof(MessageMetadataDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get([FromBody] int[] messagesIds, bool metadata = false)
    {
        var messages = GetPreparedQueryable(metadata);

        var existingMessages = messages.Where(m => messagesIds.Contains(m.Id));
        var nonExistignMessages = messagesIds.Except(existingMessages.Select(g => g.Id)).ToArray();

        if (nonExistignMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status404NotFound,
               new ErrorDto
               {
                   Description = "The following messages don't exist.",
                   Data = new() { ["NonExistingMessages"] = nonExistignMessages }
               });
        }

        var user = this.GetUser()!;

        var noAccessMessages = await existingMessages
            .Where(m => m.Conversation.Group != null ?
                        !m.Conversation.Group.Members.Any(gm => gm.UserId == user.Id) :
                        !m.Conversation.Members.Any(cm => cm.Id == user.Id))
            .Select(m => m.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
        if (noAccessMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You are not a member in the following messages conversations.",
                    Data = new() { ["NoAccessMessages"] = noAccessMessages }
                });
        }

        if (!metadata)
        {
            var notOwnedMessages = await existingMessages
                .Where(m => m.SenderId != user.Id)
                .Select(m => m.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notOwnedMessages.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You don't own the following messages.",
                        Data = new() { ["NotOwnedMessages"] = notOwnedMessages }
                    });
            }
        }
        if (metadata)
        {
            return Ok(_mapper.ProjectTo<MessageMetadataDto>(existingMessages));
        }
        return Ok(_mapper.ProjectTo<MessageDto>(existingMessages));
    }

    /// <summary>
    /// Get all ids of all messages in conversation with pagination and registers reading time for returned messages.
    /// Messages will be in descending order by sending time.
    /// </summary>
    /// <response code="200">Ids of all messages in the requested conversation.</response>
    [LoggedInFilter]
    [HttpPost("GetInConversation")]
    [ProducesResponseType(typeof(int[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<int[]>> GetInConversation([FromBody] GetInConversationDto dto)
    {
        var user = this.GetUser()!;

        var messages = _dbContext.Messages
            .Where(m => m.ConversationId == dto.ConversationId)
            .Where(m => !m.DeliveryInfo.Any(di => di.RecipientId == user.Id && di.IsDeleted))
            .OrderByDescending(m => m.SendingTime)
            .Skip(dto.Offset)
            .Take(dto.Count);

        //Register reading time
        var newlyReadMessagesIds = await messages
            .Where(m => !m.DeliveryInfo.Any(di => di.RecipientId == user.Id))
            .Select(m => m.Id).ToArrayAsync()
            .ConfigureAwait(false);
        var newMessagesInfo = newlyReadMessagesIds
           .Select(id => new MessageDeliveryInfo
           {
               MessageId = id,
               RecipientId = user.Id,
               ReadingTime = DateTimeOffset.Now
           });
        await _dbContext.MessagesDeliveryInfo.AddRangeAsync(newMessagesInfo).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        var messagesIds = await messages.Select(m => m.Id).ToArrayAsync().ConfigureAwait(false);
        return Ok(messagesIds);
    }

    /// <summary>
    /// Get all ids of all messages in conversation with pagination and registers reading time for returned messages.
    /// Messages will be in descending order by sending time.
    /// </summary>
    /// <response code="200">Ids of all messages in the requested conversation.</response>
    [LoggedInFilter]
    [HttpPost("GetMessagesInConversation")]
    [ProducesResponseType(typeof(MessageMetadataDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageMetadataDto[]>> GetMessagesInConversation([FromBody] GetInConversationDto dto)
    {
        var user = this.GetUser()!;

        var messages = GetPreparedQueryable(false)
            .Where(m => m.ConversationId == dto.ConversationId)
            .Where(m => !m.DeliveryInfo.Any(di => di.RecipientId == user.Id && di.IsDeleted))
            .OrderBy(m => m.SendingTime)
            .Skip(dto.Offset)
            .Take(dto.Count);

        //Register reading time
        var newlyReadMessagesIds = await messages
            .Where(m => !m.DeliveryInfo.Any(di => di.RecipientId == user.Id))
            .Select(m => m.Id).ToArrayAsync()
            .ConfigureAwait(false);
        var newMessagesInfo = newlyReadMessagesIds
           .Select(id => new MessageDeliveryInfo
           {
               MessageId = id,
               RecipientId = user.Id,
               ReadingTime = DateTimeOffset.Now
           });
        await _dbContext.MessagesDeliveryInfo.AddRangeAsync(newMessagesInfo).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        var Result = _mapper.ProjectTo<MessageMetadataDto>(messages);
        return Ok(Result);
    }


    /// <summary>
    /// Creates a message with the caller as its sender.
    /// </summary>
    /// <param name="dto">New message info.</param>
    /// <response code="201">The created message metadata.</response>
    [LoggedInFilter]
    [HttpPost("Create")]
    [ProducesResponseType(typeof(MessageMetadataDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MessageMetadataDto>> Create([FromBody] CreateMessageDto dto)
    {
        var user = this.GetUser()!;
        Message message = new()
        {
            Content = dto.Content,
            ConversationId = dto.ConversationId,
            ReferencedMessageId = dto.ReferencedMessageId,
            SenderId = user.Id,
            SendingTime = DateTime.UtcNow,
            EncryptionSalt = new byte[] { }
        };

        await _dbContext.Messages.AddAsync(message).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        message.EncryptionSalt = _saltBae.SaltSteak(user, message.Id);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        if (dto.Attachment != null)
        {
            await _messageFileManager.SaveBase64File(message, dto.Attachment.FileContentBase64).ConfigureAwait(false);
            message.AttachmentFileName = dto.Attachment.FileName;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        message.Sender = user;
        var messageDto = _mapper.Map<MessageMetadataDto>(message);
        var conversation = await _dbContext.Conversations
            .Include(s => s.Members)
            .Include(s => s.Group)
            .ThenInclude(s => s.Members)
            .FirstAsync(c => c.Id == dto.ConversationId)
            .ConfigureAwait(false);

        var conversationMembers = (conversation.IsDirect ? conversation.Members.Select(m => m.Id) : conversation.Group.Members.Select(m => m.UserId)).ToHashSet();
        await _socketsContext.SendToUsers(conversation.Id, conversationMembers, ConversationController.Events.NewMessage, messageDto);
        await _socketsContext.SendToUsers(conversationMembers, ConversationController.Events.NewMessage, messageDto);

        return CreatedAtAction(nameof(Get),
            new { messagesIds = new int[] { message.Id }, metadata = true },
            messageDto);
    }

    /// <summary>
    /// Marks a message as DELETED for caller.
    /// It won't delete it from conversation.
    /// </summary>
    /// <param name="messagesIds">Ids of the messages to mark as deleted.</param>
    /// <response code="204">Messages are marked as deleted successfully.</response>
    /// <response code="404">Id of the non existing messages.</response>
    /// <response code="403">Ids of the messages user has no access to their conversations.</response>
    [LoggedInFilter]
    [HttpDelete("Delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete([FromBody] int[] messagesIds)
    {
        var messages = _dbContext.Messages.AsQueryable();

        var existingMessages = messages.Where(m => messagesIds.Contains(m.Id));
        var nonExistignMessages = messagesIds.Except(existingMessages.Select(g => g.Id)).ToArray();

        if (nonExistignMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status404NotFound,
               new ErrorDto
               {
                   Description = "The following messages don't exist.",
                   Data = new() { ["NonExistingMessages"] = nonExistignMessages }
               });
        }

        var user = this.GetUser()!;

        var noAccessMessages = await existingMessages
            .Where(m => m.Conversation.Group != null ?
                        !m.Conversation.Group.Members.Any(gm => gm.UserId == user.Id) :
                        !m.Conversation.Members.Any(cm => cm.Id == user.Id))
            .Select(m => m.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
        if (noAccessMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You are not a member in the following messages conversations.",
                    Data = new() { ["NoAccessMessages"] = noAccessMessages }
                });
        }

        var existingMessagesInfo = existingMessages.Select(m => m.DeliveryInfo.First(d => d.RecipientId == user.Id));
        foreach (var info in existingMessagesInfo)
        {
            info.IsDeleted = true;
        }

        var newMessagesInfo = messagesIds
            .Except(existingMessagesInfo.Select(m => m.MessageId))
            .Select(id => new MessageDeliveryInfo
            {
                MessageId = id,
                RecipientId = user.Id,
                IsDeleted = true
            });

        await _dbContext.MessagesDeliveryInfo.AddRangeAsync(newMessagesInfo).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        return Ok();
    }

    /// <summary>
    /// Unsend messages.
    /// </summary>
    /// <param name="messagesIds"> Ids of the messages to unsend. </param>
    /// <remarks> There is a time window for user to delete a message after sending. </remarks>
    /// <response code="204">Messages are unsent successfully.</response>
    /// <response code="404">Id of the non existing messages.</response>
    /// <response code="403">Ids of the messages caller doesn't own or they are past deleteing window.</response>
    [LoggedInFilter]
    [HttpPost("Unsend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Unsend([FromBody] int[] messagesIds)
    {
        var messages = _dbContext.Messages.AsQueryable();

        var existingMessages = messages.Where(m => messagesIds.Contains(m.Id));
        var nonExistignMessages = messagesIds.Except(existingMessages.Select(g => g.Id)).ToArray();

        if (nonExistignMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status404NotFound,
               new ErrorDto
               {
                   Description = "The following messages don't exist.",
                   Data = new() { ["NonExistingMessages"] = nonExistignMessages }
               });
        }

        var user = this.GetUser()!;

        var notOwnedMessages = await existingMessages
            .Where(m => m.SenderId != user.Id)
            .Select(m => m.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
        if (notOwnedMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You don't own the following messages.",
                    Data = new() { ["NotOwnedMessages"] = notOwnedMessages }
                });
        }
        var minDeleteTime = DateTimeOffset.Now - MessageUnsendWindow;
        var pastUnsendWindowMessages = await existingMessages
            .Where(m => m.SendingTime < minDeleteTime)
            .Select(m => m.Id)
            .ToArrayAsync()
            .ConfigureAwait(false);
        if (pastUnsendWindowMessages.Length > 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You can't unsend the following messages after unsend window.",
                    Data = new()
                    {
                        ["PastUnsendWindowMessages"] = pastUnsendWindowMessages,
                        ["UnsendWindow"] = MessageUnsendWindow
                    }
                });
        }

        _dbContext.Messages.RemoveRange(existingMessages);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        return NoContent();
    }
    /// <summary>
    /// Gets a message attachment as downloadable file.
    /// </summary>
    /// <param name="messageId"> Id of the message to get its attachment. </param>
    /// <remarks> Caller must be a member of message conversation to download its attachment. </remarks>
    /// <response code="200">Message attachment stream.</response>
    /// <response code="404">Id of the non existing message.</response>
    /// <response code="403">Caller doesn't have access to message or it doesn't have an attachment.</response>
    [LoggedInFilter]
    [HttpGet("GetAttachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAttachment([FromQuery] int messageId)
    {
        var messages = _dbContext.Messages
            .Include(m => m.Conversation)
            .ThenInclude(c => c.Members);

        var message = await messages.FirstAsync(m => m.Id == messageId).ConfigureAwait(false);
        if (message == null)
        {
            return StatusCode(StatusCodes.Status404NotFound,
               new ErrorDto
               {
                   Description = "The following messag doesn't exist.",
                   Data = new() { ["MessageId"] = messageId }
               });
        }

        var user = this.GetUser()!;

        var isAccessable = true;
        if (message.Conversation.IsDirect)
        {
            isAccessable = message.Conversation.Members.Any(u => u.Id == user.Id);
        }
        else
        {
            isAccessable = await _dbContext.GroupsMembers
                .AnyAsync(gm => gm.GroupId == message.Conversation.GroupId && gm.UserId == user.Id)
                .ConfigureAwait(false);
        }
        if (!isAccessable)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new ErrorDto
                {
                    Description = "You don't belong to the following message conversation.",
                    Data = new() { ["MessageId"] = messageId }
                });
        }
        if (message.AttachmentFileName == null)
        {
            return StatusCode(StatusCodes.Status404NotFound,
                new ErrorDto
                {
                    Description = "The following message doesn't have an attachment.",
                    Data = new() { ["MessageId"] = messageId }
                });
        }
        FileExtensionContentTypeProvider mimeProvider = new();

        if (!mimeProvider.TryGetContentType(message.AttachmentFileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return File(_messageFileManager.GetFile(message), contentType, message.AttachmentFileName, true);
    }
}
