using System;
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
using Server.Dto.GroupMembers;
using Server.Dto.Groups;
using Server.Dto.Users;
using Server.Services;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GroupController : ControllerBase
    {
        private IQueryable<Group> GetPreparedQueryable(bool metadata = false)
        {
            var q = _dbContext.Groups.AsQueryable();
            q = q.Include(q => q.Conversation)
                .Include(q => q.Section);
            if (!metadata)
            {
                q = q.Include(u => u.Members)
                    .ThenInclude(s => s.User);
            }
            return q;
        }

        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly GroupFileManager _groupFileManager;
        private readonly SaltBae _saltBae;
        public GroupController(AppDbContext dbContext, IMapper mapper, GroupFileManager groupFileManager, SaltBae saltBae)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _groupFileManager = groupFileManager;
            _saltBae = saltBae;
        }
        /// <summary>
        /// Ids of all groups caller is a member of.
        /// </summary>
        [LoggedInFilter]
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(int[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<int[]>> GetAll([FromBody] GetAllDto dto)
        {
            var user = this.GetUser()!;
            var groups = await _dbContext.Groups
                .Where(g => g.Members!.Any(gm => gm.UserId == user.Id))
                .Select(g => g.Id)
                .Skip(dto.Offset)
                .Take(dto.Count)
                .ToArrayAsync()
                .ConfigureAwait(false);
            return Ok(groups);
        }

        [LoggedInFilter]
        [HttpPost("GetGroups")]
        [ProducesResponseType(typeof(GroupMetadataDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<GroupMetadataDto[]>> GetGroups([FromBody] GetAllDto dto)
        {
            var user = this.GetUser()!;
            var groups = await _dbContext.Groups
                .Where(g => g.Members!.Any(gm => gm.UserId == user.Id))
                .Include(q => q.Conversation)
                .Include(q => q.Members).ThenInclude(s => s.User)
                .OrderByDescending(g => g.Id)
                .Skip(dto.Offset)
                .Take(dto.Count)
                .ToArrayAsync()
                .ConfigureAwait(false);

            return Ok(_mapper.Map<GroupMetadataDto[]>(groups));
        }

        /// <param name="groupsIds">Ids of the groups to get.</param>
        /// <param name="metadata">Whether to return GroupMetadataDto or GroupDto.</param>
        /// <remarks>
        /// A user can get the groups he is a member of only.
        /// </remarks>
        /// <response code="404">Ids of the non existing groups.</response>
        /// <response code="403">Ids of groups caller has no access rights to.</response>
        [LoggedInFilter]
        [HttpPost("Get")]
        [ProducesResponseType(typeof(GroupMetadataDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GroupDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Get([FromBody] int[] groupsIds, bool metadata = false)
        {
            var groups = GetPreparedQueryable(metadata);

            var existingGroups = groups.Where(g => groupsIds.Contains(g.Id));
            var nonExistingGroups = groupsIds.Except(existingGroups.Select(g => g.Id)).ToArray();

            if (nonExistingGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following groups don't exist.",
                       Data = new() { ["NonExistingGroups"] = nonExistingGroups }
                   });
            }

            var user = this.GetUser()!;

            var notMemberGroups = await existingGroups
                .Where(g => !g.Members!.Any(m => m.UserId == user.Id))
                .Select(g => g.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notMemberGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You are not a member of the following groups.",
                        Data = new() { ["NotMemberGroups"] = notMemberGroups }
                    });
            }
            var result = await existingGroups.ToArrayAsync().ConfigureAwait(false);
            if (metadata)
            {
                return Ok(_mapper.Map<GroupMetadataDto[]>(result));
            }
            return Ok(_mapper.Map<GroupDto[]>(result));
        }
        /// <summary>
        /// Creates a group with the logged in user as admin.
        /// </summary>
        /// <param name="dto">New group info.</param>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// </remarks>
        /// <response code="201">The created group metadata.</response>
        /// <response code="404">Ids of the non existing user.</response>
        /// <response code="403">User can't create a conversation with himself.</response>
        [InstructorFilter]
        [HttpPost("Create")]
        [ProducesResponseType(typeof(GroupMetadataDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupMetadataDto>> Create([FromBody] CreateGroupDto dto)
        {
            var group = new Group
            {
                Conversation = new(),
                Name = dto.Name,
                Members = new GroupMember[]
                {
                    new GroupMember
                    {
                        IsAdmin = true,
                        UserId = this.GetUser()!.Id
                    }
                },
                EncryptionSalt = new byte[] { }
            };
            await _dbContext.Groups.AddAsync(group).ConfigureAwait(false);
            group.Conversation.Group = group;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            group.EncryptionSalt = _saltBae.SaltSteak(this.GetUser()!, group.Id);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            if (dto.GroupPictureJpgBase64 != null)
            {
                await _groupFileManager.SaveBase64Image(group, dto.GroupPictureJpgBase64).ConfigureAwait(false);
            }

            return CreatedAtAction(nameof(Get), new { gropsIds = new int[] { group.Id }, metadata = true }, _mapper.Map<GroupMetadataDto>(group));
        }
        /// <summary>
        /// Delete groups with all their members and conversations.
        /// </summary>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// Caller should be an ADMIN in group to delete it.
        /// </remarks>
        /// <param name="groupsIds">Ids of the groups to delete.</param>
        /// <response code="204">Groups are deleted successfully.</response>
        /// <response code="404">Id of the non existing groups.</response>
        /// <response code="403">Ids of the groups caller is not an admin in.</response>
        [InstructorFilter]
        [HttpDelete("Delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete([FromBody] int[] groupsIds)
        {
            var existingGroups = _dbContext.Groups.Where(g => groupsIds.Contains(g.Id));
            var nonExistignGroups = groupsIds.Except(existingGroups.Select(g => g.Id)).ToArray();

            if (nonExistignGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following groups don't exist.",
                       Data = new() { ["NonExistingGroups"] = nonExistignGroups }
                   });
            }

            var user = this.GetUser()!;

            var notMemberGroups = await existingGroups
                .Where(g => !g.Members.Any(m => m.UserId == user.Id))
                .Select(g => g.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notMemberGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You are not a member of the following groups.",
                        Data = new() { ["NotMemberGroups"] = notMemberGroups }
                    });
            }

            var notAdminGroups = await existingGroups
                .Where(g => !g.Members.First(m => m.UserId == user.Id).IsAdmin)
                .Select(g => g.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notAdminGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You are not an admin in the following groups.",
                        Data = new() { ["NotAdminGroups"] = notAdminGroups }
                    });
            }
            foreach (var g in existingGroups)
            {
                _groupFileManager.DeleteFile(g);
            }
            _dbContext.Groups.RemoveRange(existingGroups);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return NoContent();
        }
        /// <summary>
        /// Updates a group.
        /// </summary>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// Caller must be an admin in the group.
        /// </remarks>
        /// <param name="update">The update to apply, null fields mean no update to this property.</param>
        /// <response code="204">The update was done successfully.</response>
        [InstructorFilter]
        [HttpPatch("Update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([FromBody] UpdateGroupDto update)
        {
            var group = await _dbContext.Groups.FindAsync(update.Id).ConfigureAwait(false);
            if (update.Name != null)
            {
                group.Name = update.Name;
            }
            if (update.GroupPictureJpgBase64 != null)
            {
                await _groupFileManager.SaveBase64Image(group, update.GroupPictureJpgBase64).ConfigureAwait(false);
            }
            if (update.DeletePicture == true)
            {
                _groupFileManager.DeleteFile(group);
            }

            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
        /// <summary>
        /// Gets the group picture as stream of bytes with header Content-Type: image/jpeg.
        /// </summary>
        /// <param name="groupId">Id of the group to get its picture.</param>
        /// <response code="200">The group picture.</response>
        /// <response code="204">If the group doesn't have a picture.</response>
        /// <response code="404">If there is no group with this id.</response>
        [HttpGet("GetPicture")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPicture([FromQuery] int groupId)
        {
            var group = await _dbContext.Groups.FindAsync(groupId).ConfigureAwait(false);
            if (group == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                    new ErrorDto
                    {
                        Description = "There is no group with the following Id.",
                        Data = new() { ["GroupId"] = groupId }
                    });
            }

            var groupImage = _groupFileManager.GetFile(group);
            if (groupImage == null) { return NoContent(); }

            var result = File(groupImage, "image/jpeg");
            result.FileDownloadName = $"Group_{groupId}_Picture.jpg";
            return result;
        }
    }
}