using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Db;
using Server.Dto.Users;
using Server.Services;
using Server.Services.UserSystem;

namespace GradProjectServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly InitializationManager _initializationManager;
        private readonly UserManager _userManager;
        private readonly IMapper _mapper;
        private readonly AppDbContext _dbContext;
        private readonly RegnewManager _regnewManager;
        public TestController(InitializationManager initializationManager, UserManager userManager, IMapper mapper, AppDbContext dbContext, RegnewManager regnewManager)
        {
            _initializationManager = initializationManager;
            _userManager = userManager;
            _mapper = mapper;
            _dbContext = dbContext;
            _regnewManager = regnewManager;
        }

        [HttpGet("RecreateAndSeedDb")]
        public async Task<IActionResult> RecreateAndSeedDb()
        {
            if (!await _regnewManager.TestConnection().ConfigureAwait(false))
            {
                return NotFound("Can't connect to university system.");
            }
            await _initializationManager.RecreateAndSeedDb().ConfigureAwait(false);
            return Ok("Recreated and seeded successfully.");
        }

        [HttpGet("RecreateDb")]
        public async Task<IActionResult> RecreateDb()
        {
            await _initializationManager.RecreateDb().ConfigureAwait(false);
            return Ok("Recreated successfully.");
        }
        /// <summary>
        /// FOR TESTING ONLY.
        /// Generates login cookie for a user to be used in subsequent requests.
        /// </summary>
        /// <remarks>
        /// A user can't be logged in before calling this method.
        /// </remarks>
        /// <response code="200">Successful login, will return the token and Create a set session cookie.</response>
        /// <response code="401">Invalid login credentials.</response>
        [NotLoggedInFilter]
        [HttpGet("Impersonate")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<ActionResult<LoginResultDto>> Impersonate([FromQuery] string email)
        {
            var userId = await _userManager.GmailLogin(email, Response.Cookies).ConfigureAwait(false);
            if (userId == null)
            {
                return Unauthorized("Invalid login credentials.");
            }

            var user = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            LoginResultDto result = new()
            {
                User = _mapper.Map<UserDto>(user),
                Token = user.Token!
            };
            return Ok(result);
        }
    }
}