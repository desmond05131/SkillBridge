# SkillBridge

WAPP Group 3 · ASP.NET Core Razor Pages · MySQL

Read [TEAM_START_HERE.txt](TEAM_START_HERE.txt) for your part, folders and steps.
Read [JIAN_YI_EXAMPLE.txt](JIAN_YI_EXAMPLE.txt) to follow a complete form through C# and SQL, then start your own feature.
Timothy: read [TIMOTHY_START_HERE.txt](TIMOTHY_START_HERE.txt) to run the quiz example and continue your part.
Read [TEAM_INTEGRATION_REVIEW.txt](TEAM_INTEGRATION_REVIEW.txt) for the integrated work, corrections and verification.
All five modules are integrated, including resource uploads and question management.

## Run

1. Install the .NET 10 SDK (10.0.301 or later in .NET 10) and start MySQL.
2. Open `database/skillbridge.sql` in MySQL Workbench and execute it.
   If you already imported the older database, run `database/migrations/001_add_course_thumbnail.sql` once instead. It preserves existing data.
3. Run `database/course_content.sql` for three ready-to-use courses, lessons, resources and quizzes.
   It can be run again without adding duplicates. Keep `database/media/` with the project.
4. Open a terminal in this project folder. Replace the database details below:

```powershell
dotnet user-secrets set "ConnectionStrings:SkillBridge" "Server=localhost;Port=3306;Database=skillbridge;User ID=YOUR_USER;Password=YOUR_PASSWORD"
dotnet run
```

Open **http://localhost:5272**. Register a Member through the website.
To create your local Admin, stop the website, run `dotnet run -- --create-admin`,
and enter a different email. Then run `dotnet run` again.

Set the connection once per machine. After that, just run `dotnet run`.
Passwords stay on your machine. Importing `course_content.sql` publishes the starter courses. Without it, they remain drafts.
Uploaded resources are saved privately in `App_Data/resources/`; back up that folder with your database.
Course thumbnails are saved in `Frontend/wwwroot/uploads/courses/`. For submission, include any uploaded resources and thumbnails your exported database references. Never include local secrets.

## Your Figma pages

30 layouts in total. Create and Edit share one form.

| Owner | Layouts | Design |
|---|---:|---|
| Jian Yi | 9 | [Accounts and Admin](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-2) |
| Chang Zhe | 5 | [Courses](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-37) |
| Darren | 5 | [Lessons and resources](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-23) |
| Timothy | 7 | [Quizzes](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-72) |
| Hamzah | 4 | [Forum](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-56) |
