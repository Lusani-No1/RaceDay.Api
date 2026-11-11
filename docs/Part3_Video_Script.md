# Part 3 video: full script and editing guide

This is a record-and-edit script for the unlisted Part 3 walkthrough. It tells you
what to show, what to say, and how to cut it together.

Target length: 9 to 11 minutes. Speaking pace: about 130 words per minute.

---

## Before you record

- [ ] Start SQL Server, then the API and the MVC app:

  ```bash
  dotnet run --project RaceDay.Api.csproj --launch-profile http
  dotnet run --project RaceDay.Mvc/RaceDay.Mvc.csproj --launch-profile http
  ```

- [ ] Confirm http://localhost:5062/swagger and http://localhost:5080 both load.
- [ ] Have these accounts ready (password `Password123!`):
  - `demo.runner@raceday.test` - participant, 47th of 312 finishers
  - `demo.organiser@raceday.test` - organiser
- [ ] Have the organiser access code ready: `RACEDAY-ORG-2026`.
- [ ] Have a test image on the Desktop for the banner and profile uploads.
- [ ] Close personal tabs, notifications and chat apps.
- [ ] Set the browser zoom to 110% so text is readable on video.
- [ ] Record at 1920x1080, 30 fps, with a microphone close to you.
- [ ] Record each section as a separate clip. Do not try to do it in one take.

Recording tools: Windows **Snipping Tool** (record screen), **OBS Studio** (free,
better audio control), or **Clipchamp** (built into Windows 11) for both recording
and editing.

---

## Scene 1 - Title (0:00 to 0:10)

**On screen:** A title card or the RaceDay home page.

**Say:**

> Hi, I'm Lusani Mudau, student number ST10474461. This is Part 3 of my RaceDay
> Portfolio of Evidence: the MVC web application that consumes my Part 2 API.
>
> Module: PROG6222.

**Edit:** Add a text overlay with your name, student number and module code.

---

## Scene 2 - What Part 3 is (0:10 to 0:50)

**On screen:** The repository folder tree or the solution open in Visual Studio.

**Say:**

> RaceDay is an event management system for the South African running, walking and
> cycling community. Organisers create events and categories and capture results.
> Participants browse events, enter a category and track their personal results.
>
> Part 1 was the planning and database. Part 2 was the REST API. Part 3 is the MVC
> web application. The important rule is that this MVC app talks only to the
> Part 2 API. It has no database connection and no duplicated business rules.

**Edit:** Zoom in on the folder names as you say them. Cut out any pauses.

---

## Scene 3 - Architecture (0:50 to 2:00)

**On screen:** The architecture diagram in the README, or `RaceDay.Mvc/Services/RaceDayApiClient.cs`.

**Say:**

> The browser talks to the MVC app using a cookie. The MVC app talks to the API
> over HTTP. The API talks to SQL Server with Entity Framework Core, and to Azure
> Blob Storage for uploaded images. The MVC app never talks to SQL Server or Azure
> directly.
>
> When a user signs in, MVC calls the API login endpoint. The API returns its
> encrypted RaceDay dot Session cookie. MVC stores that cookie on the server and
> attaches it to every later API call. MVC also has its own cookie for the signed-in
> user, which carries their name and role. That is what controls the navigation and
> the Authorize attributes.
>
> This file, RaceDayApiClient, is the only place that calls the API. Every
> controller depends on it. If the API returns 401 because the session expired,
> an action filter signs the user out and sends them to the login page instead of
> showing a crash.

**Edit:** Slow-pan or zoom on the class name and the method names. If the file is
long, jump-cut between the login method, the events method and the upload method.

---

## Scene 4 - Azure Blob Storage (2:00 to 3:00)

**On screen:** `Services/IFileStorage.cs`, `Services/AzureBlobFileStorage.cs`, `Services/FileStorageRegistration.cs`, then `appsettings.json`.

**Say:**

> Uploads are abstracted behind IFileStorage. There are two implementations. The
> local one writes into w w w root slash uploads, which is the default for
> development. The Azure one writes into a blob container using the Azure Storage
> SDK.
>
> Program dot c s registers one of them based on the Storage Provider setting. If
> it is Azure, the Azure implementation is registered. Otherwise the local one is.
> The same upload endpoints work with either provider.
>
> The connection string and container name come from configuration. In production
> they are supplied as environment variables, so no secret is committed to the
> repository.
>
> Here is the proof: I have set the provider to Azure, uploaded a banner, and the
> URL that came back is a blob URL pointing at the container.

**On screen for the proof:** show the returned `bannerUrl` in Swagger or in the
browser, and the blob in Azure Storage Explorer or at the blob URL.

**Say:**

> Notice the MVC app did not upload anything to Azure. It posted the file to the
> API banner endpoint, and the API did the upload. That keeps Azure behind the API.

**Edit:** When the blob URL appears, freeze for two seconds and add a yellow
callout box around `blob.core.windows.net` (or the Azurite address if you demo with
the emulator). Mention in a caption whether this is a real Azure account or the
Azurite emulator.

---

## Scene 5 - Participant journey (3:00 to 5:30)

**On screen:** MVC at http://localhost:5080.

**Say:**

> I am signed in as Sipho Dlamini, a participant. The home page shows upcoming
> events soonest first, with a banner for each event. I can search and filter by
> event type.

**Action:** Type "Cape" in the search box and submit.

**Say:**

> The events page has search, type and from-date filters.

**Action:** Open Cape Town Summer Half Marathon.

**Say:**

> The event details show the description, date, location, distance and every
> category with its distance, age limits and entry fee.

**Action:** Choose a category and click Enter event.

**Say:**

> Enrolling posts to the API, and the API checks that I am a participant, that the
> category exists and that I have not already entered. Those rules are not copied
> in MVC.

**Action:** Open My enrolments.

**Say:**

> My enrolments shows every entry with a colour-coded status: green for confirmed,
> amber for pending and red for cancelled.

**Action:** Cancel one entry, then confirm the dialog.

**Say:**

> Cancelling sets the status to cancelled instead of deleting the row, so the
> history is kept.

**Action:** Open My results.

**Say:**

> My results show the finish time, the position and the total number of finishers.
> This is the required format: 47th out of 312 finishers, with the time two hours,
> thirty-six minutes and fifty seconds.

**Edit:** This is the money shot. Zoom into the "47th out of 312 finishers" line,
hold for three seconds, and add a callout. This is an explicit requirement, so
make it easy to see.

**Action:** Open Profile, upload the test image.

**Say:**

> The profile page lets me update my details and upload a profile picture, which
> again goes through the API upload endpoint.

---

## Scene 6 - Authentication and roles (5:30 to 6:15)

**Action:** Sign out, then type http://localhost:5080/Organiser directly.

**Say:**

> If I try to open an organiser page as a participant, MVC blocks me with an access
> denied page. The API also enforces this: 401 without a session and 403 for the
> wrong role or someone else's record, so the rule is enforced in two places even
> though the logic lives in the API.

**Action:** Sign in as `demo.organiser@raceday.test`.

**Say:**

> As the organiser, the navigation changes and the organiser menu appears.

---

## Scene 7 - Organiser journey (6:15 to 8:30)

**On screen:** Organiser dashboard.

**Say:**

> The dashboard summarises my events, upcoming events and enrolments.

**Action:** Create an event with a banner, then open its categories.

**Say:**

> I can create an event with a banner. The banner upload goes to the API, which
> validates the file type and the five megabyte limit and stores it.

**Action:** Add a category.

**Say:**

> Each event has categories with a distance, optional age limits and a price.

**Action:** Open the event's enrolments.

**Say:**

> Here I can see every enrolment for my event with the participant and the status.

**Action:** Capture a result, then correct the position.

**Say:**

> I can capture a finish time and position, and correct it later if there is a
> mistake.

**Action:** Sign in as a second organiser and try to open the first organiser's
event, or just explain it.

**Say:**

> A different organiser cannot see or change this event. The API checks ownership
> on every write, not just the role.

**Edit:** Speed up any waiting with a 2x to 4x speed ramp. Add short captions such
as "Create event", "Add category", "Capture result".

---

## Scene 8 - Validation (8:30 to 9:15)

**Action:** Register with a bad email, a one-character password and no role.

**Say:**

> The forms validate on the client and on the server. The API errors come back as
> friendly messages instead of a crash.

**Action:** Try to register with an email that already exists.

**Say:**

> For example, a duplicate email shows a clear message from the API.

**Edit:** Zoom on the error messages.

---

## Scene 9 - Docker (9:15 to 10:15)

**On screen:** `Dockerfile`, `RaceDay.Mvc/Dockerfile`, `docker-compose.yml`, then a terminal.

**Say:**

> Both projects have Dockerfiles, and docker compose starts SQL Server, the API and
> the MVC app together. The API waits for SQL Server and applies migrations on
> startup, so one command starts the whole stack.

**Action:** Run the command, let it build, then open both URLs.

```bash
docker compose up --build
```

**Say:**

> MVC is on 8081 and Swagger is on 8080.

**Edit:** Speed up the build with a 4x ramp, or cut to the "running" state. Add a
caption: "one command: docker compose up --build".

---

## Scene 10 - CI/CD and close (10:15 to 11:00)

**On screen:** The green GitHub Actions run.

**Say:**

> On every push, GitHub Actions restores, builds and tests the API, builds the MVC
> app and publishes it as an artifact. This is the green run for the latest commit.

**Action:** Show the test count and the artifact.

**Say:**

> To summarise: the MVC app consumes only the API, Azure Blob Storage sits behind
> the API, the stack runs in Docker with one command, and CI builds both projects.
> The repository link is in the description. Thank you for watching.

**Edit:** End on a title card with your name, student number and the repository URL.

---

## Editing workflow

1. **Import** all clips into Clipchamp (or DaVinci Resolve / CapCut).
2. **Order** them by scene number and delete false starts, dead air and loading.
3. **Trim silence** at the start and end of every clip. Keep gaps short.
4. **Speed ramps:** use 2x to 4x on builds, page loads and Docker startup. Never
   speed up a sentence you are speaking.
5. **Zooms (2 to 3 seconds each):**
   - the "47th out of 312 finishers" line
   - the returned Azure blob URL
   - the access denied page
   - the validation messages
   - the green GitHub Actions check
6. **Callouts:** yellow box or arrow on the items above. Keep them on screen for
   at least two seconds.
7. **Captions:** add short lower-third labels for each scene, for example
   "Participant journey", "Azure Blob Storage", "Docker Compose".
8. **Chapters:** add the scene titles as YouTube chapters (start each at 0:00 and
   use the timestamps below).
9. **Audio:** normalise the voice to about -14 LUFS, then add background music at
   about -30 dB so it never competes with your voice. Use copyright-free music.
10. **Export:** 1080p, 30 fps, MP4, H.264. Watch the exported file once from start
    to finish before uploading.

### Suggested chapter timestamps

```text
0:00 Introduction
0:50 Architecture
2:00 Azure Blob Storage
3:00 Participant journey
5:30 Authentication and roles
6:15 Organiser journey
8:30 Validation
9:15 Docker
10:15 CI/CD and summary
```

## Uploading to YouTube

- Visibility: **Unlisted**.
- Title: `RaceDay Part 3 - MVC Web Application (Portfolio of Evidence)`.
- Description: one paragraph on what the app does, the technology list, the
  repository URL, and the demo credentials (do not include real secrets).
- Add the chapters above.
- After uploading, copy the link and replace `_[Insert link]_` in the Part 3
  section of `README.md`.

## If something fails on the day

- **Docker is not installed:** say so, show the Dockerfiles and the compose file,
  and explain the single command. Do not fake the output.
- **Azure is not available:** use the Azurite emulator, and say clearly that it is
  the official Azure Storage emulator running the same SDK code path.
- **The API will not start:** check that SQL Server is running and that the
  connection string in `appsettings.json` points at the right instance.
- **A page is slow:** keep recording, then cut or speed it up in the edit.
