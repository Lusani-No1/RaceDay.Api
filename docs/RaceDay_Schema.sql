
/* Matches: RaceDay ERD (Section A) and API Endpoint Plan (Section B) */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if they already exist (in FK-safe order), so the script
can be re-run cleanly on the same instance during testing. */

IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.UserProfiles', 'U') IS NOT NULL DROP TABLE dbo.UserProfiles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* TABLE: Users
Holds both Organisers and Participants, distinguished by Role. */

CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(100)       NOT NULL,
    Email           NVARCHAR(150)       NOT NULL,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    Role            NVARCHAR(20)        NOT NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* TABLE: UserProfiles
Optional extended profile info. 1-to-0..1 with Users. */

CREATE TABLE dbo.UserProfiles (
    ProfileId           INT IDENTITY(1,1)  NOT NULL,
    UserId              INT                NOT NULL,
    PhoneNumber         NVARCHAR(20)       NULL,
    DateOfBirth         DATE               NULL,
    EmergencyContact    NVARCHAR(100)      NULL,
    ProfileImageUrl     NVARCHAR(255)      NULL,
    CONSTRAINT PK_UserProfiles PRIMARY KEY (ProfileId),
    CONSTRAINT UQ_UserProfiles_UserId UNIQUE (UserId),
    CONSTRAINT FK_UserProfiles_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId)
        ON DELETE CASCADE
);
GO

/* TABLE: Events
Created and owned by an Organiser (Users.Role = 'Organiser'). */

CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1)   NOT NULL,
    OrganiserId     INT                 NOT NULL,
    Name            NVARCHAR(150)       NOT NULL,
    Description     NVARCHAR(1000)      NULL,
    EventDate       DATE                NOT NULL,
    Location        NVARCHAR(150)       NOT NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId)
);
GO

/* TABLE: Categories
Each Event has one or more Categories (e.g. 10km, 21km, Fun Ride). */

CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1)   NOT NULL,
    EventId         INT                 NOT NULL,
    Name            NVARCHAR(100)       NOT NULL,
    DistanceKm      DECIMAL(6,2)        NOT NULL,
    Price           DECIMAL(8,2)        NOT NULL DEFAULT 0,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId)
        ON DELETE CASCADE,
    CONSTRAINT CK_Categories_DistanceKm CHECK (DistanceKm > 0),
    CONSTRAINT CK_Categories_Price CHECK (Price >= 0)
);
GO

/* TABLE: Enrolments
A Participant (Users.Role = 'Participant') enters a Category. */

CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT                 NOT NULL,
    CategoryId      INT                 NOT NULL,
    EnrolmentDate   DATETIME2           NOT NULL DEFAULT SYSDATETIME(),
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled', 'Pending'))
);
GO

--TABLE: Results 1-to-0..1 with Enrolments. Captured by an Organiser (CapturedByUserId). */

CREATE TABLE dbo.Results (
    ResultId            INT IDENTITY(1,1)  NOT NULL,
    EnrolmentId         INT                NOT NULL,
    CapturedByUserId    INT                NOT NULL,
    FinishTime          TIME(0)            NOT NULL,
    Position             INT                NULL,
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_EnrolmentId UNIQUE (EnrolmentId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId)
        ON DELETE CASCADE,
    CONSTRAINT FK_Results_Users FOREIGN KEY (CapturedByUserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Results_Position CHECK (Position IS NULL OR Position > 0)
);
GO


-- SAMPLE DATA 

-- 2 Organisers + 2 Participants (minimum required) + 1 extra participant
-- for richer sample enrolments/results

INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Sipho Nkosi',      'sipho.nkosi@raceday.co.za',   'HASHED_PW_1', 'Organiser'),
('Lerato van Wyk',    'lerato.vanwyk@raceday.co.za', 'HASHED_PW_2', 'Organiser'),
('Thabo Mokoena',     'thabo.mokoena@example.com',   'HASHED_PW_3', 'Participant'),
('Amy Petersen',      'amy.petersen@example.com',    'HASHED_PW_4', 'Participant'),
('Jason Naidoo',      'jason.naidoo@example.com',    'HASHED_PW_5', 'Participant');
GO

-- Profiles for the participants
INSERT INTO dbo.UserProfiles (UserId, PhoneNumber, DateOfBirth, EmergencyContact, ProfileImageUrl) VALUES
(3, '0821234567', '1994-03-12', 'Nomsa Mokoena - 0839876543', NULL),
(4, '0827654321', '1990-07-25', 'Chris Petersen - 0812223344', NULL),
(5, '0839988776', '1988-11-02', 'Priya Naidoo - 0845556677',  NULL);
GO

-- 3 Events, run by the 2 organisers
INSERT INTO dbo.Events (OrganiserId, Name, Description, EventDate, Location) VALUES
(1, 'Johannesburg City Marathon',   'Annual road marathon through the Johannesburg CBD and northern suburbs.', '2026-11-08', 'Johannesburg, Gauteng'),
(1, 'Soweto Fun Run',               'Community fun run supporting local youth sports programmes.',            '2026-09-27', 'Soweto, Gauteng'),
(2, 'Cape Winelands Cycle Tour',    'Scenic cycling event through the Cape Winelands region.',                 '2026-10-18', 'Stellenbosch, Western Cape');
GO

-- Categories for each event (at least one per event, several here)
INSERT INTO dbo.Categories (EventId, Name, DistanceKm, Price) VALUES
(1, '42.2km Marathon', 42.20, 350.00),
(1, '21.1km Half Marathon', 21.10, 250.00),
(2, '5km Fun Run', 5.00, 100.00),
(2, '10km Run', 10.00, 150.00),
(3, '60km Cycle', 60.00, 400.00),
(3, '100km Cycle', 100.00, 550.00);
GO

-- Sample enrolments
INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status) VALUES
(3, 1, 'Confirmed'),  -- Thabo -> Marathon 42.2km
(4, 3, 'Confirmed'),  -- Amy   -> Soweto 5km
(5, 5, 'Confirmed'),  -- Jason -> Cape Winelands 60km
(3, 4, 'Confirmed'),  -- Thabo -> Soweto 10km
(4, 6, 'Pending');    -- Amy   -> Cape Winelands 100km
GO

-- Sample results (captured by the relevant organiser)
INSERT INTO dbo.Results (EnrolmentId, CapturedByUserId, FinishTime, Position) VALUES
(1, 1, '03:45:12', 152),
(2, 1, '00:28:40', 9),
(3, 2, '02:10:05', 34);
GO

PRINT 'RaceDay database schema created and seeded successfully.';
