# SkillBridge — WAPP Group 3

## Requirements
.NET 10 SDK 10.0.301 or a later .NET 10 feature band; MySQL 8.0; MySQL Workbench.

## Set up and run
1. Start MySQL. In Workbench, execute `database/skillbridge.sql`, then `database/course_content.sql`. These target the `skillbridge` database. Use a fresh local database/server for grading; the scripts do not erase an existing database. Keep `database/media` in the project.
2. Open PowerShell in this folder. Restore and build:

```powershell
dotnet restore --locked-mode
dotnet build --no-restore
```

3. Set your own connection details in that same terminal without saving a password in the project:

```powershell
$dbSecret = Read-Host 'Enter Server=localhost;Port=3306;Database=skillbridge;User ID=YOUR_USER;Password=YOUR_PASSWORD' -AsSecureString
$env:ConnectionStrings__SkillBridge = [System.Net.NetworkCredential]::new('', $dbSecret).Password
Remove-Variable dbSecret
```

4. Create the administrator:

```powershell
dotnet run -- --create-admin
```

Enter a name, email and password. New passwords require 12–128 characters, at least one uppercase letter A–Z and one symbol such as ! or #. No default password is included.

5. Start the site:

```powershell
dotnet run
```

Open http://localhost:5272. Sign in using the Admin you created. Register a separate Member through Create account. Stop the running app with Ctrl+C before rebuilding or starting it again.

## Check the features
- Admin: users, courses, lessons/resources, quizzes/questions and forum moderation.
- Member: browse/enrol, study text/video/audio/PDF, track progress, attempt quizzes, review answers and create discussions/replies.
- Three seeded courses include six lessons, three quizzes and nine questions. The forum starts empty; create a discussion as a Member.
- Quiz deletion removes questions/options only when no learner attempts exist. Existing learner attempts are protected.
- Sign out all devices invalidates old sessions; it does not change the password.

## Folders
`Frontend`: Razor pages, CSS, JavaScript and local fonts.
`Backend`: C# page handlers, account rules and parameterised SQL, grouped by member.
`database`: schema, starter content and required teaching media.
`Properties`: local launch configuration.

The package uses reproducible starter data, not a copy of personal accounts or test records. Uploaded files are created under App_Data/resources and Frontend/wwwroot/uploads/courses while using the site. No personal uploads are needed by the supplied SQL. The build creates bin and obj automatically. Font licence notices and teaching-media provenance are retained.
