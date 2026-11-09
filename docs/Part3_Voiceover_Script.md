# Part 3 — voice-over script

Generated from the same data as the edited video, so the timings below match
the CapCut timeline *RaceDay Part 3* exactly.

- Read one line, then breathe. A full stop ends a sentence; keep going.
- Replace **`[your name]`**, **`[student number]`** and **`[module]`**.
- Do not read the section headings or the notes in italics.
- Record one file per section, `S1` to `S9`, and drop each file at the
  section start time. That keeps the captions in step with your voice.
- If a line is too long for one breath, split it at a comma and keep the
  same words in the same order.

Total length of the edit: **12:11.340** (9 sections).

| Section | Starts | Length | Words | Pace |
| --- | --- | --- | --- | --- |
| S1 — Introduction | 00:00.000 | 00:40.900 | 118 | 173 wpm |
| S2 — Solution structure | 00:40.900 | 01:03.770 | 141 | 133 wpm |
| S3 — Consuming the API | 01:44.670 | 01:45.730 | 159 | 90 wpm |
| S4 — Sign in and role-based navigation | 03:30.400 | 00:34.370 | 99 | 173 wpm |
| S5 — The participant journey | 04:04.770 | 01:40.410 | 180 | 108 wpm |
| S6 — The organiser journey | 05:45.180 | 02:01.580 | 231 | 114 wpm |
| S7 — Azure Blob Storage | 07:46.760 | 01:48.210 | 165 | 91 wpm |
| S8 — Docker and continuous integration | 09:34.970 | 02:18.370 | 190 | 82 wpm |
| S9 — Close | 11:53.340 | 00:18.000 | 39 | 130 wpm |

---

## S1 — Introduction   `00:00.000 · 00:40.900 · 118 words · 173 wpm`

*Title card, then the public sign-in and registration pages, then the participant home page.*

Hello. My name is [your name], student number [student number], and this is Part 3 of my RaceDay portfolio of evidence for [module].  
Part 1 planned the system and designed the database. Part 2 built the REST API. Part 3, this video, is the MVC web application that consumes that API.  
Sign-in and registration are the only public pages. A new organiser account also needs the access code, RACEDAY-ORG-2026.  
From here, every request follows one path. The browser talks to the MVC application, the MVC application talks to the API, and only the API talks to SQL Server.  
The home page is the participant landing page. The upcoming events and their banners are all served by the API.  

## S2 — Solution structure   `00:40.900 · 01:03.770 · 141 words · 133 wpm`

*Solution Explorer: the three projects, then the MVC project file and a search for DbContext.*

Here is the solution in Visual Studio. It contains three projects.  
RaceDay.Api is the REST API from Part 2. RaceDay.Mvc is the web application in this video. RaceDay.Api.test holds the automated tests, eighty-five of them, and all of them pass.  
The MVC project follows the normal ASP.NET Core layout. Controllers handle requests. Views and ViewModels build the interface. Services contain the API client. wwwroot holds the static assets.  
The important detail is what the MVC project does not contain. Open the project file and there are no NuGet packages at all. No Entity Framework, no SQL client, no Azure SDK.  
If I search the whole MVC project for DbContext, there are zero results.  
The web application has no database connection string and no way to reach the database directly. That is deliberate. Every read and every write goes through the API.  

## S3 — Consuming the API   `01:44.670 · 01:45.730 · 159 words · 90 wpm`

*RaceDayApiClient: the methods, the captured API session cookie, and the 401 filter.*

All API calls live in one typed client, RaceDayApiClient. Each method maps to exactly one API endpoint.  
There is no business logic here. The client sends a request, receives JSON, and hands it back to the controller.  
The interesting part is how the API session is carried. The API issues its own session cookie, called RaceDay.Session. The browser never sees that cookie. It is kept server-side in the MVC session, separately for each signed-in user.  
These lines capture the Set-Cookie header after every response. These lines attach it to the next request and handle an unauthorised response.  
The HTTP handler is created with cookie use switched off, so it keeps no shared cookie jar. That guarantees one user API session can never leak into another user request.  
If the API returns 401, the client throws an ApiUnauthorizedException. A global exception filter catches it, signs the MVC user out, clears the stored API cookie, and redirects to the login page.  

## S4 — Sign in and role-based navigation   `03:30.400 · 00:34.370 · 99 words · 173 wpm`

*Participant sign-in, then organiser sign-in, showing the navigation change.*

The MVC application has its own cookie authentication, separate from the API session.  
Signing in posts the credentials to the API, and the app keeps a local cookie containing the user name and role.  
I will sign in as a participant.  
The navigation is rendered from that role: Home, Events, My enrolments, My results. There is no organiser menu.  
Now I will sign out and sign in as the organiser.  
The menu changes to Dashboard, My events and Browse events.  
Every organiser controller also carries an authorise attribute for the Organiser role, so the server enforces this as well.  

## S5 — The participant journey   `04:04.770 · 01:40.410 · 180 words · 108 wpm`

*Events, filtering, event detail, enrolling, the enrolments list, results and the profile picture upload.*

The Events page lists every event and supports search and filtering by event type.  
Opening an event shows the banner, the date, the location, the distance, and every category with its price.  
I will enter the Johannesburg Sunrise Walk, in the five kilometre walk category.  
That entry now appears on My enrolments, with colour-coded statuses. Confirmed is green, Pending is amber, and Cancelled is red.  
This participant already has all three, so the states are visible at once. The badge class is chosen in code from the status value, and the same component is reused on the organiser screen.  
My results is the page that proves the results feature. This participant finished the Pretoria Capital Marathon in two hours, thirty-six minutes and fifty seconds.  
The page reads forty-seventh of three hundred and twelve finishers, with the ordinal suffix generated in code, so it says 47th and not 47.  
Finally, the profile page. The personal details come from the API, and the picture is uploaded as a file to the API upload endpoint. The URL that comes back is displayed here.  

## S6 — The organiser journey   `05:45.180 · 02:01.580 · 231 words · 114 wpm`

*Dashboard, validation, creating an event, categories, enrolments, results and capturing a result.*

Signing in as the organiser opens the dashboard: five events, four upcoming, and four hundred and thirty-three enrolments. Every number comes from the API.  
I will create a new event. First I submit the form empty. Instead of an error page, the form returns with inline validation messages, and it keeps the values I already typed. That is the graceful validation requirement.  
Now I fill it in and attach a banner image. The event is created through the API, and the banner goes through the API dedicated banner endpoint. The returned URL is displayed on the card immediately.  
From My events I can manage everything for that event. Under Categories, I add a category with its distance and price, and the table updates.  
Under Enrolments, every participant is listed with their status. This view is read-only for the organiser, because entries change when a participant enrols or cancels.  
Under Results, the Pretoria Marathon already has three hundred and twelve captured results, each shown as a position out of the total finishers.  
For an event with no results yet, the page lists the confirmed participants who are still waiting. I capture one, with a finish time and a position. It is saved through the API and appears in the table immediately.  
Every write action, for events, categories, enrolments and results, goes through the API. The MVC application never writes to the database.  

## S7 — Azure Blob Storage   `07:46.760 · 01:48.210 · 165 words · 91 wpm`

*The IFileStorage abstraction, the Azure implementation, the configuration, then a banner uploaded through Azurite.*

Image uploads go through the API as well. Storage sits behind an interface called IFileStorage, with two implementations, and FileStorageRegistration chooses between them once at startup.  
When the provider is set to Azure, it registers AzureBlobFileStorage. Otherwise it registers the local provider that writes into wwwroot. The rest of the application only depends on the interface.  
AzureBlobFileStorage uses a blob container client. It creates the container if it does not exist, uploads the stream with a content type and a generated name, and returns the public URL. Deleting an image removes the blob.  
To demonstrate this honestly, without an Azure subscription, I am running the Azurite storage emulator, which implements the same Blob API. The provider is set to Azure and the connection string points at Azurite.  
Uploading a banner now writes a real blob, and the API serves that URL straight back to the MVC application. None of this storage code is in the MVC project. The web application only ever receives a URL.  

## S8 — Docker and continuous integration   `09:34.970 · 02:18.370 · 190 words · 82 wpm`

*Compose file, Dockerfiles, CI workflow, the running containers, Swagger and the green GitHub Actions run.*

The whole system runs in Docker. The API and the MVC app each have a multi-stage Dockerfile: restore and publish in the SDK image, then copy only the published output into the small runtime image.  
Docker Compose defines three services: SQL Server on 1433, the API on 8080, and the MVC application on 8081.  
The connection string, the organiser access code and the storage provider all come from environment variables in the Compose file, so the same images can run against a different database, or a real Azure account, without a rebuild.  
One command starts everything: docker compose up, with the build flag. The API applies its migrations at startup, and the MVC app reaches the API through the service name on the Compose network, so there are no hard-coded host addresses.  
Here are the three containers running, the MVC app answering on 8081, and Swagger documenting the API on 8080.  
The pipeline is in GitHub Actions. On every push it restores and builds the whole solution in Release, runs the eighty-five tests, publishes the MVC project, and uploads the publish output as an artifact.  
The latest run is green.  

## S9 — Close   `11:53.340 · 00:18.000 · 39 words · 130 wpm`

*End card.*

That is Part 3: an MVC application that consumes the Part 2 API, with session-based authentication, role-based navigation, complete participant and organiser journeys, Azure Blob Storage behind the API, and a Docker and CI pipeline.  
Thank you for watching.  
