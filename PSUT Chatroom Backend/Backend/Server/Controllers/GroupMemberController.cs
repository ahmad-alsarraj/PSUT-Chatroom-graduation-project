using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegnewCommon;
using Server.Db;
using Server.Db.Entities;
using Server.Dto;
using Server.Dto.GroupMembers;
using Server.Dto.Groups;
using Server.Dto.Users;
using Server.Services.UserSystem;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GroupMemberController : ControllerBase
    {
        private IQueryable<GroupMember> GetPreparedQueryable()
        {
            var q = _dbContext.GroupsMembers.AsQueryable()
                .Include(m => m.User)
                .Include(m => m.Group);

            return q;
        }
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public GroupMemberController(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        /// <summary>
        /// Adds a new member to a group.
        /// </summary>
        /// <param name="dto">New group member info.</param>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// Caller must be an admin in group.
        /// </remarks>
        /// <response code="201">The created group member.</response>
        [InstructorFilter]
        [HttpPost("Create")]
        [ProducesResponseType(typeof(GroupMemberDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupMemberDto>> Create([FromBody] CreateGroupMemberDto dto)
        {
            var targetUser = await _dbContext.Users.FindAsync(dto.UserId).ConfigureAwait(false);
            GroupMember member = new()
            {
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                User = targetUser,
                IsAdmin = dto.IsAdmin
            };
            await _dbContext.GroupsMembers.AddAsync(member).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            return StatusCode(StatusCodes.Status201Created, _mapper.Map<GroupMemberDto>(member));
        }
        /// <summary>
        /// Removes a user from group.
        /// </summary>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// Caller should be an ADMIN in group to remove a user from it.
        /// </remarks>
        /// <param name="groupsMembersIds">Ids of the groups members to remove.</param>
        /// <response code="204">Members are deleted successfully.</response>
        /// <response code="404">Id of the non existing members.</response>
        /// <response code="403">Ids of the groups caller is not an admin in.</response>
        [InstructorFilter]
        [HttpDelete("Delete")]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete([FromBody] int[] groupsMembersIds)
        {
            //TODO: validate there still would be admins in group.
            var groupsMembers = GetPreparedQueryable();

            var existingGroupsMembers = groupsMembers.Where(m => groupsMembersIds.Contains(m.Id));
            var nonExistignGroupsMembers = groupsMembersIds.Except(existingGroupsMembers.Select(g => g.Id)).ToArray();

            if (nonExistignGroupsMembers.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following groups members don't exist.",
                       Data = new() { ["NonExistingGroupsMembers"] = nonExistignGroupsMembers }
                   });
            }

            var user = this.GetUser()!;

            var notAdminGroups = await existingGroupsMembers
                .Where(m => !m.Group.Members.Any(gm => gm.UserId == user.Id && gm.IsAdmin))
                .Select(g => g.GroupId)
                .Distinct()
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notAdminGroups.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You are not an admin of the following groups.",
                        Data = new() { ["NotAdminGroups"] = notAdminGroups }
                    });
            }

            _dbContext.GroupsMembers.RemoveRange(existingGroupsMembers);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return Ok();
        }

        [InstructorFilter]
        [HttpDelete("DeleteOne")]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteOne([FromBody] DeleteGroupMemberDto deleteDto)
        {
            var groupsMembers = GetPreparedQueryable();

            var member = await groupsMembers
                .FirstOrDefaultAsync(m => m.GroupId == deleteDto.GroupId && m.UserId == deleteDto.UserId)
                .ConfigureAwait(false);

            if (member == null)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The group member with identified by user id does not exist.",
                       Data = new() { ["userId"] = deleteDto.UserId }
                   });
            }

            _dbContext.GroupsMembers.Remove(member);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return Ok();
        }

        [InstructorFilter]
        [HttpGet("GetStudentsNotInGroup/{groupId:int}")]
        [ProducesResponseType(typeof(IEnumerable<UserMetadataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStudentsNotInGroup(int groupId)
        {
            var users = _dbContext.Users.AsQueryable();
            var groupMembers = _dbContext.GroupsMembers.AsQueryable().Include(m => m.User);

            var nonExistingUsers = users
                .Where(u => u.Role == UserRole.Student && !groupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == u.Id));

            var user = this.GetUser();
            if (!(user?.IsInstructor ?? false))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "Only instructor can get users",
                        Data = new Dictionary<string, object> { ["NotAllowed"] = "NotAllowed" }
                    });
            }

            return Ok(_mapper.ProjectTo<UserMetadataDto>(nonExistingUsers));
        }

        /// <summary>
        /// Updates a group member.
        /// </summary>
        /// <remarks>
        /// INSTRUCTOR ONLY.
        /// Caller must be an admin in the group.
        /// </remarks>
        /// <param name="update">The update to apply, null fields mean no update to this property.</param>
        /// <response code="204">The update was done successfully.</response>
        [InstructorFilter]
        [HttpPatch("Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UpdateGroupMemberDto update)
        {
            //Todo: maybe disallow modifications if its a sections group ?
            var member = await _dbContext.GroupsMembers.FindAsync(update.Id).ConfigureAwait(false);
            if (update.IsAdmin != null)
            {
                member.IsAdmin = update.IsAdmin.Value;
            }

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return Ok();
        }
    }
}