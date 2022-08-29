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
using Server.Services;
using Server.Services.UserSystem;

namespace Server.Controllers;
[AdminFilter]
[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
    private readonly BackupManager _backupManager;
    private readonly RegnewManager _regnewManager;

    public AdminController(BackupManager backupManager, RegnewManager regnewManager)
    {
        _backupManager = backupManager;
        _regnewManager = regnewManager;
    }
    /// <summary>
    /// Backs up the db + file system, and returns name of backup file.
    /// </summary>
    [HttpPost("CreateBackup")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> BackupDb()
    {
        return Ok(await _backupManager.CreateBackup().ConfigureAwait(false));
    }

    /// <summary>
    /// Clears the db and file system.
    /// </summary>
    [HttpPost("ClearDb")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearDb()
    {
        await _backupManager.ClearDb().ConfigureAwait(false);
        return Ok();
    }
    /// <summary>
    /// Patches the db from university system.
    /// </summary>
    /// <response code="404">Can't reach university system.</response>
    [HttpPost("PatchDb")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> PatchDb()
    {
        if (!await _regnewManager.TestConnection().ConfigureAwait(false))
        {
            return NotFound("Can't connect to university system.");
        }
        await _regnewManager.PatchDb().ConfigureAwait(false);
        return Ok();
    }

}
