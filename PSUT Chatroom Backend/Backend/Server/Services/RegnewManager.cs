using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RegnewCommon;
using RestSharp;
using RestSharp.Serializers.SystemTextJson;
using Server.Db;
using Server.Db.Entities;

namespace Server.Services;
public class RegnewManager
{
    private readonly IServiceProvider _sp;
    private readonly IRestClient _restClient;
    public RegnewManager(IServiceProvider sp, IOptions<AppOptions> appOptions)
    {
        _sp = sp;
        _restClient = new RestClient(appOptions.Value.UniversitySystemAddress).UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }
    private static SectionDay DayOfWeekToSectionDay(DayOfWeek[] days)
    {
        SectionDay result = 0;
        foreach (var day in days)
        {
            result |= day switch
            {
                DayOfWeek.Sunday => SectionDay.Sunday,
                DayOfWeek.Monday => SectionDay.Monday,
                DayOfWeek.Tuesday => SectionDay.Tuesday,
                DayOfWeek.Wednesday => SectionDay.Wednesday,
                DayOfWeek.Thursday => SectionDay.Thursday,
                _ => 0
            };
        }
        return result;
    }
    private async Task UpdateOldCourse(RegnewCourseDto course)
    {
        await using var scope = _sp.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saltBae = scope.ServiceProvider.GetRequiredService<SaltBae>();
        var dbCourse = await dbContext.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Group)
            .FirstAsync(c => c.RegnewId == course.Id)
            .ConfigureAwait(false);
        dbCourse.Name = course.Name;
        async Task UpdateOldSection(RegnewSectionDto regnewSection)
        {
            if (regnewSection.StudentsIds.Length == 0)
            {
                throw new ArgumentException(nameof(regnewSection.StudentsIds));
            }
            await using var scope = _sp.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbSection = await dbContext.Sections
                .Include(s => s.Group)
                .ThenInclude(s => s.Members)
                .ThenInclude(s => s.User)
                .FirstAsync(s => s.RegnewId == regnewSection.Id)
                .ConfigureAwait(false);
            dbSection.Time = regnewSection.Time;
            dbSection.Days = DayOfWeekToSectionDay(regnewSection.Days);
            var newMembers = regnewSection.StudentsIds.Except(dbSection.Group!.Members!.Select(gm => gm.User.RegnewId))
                .Select(sid => new GroupMember
                {
                    IsAdmin = false,
                    GroupId = dbSection.GroupId,
                    UserId = dbContext.Users.First(x => x.RegnewId == sid).Id
                });

            await dbContext.GroupsMembers.AddRangeAsync(newMembers).ConfigureAwait(false);
            var instructorDbId = (await dbContext.Users.FirstAsync(x => x.RegnewId == regnewSection.InstructorId)).Id;
            if (!dbSection.Group.Members.Any(m => m.UserId == instructorDbId))
            {
                await dbContext.GroupsMembers.AddAsync(new GroupMember
                {
                    IsAdmin = true,
                    GroupId = dbSection.GroupId,
                    UserId = instructorDbId
                }).ConfigureAwait(false);
            }
            else
            {
                var admin = await dbContext.GroupsMembers.FirstAsync(m => m.GroupId == dbSection.GroupId && m.User.RegnewId == regnewSection.InstructorId).ConfigureAwait(false);
                admin.IsAdmin = true;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            var regnewSectionUsersIds = regnewSection.StudentsIds.ToHashSet();
            regnewSectionUsersIds.Add(regnewSection.InstructorId);
            var removedMembers = dbSection.Group!.Members!.Where(gm => !regnewSectionUsersIds.Contains(gm.User.RegnewId));
            dbContext.GroupsMembers.RemoveRange(removedMembers);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);


            //sanity check
            var cnt = await dbContext.GroupsMembers.CountAsync(gm => gm.GroupId == dbSection.GroupId).ConfigureAwait(false);
            if (cnt == 0)
            {
                throw new InvalidOperationException("COUNT IS 0");
            }
        }
        //Old
        await Parallel.ForEachAsync(course.Sections
            .Join(dbCourse.Sections, s => s.Id, s => s.RegnewId, (s, dbs) => (RegnewSection: s, DbSection: dbs)),
            async (s, ct) => await UpdateOldSection(s.RegnewSection).ConfigureAwait(false))
            .ConfigureAwait(false);

        //New
        var dbCourseSectionsIds = dbCourse.Sections.Select(s => s.RegnewId).ToHashSet();
        var newRegnewSections = course.Sections.Where(s => !dbCourseSectionsIds.Contains(s.Id));
        var newSectionsCreationTasks = new List<Task>();
        foreach (var regnewSection in newRegnewSections)
        {
            var group = new Group
            {
                Name = $"{course.Name} - {regnewSection.Id}",
                Members = new GroupMember[] { },
                EncryptionSalt = new byte[] { }
            };
            await dbContext.Groups.AddAsync(group).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            group.EncryptionSalt = saltBae.SaltSteak(null, group.Id);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            var conversation = new Conversation { GroupId = group.Id };
            await dbContext.Conversations.AddAsync(conversation).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            var dbSection = new Section
            {
                CourseId = dbCourse.Id,
                RegnewId = regnewSection.Id,
                GroupId = group.Id
            };
            await dbContext.Sections.AddAsync(dbSection).ConfigureAwait(false);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            newSectionsCreationTasks.Add(UpdateOldSection(regnewSection));
        }

        var regnewCourseSectionsIds = course.Sections.Select(s => s.Id).ToHashSet();
        var removedSections = dbCourse.Sections.Where(s => !regnewCourseSectionsIds.Contains(s.RegnewId));
        dbContext.Groups.RemoveRange(removedSections.Select(s => s.Group));
        dbContext.Sections.RemoveRange(removedSections);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
    private async Task AddCourse(RegnewCourseDto course)
    {
        await using var scope = _sp.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbCourse = new Course
        {
            RegnewId = course.Id,
            Name = course.Name
        };
        await dbContext.Courses.AddAsync(dbCourse).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        await UpdateOldCourse(course).ConfigureAwait(false);
    }

    private async Task UpdateOldUser(RegnewUserDto user)
    {
        await using var scope = _sp.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbUser = await dbContext.Users
            .FirstAsync(u => u.RegnewId == user.Id)
            .ConfigureAwait(false);
        dbUser.Name = user.Name;
        dbUser.Email = user.Email;
        dbUser.Role = user.Role;

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
    private async Task AddUser(RegnewUserDto user)
    {
        await using var scope = _sp.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saltBae = scope.ServiceProvider.GetRequiredService<SaltBae>();
        if (user.Id == null) { Debugger.Break(); }
        var dbUser = new User
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            RegnewId = user.Id,
            EncryptionSalt = new byte[] { }
        };
        await dbContext.Users.AddAsync(dbUser).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        dbUser.EncryptionSalt = saltBae.SaltSteak(dbUser);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
    public async Task<bool> TestConnection()
    {
        var req = new RestRequest("Ping", Method.GET);
        var res = await _restClient.ExecuteAsync<string>(req).ConfigureAwait(false);
        return res.IsSuccessful && res.Data.Equals("alive", StringComparison.OrdinalIgnoreCase);
    }
    private async Task UpdateUsers()
    {
        var req = new RestRequest("Users", Method.GET);
        var res = await _restClient.ExecuteAsync<RegnewUserDto[]>(req).ConfigureAwait(false);

        await using var scope = _sp.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        List<Task> updateTasks = new(res.Data.Length);

        await Parallel.ForEachAsync(res.Data.Where(user => dbContext.Users.Any(u => u.RegnewId == user.Id)),
        async (user, ct) => await UpdateOldUser(user).ConfigureAwait(false))
        .ConfigureAwait(false);

        await Parallel.ForEachAsync(res.Data.Where(user => !dbContext.Users.Any(u => u.RegnewId == user.Id)),
        async (user, ct) => await AddUser(user).ConfigureAwait(false))
        .ConfigureAwait(false);
    }
    private async Task UpdateCourses()
    {
        var req = new RestRequest("Courses", Method.GET);
        var res = await _restClient.ExecuteAsync<RegnewCourseDto[]>(req).ConfigureAwait(false);

        var dbContext = _sp.GetRequiredService<AppDbContext>();
        List<Task> updateTasks = new(res.Data.Length);
        await Parallel.ForEachAsync(res.Data.Where(cs => dbContext.Courses.Any(c => c.RegnewId == cs.Id)),
        async (course, ct) => await UpdateOldCourse(course).ConfigureAwait(false))
        .ConfigureAwait(false);

        await Parallel.ForEachAsync(res.Data.Where(cs => !dbContext.Courses.Any(c => c.RegnewId == cs.Id)),
        async (course, ct) => await AddCourse(course).ConfigureAwait(false))
        .ConfigureAwait(false);
    }
    public async Task PatchDb()
    {
        await UpdateUsers().ConfigureAwait(false);
        await UpdateCourses().ConfigureAwait(false);
    }
}