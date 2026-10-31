-- Optional complete teaching content. Run after skillbridge.sql. Existing records are not replaced.

USE skillbridge;

START TRANSACTION;

SET @course=(SELECT id FROM courses WHERE normalized_title='GIT ESSENTIALS');

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Track your first change','Create a small repository and understand the working tree, staging area and commit history.','Git records snapshots of a project. The working tree contains the files you edit. The staging area selects changes for the next snapshot. A commit records that snapshot with a message and a link to the previous commit.
Practise in a new folder rather than an existing assignment. Run: mkdir git-practice, then cd git-practice, then git init. Create a file named notes.txt containing one sentence. Run git status to see that the new file is untracked.
Run git add notes.txt to stage the file. Run git diff --cached to inspect the staged change. Commit it with git commit -m "Add practice notes". If Git asks for an identity, configure your own name and email before retrying.
Edit the sentence and run git diff. This compares the working file with the staged version. Stage the update, review git diff --cached again and make a second focused commit. Run git log --oneline to inspect the two snapshots.
Checkpoint: explain why saving a file does not automatically create a Git commit. Your answer should distinguish editing, staging and committing. Never stage passwords, database credentials or build output.',1,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=1);

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Work on a feature branch','Use a branch to isolate changes and review them before combining work.','A branch is a movable name pointing to a commit. It lets you develop a change without immediately moving the main branch. It is not a separate backup of every file.
In the practice repository, run git switch -c improve-notes. Add a second paragraph to notes.txt, stage it, inspect the staged diff and commit with a descriptive message.
Run git switch main if your initial branch is named main; otherwise use the branch name shown by git branch. Run git merge improve-notes to combine the completed practice change. If no competing commits exist, Git may fast-forward the branch pointer.
A merge conflict means Git could not safely combine overlapping changes. Open the reported files, decide the intended final text, remove conflict markers, test the result, stage the resolved files and complete the merge. Do not choose a version blindly.
For group work, get the latest shared branch before starting, use a feature branch, commit source files rather than a ZIP, and review the changes before merging. Use git status to check for uncommitted work before switching tasks.
Checkpoint: describe which branch you would use for a quiz-editor change and what you would review before integrating it.',2,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=2);

SET @lesson=(SELECT id FROM lessons WHERE course_id=@course AND title='Track your first change' LIMIT 1);

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Git Essentials - Practice guide','Two lessons with exercises and checkpoints.','Pdf','git-guide.pdf','git-guide.pdf','application/pdf',3420,'Track your first change
Git records snapshots of a project. The working tree contains the files you edit. The staging area selects changes for the next snapshot. A commit records that snapshot with a message and a link to the previous commit.
Practise in a new folder rather than an existing assignment. Run: mkdir git-practice, then cd git-practice, then git init. Create a file named notes.txt containing one sentence. Run git status to see that the new file is untracked.
Run git add notes.txt to stage the file. Run git diff --cached to inspect the staged change. Commit it with git commit -m "Add practice notes". If Git asks for an identity, configure your own name and email before retrying.
Edit the sentence and run git diff. This compares the working file with the staged version. Stage the update, review git diff --cached again and make a second focused commit. Run git log --oneline to inspect the two snapshots.
Checkpoint: explain why saving a file does not automatically create a Git commit. Your answer should distinguish editing, staging and committing. Never stage passwords, database credentials or build output.

Work on a feature branch
A branch is a movable name pointing to a commit. It lets you develop a change without immediately moving the main branch. It is not a separate backup of every file.
In the practice repository, run git switch -c improve-notes. Add a second paragraph to notes.txt, stage it, inspect the staged diff and commit with a descriptive message.
Run git switch main if your initial branch is named main; otherwise use the branch name shown by git branch. Run git merge improve-notes to combine the completed practice change. If no competing commits exist, Git may fast-forward the branch pointer.
A merge conflict means Git could not safely combine overlapping changes. Open the reported files, decide the intended final text, remove conflict markers, test the result, stage the resolved files and complete the merge. Do not choose a version blindly.
For group work, get the latest shared branch before starting, use a feature branch, commit source files rather than a ZIP, and review the changes before merging. Use git status to check for uncommitted work before switching tasks.
Checkpoint: describe which branch you would use for a quiz-editor change and what you would review before integrating it.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='git-guide.pdf');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Git Essentials - Captioned walkthrough','A short, silent illustrated walkthrough. Pause to practise each step.','Video','git-walkthrough.mp4','git-walkthrough.mp4','video/mp4',128613,'Git Essentials. Create a repository. Start in a new practice folder so your assignment files stay separate. Inspect before staging. Status lists changed files. Diff shows changes that have not been staged. Stage and review. Stage one intended file, then inspect exactly what the commit will contain. Record the snapshot. A commit records the staged snapshot. Log shows the saved history.','git-captions.vtt' WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='git-walkthrough.mp4');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Git Essentials - Audio recap','A synthetic-voice recap with an equivalent text transcript.','Audio','git-recap.wav','git-recap.wav','audio/wav',1408150,'Git Essentials. Create a repository. Start in a new practice folder so your assignment files stay separate. Inspect before staging. Status lists changed files. Diff shows changes that have not been staged. Stage and review. Stage one intended file, then inspect exactly what the commit will contain. Record the snapshot. A commit records the staged snapshot. Log shows the saved history.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='git-recap.wav');

INSERT INTO quizzes (course_id,title,instructions,pass_percentage,is_published) SELECT @course,'Git Essentials check','Choose one answer for each question. Review every explanation after submission.',60,FALSE WHERE @course IS NOT NULL AND NOT EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course);

SET @quiz=(SELECT id FROM quizzes WHERE course_id=@course);

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Which command stages notes.txt for the next commit?','git add copies the selected file change into the staging area.',1,1 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=1);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git add notes.txt',1,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git log notes.txt',2,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git init notes.txt',3,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git branch notes.txt',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Which command shows staged changes before committing?','git diff --cached compares staged changes with the last commit.',1,2 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=2);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git status --short',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git diff --cached',2,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git switch main',3,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'git init',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Why use a feature branch for a team change?','A feature branch separates ongoing work so the team can review and integrate it deliberately.',1,3 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=3);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'To include passwords safely',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'To avoid reviewing code',2,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'To isolate work before integration',3,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'To replace database backups',4,0 WHERE @new_question IS NOT NULL;

UPDATE quizzes SET is_published=TRUE WHERE id=@quiz AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions q WHERE q.quiz_id=@quiz AND ((SELECT COUNT(*) FROM options o WHERE o.question_id=q.id)<>4 OR (SELECT COUNT(*) FROM options o WHERE o.question_id=q.id AND o.is_correct=TRUE)<>1));

UPDATE courses SET summary='Track changes, make focused commits and practise a safe feature-branch workflow.', description='Two practical lessons cover track changes, make focused commits and practise a safe feature-branch workflow. Includes a downloadable guide, captioned walkthrough, audio recap and self-assessment.',duration_minutes=30,is_published=TRUE WHERE id=@course AND description LIKE 'A planned beginner course%' AND (SELECT COUNT(*) FROM lessons WHERE course_id=@course)>=2 AND EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course AND is_published=TRUE);

SET @course=(SELECT id FROM courses WHERE normalized_title='SQL FUNDAMENTALS');

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Read and filter rows','Use SELECT, WHERE and ORDER BY to answer clear questions about a table.','A relational table stores rows with a consistent set of columns. A primary key identifies each row. A foreign key links a row to a related table and helps prevent invalid references.
Imagine courses(id, title, level) with three sample rows. SELECT title FROM courses returns the title column. SELECT title FROM courses WHERE level = ''Beginner'' returns only matching rows. Add ORDER BY title to request alphabetical ordering; without ORDER BY, a particular order is not guaranteed.
SELECT COUNT(*) FROM courses counts rows. SELECT level, COUNT(*) FROM courses GROUP BY level counts rows within each level. Choose the columns and grouping that match the question you are answering.
SQL NULL represents missing or unknown information. Use IS NULL or IS NOT NULL to test it rather than comparing with = NULL. A condition involving unknown information can behave differently from a true/false comparison.
Practice: write a query that returns beginner course IDs and titles, sorted by title. Then change it to count beginner courses. Explain why selecting only the needed columns is clearer than using SELECT * everywhere.
Checkpoint: WHERE filters rows; ORDER BY controls the output order. They solve different problems.',1,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=1);

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Join and change data safely','Connect related tables and use parameters and transactions for data changes.','Suppose enrolments contains user_id and course_id. Join it to courses with SELECT c.title FROM courses c INNER JOIN enrolments e ON e.course_id = c.id WHERE e.user_id = @userId. The join condition connects each enrolment to its course.
An INNER JOIN keeps matching pairs. A LEFT JOIN keeps every row from the left table and uses NULL values where no right-side match exists. Choose deliberately: listing every course, including those with no enrolments, needs a different query from listing only enrolled courses.
INSERT adds a row, UPDATE changes rows and DELETE removes rows. An UPDATE or DELETE without a WHERE condition can affect every row. First inspect the intended target with SELECT using the same condition and practise only in a disposable database.
In C#, write SQL containing a placeholder such as @userId and bind the value with a database parameter. Do not concatenate user input into SQL. Parameters keep values separate from SQL syntax; validation and permission checks are still required.
A transaction groups related changes. Commit when all steps succeed; roll back if a step fails. For a quiz submission, answer rows and the final score should succeed together so a partial result is not saved.
Checkpoint: explain why a valid numeric user ID still requires an ownership check before showing that user''s results.',2,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=2);

SET @lesson=(SELECT id FROM lessons WHERE course_id=@course AND title='Read and filter rows' LIMIT 1);

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'SQL Fundamentals - Practice guide','Two lessons with exercises and checkpoints.','Pdf','sql-guide.pdf','sql-guide.pdf','application/pdf',3642,'Read and filter rows
A relational table stores rows with a consistent set of columns. A primary key identifies each row. A foreign key links a row to a related table and helps prevent invalid references.
Imagine courses(id, title, level) with three sample rows. SELECT title FROM courses returns the title column. SELECT title FROM courses WHERE level = ''Beginner'' returns only matching rows. Add ORDER BY title to request alphabetical ordering; without ORDER BY, a particular order is not guaranteed.
SELECT COUNT(*) FROM courses counts rows. SELECT level, COUNT(*) FROM courses GROUP BY level counts rows within each level. Choose the columns and grouping that match the question you are answering.
SQL NULL represents missing or unknown information. Use IS NULL or IS NOT NULL to test it rather than comparing with = NULL. A condition involving unknown information can behave differently from a true/false comparison.
Practice: write a query that returns beginner course IDs and titles, sorted by title. Then change it to count beginner courses. Explain why selecting only the needed columns is clearer than using SELECT * everywhere.
Checkpoint: WHERE filters rows; ORDER BY controls the output order. They solve different problems.

Join and change data safely
Suppose enrolments contains user_id and course_id. Join it to courses with SELECT c.title FROM courses c INNER JOIN enrolments e ON e.course_id = c.id WHERE e.user_id = @userId. The join condition connects each enrolment to its course.
An INNER JOIN keeps matching pairs. A LEFT JOIN keeps every row from the left table and uses NULL values where no right-side match exists. Choose deliberately: listing every course, including those with no enrolments, needs a different query from listing only enrolled courses.
INSERT adds a row, UPDATE changes rows and DELETE removes rows. An UPDATE or DELETE without a WHERE condition can affect every row. First inspect the intended target with SELECT using the same condition and practise only in a disposable database.
In C#, write SQL containing a placeholder such as @userId and bind the value with a database parameter. Do not concatenate user input into SQL. Parameters keep values separate from SQL syntax; validation and permission checks are still required.
A transaction groups related changes. Commit when all steps succeed; roll back if a step fails. For a quiz submission, answer rows and the final score should succeed together so a partial result is not saved.
Checkpoint: explain why a valid numeric user ID still requires an ownership check before showing that user''s results.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='sql-guide.pdf');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'SQL Fundamentals - Captioned walkthrough','A short, silent illustrated walkthrough. Pause to practise each step.','Video','sql-walkthrough.mp4','sql-walkthrough.mp4','video/mp4',124904,'SQL Fundamentals. Select needed columns. A SELECT query reads data. Choose columns that answer your question. Filter the result. WHERE keeps only rows matching the condition. Make order explicit. ORDER BY requests a predictable output order. Bind application values. Bind user values as parameters. Do not paste them into SQL text.','sql-captions.vtt' WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='sql-walkthrough.mp4');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'SQL Fundamentals - Audio recap','A synthetic-voice recap with an equivalent text transcript.','Audio','sql-recap.wav','sql-recap.wav','audio/wav',1262416,'SQL Fundamentals. Select needed columns. A SELECT query reads data. Choose columns that answer your question. Filter the result. WHERE keeps only rows matching the condition. Make order explicit. ORDER BY requests a predictable output order. Bind application values. Bind user values as parameters. Do not paste them into SQL text.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='sql-recap.wav');

INSERT INTO quizzes (course_id,title,instructions,pass_percentage,is_published) SELECT @course,'SQL Fundamentals check','Choose one answer for each question. Review every explanation after submission.',60,FALSE WHERE @course IS NOT NULL AND NOT EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course);

SET @quiz=(SELECT id FROM quizzes WHERE course_id=@course);

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Which clause restricts returned rows?','WHERE applies a condition to the rows considered by the query.',1,1 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=1);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'ORDER BY',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'WHERE',2,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'VALUES',3,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'SET',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Which join keeps unmatched rows from the left table?','LEFT JOIN retains left-side rows and fills missing right-side columns with NULL.',1,2 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=2);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'INNER JOIN',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'CROSS JOIN',2,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'LEFT JOIN',3,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'An equality comparison alone',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'How should a C# application pass a user-entered value into SQL?','A parameter treats the supplied input as a value rather than SQL syntax.',1,3 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=3);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Concatenate it into the query',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Remove spaces from it',2,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Use a bound SQL parameter',3,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Put it in an HTML comment',4,0 WHERE @new_question IS NOT NULL;

UPDATE quizzes SET is_published=TRUE WHERE id=@quiz AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions q WHERE q.quiz_id=@quiz AND ((SELECT COUNT(*) FROM options o WHERE o.question_id=q.id)<>4 OR (SELECT COUNT(*) FROM options o WHERE o.question_id=q.id AND o.is_correct=TRUE)<>1));

UPDATE courses SET summary='Read relational tables, join related rows and make targeted, parameterised changes.', description='Two practical lessons cover read relational tables, join related rows and make targeted, parameterised changes. Includes a downloadable guide, captioned walkthrough, audio recap and self-assessment.',duration_minutes=30,is_published=TRUE WHERE id=@course AND description LIKE 'A planned beginner course%' AND (SELECT COUNT(*) FROM lessons WHERE course_id=@course)>=2 AND EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course AND is_published=TRUE);

SET @course=(SELECT id FROM courses WHERE normalized_title='LINUX COMMAND LINE');

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Navigate and inspect files','Understand paths and practise common file commands in a disposable directory.','The shell runs commands in a current working directory. Run pwd to print that location. Run ls to list visible entries, and ls -la to include hidden names and show detailed information.
An absolute path begins at the filesystem root, such as /home/student/practice. A relative path is interpreted from the current directory. The dot names the current directory; two dots name its parent. A tilde commonly expands to the current user''s home directory.
Create a safe practice area with mkdir -p ~/skillbridge-practice, then cd ~/skillbridge-practice. Use printf ''hello\\n'' > notes.txt to create a small text file, then cat notes.txt to read it. The > operator replaces the destination contents; >> appends instead.
Use cp notes.txt notes-copy.txt to copy the file. Use mv notes-copy.txt archive.txt to rename the copy. Confirm both the current path and the exact target before removing a file with rm. File removal from the shell may not use a recycle bin.
Quotes matter when paths contain spaces. For example, mkdir ''practice folder'' creates one directory with a space in its name. Without quotes, the shell separates arguments at spaces.
Checkpoint: explain how pwd and ls help you avoid running a file operation in the wrong directory.',1,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=1);

INSERT INTO lessons (course_id,title,summary,body,sequence_number,duration_minutes) SELECT @course,'Read permissions and inspect output','Interpret file permissions and combine simple read-only commands.','Run ls -l notes.txt to inspect a file''s mode and ownership. The permission groups describe access for the owner, group and others. For regular files, r means read, w means write and x means execute. Directory permissions have different effects: execute permits traversal, while read permits listing names.
The mode rw-r----- gives the owner read/write, the group read-only access and others no access. Use chmod u+x script.sh to add execute permission for the owner of your own practice script. Avoid granting broad permissions just to hide an access error.
A pipe sends standard output from one command to the next command''s standard input. For example, ls -l | head -n 5 shows the first five output lines. Use grep ''hello'' notes.txt to print matching lines in the practice file.
Use man command-name or command-name --help to check supported options. Read the help before using recursive or force options. Work with a regular user account and request elevated rights only for a task that actually requires them.
Practice: create two text files, list their permissions and search for a word in one file. Explain what changes if you remove write permission from a file you own.
Checkpoint: permissions are an access control mechanism, not a substitute for keeping backups or checking command targets.',2,15 WHERE @course IS NOT NULL AND NOT EXISTS (SELECT 1 FROM lessons WHERE course_id=@course AND sequence_number=2);

SET @lesson=(SELECT id FROM lessons WHERE course_id=@course AND title='Navigate and inspect files' LIMIT 1);

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Linux Command Line - Practice guide','Two lessons with exercises and checkpoints.','Pdf','linux-guide.pdf','linux-guide.pdf','application/pdf',3564,'Navigate and inspect files
The shell runs commands in a current working directory. Run pwd to print that location. Run ls to list visible entries, and ls -la to include hidden names and show detailed information.
An absolute path begins at the filesystem root, such as /home/student/practice. A relative path is interpreted from the current directory. The dot names the current directory; two dots name its parent. A tilde commonly expands to the current user''s home directory.
Create a safe practice area with mkdir -p ~/skillbridge-practice, then cd ~/skillbridge-practice. Use printf ''hello\\n'' > notes.txt to create a small text file, then cat notes.txt to read it. The > operator replaces the destination contents; >> appends instead.
Use cp notes.txt notes-copy.txt to copy the file. Use mv notes-copy.txt archive.txt to rename the copy. Confirm both the current path and the exact target before removing a file with rm. File removal from the shell may not use a recycle bin.
Quotes matter when paths contain spaces. For example, mkdir ''practice folder'' creates one directory with a space in its name. Without quotes, the shell separates arguments at spaces.
Checkpoint: explain how pwd and ls help you avoid running a file operation in the wrong directory.

Read permissions and inspect output
Run ls -l notes.txt to inspect a file''s mode and ownership. The permission groups describe access for the owner, group and others. For regular files, r means read, w means write and x means execute. Directory permissions have different effects: execute permits traversal, while read permits listing names.
The mode rw-r----- gives the owner read/write, the group read-only access and others no access. Use chmod u+x script.sh to add execute permission for the owner of your own practice script. Avoid granting broad permissions just to hide an access error.
A pipe sends standard output from one command to the next command''s standard input. For example, ls -l | head -n 5 shows the first five output lines. Use grep ''hello'' notes.txt to print matching lines in the practice file.
Use man command-name or command-name --help to check supported options. Read the help before using recursive or force options. Work with a regular user account and request elevated rights only for a task that actually requires them.
Practice: create two text files, list their permissions and search for a word in one file. Explain what changes if you remove write permission from a file you own.
Checkpoint: permissions are an access control mechanism, not a substitute for keeping backups or checking command targets.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='linux-guide.pdf');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Linux Command Line - Captioned walkthrough','A short, silent illustrated walkthrough. Pause to practise each step.','Video','linux-walkthrough.mp4','linux-walkthrough.mp4','video/mp4',126504,'Linux Command Line. Check your location. Inspect the current directory before changing files. Create a practice area. Keep exercises in a disposable directory you own. Copy and rename. Copy leaves the source intact. Move can rename the copied file. Inspect permissions. Read the permission groups. Add only the access the task needs.','linux-captions.vtt' WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='linux-walkthrough.mp4');

INSERT INTO resources (lesson_id,title,description,resource_type,storage_key,original_filename,content_type,size_bytes,transcript,captions_storage_key) SELECT @lesson,'Linux Command Line - Audio recap','A synthetic-voice recap with an equivalent text transcript.','Audio','linux-recap.wav','linux-recap.wav','audio/wav',1248750,'Linux Command Line. Check your location. Inspect the current directory before changing files. Create a practice area. Keep exercises in a disposable directory you own. Copy and rename. Copy leaves the source intact. Move can rename the copied file. Inspect permissions. Read the permission groups. Add only the access the task needs.',NULL WHERE @lesson IS NOT NULL AND NOT EXISTS (SELECT 1 FROM resources WHERE storage_key='linux-recap.wav');

INSERT INTO quizzes (course_id,title,instructions,pass_percentage,is_published) SELECT @course,'Linux Command Line check','Choose one answer for each question. Review every explanation after submission.',60,FALSE WHERE @course IS NOT NULL AND NOT EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course);

SET @quiz=(SELECT id FROM quizzes WHERE course_id=@course);

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'Which command prints the current working directory?','pwd prints the current working directory.',1,1 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=1);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'ls',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'pwd',2,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'cp',3,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'chmod',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'What does cp notes.txt backup.txt do?','cp creates a copy at the destination while retaining the source.',1,2 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=2);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Renames notes.txt without keeping it',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Copies notes.txt to backup.txt',2,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Changes notes.txt permissions',3,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Prints the current directory',4,0 WHERE @new_question IS NOT NULL;

INSERT INTO questions (quiz_id,question_text,explanation,marks,sequence_number) SELECT @quiz,'In regular-file permissions, what does x mean?','The x permission allows execution of a regular file, subject to other system controls.',1,3 WHERE @quiz IS NOT NULL AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz AND sequence_number=3);

SET @new_question=IF(ROW_COUNT()=1,LAST_INSERT_ID(),NULL);

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Delete',1,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Read',2,0 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Execute',3,1 WHERE @new_question IS NOT NULL;

INSERT INTO options (question_id,option_text,sequence_number,is_correct) SELECT @new_question,'Change owner',4,0 WHERE @new_question IS NOT NULL;

UPDATE quizzes SET is_published=TRUE WHERE id=@quiz AND NOT EXISTS(SELECT 1 FROM attempts WHERE quiz_id=@quiz) AND EXISTS(SELECT 1 FROM questions WHERE quiz_id=@quiz) AND NOT EXISTS(SELECT 1 FROM questions q WHERE q.quiz_id=@quiz AND ((SELECT COUNT(*) FROM options o WHERE o.question_id=q.id)<>4 OR (SELECT COUNT(*) FROM options o WHERE o.question_id=q.id AND o.is_correct=TRUE)<>1));

UPDATE courses SET summary='Navigate a practice directory, handle files carefully and interpret basic permissions.', description='Two practical lessons cover navigate a practice directory, handle files carefully and interpret basic permissions. Includes a downloadable guide, captioned walkthrough, audio recap and self-assessment.',duration_minutes=30,is_published=TRUE WHERE id=@course AND description LIKE 'A planned beginner course%' AND (SELECT COUNT(*) FROM lessons WHERE course_id=@course)>=2 AND EXISTS(SELECT 1 FROM quizzes WHERE course_id=@course AND is_published=TRUE);

COMMIT;
