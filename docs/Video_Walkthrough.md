# Part 3 video walkthrough script

A suggested running order for the unlisted Part 3 video. Each section maps to an
assessment requirement, so the marker can follow along. Record in one take if
possible; total target length is 8 to 12 minutes.

Before recording:

```bash
# terminal 1 - API
dotnet run --project RaceDay.Api.csproj --launch-profile http

# terminal 2 - MVC
dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj --launch-profile http
```

Sign in as `demo.runner@raceday.test` / `Password123!` for the participant
sections and `demo.organiser@raceday.test` / `Password123!` for the organiser
sections. The organiser access code for new registrations is `RACEDAY-ORG-2026`.

## 1. Introduction (30 seconds)

- Name, student number, module.
- State that this is Part 3: the MVC web application that consumes the Part 2 API.
- State the architecture: browser -> RaceDay.Mvc -> RaceDay.Api -> SQL Server, with
  Azure Blob Storage behind the API only.

## 2. Solution structure (45 seconds)

- Open the solution in Visual Studio (or show the folder tree).
- Point out `RaceDay.Mvc` has no `DbContext`, no connection string and no direct
  database access.
- Open `RaceDay.Mvc/Services/RaceDayApiClient.cs` and show that every data call is
  an HTTP call to the API.
- Open `RaceDay.Mvc/Program.cs` and show the cookie auth, session store and the
  typed `HttpClient` registration.

## 3. Azure Blob Storage (60 to 90 seconds)

- Open `Services/AzureBlobFileStorage.cs` and `Services/IFileStorage.cs`.
- Explain the provider switch: `Storage:Provider` is `Local` by default and `Azure`
  in production, so the same upload endpoints work in both.
- Show the configuration in `appsettings.json` (`Storage:Azure:ConnectionString`,
  `ContainerName`).
- If Azure is configured, upload an event banner and show the saved URL points at
  `https://<account>.blob.core.windows.net/...`.
- Emphasise that the MVC app never talks to Azure directly; it posts the file to
  `POST /api/events/{id}/banner` and the API does the upload.

## 4. Participant journey (3 minutes)

1. Home page: banners, search and event-type filter.
2. Events list: search, type and from-date filters.
3. Event details: categories, distances, age limits, entry fees.
4. Enrol in a category, then open **My enrolments**.
   - Show the colour-coded statuses: Confirmed (green), Pending (amber),
     Cancelled (red).
   - Cancel one enrolment and show the status change.
5. Open **My results** and point at *"47th out of 312 finishers"* and the finish
   time.
6. Open **Profile** and upload a profile picture.

## 5. Authentication and roles (1 minute)

- Sign out, then try to open `/Organiser` directly as a participant. Show
  **Access denied**.
- Sign in as the organiser and show the organiser-only navigation appears.
- Mention that the API also enforces roles: 401 without a session, 403 for the
  wrong role or another organiser's event.

## 6. Organiser journey (3 minutes)

1. Dashboard: totals for events, upcoming events and enrolments.
2. Create an event, including a banner upload.
3. Add a category to the event (distance, age limits, price).
4. Open the event's enrolments and show participant and status detail.
5. Capture a result (finish time and position), then correct it.
6. Show that a different organiser cannot see or edit this event.

## 7. Validation and error handling (45 seconds)

- Submit the registration form with a bad email, a short password and no role.
- Show the inline validation messages.
- Submit a duplicate email and show the API error coming back as a friendly
  banner message rather than a crash.

## 8. Docker (1 minute)

- Show the `Dockerfile` for the API and the one for MVC, plus `docker-compose.yml`.
- Run:

  ```bash
  docker compose up --build
  ```

- Open http://localhost:8081 for MVC and http://localhost:8080/swagger for the API.
- Point out the single command in the README.

## 9. CI/CD (30 seconds)

- Open the GitHub Actions run and show the green build for the API, MVC and tests.
- Show the published MVC artifact.

## 10. Wrap-up (20 seconds)

- Summarise: MVC consumes only the API, Azure Blob Storage sits behind the API,
  the stack runs in Docker, and CI builds both projects.
- Read out the repository URL.
