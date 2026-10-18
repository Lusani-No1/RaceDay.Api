# RaceDay.Api


RaceDay is an event management system for the South African road running, walking and cycling community. Organisers create events and categories and capture results; Participants browse events, enter a category, and track their personal results.

This repository holds a three-part Portfolio of Evidence:

| Part | Content | Status |
|---|---|---|
| Part 1 | System planning and database: ERD, API endpoint plan, SQL script | Complete |
| Part 2 | RESTful API (ASP.NET Core, EF Core, cookie sessions, Swagger, unit tests, CI/CD) | In progress |
| Part 3 | MVC web application, Azure Blob Storage, Docker | Not started |

Roles

The system has two roles, enforced at the API level in Part 2 and reflected in the MVC app in Part 3.

- Organiser - creates, edits and deletes their own events; manages the categories of those events; views all enrolments for their events; and captures finish times and finishing positions for participants.
- Participant - registers for an account, browses events and categories, enters an event by choosing a category, views and cancels their own enrolments, and tracks their personal results.

Both roles can view and update their own profile.

Repository structure

/docs                        Part 1 planning documents
  RaceDay_ERD.png            Entity Relationship Diagram
  RaceDay_Endpoint_Plan.md   API endpoint plan
  RaceDay_Schema.sql         SQL Server creation and seed script
/Part2                       Part 2 - the RESTful API
  RaceDay.Api/               ASP.NET Core Web API project
  RaceDay.Api.Tests/         Unit tests
.github/workflows/           GitHub Actions workflows
README.md

Part 1 - System Planning and Database

Everything for Part 1 is in /docs:

- ERD -six entities (Users, UserProfiles, Events, Categories, Enrolments, Results) with primary keys, foreign keys and cardinality.
- Endpoint plan -every endpoint with method, route, description, role required, request body and expected responses.
- SQL script -creates the schema for SQL Server and seeds two Organisers, two Participants, three Events, categories, enrolments and results. Open it in SSMS and click Execute; it can be re-run on a clean instance.

Part 1 video: https://youtu.be/tNPN8o0Gkxo

Part 2 - RESTful API

Technology

- .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10, code-first, SQL Server
- Cookie-based session authentication with role claims
- ASP.NET Core PasswordHasher for password hashing
- Swagger (Swashbuckle) for live endpoint documentation and testing
- GitHub Actions for build and test on every push

Prerequisites

- Visual Studio 2026 with the ASP.NET and web development workload, or the .NET 10 SDK
- SQL Server (Express is fine) and SQL Server Management Studio

Setup and run

1. Clone the repository
  
   git clone <your-repository-url>
   
2. Set the connection string. Open Part2/RaceDay.Api/appsettings.json and change Server= to your own SQL Server instance, for example localhost\\SQLEXPRESS:
   json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=RaceDayApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
  
3. Check the Organiser access code. appsettings.json has an Auth:OrganiserAccessCode value. This code is required to register as an Organiser . The development value is RACEDAY-ORG-2026.

4. Create the database. In Visual Studio open Tools > NuGet Package Manager > Package Manager Console and run:
   
   Update-Database
   
   Or, from a terminal in Part2/RaceDay.Api:
   
   dotnet ef database update
   
   This creates RaceDayApiDb with all six tables from the migrations.
5. Run the API. Press F5 in Visual Studio (choose the **https** profile), or run dotnet run in Part2/RaceDay.Api.
6. Open Swagger. Browse to https://localhost:7089/swagger`. Every endpoint can be tried from this page.

Trying the API in Swagger

1. POST /api/auth/register - create a Participant ("role": "Participant") or an Organiser ("role": "Organiser"plus the organiserAccessCode`).
2. POST /api/auth/login - log in. The browser stores the session cookie, so later requests from the Swagger page are authenticated automatically.
3. Call any endpoint. A missing session returns 401; the wrong role or someone else's record returns 403.
4. POST /api/auth/logout ends the session.

Use one address (https://localhost:7089 or http://localhost:5062) for a whole session, because each address has its own cookie.

Endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Any logged-in user |
| GET | `/api/users/me` | Any logged-in user |
| PUT | `/api/users/me` | Any logged-in user |
| GET | `/api/events` | Any logged-in user |
| GET | `/api/events/{id}` | Any logged-in user |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| GET | `/api/events/{eventId}/categories` | Any logged-in user |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| PUT | `/api/categories/{id}` | Organiser (owner) |
| DELETE | `/api/categories/{id}` | Organiser (owner) |
| POST | `/api/categories/{categoryId}/enrolments` | Participant |
| GET | `/api/enrolments/me` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| DELETE | `/api/enrolments/{id}` | Participant (owner) |
| POST | `/api/enrolments/{enrolmentId}/results` | Organiser (owner) |
| PUT | `/api/results/{id}` | Organiser (owner) |
| GET | `/api/results/me` | Participant |
| GET | `/api/events/{eventId}/results` | Organiser (owner) |

"Owner" means the Organiser who created the event, or the Participant who made the enrolment. The full specification, with request bodies and response codes, is in docs/RaceDay_Endpoint_Plan.md.

Design decisions

- Sessions. Logging in issues an encrypted, HTTP-only cookie holding the user's id and role. The API answers 401 for no session and 403 for the wrong role, rather than redirecting to a login page.
- Passwords are salted and hashed with ASP.NET Core's PasswordHasher; the plain password is never stored. Login failures use one message for both "unknown email" and "wrong password".
- Organiser registration. Users choose their role when registering, but registering as an Organiser also needs a secret access code held in configuration. Without it, anyone could make themselves an Organiser.
- Ownership checks. Role alone is not enough: an Organiser can only change their own events, categories, enrolment views and results, and a Participant can only see or cancel their own enrolments.
- Cancelling an enrolment** sets its status to Cancelled instead of deleting it, so history is kept. Enrolling in the same category again re-activates the record.
- Protected deletes. An event or category that already has enrolments cannot be deleted (409), because enrolments are not cascade-deleted in the schema.
- Categories can be distance-based (10km), age-based (Under 20, Senior), or both.

Database: differences from the Part 1 script

The EF Core migrations create the same six tables, columns, keys and constraints as docs/RaceDay_Schema.sql. The deliberate differences are:

- The API uses a separate database, RaceDayApiDb, so it does not clash with the RaceDayDB created by the Part 1 script.
- Unique constraints are created by EF as unique indexes with EF's default names (IX_...) instead of the UQ_... names in the script.
- Enrolments.Status has no database-level default; the API sets it to Confirmed in code.

Unit tests

The tests are in Part2/RaceDay.Api.Tests and use xUnit. Each test starts the real API in memory and sends real HTTP requests (including the login cookie), with an in-memory database in place of SQL Server. They therefore need no database and run the same locally and on GitHub Actions.

Run them in Visual Studio with Test > Test Explorer > Run All Tests, or from a terminal in the Part2 folder:

dotnet test

The tests cover success and failure cases for:

- Registration and login- both roles, duplicate emails, invalid input, the Organiser access code, hashed passwords, wrong credentials, sessions and logout, and 401 for every protected endpoint without a login.
- Event management- Organisers creating, updating and deleting their own events and categories, invalid data, and missing records.
- Role enforcement- Participants rejected from Organiser actions, Organisers rejected from Participant actions, and one Organiser rejected from another Organiser's event.
- Event enrolments- Participants enrolling and the enrolment being recorded against the right event and category, duplicate enrolments, cancelling and re-enrolling, and enrolment privacy between users.
- Results- Organisers capturing and correcting results, Participants viewing only their own, and the rules around cancelled enrolments.

The in-memory database does not enforce unique indexes or check constraints, so these tests check the API's own validation. The database-level constraints are shown by the SQL script in Part 1.

CI/CD

A GitHub Actions workflow (.github/workflows/dotnet-ci.yml) restores, builds and runs all unit tests on every push.

**Green build screenshot:**

_[Insert a screenshot of the successful GitHub Actions run here]_

Part 2 video

https://youtu.be/tNPN8o0Gkxo


Part 3 - MVC Web Application


 AI use disclosure

I used Claude (an AI assistant) while working on this project. It helped me draft the endpoint plan and API code errors. I reviewed, ran and tested what it produced, and I explained how each part works.
