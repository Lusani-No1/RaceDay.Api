# RaceDay.Api


RaceDay is an event management system for the South African road running, walking and cycling community. Organisers create events and categories and capture results; Participants browse events, enter a category, and track their personal results.

This repository holds a three-part Portfolio of Evidence:

| Part | Content | Status |
|---|---|---|
| Part 1 | System planning and database: ERD, API endpoint plan, SQL script | Complete |
| Part 2 | RESTful API (ASP.NET Core, EF Core, cookie sessions, Swagger, unit tests, CI/CD) | Complete |
| Part 3 | MVC web application, Azure Blob Storage, Docker | Complete |

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
/                           Part 2 - the RESTful API (RaceDay.Api.csproj)
  Controllers/              API controllers
  Data/                     EF Core DbContext
  Dtos/                     Request and response contracts
  Migrations/               Code-first migrations
  Models/                   Entities
  Services/                 File storage (local + Azure Blob)
  Dockerfile                API container image
/RaceDay.Api.test           xUnit integration tests for the API
/RaceDay.Mvc                Part 3 - ASP.NET Core MVC front end
  Controllers/              MVC controllers (API-only data access)
  Models/                   API DTOs
  Services/                 RaceDayApiClient, session + auth plumbing
  ViewModels/               Page view models
  Views/                    Razor views
  wwwroot/                  Theme, Bootstrap, local images
  Dockerfile                MVC container image
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
   
2. Set the connection string. Open appsettings.json in the repository root and change Server= to your own SQL Server instance, for example localhost\\SQLEXPRESS:
   json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=RaceDayApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
   }
  
3. Check the Organiser access code. appsettings.json has an Auth:OrganiserAccessCode value. This code is required to register as an Organiser . The development value is RACEDAY-ORG-2026.

4. Create the database. In Visual Studio open Tools > NuGet Package Manager > Package Manager Console and run:
   
   Update-Database
   
   Or, from the repository root:
   
   dotnet ef database update
   
   This creates RaceDayApiDb with all six tables from the migrations.
5. Run the API. Press F5 in Visual Studio (choose the **https** profile), or run dotnet run in the repository root.
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
| POST | `/api/users/me/profile-picture` | Any logged-in user |
| GET | `/api/events` | Any logged-in user |
| GET | `/api/events/{id}` | Any logged-in user |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| POST | `/api/events/{id}/banner` | Organiser (owner) |
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

The tests are in RaceDay.Api.test and use xUnit. Each test starts the real API in memory and sends real HTTP requests (including the login cookie), with an in-memory database in place of SQL Server. They therefore need no database and run the same locally and on GitHub Actions.

Run them in Visual Studio with Test > Test Explorer > Run All Tests, or from the repository root:

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

The Part 3 front end is an ASP.NET Core MVC application that consumes the Part 2 API for every data operation. It has no direct database access and no duplicated business rules: authentication, ownership checks, enrolment rules and result rules all stay in the API.

Technology

- .NET 10, ASP.NET Core MVC with Razor views
- Cookie authentication for the MVC sign-in, plus server-side session storage for the API session cookie
- A single typed `RaceDayApiClient` for all API calls
- Bootstrap 5 with a custom RaceDay theme (orange, charcoal and sand palette)
- Azure Blob Storage in the API, with a local file-system fallback for development
- Docker and Docker Compose for the API, MVC app and SQL Server
- GitHub Actions building both projects, running the API tests and publishing the MVC output

Architecture

```text
Browser
  │  cookie auth + session
  ▼
RaceDay.Mvc  ──HTTP──▶  RaceDay.Api  ──EF Core──▶  SQL Server
  │                        │
  │                        └──▶ Azure Blob Storage (or wwwroot/uploads locally)
  └── Razor views, no database access
```

- When a user signs in, the MVC app calls `POST /api/auth/login`. The API returns its encrypted `RaceDay.Session` cookie; MVC stores that cookie server-side in session and attaches it to every later API request.
- MVC identity uses a separate cookie (`RaceDay.Mvc.Auth`) carrying the user id, name and role. Navigation and `[Authorize(Roles = ...)]` attributes are based on that role.
- If the API returns 401 (for example the API session expired), `ApiUnauthorizedFilter` signs the MVC user out and redirects to the login page.
- Organiser ownership is checked again by the API on every write. MVC also filters "my events" using the organiser id so users only see their own management pages.
- Uploaded images are sent to dedicated API endpoints (`POST /api/events/{id}/banner` and `POST /api/users/me/profile-picture`). The API validates type and size and returns a URL stored on the event or profile record.

Features

Participant

- Home page with upcoming events and search/type filters
- Full event list with search, event type and from-date filters
- Event details with categories, distances, age limits and entry fees
- Enrol in a category, view all personal enrolments and cancel an entry
- Colour-coded enrolment statuses: Confirmed (green), Pending (amber), Cancelled (red)
- My Results with finish time, ordinal position and total finishers, e.g. "47th out of 312 finishers"
- Profile view and edit, including profile picture upload

Organiser

- Dashboard with event, upcoming event and enrolment totals
- Create, edit and delete own events, including banner upload
- Manage categories per event (distance, age limits and price)
- View every enrolment for an event, with participant and status details
- Capture and correct results (finish time and position)
- Organiser-only navigation and pages; Participants cannot open organiser routes

Shared

- Registration and login with validation and graceful API error messages
- All pages require authentication except login, register and access denied
- Consistent RaceDay theme, responsive layout, empty states and confirmation prompts for destructive actions

Running Part 3 locally

Prerequisites: .NET 10 SDK and SQL Server (Express is fine).

1. Apply the database migrations from the repository root:

   ```bash
   dotnet tool restore
   dotnet ef database update --project RaceDay.Api.csproj --startup-project RaceDay.Api.csproj
   ```

2. Start the API (http://localhost:5062):

   ```bash
   dotnet run --project RaceDay.Api.csproj --launch-profile http
   ```

3. In a second terminal, start the MVC app (http://localhost:5080):

   ```bash
   dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj --launch-profile http
   ```

4. Open http://localhost:5080 and register. To create an Organiser, enter the organiser access code configured in `Auth:OrganiserAccessCode` (development value: `RACEDAY-ORG-2026`).

MVC configuration

`RaceDay.Mvc/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5062",
    "PublicBaseUrl": ""
  }
}
```

- `Api:BaseUrl` is the address MVC uses for server-to-server API calls.
- `Api:PublicBaseUrl` is optional. It is the address the browser uses for uploaded images. When empty it falls back to `Api:BaseUrl`. This matters in Docker, where MVC calls `http://api:8080` but the browser must load images from `http://localhost:8080`.

Azure Blob Storage

Azure Blob Storage is integrated in the API only. Local development uses the local provider by default, so no Azure account is required to run the project.

To use Azure:

1. Create a Storage account and a container (for example `raceday`). Enable public blob access for the container, or put a CDN/custom domain in front of it.
2. Set the API's storage configuration:

   ```json
   "Storage": {
     "Provider": "Azure",
     "Azure": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
       "ContainerName": "raceday",
       "PublicBaseUrl": ""
     }
   }
   ```

   The same values can be supplied as environment variables, which is how Docker Compose does it:

   ```text
   Storage__Provider=Azure
   Storage__Azure__ConnectionString=...
   Storage__Azure__ContainerName=raceday
   Storage__Azure__PublicBaseUrl=
   ```

3. Upload a banner as an Organiser or a profile picture as any user. The API stores the image in the container and saves its public URL.

With the default local provider, images are stored in `wwwroot/uploads` in the API project and served by `UseStaticFiles`. In Docker this folder is a named volume (`raceday-uploads`), so uploads survive container restarts.

Image rules: JPEG, PNG or WebP, maximum 5 MB. The API validates the content type and extension, rejects other files, and deletes the previous image when one is replaced.

Events without an uploaded banner fall back to one of the bundled RaceDay banner images in `wwwroot/images/event-banners`, served by the API.

Docker

Build and run the whole stack with one command from the repository root:

```bash
docker compose up --build
```

- MVC: http://localhost:8081
- API and Swagger: http://localhost:8080/swagger
- SQL Server: `localhost:1433`

Compose starts SQL Server, waits for it via the API's migrate-on-startup retry loop, then starts the API and MVC. Data is kept in the `raceday-sql` and `raceday-uploads` volumes. Override secrets with environment variables if needed:

```text
SA_PASSWORD
ORGANISER_ACCESS_CODE
STORAGE_PROVIDER=Azure
AZURE_STORAGE_CONNECTION_STRING
AZURE_STORAGE_CONTAINER
AZURE_STORAGE_PUBLIC_BASE_URL
```

Single container

The MVC Dockerfile can be built and run on its own against an API already running on the host:

```bash
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc .
```

```bash
docker run --rm -p 8081:8080 -e Api__BaseUrl=http://host.docker.internal:5062 -e Api__PublicBaseUrl=http://localhost:5062 raceday-mvc
```

On Linux, add `--add-host=host.docker.internal:host-gateway` to the `docker run` command so the container can reach the host API.

CI/CD

`.github/workflows/dotnet-ci.yml` now:

1. restores and builds the full solution (API, MVC and tests) in Release;
2. runs the API's xUnit integration tests;
3. publishes the MVC application and uploads it as a build artifact.

Screenshots

**Participant home page**

_[Insert screenshot]_

**Event details and enrolment**

_[Insert screenshot]_

**My results**

_[Insert screenshot]_

**Organiser dashboard**

_[Insert screenshot]_

**Category and result management**

_[Insert screenshot]_

**Profile with picture upload**

_[Insert screenshot]_

**Docker Compose running**

_[Insert screenshot]_

Part 3 video

_[Insert link]_


 AI use disclosure

I used Claude (an AI assistant) while working on this project. It helped me draft the endpoint plan and API code errors. I reviewed, ran and tested what it produced, and I explained how each part works.
