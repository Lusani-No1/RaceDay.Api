/*
    RaceDay demo data
    -----------------
    Replaces the automated test records in RaceDayApiDb with a realistic demo
    data set. The existing real account and the "Running" event (EventId 8)
    are preserved.

    Demo sign-ins (all use the password Password123!):
      Organiser   demo.organiser@raceday.test
      Participant demo.runner@raceday.test
      Participant demo.walker@raceday.test

    The Pretoria Capital Marathon is a completed event with 312 finishers.
    Sipho Dlamini (demo.runner) finishes 47th, which demonstrates the
    "47th out of 312 finishers" result format.
*/

USE RaceDayApiDb;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Password123!, hashed by ASP.NET Core Identity PasswordHasher<User>.
DECLARE @hash nvarchar(255) = N'AQAAAAIAAYagAAAAEOCNrUFRbXBdb9Jc2miHdVYvnCKUp97VvEMUL/fL7fCJC1OSaGr4hkBMJh+nIfEVXg==';

-- Remove the automated test records but keep EventId 8 and its owner (UserId 17).
DELETE r
FROM Results r
JOIN Enrolments e ON e.EnrolmentId = r.EnrolmentId
JOIN Categories c ON c.CategoryId = e.CategoryId
WHERE c.EventId <> 8;

DELETE e
FROM Enrolments e
JOIN Categories c ON c.CategoryId = e.CategoryId
WHERE c.EventId <> 8;

DELETE c FROM Categories c WHERE c.EventId <> 8;
DELETE ev FROM Events ev WHERE ev.EventId <> 8;
DELETE up FROM UserProfiles up WHERE up.UserId <> 17;
DELETE u FROM Users u WHERE u.UserId <> 17;

-- Demo accounts.
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt) VALUES
    (N'Thandi Mokoena', N'demo.organiser@raceday.test', @hash, N'Organiser',   SYSDATETIME()),
    (N'Sipho Dlamini',  N'demo.runner@raceday.test',    @hash, N'Participant', SYSDATETIME()),
    (N'Naledi Khumalo', N'demo.walker@raceday.test',    @hash, N'Participant', SYSDATETIME());

DECLARE @org int    = (SELECT UserId FROM Users WHERE Email = N'demo.organiser@raceday.test');
DECLARE @runner int = (SELECT UserId FROM Users WHERE Email = N'demo.runner@raceday.test');
DECLARE @walker int = (SELECT UserId FROM Users WHERE Email = N'demo.walker@raceday.test');

INSERT INTO UserProfiles (UserId, PhoneNumber, DateOfBirth, EmergencyContact, ProfileImageUrl) VALUES
    (@org,    N'082 555 0100', '1988-04-12', N'Lerato Mokoena 083 555 0199', NULL),
    (@runner, N'083 555 0142', '1994-09-03', N'Nomsa Dlamini 084 555 0177',  NULL),
    (@walker, N'084 555 0123', '1990-01-25', N'Thabo Khumalo 082 555 0166',  NULL);

-- 310 additional participants so the completed event has 312 finishers.
;WITH n AS (
    SELECT TOP (310) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
SELECT
    CHOOSE((i % 20) + 1,
        N'Thabo', N'Lerato', N'Sipho', N'Naledi', N'Kagiso', N'Zanele', N'Ayanda',
        N'Bongani', N'Nomvula', N'Tshepo', N'Palesa', N'Sibusiso', N'Karabo',
        N'Refilwe', N'Mpho', N'Lindiwe', N'Andile', N'Zinhle', N'Tumelo', N'Nomsa')
    + N' ' +
    CHOOSE(((i * 7) % 20) + 1,
        N'Mokoena', N'Dlamini', N'Nkosi', N'Botha', N'van Wyk', N'Naidoo',
        N'Pillay', N'Mthembu', N'Khumalo', N'Sithole', N'Jacobs', N'Adams',
        N'Mahlangu', N'Ndlovu', N'Mabaso', N'Raman', N'Petersen', N'Williams',
        N'Cele', N'Zulu'),
    N'synthetic' + RIGHT(N'000' + CAST(i AS nvarchar(3)), 3) + N'@raceday.test',
    @hash, N'Participant', SYSDATETIME()
FROM n;

-- Upcoming and completed events.
INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType, CreatedAt, BannerUrl) VALUES
    (@org, N'Pretoria Capital Marathon',
        N'Fast, flat city marathon through the capital. Start and finish at the Union Buildings.',
        '2026-10-18', N'Pretoria', 42.2, N'Run', SYSDATETIME(), N'/images/event-banners/event-banner-2.jpg'),
    (@org, N'Cape Town Summer Half Marathon',
        N'Coastal half marathon along the Sea Point Promenade with a 10 km and a 5 km fun run.',
        '2026-11-28', N'Cape Town', 21.1, N'Run', SYSDATETIME(), N'/images/event-banners/event-banner-1.jpg'),
    (@org, N'Winelands Trail Challenge',
        N'Scenic vineyard trails in Stellenbosch with 15 km and 8 km options.',
        '2026-12-12', N'Stellenbosch', 15.0, N'Run', SYSDATETIME(), N'/images/event-banners/event-banner-3.jpg'),
    (@org, N'Durban Beachfront Cycle Tour',
        N'Flat coastal cycle tour along the Golden Mile, open to road and mountain bikes.',
        '2026-12-20', N'Durban', 40.0, N'Cycle', SYSDATETIME(), N'/images/event-banners/event-banner-1.jpg'),
    (@org, N'Johannesburg Sunrise Walk',
        N'Early-morning 5 km walk through Emmarentia Dam and the botanical gardens.',
        '2027-01-10', N'Johannesburg', 5.0, N'Walk', SYSDATETIME(), N'/images/event-banners/event-banner-3.jpg');

DECLARE @pretoria  int = (SELECT EventId FROM Events WHERE Name = N'Pretoria Capital Marathon');
DECLARE @capetown  int = (SELECT EventId FROM Events WHERE Name = N'Cape Town Summer Half Marathon');
DECLARE @winelands int = (SELECT EventId FROM Events WHERE Name = N'Winelands Trail Challenge');
DECLARE @durban    int = (SELECT EventId FROM Events WHERE Name = N'Durban Beachfront Cycle Tour');
DECLARE @joburg    int = (SELECT EventId FROM Events WHERE Name = N'Johannesburg Sunrise Walk');

INSERT INTO Categories (EventId, Name, DistanceKm, MinAge, MaxAge, Price) VALUES
    (@pretoria, N'Marathon',       42.195, 18, 80, 550),
    (@pretoria, N'Half Marathon',  21.100, 16, 80, 350),
    (@capetown, N'Half Marathon',  21.100, 16, 80, 450),
    (@capetown, N'10km',           10.000, 14, 80, 250),
    (@capetown, N'5km Fun Run',     5.000,  8, 90, 120),
    (@winelands, N'15km Trail',    15.000, 16, 75, 350),
    (@winelands, N'8km Trail',      8.000, 12, 75, 200),
    (@durban, N'40km Road',        40.000, 16, 80, 300),
    (@durban, N'20km Road',        20.000, 14, 80, 180),
    (@joburg, N'5km Walk',          5.000,  8, 90, 100);

DECLARE @pMarathon int = (SELECT CategoryId FROM Categories WHERE EventId = @pretoria AND Name = N'Marathon');
DECLARE @pHalf     int = (SELECT CategoryId FROM Categories WHERE EventId = @pretoria AND Name = N'Half Marathon');
DECLARE @ctHalf    int = (SELECT CategoryId FROM Categories WHERE EventId = @capetown AND Name = N'Half Marathon');
DECLARE @ct10      int = (SELECT CategoryId FROM Categories WHERE EventId = @capetown AND Name = N'10km');
DECLARE @ct5       int = (SELECT CategoryId FROM Categories WHERE EventId = @capetown AND Name = N'5km Fun Run');
DECLARE @w15       int = (SELECT CategoryId FROM Categories WHERE EventId = @winelands AND Name = N'15km Trail');
DECLARE @w8        int = (SELECT CategoryId FROM Categories WHERE EventId = @winelands AND Name = N'8km Trail');
DECLARE @d40       int = (SELECT CategoryId FROM Categories WHERE EventId = @durban AND Name = N'40km Road');
DECLARE @d20       int = (SELECT CategoryId FROM Categories WHERE EventId = @durban AND Name = N'20km Road');
DECLARE @j5        int = (SELECT CategoryId FROM Categories WHERE EventId = @joburg AND Name = N'5km Walk');

-- Completed event: all 312 participants, split across the marathon and half marathon.
;WITH participants AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users
    WHERE Role = N'Participant' AND Email LIKE N'%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId,
       CASE WHEN rn <= 200 THEN @pMarathon ELSE @pHalf END,
       DATEADD(DAY, -30, SYSDATETIME()),
       N'Confirmed'
FROM participants;

INSERT INTO Results (EnrolmentId, CapturedByUserId, FinishTime, Position)
SELECT e.EnrolmentId,
       @org,
       CAST(DATEADD(SECOND, (ROW_NUMBER() OVER (ORDER BY u.UserId) - 1) * 35, CAST('02:10:00' AS time)) AS time),
       ROW_NUMBER() OVER (ORDER BY u.UserId)
FROM Enrolments e
JOIN Users u ON u.UserId = e.ParticipantId
JOIN Categories c ON c.CategoryId = e.CategoryId
WHERE c.EventId = @pretoria;

-- Put Sipho Dlamini 47th, and keep every other position unique.
;WITH others AS (
    SELECT r.ResultId, ROW_NUMBER() OVER (ORDER BY u.UserId) AS rn
    FROM Results r
    JOIN Enrolments e ON e.EnrolmentId = r.EnrolmentId
    JOIN Users u ON u.UserId = e.ParticipantId
    JOIN Categories c ON c.CategoryId = e.CategoryId
    WHERE c.EventId = @pretoria AND u.UserId <> @runner
)
UPDATE r
SET Position = CASE
    WHEN u.UserId = @runner THEN 47
    ELSE o.rn + CASE WHEN o.rn >= 47 THEN 1 ELSE 0 END
END
FROM Results r
JOIN Enrolments e ON e.EnrolmentId = r.EnrolmentId
JOIN Users u ON u.UserId = e.ParticipantId
JOIN Categories c ON c.CategoryId = e.CategoryId
LEFT JOIN others o ON o.ResultId = r.ResultId
WHERE c.EventId = @pretoria;

UPDATE r
SET FinishTime = CAST(DATEADD(SECOND, (r.Position - 1) * 35, CAST('02:10:00' AS time)) AS time)
FROM Results r
JOIN Enrolments e ON e.EnrolmentId = r.EnrolmentId
JOIN Categories c ON c.CategoryId = e.CategoryId
WHERE c.EventId = @pretoria;

-- Upcoming events, with a mix of Confirmed, Pending and Cancelled entries.
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status) VALUES
    (@runner, @ct10, DATEADD(DAY, -12, SYSDATETIME()), N'Confirmed'),
    (@walker, @ct5,  DATEADD(DAY, -10, SYSDATETIME()), N'Pending'),
    (@runner, @w15,  DATEADD(DAY,  -8, SYSDATETIME()), N'Pending'),
    (@walker, @w8,   DATEADD(DAY,  -7, SYSDATETIME()), N'Confirmed'),
    (@runner, @d40,  DATEADD(DAY,  -6, SYSDATETIME()), N'Cancelled'),
    (@walker, @j5,   DATEADD(DAY,  -3, SYSDATETIME()), N'Confirmed');

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @ctHalf, DATEADD(DAY, -20, SYSDATETIME()), N'Confirmed' FROM s WHERE rn BETWEEN 1 AND 25;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @ct10, DATEADD(DAY, -18, SYSDATETIME()), N'Confirmed' FROM s WHERE rn BETWEEN 26 AND 40;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @ct5, DATEADD(DAY, -16, SYSDATETIME()), N'Pending' FROM s WHERE rn BETWEEN 41 AND 50;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @w15, DATEADD(DAY, -14, SYSDATETIME()), N'Confirmed' FROM s WHERE rn BETWEEN 1 AND 20;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @w8, DATEADD(DAY, -13, SYSDATETIME()), N'Confirmed' FROM s WHERE rn BETWEEN 21 AND 30;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @d40, DATEADD(DAY, -9, SYSDATETIME()), N'Confirmed' FROM s WHERE rn BETWEEN 1 AND 15;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @d20, DATEADD(DAY, -8, SYSDATETIME()), N'Pending' FROM s WHERE rn BETWEEN 16 AND 25;

;WITH s AS (
    SELECT UserId, ROW_NUMBER() OVER (ORDER BY UserId) AS rn
    FROM Users WHERE Email LIKE N'synthetic%@raceday.test'
)
INSERT INTO Enrolments (ParticipantId, CategoryId, EnrolmentDate, Status)
SELECT UserId, @j5, DATEADD(DAY, -4, SYSDATETIME()), N'Pending' FROM s WHERE rn BETWEEN 1 AND 10;

COMMIT TRANSACTION;

PRINT N'Demo data seeded.';
PRINT N'Organiser:   demo.organiser@raceday.test / Password123!';
PRINT N'Participant: demo.runner@raceday.test / Password123! (47th of 312)';
PRINT N'Participant: demo.walker@raceday.test / Password123!';
