CREATE DATABASE IF NOT EXISTS skillbridge CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE skillbridge;

CREATE TABLE IF NOT EXISTS roles (
    id TINYINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name ENUM('Member', 'Admin') NOT NULL,
    CONSTRAINT uq_roles_name UNIQUE (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS users (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    display_name VARCHAR(80) NOT NULL,
    email VARCHAR(254) NOT NULL,
    normalized_email VARCHAR(254) COLLATE utf8mb4_bin NOT NULL,
    password_hash VARCHAR(512) NOT NULL,
    role_id TINYINT UNSIGNED NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    security_stamp CHAR(32) COLLATE utf8mb4_bin NOT NULL,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uq_users_normalized_email UNIQUE (normalized_email),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE RESTRICT,
    CONSTRAINT ck_users_name CHECK (CHAR_LENGTH(TRIM(display_name)) BETWEEN 2 AND 80),
    CONSTRAINT ck_users_active CHECK (is_active IN (0, 1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS categories (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(60) NOT NULL,
    normalized_name VARCHAR(60) COLLATE utf8mb4_bin NOT NULL,
    description VARCHAR(300) NOT NULL DEFAULT '',
    CONSTRAINT uq_categories_name UNIQUE (normalized_name),
    CONSTRAINT ck_categories_name CHECK (CHAR_LENGTH(TRIM(name)) BETWEEN 2 AND 60)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS courses (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    category_id BIGINT NOT NULL,
    title VARCHAR(100) NOT NULL,
    normalized_title VARCHAR(100) COLLATE utf8mb4_bin NOT NULL,
    summary VARCHAR(180) NOT NULL,
    description TEXT NOT NULL,
    level ENUM('Beginner', 'Intermediate', 'Advanced') NOT NULL DEFAULT 'Beginner',
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    artwork_key VARCHAR(40) NOT NULL DEFAULT 'default',
    duration_minutes INT NOT NULL DEFAULT 0,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uq_courses_title UNIQUE (normalized_title),
    CONSTRAINT fk_courses_category FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE RESTRICT,
    CONSTRAINT ck_courses_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 3 AND 100),
    CONSTRAINT ck_courses_summary CHECK (CHAR_LENGTH(TRIM(summary)) BETWEEN 10 AND 180),
    CONSTRAINT ck_courses_description CHECK (CHAR_LENGTH(description) BETWEEN 20 AND 5000),
    CONSTRAINT ck_courses_duration CHECK (duration_minutes >= 0),
    CONSTRAINT ck_courses_publication CHECK (is_published IN (0, 1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS enrolments (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    course_id BIGINT NOT NULL,
    enrolled_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_enrolments_user_course UNIQUE (user_id, course_id),
    CONSTRAINT fk_enrolments_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_enrolments_course FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS lessons (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    course_id BIGINT NOT NULL,
    title VARCHAR(120) NOT NULL,
    summary VARCHAR(250) NOT NULL,
    body TEXT NOT NULL,
    sequence_number INT NOT NULL,
    duration_minutes INT NOT NULL,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uq_lessons_sequence UNIQUE (course_id, sequence_number),
    CONSTRAINT fk_lessons_course FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE RESTRICT,
    CONSTRAINT ck_lessons_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 3 AND 120),
    CONSTRAINT ck_lessons_body CHECK (CHAR_LENGTH(body) BETWEEN 20 AND 20000),
    CONSTRAINT ck_lessons_sequence CHECK (sequence_number > 0),
    CONSTRAINT ck_lessons_duration CHECK (duration_minutes BETWEEN 1 AND 240)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS resources (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    lesson_id BIGINT NOT NULL,
    title VARCHAR(120) NOT NULL,
    description VARCHAR(500) NOT NULL DEFAULT '',
    resource_type ENUM('Pdf', 'Video', 'Audio') NOT NULL,
    storage_key VARCHAR(100) COLLATE utf8mb4_bin NOT NULL,
    original_filename VARCHAR(255) NOT NULL,
    content_type VARCHAR(80) NOT NULL,
    size_bytes BIGINT NOT NULL,
    transcript TEXT NOT NULL,
    captions_storage_key VARCHAR(100) COLLATE utf8mb4_bin NULL,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_resources_storage_key UNIQUE (storage_key),
    CONSTRAINT fk_resources_lesson FOREIGN KEY (lesson_id) REFERENCES lessons(id) ON DELETE RESTRICT,
    CONSTRAINT ck_resources_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 3 AND 120),
    CONSTRAINT ck_resources_size CHECK (size_bytes > 0 AND ((resource_type = 'Video' AND size_bytes <= 52428800) OR (resource_type IN ('Pdf', 'Audio') AND size_bytes <= 10485760))),
    CONSTRAINT ck_resources_transcript CHECK (CHAR_LENGTH(transcript) BETWEEN 1 AND 20000)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS lesson_progress (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    lesson_id BIGINT NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at_utc TIMESTAMP NULL DEFAULT NULL,
    CONSTRAINT uq_progress_user_lesson UNIQUE (user_id, lesson_id),
    CONSTRAINT fk_progress_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_progress_lesson FOREIGN KEY (lesson_id) REFERENCES lessons(id) ON DELETE RESTRICT,
    CONSTRAINT ck_progress_completion CHECK ((is_completed = 0 AND completed_at_utc IS NULL) OR (is_completed = 1 AND completed_at_utc IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS quizzes (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    course_id BIGINT NOT NULL,
    title VARCHAR(120) NOT NULL,
    instructions VARCHAR(2000) NOT NULL,
    pass_percentage DECIMAL(5,2) NOT NULL DEFAULT 60.00,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_quizzes_course UNIQUE (course_id),
    CONSTRAINT fk_quizzes_course FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE RESTRICT,
    CONSTRAINT ck_quizzes_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 3 AND 120),
    CONSTRAINT ck_quizzes_threshold CHECK (pass_percentage BETWEEN 1 AND 100),
    CONSTRAINT ck_quizzes_publication CHECK (is_published IN (0, 1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS questions (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    quiz_id BIGINT NOT NULL,
    question_text VARCHAR(1000) NOT NULL,
    explanation VARCHAR(1500) NOT NULL,
    marks INT NOT NULL DEFAULT 1,
    sequence_number INT NOT NULL,
    CONSTRAINT uq_questions_sequence UNIQUE (quiz_id, sequence_number),
    CONSTRAINT uq_questions_id_quiz UNIQUE (id, quiz_id),
    CONSTRAINT fk_questions_quiz FOREIGN KEY (quiz_id) REFERENCES quizzes(id) ON DELETE RESTRICT,
    CONSTRAINT ck_questions_text CHECK (CHAR_LENGTH(TRIM(question_text)) BETWEEN 5 AND 1000),
    CONSTRAINT ck_questions_marks CHECK (marks BETWEEN 1 AND 100),
    CONSTRAINT ck_questions_sequence CHECK (sequence_number > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS options (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    question_id BIGINT NOT NULL,
    option_text VARCHAR(500) NOT NULL,
    sequence_number TINYINT NOT NULL,
    is_correct BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_options_sequence UNIQUE (question_id, sequence_number),
    CONSTRAINT uq_options_id_question UNIQUE (id, question_id),
    CONSTRAINT fk_options_question FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE RESTRICT,
    CONSTRAINT ck_options_text CHECK (CHAR_LENGTH(TRIM(option_text)) BETWEEN 1 AND 500),
    CONSTRAINT ck_options_sequence CHECK (sequence_number BETWEEN 1 AND 4),
    CONSTRAINT ck_options_correct CHECK (is_correct IN (0, 1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS attempts (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    quiz_id BIGINT NOT NULL,
    status ENUM('InProgress', 'Submitted') NOT NULL DEFAULT 'InProgress',
    active_slot TINYINT GENERATED ALWAYS AS (CASE WHEN status = 'InProgress' THEN 1 ELSE NULL END) STORED,
    started_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    submitted_at_utc TIMESTAMP NULL DEFAULT NULL,
    earned_marks INT NULL,
    maximum_marks INT NULL,
    CONSTRAINT uq_attempts_active UNIQUE (user_id, quiz_id, active_slot),
    CONSTRAINT uq_attempts_id_quiz UNIQUE (id, quiz_id),
    CONSTRAINT fk_attempts_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_attempts_quiz FOREIGN KEY (quiz_id) REFERENCES quizzes(id) ON DELETE RESTRICT,
    CONSTRAINT ck_attempts_submission CHECK ((status = 'InProgress' AND submitted_at_utc IS NULL AND earned_marks IS NULL AND maximum_marks IS NULL) OR (status = 'Submitted' AND submitted_at_utc IS NOT NULL AND earned_marks IS NOT NULL AND maximum_marks IS NOT NULL AND maximum_marks > 0 AND earned_marks BETWEEN 0 AND maximum_marks))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS answers (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    attempt_id BIGINT NOT NULL,
    quiz_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    selected_option_id BIGINT NOT NULL,
    earned_marks INT NULL,
    CONSTRAINT uq_answers_attempt_question UNIQUE (attempt_id, question_id),
    CONSTRAINT fk_answers_attempt_quiz FOREIGN KEY (attempt_id, quiz_id) REFERENCES attempts(id, quiz_id) ON DELETE RESTRICT,
    CONSTRAINT fk_answers_question_quiz FOREIGN KEY (question_id, quiz_id) REFERENCES questions(id, quiz_id) ON DELETE RESTRICT,
    CONSTRAINT fk_answers_option_question FOREIGN KEY (selected_option_id, question_id) REFERENCES options(id, question_id) ON DELETE RESTRICT,
    CONSTRAINT ck_answers_marks CHECK (earned_marks IS NULL OR earned_marks >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS forum_threads (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    author_id BIGINT NOT NULL,
    course_id BIGINT NULL,
    title VARCHAR(120) NOT NULL,
    body TEXT NOT NULL,
    is_pinned BOOLEAN NOT NULL DEFAULT FALSE,
    is_locked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_activity_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_threads_author FOREIGN KEY (author_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT fk_threads_course FOREIGN KEY (course_id) REFERENCES courses(id) ON DELETE RESTRICT,
    CONSTRAINT ck_threads_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 5 AND 120),
    CONSTRAINT ck_threads_body CHECK (CHAR_LENGTH(body) BETWEEN 10 AND 5000),
    CONSTRAINT ck_threads_pinned CHECK (is_pinned IN (0, 1)),
    CONSTRAINT ck_threads_locked CHECK (is_locked IN (0, 1)),
    INDEX ix_threads_activity (is_pinned, last_activity_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS forum_replies (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    thread_id BIGINT NOT NULL,
    author_id BIGINT NOT NULL,
    body TEXT NOT NULL,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_replies_thread FOREIGN KEY (thread_id) REFERENCES forum_threads(id) ON DELETE RESTRICT,
    CONSTRAINT fk_replies_author FOREIGN KEY (author_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT ck_replies_body CHECK (CHAR_LENGTH(body) BETWEEN 2 AND 3000)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS announcements (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    author_id BIGINT NOT NULL,
    title VARCHAR(120) NOT NULL,
    body VARCHAR(2000) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    activated_at_utc TIMESTAMP NULL DEFAULT NULL,
    created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_announcements_author FOREIGN KEY (author_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT ck_announcements_title CHECK (CHAR_LENGTH(TRIM(title)) BETWEEN 3 AND 120),
    CONSTRAINT ck_announcements_body CHECK (CHAR_LENGTH(body) BETWEEN 10 AND 2000),
    CONSTRAINT ck_announcements_active CHECK (is_active IN (0, 1)),
    CONSTRAINT ck_announcements_activation CHECK (is_active = 0 OR activated_at_utc IS NOT NULL),
    INDEX ix_announcements_active (is_active, activated_at_utc, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO roles (name) SELECT 'Member' WHERE NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Member');
INSERT INTO roles (name) SELECT 'Admin' WHERE NOT EXISTS (SELECT 1 FROM roles WHERE name = 'Admin');

START TRANSACTION;

INSERT INTO categories (name, normalized_name, description)
SELECT 'Developer tools', 'DEVELOPER TOOLS', 'Practical tools for managing and collaborating on software projects.'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE normalized_name = 'DEVELOPER TOOLS');

INSERT INTO categories (name, normalized_name, description)
SELECT 'Databases', 'DATABASES', 'Store, retrieve and maintain structured information.'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE normalized_name = 'DATABASES');

INSERT INTO categories (name, normalized_name, description)
SELECT 'Operating systems', 'OPERATING SYSTEMS', 'Work confidently with a command-line environment.'
WHERE NOT EXISTS (SELECT 1 FROM categories WHERE normalized_name = 'OPERATING SYSTEMS');

INSERT INTO courses (category_id, title, normalized_title, summary, description, level, is_published, artwork_key, duration_minutes)
SELECT id, 'Git Essentials', 'GIT ESSENTIALS', 'Learn to track changes, create branches and collaborate with Git.',
    'A planned beginner course covering repository setup, useful commits and a small branching workflow. The course remains a draft until its lessons, media and quiz are implemented and reviewed.',
    'Beginner', FALSE, 'git', 0
FROM categories WHERE normalized_name = 'DEVELOPER TOOLS'
AND NOT EXISTS (SELECT 1 FROM courses WHERE normalized_title = 'GIT ESSENTIALS');

INSERT INTO courses (category_id, title, normalized_title, summary, description, level, is_published, artwork_key, duration_minutes)
SELECT id, 'SQL Fundamentals', 'SQL FUNDAMENTALS', 'Read and update relational data using clear, parameterised SQL queries.',
    'A planned beginner course covering relational tables, SELECT queries and safe data changes. The course remains a draft until its lessons, media and quiz are implemented and reviewed.',
    'Beginner', FALSE, 'sql', 0
FROM categories WHERE normalized_name = 'DATABASES'
AND NOT EXISTS (SELECT 1 FROM courses WHERE normalized_title = 'SQL FUNDAMENTALS');

INSERT INTO courses (category_id, title, normalized_title, summary, description, level, is_published, artwork_key, duration_minutes)
SELECT id, 'Linux Command Line', 'LINUX COMMAND LINE', 'Navigate files, inspect permissions and practise essential Linux commands.',
    'A planned beginner course covering directory navigation, file operations and basic permissions. The course remains a draft until its lessons, media and quiz are implemented and reviewed.',
    'Beginner', FALSE, 'linux', 0
FROM categories WHERE normalized_name = 'OPERATING SYSTEMS'
AND NOT EXISTS (SELECT 1 FROM courses WHERE normalized_title = 'LINUX COMMAND LINE');

COMMIT;
