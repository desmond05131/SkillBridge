# SkillBridge

WAPP Group 3 · ASP.NET Core Razor Pages · MySQL

## Run

1. Install the .NET 10 SDK and start MySQL.
2. Open `database/skillbridge.sql` in MySQL Workbench and execute it.
3. In this project folder, set your local database connection once, then run:

```powershell
dotnet user-secrets set "ConnectionStrings:SkillBridge" "Server=localhost;Port=3306;Database=skillbridge;User ID=YOUR_USER;Password=YOUR_PASSWORD"
dotnet run
```

Open **http://localhost:5272**. Register a Member through the website. To create your local Admin, stop the website and run `dotnet run -- --create-admin`, then `dotnet run` again.

Database passwords stay on your machine. If your connection is already configured, just run `dotnet run`.

## Your part

Click your name to open your Figma pages.

| Owner / folder | Pages and features |
|---|---|
| [Jian Yi](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-2) · `JianYi` | Home, accounts, users and shared layout. Finish user create/edit/delete; coordinate integration. |
| [Chang Zhe](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-37) · `ChangZhe` | Courses, categories, My Courses and enrol/drop; Admin course/category create/edit/delete. |
| [Darren](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-23) · `Darren` | Lessons, video/resources and progress; Admin lesson/resource create/edit/delete and protected downloads. |
| [Timothy](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-72) · `Timothy` | Quiz attempt, results/history and grading; Admin quiz/question create/edit/delete. |
| [Hamzah](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-56) · `Hamzah` | Threads, replies, moderation and announcements; author/Admin permissions and create/edit/delete. |

Each person builds their pages, backend, SQL, validation and access checks. Reuse forms for Create/Edit and show errors on the same page. No separate mobile pages, quiz timer or self-service account closure.

## Folders

```text
Frontend/Pages/<owner>/   Page markup (.cshtml)
Frontend/Pages/Shared/    Shared layout and components
Frontend/wwwroot/        CSS, JavaScript, images and fonts
Backend/Pages/<owner>/    Page handlers (.cshtml.cs)
Backend/<owner>/         Module logic and queries
Backend/Shared/          Shared database code
database/                Schema and starting data
appsettings*.json        General settings; keep passwords in user secrets
```

Keep each page and its handler under matching owner/subfolder names. Add Darren/Timothy folders with their first files. Put each owner's Admin pages in `Frontend/Pages/<owner>/Admin/`. The browser URLs stay simple, such as `/Courses` and `/Admin/Courses`.

## Start here

Accounts, styling, the Admin dashboard and basic course/forum reads work. The other features above still need development. The three starting courses are drafts, so the public catalogue is initially empty.

Work on your own branch. Build and test your feature, including invalid input and denied access, then open a pull request. Coordinate shared layout, database and `Program.cs` changes with Jian Yi. Keep screenshots and SQL explanations for your report section.

Do not commit passwords, `bin/`, `obj/`, local editor files or helper material.
