using Microsoft.AspNetCore.Mvc;
using RegnewCommon;
namespace RegnewMock;
[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    private static readonly RegnewUserDto[] s_users;
    private static readonly RegnewCourseDto[] s_courses;
    static HomeController()
    {
        Random rand = new();
        var firstNames = new string[]
        {
            "Abdallah", "Hashim", "Shatha", "Jannah", "Malik", "Basel", "Al-Bara", "Mohammad", "Aya", "Issra",
            "Huda", "Tuqa", "Deema"
        };
        var lastNames = new string[]
        {
            "Al-Omari", "Al-Mansour", "Shreim", "Barqawi", "Arabiat", "Azaizeh", "Zeer", "Faroun", "Abu-Rumman",
            "Allan", "Odeh"
        };
        List<RegnewUserDto> users = new();
        int id = 1;
        for (char i = 'a'; i <= 'z'; i++)
        {
            var user = new RegnewUserDto
            {
                Id = $"REGNEWID{id++}",
                Email = $"{i}@std.psut.edu.jo",
                Name = $"{rand.NextElement(firstNames)} {rand.NextElement(lastNames)}"
            };
            users.Add(user);
        }
        users.Add(new RegnewUserDto
        {
            Id = "ahm",
            Email = "ahm@std.psut.edu.jo",
            Name = "Ahmad Al-Sarraj",
            Role = UserRole.Student
        });
        for (int i = 0; i < 5 && i < users.Count; i++)
        {
            users[i].Role = UserRole.Admin;
        }
        for (int i = 5; i < 10 && i < users.Count; i++)
        {
            users[i].Role = UserRole.Instructor;
        }
        for (int i = 10; i < users.Count; i++)
        {
            users[i].Role = UserRole.Student;
        }
        s_users = users.ToArray();

        s_courses = new RegnewCourseDto[]
                        {
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{1}",
                                Name = "Arabic Language"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{2}",
                                Name = "National Education"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{3}",
                                Name = "Military Science"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{4}",
                                Name = "Introduction to Computer Science"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{5}",
                                Name = "Structured Programming"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{6}",
                                Name = "Structured ProgrammingLab"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{7}",
                                Name = "Calculus (1)"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{8}",
                                Name = "Calculus (2)"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{9}",
                                Name = "Theory of Computation"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{10}",
                                Name = "Database Systems"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{11}",
                                Name = "Software Engineering"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{12}",
                                Name = "Graduation Project 2"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{13}",
                                Name = "Digital Logic Design"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{14}",
                                Name = "Graduation Project 1"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{15}",
                                Name = "Wireless Networks and Applications"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{16}",
                                Name = "Computer Graphics"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{17}",
                                Name = "Operations Research"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{18}",
                                Name = "Mobile Application Development"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{19}",
                                Name = "Multimedia Systems"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{20}",
                                Name = "History of Science"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{21}",
                                Name = "Arab Islamic Scientific Heritage"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{22}",
                                Name = "Sports and Health"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{23}",

                                Name = "Arabic Literature"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{24}",
                                Name = "Foreign languages"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{25}",
                                Name = "Entrepreneurship for Business"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{26}",
                                Name = "Scientific Research Methods"
                            },
                            new RegnewCourseDto
                            {
                                Id = $"REGNEWID{27}",
                                Name = "Business Skills"
                            }
                        };
        int sectionId = 0;
        var instructors = s_users.Where(u => u.Role.HasFlag(UserRole.Instructor)).ToArray();
        var students = s_users.Where(u => u.Role.HasFlag(UserRole.Student)).ToArray();
        var days = new DayOfWeek[][]
        {
            new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday },
            new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday }
        };
        List<string> sectionStudents = new();

        foreach (var course in s_courses)
        {
            course.Sections = new RegnewSectionDto[rand.Next(1, 3)];
            for (int i = 0; i < course.Sections.Length; i++)
            {
                sectionStudents.Clear();
                int studentsCount = rand.Next(1, 3);
                for (int j = students.Length - 1; j >= 0 && studentsCount > 0; j--)
                {
                    if (j >= studentsCount && !rand.NextBool()) { continue; }
                    studentsCount--;
                    sectionStudents.Add(students[j].Id);
                }
                course.Sections[i] = new RegnewSectionDto
                {
                    Id = $"REGNEWID{sectionId++}",
                    InstructorId = rand.NextElement(instructors).Id,
                    Time = rand.NextTimeSpan(),
                    Days = rand.NextElement(days),
                    StudentsIds = sectionStudents.ToArray()
                };
            }
        }
    }
    [HttpGet("Ping")]
    public string Ping() => "Alive";
    [HttpGet("Users")]
    public RegnewUserDto[] Users() => s_users;
    [HttpGet("Courses")]
    public RegnewCourseDto[] Courses() => s_courses;
}