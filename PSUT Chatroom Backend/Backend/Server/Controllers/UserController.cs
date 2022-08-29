using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Google.Apis.Auth;
using Google.Apis.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Server.Db;
using Server.Db.Entities;
using Server.Dto;
using Server.Dto.Users;
using Server.Services;
using Server.Services.FilesManagers;
using Server.Services.UserSystem;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly UserManager _userManager;
    private readonly UserFileManager _userFileManager;
    private readonly AppOptions _appOptions;

    public UserController(AppDbContext dbContext, IMapper mapper, UserManager userManager, UserFileManager userFileManager, IOptions<AppOptions> appOptions)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _userManager = userManager;
        _userFileManager = userFileManager;
        _appOptions = appOptions.Value;
    }

    /// <param name="usersIds">Ids of the users to get.</param>
    /// <param name="metadata">Whether to return UserMetadataDto or UserDto.</param>
    /// <remarks>
    /// A user can get:
    ///     1- UserDto of HIMSELF only.
    ///     2- UserMetadataDto of all users.
    /// An instructor can get:
    ///     All users.
    /// </remarks>
    /// <response code="404">Ids of the non existing users.</response>
    /// <response code="403">Ids of user caller has no access rights to.</response>
    [HttpPost("Get")]
    [ProducesResponseType(typeof(IEnumerable<UserMetadataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get([FromBody] int[] usersIds, bool metadata = false)
    {
        var users = _dbContext.Users.AsQueryable();
        var existingUsers = users.Where(e => usersIds.Contains(e.Id));
        var nonExistingUsers = usersIds.Except(existingUsers.Select(e => e.Id)).ToArray();
        if (nonExistingUsers.Length > 0)
        {
            return StatusCode(StatusCodes.Status404NotFound,
                new ErrorDto
                {
                    Description = "The following users don't exist.",
                    Data = new Dictionary<string, object> { ["NonExistingUsers"] = nonExistingUsers }
                });
        }

        var user = this.GetUser();
        if (!(user?.IsInstructor ?? false) && !metadata)
        {
            var userId = user?.Id ?? -1;
            //any requested users that are not the currently logged in user
            var notAllowedToGetUsers = await existingUsers.Where(e => e.Id != userId)
                .Select(u => u.Id)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (notAllowedToGetUsers.Length > 0)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorDto
                    {
                        Description = "User can't get the following users.",
                        Data = new Dictionary<string, object> { ["NotAllowedToGetUsers"] = notAllowedToGetUsers }
                    });
            }
        }

        if (metadata)
        {
            return Ok(_mapper.ProjectTo<UserMetadataDto>(existingUsers));
        }

        return Ok(_mapper.ProjectTo<UserDto>(existingUsers));
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <remarks>
    /// A user can update only himself.
    /// An instructor can update any user.
    /// </remarks>
    /// <param name="update">The update to apply, null fields mean no update to this property.</param>
    [LoggedInFilter]
    [HttpPatch("Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto update)
    {
        var user = await _dbContext.Users.FindAsync(update.Id).ConfigureAwait(false);
        await _userManager.UpdateUser(user.Id, update.Name).ConfigureAwait(false);
        if (update.ProfilePictureJpgBase64 != null)
        {
            await _userFileManager.SaveBase64Image(user, update.ProfilePictureJpgBase64).ConfigureAwait(false);
        }

        if (update.DeletePicture == true)
        {
            _userFileManager.DeleteFile(user);
        }

        return Ok();
    }

    /// <summary>
    /// Generates login cookie for a user to be used in subsequent requests.
    /// </summary>
    /// <remarks>
    /// A user can't be logged in before calling this method.
    /// </remarks>
    /// <response code="200">Successful login, will return the token with the user and Create a set session cookie.</response>
    /// <response code="401">Invalid login credentials.</response>
    [NotLoggedInFilter]
    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto)
    {
        //TODO: validate token
        var userId = await _userManager.GmailLogin(dto.Email, Response.Cookies).ConfigureAwait(false);
        if (userId == null)
        {
            return Unauthorized("Invalid email.");
        }

        var user = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);
        LoginResultDto result = new()
        {
            User = _mapper.Map<UserDto>(user),
            Token = user.Token!
        };
        return Ok(result);
    }
    /// <summary>
    /// Logs out the current user and removes his cookie.
    /// </summary>
    [LoggedInFilter]
    [HttpPost("Logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var user = this.GetUser()!;
        await _userManager.Logout(user, Response.Cookies).ConfigureAwait(false);
        return Ok();
    }
    /*
    [NotLoggedInFilter]
    [HttpPost("HandleGmailToken")]
    public async Task<IActionResult> HandleGmailToken()
    {
        try
        {
            using var bodyReader = new StreamReader(Request.Body);
            var body = await bodyReader.ReadToEndAsync().ConfigureAwait(false);
            var tokenText = body.Split('&')[0][11..];
            var token = await GoogleJsonWebSignature.ValidateAsync(tokenText).ConfigureAwait(false);
            if (token.HostedDomain?.EndsWith(_appOptions.UniversityEmailDomain, StringComparison.OrdinalIgnoreCase) != true)
            {
                return Unauthorized(new ErrorDto
                {
                    Description = "The following email doesn't belong to the university domain.",
                    Data = new() { ["Email"] = token.Email }
                });
            }
            var userId = await _userManager.GmailLogin(token.Email, Response.Cookies).ConfigureAwait(false);
            if (userId == null)
            {
                return Unauthorized("Invalid email.");
            }

            var user = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);
            LoginResultDto result = new()
            {
                User = _mapper.Map<UserDto>(user),
                Token = user.Token!
            };
            var base64Token = Uri.EscapeDataString(user.Token!);
            Response.Headers.Add(HeaderNames.Location, $"{_appOptions.AppUrlProtocol}://token={base64Token}");
            return StatusCode(StatusCodes.Status307TemporaryRedirect, result);
        }
        catch
        {
            return Unauthorized("Invalid token.");
        }
    }
    */

    /// <summary>
    /// Gets the user profile picture as stream of bytes with header Content-Type: image/jpeg.
    /// </summary>
    /// <param name="userId">Id of the user to get his profile picture.</param>
    /// <response code="404">If there is no user with this id.</response>
    /// <response code="204">If the user doesn't have a profile picture.</response>
    [HttpGet("GetPicture")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPicture([FromQuery] int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            return StatusCode(StatusCodes.Status404NotFound,
                new ErrorDto
                {
                    Description = "There is no user with the following Id.",
                    Data = new Dictionary<string, object> { ["UserId"] = userId }
                });
        }

        var userImage = _userFileManager.GetFile(user);
        if (userImage == null) { return NoContent(); }
        var result = File(userImage, "image/jpeg");
        result.FileDownloadName = $"User_{userId}_ProfilePicture.jpg";
        return result;
    }
    /// <summary>
    /// Gets UserDto for the logged in user.
    /// </summary>
    /// <response code="200">Info about the logged in user.</response>
    [LoggedInFilter]
    [HttpGet("GetLoggedIn")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public ActionResult<UserDto> GetLoggedIn()
    {
        var user = this.GetUser()!;
        return Ok(_mapper.Map<UserDto>(user));
    }
}
