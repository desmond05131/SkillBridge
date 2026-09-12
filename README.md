# SkillBridge

WAPP Group 3 · ASP.NET Core Razor Pages · MySQL

Read [TEAM_START_HERE.txt](TEAM_START_HERE.txt) for your part, folders and steps.
This is the shared starter; the team still needs to finish the features.

## Run

1. Install the .NET 10 SDK (10.0.301 or later in .NET 10) and start MySQL.
2. Open `database/skillbridge.sql` in MySQL Workbench and execute it.
3. Open a terminal in this project folder. Replace the database details below:

```powershell
dotnet user-secrets set "ConnectionStrings:SkillBridge" "Server=localhost;Port=3306;Database=skillbridge;User ID=YOUR_USER;Password=YOUR_PASSWORD"
dotnet run
```

Open **http://localhost:5272**. Register a Member through the website.
To create your local Admin, stop the website, run `dotnet run -- --create-admin`,
and enter a different email. Then run `dotnet run` again.

Set the connection once per machine. After that, just run `dotnet run`.
Passwords stay on your machine. The starting courses are drafts, so lists are initially empty.

## Your Figma pages

30 layouts in total. Create and Edit share one form.

| Owner | Layouts | Design |
|---|---:|---|
| Jian Yi | 9 | [Accounts and Admin](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-2) |
| Chang Zhe | 5 | [Courses](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-37) |
| Darren | 5 | [Lessons and resources](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-23) |
| Timothy | 7 | [Quizzes](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-72) |
| Hamzah | 4 | [Forum](https://www.figma.com/design/kpUFQaF3ozCfoMnb4ERyTi/WAPP---SkillBridge?node-id=30-56) |
