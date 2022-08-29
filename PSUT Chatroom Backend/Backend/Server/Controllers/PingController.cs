using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Entities;
using Server.Dto;
using Server.Dto.GroupMembers;
using Server.Dto.Groups;
using Server.Dto.Pings;
using Server.Services.UserSystem;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        private IQueryable<Ping> GetPreparedQueryable()
        {
            var q = _dbContext.Pings.AsQueryable()
                .Include(m => m.Sender)
                .Include(m => m.Recipient);

            return q;
        }

        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public PingController(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        /// <summary>
        /// Ids of all pings caller is a member of.
        /// </summary>
        [LoggedInFilter]
        [HttpPost("GetAll")]
        [ProducesResponseType(typeof(IEnumerable<int>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<int>>> GetAll([FromBody] GetAllDto dto)
        {
            var user = this.GetUser()!;
            var pings = await _dbContext.Pings
                .Where(p => p.SenderId == user.Id || p.RecipientId == user.Id)
                .Select(p => p.Id)
                .Skip(dto.Offset)
                .Take(dto.Count)
                .ToArrayAsync()
                .ConfigureAwait(false);
            return Ok(pings);
        }
        /// <param name="pingsIds">Ids of the pings to get.</param>
        /// <remarks>
        /// A user can get the pings he is a member of only.
        /// </remarks>
        /// <response code="404">Ids of the non existing pings.</response>
        /// <response code="403">Ids of pings the caller has no access rights to.</response>
        [LoggedInFilter]
        [HttpPost("Get")]
        [ProducesResponseType(typeof(PingDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<PingDto>>> Get([FromBody] int[] pingsIds)
        {
            var pings = GetPreparedQueryable();

            var existingPings = pings.Where(g => pingsIds.Contains(g.Id));
            var nonExistingPings = pingsIds.Except(existingPings.Select(g => g.Id)).ToArray();

            if (nonExistingPings.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following pings don't exist.",
                       Data = new() { ["NonExistingPings"] = nonExistingPings }
                   });
            }
            var user = this.GetUser()!;

            var noAccessPings = await existingPings
                .Where(p => p.SenderId != user.Id && p.RecipientId != user.Id)
                .Select(p => p.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);

            if (noAccessPings.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You don't have access to the following pings.",
                        Data = new() { ["NoAccessPings"] = noAccessPings }
                    });
            }
            return Ok(_mapper.ProjectTo<PingDto>(existingPings));
        }
        /// <summary>
        /// Creates a new ping with caller as sender.
        /// </summary>
        /// <param name="dto">New ping info.</param>
        /// <response code="201">The created ping.</response>
        [LoggedInFilter]
        [HttpPost("Create")]
        [ProducesResponseType(typeof(PingDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<PingDto>> Create([FromBody] CreatePingDto dto)
        {
            var user = this.GetUser()!;
            Ping ping = new()
            {
                RecipientId = dto.RecipientId,
                SenderId = user.Id,
                Content = dto.Content
            };
            await _dbContext.Pings.AddAsync(ping).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            ping = await GetPreparedQueryable().FirstAsync(p => p.Id == ping.Id).ConfigureAwait(false);

            return CreatedAtAction(nameof(Get), new { pingsIds = new int[] { ping.Id } }, _mapper.Map<PingDto>(ping));
        }
        /// <summary>
        /// Delete specified pings.
        /// </summary>
        /// <remarks>
        /// Caller must the sender or recipient of the ping to delete it.
        /// </remarks>
        /// <param name="pingsIds">Ids of the pings to delete.</param>
        /// <response code="204">Pings are deleted successfully.</response>
        /// <response code="404">Id of the non existing pings.</response>
        /// <response code="403">Ids of the pings caller has no access rights to.</response>
        [LoggedInFilter]
        [HttpDelete("Delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete([FromBody] int[] pingsIds)
        {
            var existingPings = _dbContext.Pings.Where(p => pingsIds.Contains(p.Id));
            var nonExistignPings = pingsIds.Except(existingPings.Select(g => g.Id)).ToArray();

            if (nonExistignPings.Length > 0)
            {
                return StatusCode(StatusCodes.Status404NotFound,
                   new ErrorDto
                   {
                       Description = "The following pings don't exist.",
                       Data = new() { ["NonExistingPings"] = nonExistignPings }
                   });
            }

            var user = this.GetUser()!;

            var noAccessPings = await existingPings
                .Where(p => p.SenderId != user.Id && p.RecipientId != user.Id)
                .Select(p => p.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (noAccessPings.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "You don't have access to the following pings.",
                        Data = new() { ["NoAccessPings"] = noAccessPings }
                    });
            }

            _dbContext.Pings.RemoveRange(existingPings);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return NoContent();
        }
        /// <summary>
        /// Updates a ping.
        /// </summary>
        /// <remarks>
        /// Caller must the sender of the ping.
        /// </remarks>
        /// <param name="update">The update to apply, null fields mean no update to this property.</param>
        /// <response code="204">The update was done successfully.</response>
        [LoggedInFilter]
        [HttpPatch("Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UpdatePingDto update)
        {
            var ping = await _dbContext.Pings.FindAsync(update.Id).ConfigureAwait(false);
            if (update.Content != null)
            {
                ping.Content = update.Content;
            }

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return Ok();
        }
    }
}