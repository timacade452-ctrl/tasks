using System.Globalization;
using System.Text;
using StudyFlow.Models;
using StudyFlow.Repositories;

namespace StudyFlow.Services;

/// <summary>
/// Populates the in-memory stores with a realistic student workspace.
/// Seeded for Global Sciences University (GSU) - Faculty of Computer Sciences & IT
/// Academic Year 2026-2027, Year Four 1st Semester.
/// </summary>
public class DemoDataSeeder
{
    private readonly InMemoryCourseRepository _courses;
    private readonly InMemoryTaskRepository _tasks;
    private readonly InMemoryMaterialRepository _materials;
    private readonly InMemoryPomodoroRepository _sessions;
    private readonly InMemoryStudyPlanRepository _plans;
    private readonly InMemoryUserProfileRepository _profiles;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public DemoDataSeeder(
        InMemoryCourseRepository courses,
        InMemoryTaskRepository tasks,
        InMemoryMaterialRepository materials,
        InMemoryPomodoroRepository sessions,
        InMemoryStudyPlanRepository plans,
        InMemoryUserProfileRepository profiles,
        IFileStorageService storage,
        ICurrentUserService currentUser)
    {
        _courses = courses;
        _tasks = tasks;
        _materials = materials;
        _sessions = sessions;
        _plans = plans;
        _profiles = profiles;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task SeedAsync()
    {
        var userId = _currentUser.UserId;

        if (await _profiles.GetAsync(userId) is null)
        {
            await _profiles.SeedAsync(new UserProfile
            {
                Id = userId,
                FullName = "Mohamed Sicid Muse",
                Email = "mohamed.abdi@gsu.edu.so",
                University = "Global Science University",
                Program = "B.Sc. Information Technology",
                YearOfStudy = "Year 4",
                Bio = "IT student at Global Science University (GSU) specializing in ISP architecture, system administration, ASP.NET Core, and networks.",
                AvatarColor = "#4f46e5",
                DailyGoalMinutes = 120,
                FocusMinutes = 25,
                ShortBreakMinutes = 5,
                LongBreakMinutes = 15,
                LongBreakInterval = 4
            });
        }

        if ((await _courses.GetAllAsync(userId)).Count > 0)
        {
            return;
        }

        var term = AcademicTerm();
        var today = DateTime.Today;
        var now = DateTime.Now;

        // The 5 official courses from GSU Year Four 1st Semester schedule:
        var isp = NewCourse(userId, "IT 411", "ISP Architecture Engineering", "Eng. Abdullahi Abdirahman", term,
            "ISP network topology, BGP routing, MPLS, peering agreements, and carrier-grade infrastructure.",
            "#059669", "globe-2");

        var sccm = NewCourse(userId, "IT 412", "Microsoft System Center (SCCM)", "Eng. Yahye Mohamed", term,
            "Microsoft System Center Configuration Manager (MECM), OS deployment, software packaging, and endpoint administration.",
            "#d97706", "cpu");

        var asp = NewCourse(userId, "IT 413", "C# & ASP.NET", "Eng SAID ABDIKHIM", term,
            "C# programming, ASP.NET Core MVC architecture, Web APIs, Entity Framework Core, and modern web application development.",
            "#4f46e5", "code-2");

        var netEng = NewCourse(userId, "IT 414", "Network Engineering", "Eng. Abdullahi", term,
            "Enterprise network design, dynamic routing (OSPF, EIGRP), VLANs, STP, network security, and router/switch configuration.",
            "#0284c7", "globe-2");

        var swe = NewCourse(userId, "IT 415", "Software Engineering", "Eng. Mohamed Abdulqadir", term,
            "Software engineering principles, Agile & Scrum methodologies, UML modeling, requirements engineering, and QA testing.",
            "#475569", "book-open");

        var allCourses = new[] { isp, sccm, asp, netEng, swe };
        foreach (var course in allCourses)
        {
            await _courses.AddAsync(course);
        }

        async Task<AcademicTask> AddTask(
            Course? course,
            string title,
            string description,
            AcademicTaskType type,
            TaskPriority priority,
            AcademicTaskStatus status,
            int startOffset,
            int dueOffset,
            int minutes,
            int? completedDaysAgo = null)
        {
            var task = new AcademicTask
            {
                UserId = userId,
                CourseId = course?.Id,
                Title = title,
                Description = description,
                Type = type,
                Priority = priority,
                Status = status,
                StartDate = today.AddDays(startOffset),
                DueDate = today.AddDays(dueOffset),
                EstimatedMinutes = minutes,
                CreatedAt = now.AddDays(-10),
                CompletedAt = completedDaysAgo.HasValue ? now.AddDays(-completedDaysAgo.Value) : null
            };
            await _tasks.AddAsync(task);
            return task;
        }

        // Seed one task for each course.
        var ispTask = await AddTask(isp, "Lab 3 — BGP Peering and Route Reflectors", "Configure eBGP and iBGP peering with route reflectors between Autonomous Systems.", AcademicTaskType.Assignment, TaskPriority.High, AcademicTaskStatus.InProgress, -3, 2, 120);
        var sccmTask = await AddTask(sccm, "Lab 2 — Windows 11 OSD Task Sequence", "Create and test an operating system deployment (OSD) task sequence with driver injection.", AcademicTaskType.Assignment, TaskPriority.High, AcademicTaskStatus.InProgress, -2, 3, 120);
        var aspTask = await AddTask(asp, "Course Project — ASP.NET Core Student System", "Implement MVC Controllers, ViewModels, repository pattern, and responsive Tailwind UI.", AcademicTaskType.Project, TaskPriority.High, AcademicTaskStatus.InProgress, -5, 7, 240);
        var netEngTask = await AddTask(netEng, "Packet Tracer Lab — Multi-Area OSPF Configuration", "Configure OSPF Area 0, stub areas, and virtual links with router ID verification.", AcademicTaskType.Assignment, TaskPriority.High, AcademicTaskStatus.InProgress, -3, 2, 120);
        var sweTask = await AddTask(swe, "Software Requirements Specification (SRS) Document", "Complete the functional and non-functional requirements for the graduation capstone system.", AcademicTaskType.Project, TaskPriority.High, AcademicTaskStatus.InProgress, -4, 5, 150);

        await SeedMaterialsAsync(userId, now, isp, sccm, asp, netEng, swe);
        await SeedSessionsAsync(userId, now, allCourses, new[] { ispTask, sccmTask, aspTask, netEngTask, sweTask });
        await SeedPlansAsync(userId, today, allCourses);
    }

    private async Task SeedMaterialsAsync(
        string userId,
        DateTime now,
        Course isp,
        Course sccm,
        Course asp,
        Course netEng,
        Course swe)
    {
        async Task Add(
            Course course,
            string title,
            string description,
            MaterialCategory category,
            string extension,
            int daysAgo,
            params string[] lines)
        {
            var bytes = DemoDocumentFactory.Create(extension, title, lines);
            var stored = await _storage.SaveBytesAsync(bytes, extension);
            var fileName = $"{Slug(course.Code)}-{Slug(title)}{extension}";

            await _materials.AddAsync(new StudyMaterial
            {
                UserId = userId,
                CourseId = course.Id,
                Title = title,
                Description = description,
                Category = category,
                OriginalFileName = fileName,
                StoredFileName = stored.RelativePath,
                ContentType = stored.ContentType,
                FileExtension = stored.Extension,
                FileSizeBytes = stored.SizeBytes,
                UploadedAt = now.AddDays(-daysAgo).AddHours(-2)
            });
        }

        // ISP Architecture Engineering (IT 411)
        await Add(isp, "ISP Architecture — Lecture 01: Core Network Topology", "Overview of Tier-1, Tier-2 and Tier-3 ISPs, transit relationships and IXPs.",
            MaterialCategory.LectureSlides, ".pdf", 2,
            "Course: IT 411 — ISP Architecture Engineering",
            "Lecturer: Eng. Abdullahi Abdirahman | Room 4",
            "Global Sciences University · Year Four Semester 1",
            "",
            "Topics",
            "1. Internet hierarchy and Autonomous Systems (AS)",
            "2. Internet Exchange Points (IXPs) and peering agreements",
            "3. BGP exterior and interior routing fundamentals",
            "4. MPLS forwarding, label distribution, and traffic engineering",
            "5. Carrier-grade network reliability and redundancy",
            "",
            "Key takeaway: Scalable ISP architectures isolate customer edge from the high-speed transit core.",
            "Schedule: Saturdays 2:00 PM - 5:00 PM (Room 4).");

        await Add(isp, "Lab 3 Guide — BGP Peering & Route Reflectors", "Step-by-step router configuration guide for iBGP and eBGP topologies.",
            MaterialCategory.AssignmentBrief, ".pdf", 3,
            "Lab 3 — Configuring BGP Peering and Route Reflectors",
            "Instructor: Eng. Abdullahi Abdirahman",
            "",
            "Lab Objectives:",
            "- Establish eBGP peering between AS 65001 and AS 65002",
            "- Configure iBGP mesh reduction using Route Reflectors",
            "- Validate route convergence and attribute propagation using show commands",
            "",
            "Submission deadline: Saturday before lecture.");

        await Add(isp, "Course Syllabus — ISP Architecture", "Course outlines, grading breakdown, exam schedules, and lab guidelines.",
            MaterialCategory.Syllabus, ".pdf", 14,
            "Global Sciences University — Faculty of CS & IT",
            "Department of IT · Academic Year 2026-2027",
            "IT 411: ISP Architecture Engineering",
            "Lecturer: Eng. Abdullahi Abdirahman",
            "",
            "Grading: Midterm 20% | Labs 20% | Assignments 10% | Final Exam 50%",
            "Lecture time: Saturday 2:00 PM – 5:00 PM (Room 4)");

        // Microsoft SCCM (IT 412)
        await Add(sccm, "SCCM Administration Guide — Endpoint Deployment", "Comprehensive guide to Microsoft System Center Configuration Manager (MECM).",
            MaterialCategory.Reference, ".pdf", 3,
            "Course: IT 412 — Microsoft System Center Configuration Manager (SCCM)",
            "Lecturer: Eng. Yahye Mohamed | Main Hall",
            "Global Sciences University",
            "",
            "Core Modules:",
            "1. Infrastructure planning: Primary Site, CAS, and Secondary Sites",
            "2. Client deployment methods (Push, GPO, Manual)",
            "3. Operating System Deployment (OSD) with PXE and WDS",
            "4. Packaging software (.msi, .exe) and Software Center deployment",
            "5. Endpoint Protection and compliance reporting",
            "",
            "Class: Mondays 2:00 PM - 5:00 PM (Main Hall).");

        await Add(sccm, "Lecture 04 — OS Deployment & Task Sequences", "Creating customized Windows 11 deployment images using SCCM task sequences.",
            MaterialCategory.LectureSlides, ".pptx", 5,
            "IT 412: OS Deployment (OSD) in SCCM",
            "Instructor: Eng. Yahye Mohamed",
            "Topics:",
            "- Capturing reference Windows 11 image with Sysprep",
            "- Importing operating system images and boot images (x64)",
            "- Configuring Task Sequence steps: partition, install, apply drivers",
            "- Deploying to device collections: Available vs Required");

        // C# & ASP.NET (IT 413)
        await Add(asp, "C# & ASP.NET Core MVC — Architecture Guide", "Clean architecture, Dependency Injection, Controller routing, and Entity Framework Core.",
            MaterialCategory.LectureNotes, ".pdf", 2,
            "Course: IT 413 — C# & ASP.NET",
            "Lecturer: Eng SAID ABDIKHIM | Room 6",
            "Global Sciences University · Year 4 Semester 1",
            "",
            "Key Architectural Concepts:",
            "1. Model-View-Controller pattern in ASP.NET Core",
            "2. Dependency Injection (DI) service lifetimes: Transient, Scoped, Singleton",
            "3. Entity Framework Core: Code-First migrations and repository pattern",
            "4. Data validation using Data Annotations and ModelState",
            "5. Secure web applications: Antiforgery tokens and authentication",
            "",
            "Schedule: Tuesdays 2:00 PM - 5:00 PM (Room 6).");

        await Add(asp, "Course Project Brief — Student Management Portal", "Requirements, user stories, and evaluation rubric for the ASP.NET Core semester project.",
            MaterialCategory.AssignmentBrief, ".pdf", 4,
            "IT 413 Project Brief: Web Application Development",
            "Instructor: Eng SAID ABDIKHIM",
            "",
            "Project Requirements:",
            "- Implement ASP.NET Core 8 MVC with Razor views and Tailwind CSS",
            "- Full CRUD functionality for Courses, Tasks, and Study Materials",
            "- Client and server-side validation",
            "- In-memory or database persistence with Repository pattern",
            "",
            "Submission: Live demonstration in Room 6.");

        // Network Engineering (IT 414)
        await Add(netEng, "Network Engineering — Advanced OSPF & BGP", "Design patterns for enterprise campus and data center networking.",
            MaterialCategory.LectureSlides, ".pptx", 4,
            "IT 414: Network Engineering",
            "Lecturer: Eng. Abdullahi | Main Hall",
            "Global Sciences University",
            "1. Link-state vs Distance-vector routing protocols",
            "2. Multi-area OSPF design and LSA types (1 through 5)",
            "3. Inter-VLAN routing with Multi-layer switching",
            "4. High Availability: HSRP, VRRP, and EtherChannel bundling",
            "5. Class Time: Wednesdays 2:00 PM - 5:00 PM (Main Hall).");

        await Add(netEng, "Lab 2 Manual — Enterprise Switching & VLANs", "Cisco switch configuration manual covering VLANs, 802.1Q trunks, and Spanning Tree.",
            MaterialCategory.Other, ".docx", 8,
            "IT 414 Lab Manual — Switching Protocols",
            "Global Sciences University - IT Department",
            "",
            "Exercises:",
            "1. Create VLANs 10, 20, 30 across access switches",
            "2. Configure 802.1Q trunk links with native VLAN security",
            "3. Enable Rapid PVST+ and verify root bridge election",
            "4. Configure LACP Port-Channels between distribution switches");

        // Software Engineering (IT 415)
        await Add(swe, "Software Engineering — Agile & Scrum Process Guide", "Modern Agile practices, user stories, sprint cycles, and software quality assurance.",
            MaterialCategory.Reading, ".pdf", 3,
            "Course: IT 415 — Software Engineering",
            "Lecturer: Eng. Mohamed Abdulqadir | Room 4",
            "Global Sciences University",
            "",
            "Methodology Overview:",
            "- Waterfall vs Agile development methodologies",
            "- Scrum ceremonies: Sprint Planning, Daily Stand-up, Sprint Review",
            "- Product Backlog grooming and User Story acceptance criteria",
            "- Software testing pyramid: Unit, Integration, System, and Acceptance",
            "",
            "Lecture: Thursdays 2:00 PM - 5:00 PM (Room 4).");

        await Add(swe, "SRS Document Template — Capstone Project", "Standard IEEE 830 format template for Software Requirements Specification.",
            MaterialCategory.AssignmentBrief, ".docx", 6,
            "IT 415: Software Requirements Specification (SRS) Template",
            "Instructor: Eng. Mohamed Abdulqadir",
            "Use this template for your Year 4 Capstone Software Project.",
            "",
            "Document Sections:",
            "1. Introduction, Scope, and System Purpose",
            "2. Overall Description & User Characteristics",
            "3. Functional Requirements and Use Case Descriptions",
            "4. Non-Functional Requirements (Security, Performance, Reliability)",
            "5. UML Diagrams and System Architecture Design");
    }

    private async Task SeedSessionsAsync(
        string userId,
        DateTime now,
        Course[] courses,
        AcademicTask[] priorityTasks)
    {
        var random = new Random(42);
        var today = DateTime.Today;

        for (var daysAgo = 20; daysAgo >= 0; daysAgo--)
        {
            var date = today.AddDays(-daysAgo);
            var isWeekend = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Sunday;
            int count;
            if (daysAgo == 0) count = 2;
            else if (daysAgo <= 3) count = 3;
            else if (isWeekend) count = random.Next(1, 3);
            else count = random.Next(2, 5);

            for (var index = 0; index < count; index++)
            {
                DateTime completedAt;
                if (daysAgo == 0)
                {
                    completedAt = now.AddMinutes(-(30 + index * 55));
                }
                else
                {
                    var hours = new[] { 9, 11, 14, 16, 19 };
                    var hour = hours[(daysAgo + index) % hours.Length];
                    completedAt = date.AddHours(hour).AddMinutes(25);
                }

                if (completedAt > now || completedAt.Date < today.AddDays(-20)) continue;

                var course = courses[(daysAgo + index) % courses.Length];
                string? taskId = null;
                if (index == 0)
                {
                    taskId = priorityTasks
                        .FirstOrDefault(t => t.CourseId == course.Id)?.Id
                        ?? priorityTasks[0].Id;
                }
                else if (index % 2 == 0)
                {
                    var courseTask = (await _tasks.GetByCourseAsync(userId, course.Id))
                        .OrderBy(_ => random.Next())
                        .FirstOrDefault();
                    taskId = courseTask?.Id;
                }

                await _sessions.AddAsync(new PomodoroSession
                {
                    UserId = userId,
                    CourseId = course.Id,
                    TaskId = taskId,
                    Phase = PomodoroPhase.Focus,
                    StartedAt = completedAt.AddMinutes(-25),
                    CompletedAt = completedAt,
                    DurationMinutes = 25
                });
            }
        }
    }

    private async Task SeedPlansAsync(string userId, DateTime today, Course[] courses)
    {
        var isp = courses[0];
        var sccm = courses[1];
        var asp = courses[2];
        var netEng = courses[3];
        var swe = courses[4];

        async Task AddPlan(
            Course? course,
            string title,
            StudyPlanType type,
            DateTime start,
            int durationMinutes,
            string? location,
            string description)
        {
            var plan = new StudyPlanEvent
            {
                UserId = userId,
                CourseId = course?.Id,
                Title = title,
                Description = description,
                Type = type,
                Start = start,
                End = start.AddMinutes(durationMinutes),
                Location = location,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            };
            await _plans.AddAsync(plan);
        }

        // Calculate the Saturday of the current week (GSU schedule: Sat, Mon, Tue, Wed, Thu)
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        var currentSat = today.AddDays(-diff);

        // Schedule recurring lectures across 3 weeks (last week, current week, next week)
        for (var w = -1; w <= 1; w++)
        {
            var weekSat = currentSat.AddDays(w * 7);
            var sat = weekSat;
            var mon = weekSat.AddDays(2);
            var tue = weekSat.AddDays(3);
            var wed = weekSat.AddDays(4);
            var thu = weekSat.AddDays(5);

            // Saturday 2:00 PM - 5:00 PM: ISP Architecture Engineering (Room 4) - Eng. Abdullahi Abdirahman
            await AddPlan(isp, "ISP Architecture Engineering", StudyPlanType.Lecture,
                sat.Date.AddHours(14), 180, "Room 4", "Lecturer: Eng. Abdullahi Abdirahman | Global Sciences University");

            // Monday 2:00 PM - 5:00 PM: Microsoft System Center Configuration Manager (Main Hall) - Eng. Yahye Mohamed
            await AddPlan(sccm, "Microsoft SCCM (MECM)", StudyPlanType.Lecture,
                mon.Date.AddHours(14), 180, "Main Hall", "Lecturer: Eng. Yahye Mohamed | Global Sciences University");

            // Tuesday 2:00 PM - 5:00 PM: IT 413 (Room 6)
            await AddPlan(asp, "C# & ASP.NET", StudyPlanType.Lecture,
                tue.Date.AddHours(14), 180, "Room 6", "Lecturer: Eng SAID ABDIKHIM | Global Sciences University");

            // Wednesday 2:00 PM - 5:00 PM: Network Engineering (Main Hall)
            await AddPlan(netEng, "Network Engineering", StudyPlanType.Lecture,
                wed.Date.AddHours(14), 180, "Main Hall", "Lecturer: Eng. Abdullahi | Global Sciences University");

            // Thursday 2:00 PM - 5:00 PM: Software Engineering (Room 4) - Eng. Mohamed Abdulqadir
            await AddPlan(swe, "Software Engineering", StudyPlanType.Lecture,
                thu.Date.AddHours(14), 180, "Room 4", "Lecturer: Eng. Mohamed Abdulqadir | Global Sciences University");
        }

        // Additional study sessions, lab practice, and project review blocks
        await AddPlan(asp, "ASP.NET Core MVC Project Coding", StudyPlanType.StudySession,
            today.AddHours(9).AddMinutes(30), 90, "Computer Lab 2", "Hands-on implementation of controllers, models, and Razor views.");
        await AddPlan(netEng, "OSPF Configuration Lab Review", StudyPlanType.StudySession,
            today.AddDays(1).AddHours(10), 60, "Network Lab", "Packet tracer multi-area OSPF testing and routing table inspection.");
        await AddPlan(isp, "BGP Peering & Autonomous Systems", StudyPlanType.StudySession,
            today.AddDays(2).AddHours(9), 90, "Library 3rd Floor", "Route reflector, peering policies, and topology diagram analysis.");
        await AddPlan(swe, "SRS Deliverables Team Review", StudyPlanType.CourseActivity,
            today.AddDays(3).AddHours(11), 60, "Discussion Room A", "Review functional requirements and UML diagrams with group members.");
        await AddPlan(sccm, "SCCM OSD Task Sequence Hands-on", StudyPlanType.StudySession,
            today.AddDays(4).AddHours(10), 90, "Server Lab", "Test customized Windows 11 capture and deployment image.");
    }

    private static Course NewCourse(
        string userId,
        string code,
        string name,
        string instructor,
        string semester,
        string description,
        string color,
        string icon) => new()
    {
        UserId = userId,
        Code = code,
        Name = name,
        Instructor = instructor,
        Semester = semester,
        Description = description,
        Color = color,
        Icon = icon,
        CreatedAt = DateTime.UtcNow.AddDays(-30)
    };

    private static string AcademicTerm() => "Year 4 · 1st Semester";

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length > 55 ? slug[..55].TrimEnd('-') : slug;
    }
}
