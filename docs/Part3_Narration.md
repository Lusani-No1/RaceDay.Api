# Part 3 narration script

Clean read-aloud text for the Part 3 walkthrough. Nothing here needs to be
memorised: read it at a relaxed pace, roughly **130 words per minute**.

- Target length: **10 minutes 45 seconds** (the plan allows 9 to 12 minutes).
- Words in this script: about 1,420.
- `[SCREEN]` lines tell you what should be visible while you say the paragraph
  below them. They are cues, not spoken text.
- Replace `[your name]`, `[student number]` and `[module]` before recording.

Record the narration **after** the screen clips are cut, or record it freely and
let the edit follow you. Both work; the second is faster.

---

## 1. Introduction — 0:00 to 0:45

`[SCREEN]` Home page of the MVC app, not signed in.

> Hello. My name is [your name], student number [student number], and this is
> Part 3 of my RaceDay portfolio of evidence for [module].
>
> Part 1 planned the system and designed the database. Part 2 built the REST
> API. Part 3, this video, is the MVC web application that consumes that API.
>
> Every request in this application follows the same path. The browser talks to
> the MVC application, the MVC application talks to the API, and only the API
> talks to SQL Server. Image uploads go through the API too, and the API is the
> only component that knows about Azure Blob Storage or the database.

## 2. Solution structure — 0:45 to 1:55

`[SCREEN]` Visual Studio, Solution Explorer expanded, `RaceDay.Api.slnx`.

> Here is the solution in Visual Studio. It contains three projects.
> RaceDay.Api is the REST API from Part 2. RaceDay.Mvc is the web application in
> this video. RaceDay.Api.test holds the automated tests — eighty-five of them,
> and all of them pass.
>
> The MVC project follows the normal ASP.NET Core layout. Controllers handle
> requests. Views and ViewModels build the interface. Services contain the API
> client. wwwroot holds the static assets.
>
> The important detail is what the MVC project does not contain. Open the
> project file and there are no NuGet packages at all. No Entity Framework, no
> SQL client, no Azure SDK. If I search the whole MVC project for DbContext,
> there are zero results. The web application has no database connection string
> and no way to reach the database directly. That is deliberate: every read and
> every write goes through the API.

## 3. Consuming the API — 1:55 to 3:20

`[SCREEN]` `RaceDay.Mvc/Services/RaceDayApiClient.cs`, lines 34 to 58, then 125
to 171.

> All API calls live in one typed client, RaceDayApiClient. Each method maps to
> exactly one API endpoint. Login posts to `/api/auth/login`. Events,
> categories, enrolments and results each call their matching endpoint. There is
> no business logic here: the client sends a request, receives JSON, and returns
> it to the controller.
>
> The interesting part is how the API session is carried. The API issues its own
> session cookie, called `RaceDay.Session`. The browser never sees that cookie.
> It is kept server-side in the MVC session, separately for each signed-in user.
> These lines capture the `Set-Cookie` header after every response. These lines
> attach it to the next request and handle an unauthorised response.
>
> The HTTP handler is also created with cookie use switched off, so it keeps no
> shared cookie jar. That guarantees one user's API session can never leak into
> another user's request.
>
> If the API returns 401 — for example when the session expires — the client
> throws an ApiUnauthorizedException. A global exception filter catches it,
> signs the MVC user out, clears the stored API cookie, and redirects to the
> login page with a return URL. That keeps the two authentication systems in
> step.

`[SCREEN]` `Program.cs` lines 32 to 67, then `ApiUnauthorizedFilter.cs`.

## 4. Sign in and role-based navigation — 3:20 to 4:30

`[SCREEN]` Login page, then the participant's navigation bar.

> The MVC application has its own cookie authentication, separate from the API
> session. Signing in posts the credentials to the API. If they are accepted,
> the app creates a local cookie containing the user's name and role.
>
> I'll sign in as a participant, demo.runner@raceday.test.
>
> Look at the navigation: Home, Events, My enrolments, My results, and the user's
> name. There is no organiser menu. The navigation is rendered from the signed-in
> user's role, so participants never see organiser links.
>
> Now I'll sign out and sign in as the organiser, demo.organiser@raceday.test.
>
> The menu changes to Dashboard, My events and Browse events. Hiding the links
> is not the only protection: every organiser controller carries an authorise
> attribute for the Organiser role, so the server enforces it as well.
>
> Registration works the same way, through the API. A new organiser account also
> needs the access code `RACEDAY-ORG-2026`.

## 5. The participant journey — 4:30 to 6:30

`[SCREEN]` Home page, Events list, event detail, enrol, My enrolments, My
results, Profile.

> The home page is public. It has a hero section and the upcoming events, each
> with a banner image that the API serves.
>
> The Events page lists every event and supports search and filtering by event
> type.
>
> Opening an event shows the banner, the date, the location, the distance, and
> every category with its price. I'll enter the Johannesburg Sunrise Walk, in
> the five kilometre walk category.
>
> The new entry now appears on My enrolments — and this screen shows the
> colour-coded statuses. Confirmed is green, Pending is amber, and Cancelled is
> red. This participant already has a confirmed entry, a pending entry and a
> cancelled entry, so all three states are visible at once. The badge class is
 chosen in code from the status value, and the same component
> is reused on the organiser's enrolment screen.
>
> My results is the page that proves the results feature. This participant
> finished the Pretoria Capital Marathon in two hours, thirty-six minutes and
> fifty seconds. The page reads **47th of 312 finishers**, with the ordinal
> suffix generated in code so that it says 47th and not 47.
>
> Finally, the profile page. The personal details come from the API, and the
> profile picture is uploaded as a file to the API's upload endpoint. The image
> is validated and stored, and the URL that comes back is displayed here.

## 6. The organiser journey — 6:30 to 8:50

`[SCREEN]` Organiser dashboard, create event, categories, enrolments, results.

> Signing in as the organiser opens the dashboard: five events, four upcoming,
> and four hundred and thirty-three enrolments. All of those numbers come from
> the API.
>
> I'll create a new event. First I'll submit the form empty. Instead of an error
> page, the form returns with inline validation messages and the values I already
> typed are kept. That is the graceful validation requirement.
>
> Now I'll fill it in and attach a banner image. The event is created through the
> API, and the banner goes through the API's dedicated banner endpoint. The
> returned URL is displayed on the card immediately.
>
> From My events I can manage everything for that event. Under Categories, I add
> a category with its distance and price, and the table updates.
>
> Under Enrolments, every participant is listed with their status. I'll approve a
> pending entry. The row changes from amber Pending to green Confirmed, and the
> change is saved through the API.
>
> Under Results, confirmed participants without a result are listed. I'll capture
> a result: a finish time and a finishing position. Once it is saved, the
> organiser's table shows the position out of the total finishers — for example,
> 47th of 312 — using exactly the same logic as the participant's page.
>
> Every write action — events, categories, enrolments and results — goes through
> the API. The MVC application never writes to the database.

## 7. Azure Blob Storage — 8:50 to 10:00

`[SCREEN]` `IFileStorage.cs`, `FileStorageRegistration.cs`, `AzureBlobFileStorage.cs`,
then the Azurite container with blobs, then the uploaded banner in the app.

> Image storage sits behind an interface called IFileStorage, with two
> implementations. FileStorageRegistration chooses between them once at startup.
> When `Storage:Provider` is set to Azure it registers AzureBlobFileStorage;
> otherwise it registers the local provider that writes into wwwroot. The rest of
> the application only depends on the interface.
>
> AzureBlobFileStorage uses a blob container client. It creates the container if
> it does not exist, uploads the stream with a content type and a generated name,
> and returns the public URL. Deleting an image removes the blob.
>
> To demonstrate this honestly, without an Azure subscription, I'm running the
> Azurite storage emulator, which implements the same Blob API. I've set the
> provider to Azure and pointed the connection string at Azurite. Uploading a
> banner now writes a real blob, and you can see it listed in the container. The
> API then serves that URL to the MVC application.
>
> Critically, none of this storage code is in the MVC project. The web
> application only ever receives a URL from the API.

## 8. Docker and continuous integration — 10:00 to 11:20

`[SCREEN]` Terminal with `docker compose ps`, then the app at `localhost:8081`,
Swagger at `localhost:8080`, then the green GitHub Actions run.

> The whole system runs in Docker. The API and the MVC app each have a
> multi-stage Dockerfile: build and publish in the SDK image, then copy the
> output into the small runtime image. Docker Compose defines three services:
> SQL Server, the API on port 8080, and the MVC application on port 8081.
>
> One command starts everything: `docker compose up --build`. The API applies its
> migrations at startup, and the MVC app reaches the API through the service name
> on the Compose network, so there are no hard-coded host addresses.
>
> Here are the three containers running, the MVC application loading from port
> 8081, and Swagger on port 8080.
>
> The pipeline is in GitHub Actions. On every push it restores and builds the
> whole solution in Release, runs the eighty-five tests, publishes the MVC
> project, and uploads the publish output as an artifact. The latest run is
> green.

## 9. Close — 11:20 to 11:40

`[SCREEN]` Back to the home page, or a title slide with your name and number.

> That's Part 3: an MVC application that consumes the Part 2 API, with
> session-based authentication, role-based navigation, complete participant and
> organiser journeys, Azure Blob Storage behind the API, and a Docker and CI
> pipeline. Thank you for watching.

---

## Delivery notes

- Pause for one full second between sections. It gives clean cut points and
  hides any small reading slips.
- Say "API" as three letters, not "appy".
- Say the numbers slowly: *eighty-five tests*, *four hundred and thirty-three
  enrolments*, *47th of 312 finishers*.
- If you misread a sentence, stop, breathe, repeat the whole sentence. Do not
  stop and restart the recording; the edit will use the good take.
- Do not read the `[SCREEN]` lines out loud.
- The two facts worth stressing, because they carry the most marks:
  1. the MVC project has **no database access of any kind**, and
  2. Azure Blob Storage is reached **only** by the API.
