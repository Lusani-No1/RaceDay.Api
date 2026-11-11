# Part 3 recording plan

Production plan for the unlisted Part 3 walkthrough. The narration is in
`docs/Part3_Narration.md`; this document is the camera and edit sheet.

---

## 1. How the work is split

| Job | Owner | Notes |
| --- | --- | --- |
| Start and stop OBS, one file per clip | Assistant | Through the OBS WebSocket API, after you say go |
| Drive the browser demo | Assistant | Every click and form fill is scripted in section 5 |
| Rename and file the clips | Assistant | `C01_...mp4` naming, bad takes moved to `discard` |
| Scroll and point in Visual Studio | **You** | Editor scrolling by hand is more reliable than synthetic input |
| Approve each recording session | **You** | Nothing is recorded until you say "go" |
| Record and clean the voice | **You** | CapCut |
| Cut the timeline in CapCut | **You** | Use the edit decision list in section 8 |

CapCut has no automation API, so the assistant cannot drive it. What it gives
you instead is a timed clip order, per-section word counts, and a caption and
export checklist, so the edit is mechanical rather than creative.

---

## 2. Verified recording setup

Measured on this machine on 9 November 2026:

| Item | Value |
| --- | --- |
| OBS | 32.2.2, profile `Untitled` |
| Canvas / output | 1920 x 1080 / 1920 x 1080 |
| Frame rate | 30 fps |
| Encoder | QuickSync (`qsv`) — the CPU is a 2-core Celeron, software encoding would drop frames |
| Container | MP4 |
| Output folder | `C:\Users\lusan\Videos` |
| Scene | `Scene`, single Display Capture, cursor visible |
| WebSocket | `ws://127.0.0.1:4455`, authentication on |
| Display | 1920 x 1080 panel at 125% scaling — capture is 1:1, text is rendered 25% larger |
| Test take | 20 s, 10.7% CPU, 15 dropped frames out of 613 (2.4%) |
| Docker stack | MVC `http://localhost:8081`, API `http://localhost:8080`, SQL `localhost:1433` |
| Free disk | about 13 GB |
| RAM | 7.8 GB total, low free — close extra browser windows before each session |

Two consequences worth knowing:

- **Keep the OBS window minimised** while recording. The scene captures the
  whole display, so an open OBS window would appear inside its own recording.
- **Do not build, restore or run Docker while a clip is recording.** The Celeron
  will drop frames and the audio sync in CapCut will need fixing.

---

## 3. Pre-flight checklist

Run before every session.

```powershell
# 1. Containers
docker compose ps

# 2. Both surfaces answer
(Invoke-WebRequest http://localhost:8081 -UseBasicParsing).StatusCode
(Invoke-WebRequest http://localhost:8080/swagger/index.html -UseBasicParsing).StatusCode
```

- [ ] Windows **Do Not Disturb** on (Win + N, or Settings > System > Notifications).
- [ ] Edge in full screen (F11), zoom **110%**, favourites bar hidden (Ctrl + Shift + B).
- [ ] Only Edge and Visual Studio on screen. Close Teams, mail and chat.
- [ ] OBS running and minimised; no red recording border visible.
- [ ] An upload image ready on the Desktop (section 5, step B11).
- [ ] Signed out of the app, so every clip starts from a known state.
- [ ] If this session shows counts, reseed first (see section 4).

Create the upload image once. It goes in the ingest folder so it is easy to
find in the file picker:

```powershell
Copy-Item "wwwroot\images\event-banners\event-banner-3.jpg" "$env:USERPROFILE\Videos\part3\raceday-upload.jpg"
```

### Resetting demo data

The seed is destructive and is meant for a fresh demo database:

```powershell
Get-Content docs/Demo_Seed.sql -Raw |
  docker compose exec -T sql /opt/mssql-tools18/bin/sqlcmd `
    -S localhost -U sa -P "RaceDay#2026!Pass" -C
```

After a reseed the state is fixed and matches the narration:

- 5 events, 433 enrolments, 312 results, 313 users
- `demo.runner@raceday.test` — 47th of 312, `02:36:50`, Pretoria Capital Marathon
- Event 2 has 11 pending and 41 confirmed enrolments, and **no results**
- Event 5 (Johannesburg Sunrise Walk) has the runner not yet enrolled

---

## 4. Clip list

Record in four groups. Grouping is faster than following the narrative, and the
voice-over is recorded afterwards, so the timeline decides the order.

> The code group (C01-C09) is already recorded as `V01`-`V11` in
> `C:\Users\lusan\Videos` and verified frame by frame. What remains is the
> browser group (C10-C24), the ops group (C25-C26) and the close (C27).

| Clip | Group | Narration section | Surface | Target | What happens |
| --- | --- | --- | --- | --- | --- |
| C01 | Code | 2 | VS | 35 s | Solution Explorer, three projects |
| C02 | Code | 2 | VS | 35 s | MVC project file, then a zero-result search for `DbContext` |
| C03 | Code | 3 | VS | 35 s | `RaceDayApiClient` account methods |
| C04 | Code | 3 | VS | 50 s | API session cookie capture and 401 handling |
| C05 | Code | 3 | VS | 20 s | `Program.cs` DI and cookie auth, then the 401 filter |
| C06 | Code | 7 | VS | 30 s | `IFileStorage`, provider registration |
| C07 | Code | 7 | VS | 25 s | `AzureBlobFileStorage` save and delete |
| C08 | Code | 8 | VS | 30 s | `docker-compose.yml` and the two Dockerfiles |
| C09 | Code | 8 | VS | 30 s | CI workflow steps |
| C10 | App | 1 + 5 | Browser | 40 s | Home page, then the events list and search |
| C11 | App | 5 | Browser | 25 s | Event detail with banner and categories |
| C12 | App | 4 | Browser | 30 s | Participant sign-in and role-based nav |
| C13 | App | 4 | Browser | 30 s | Organiser sign-in and changed nav |
| C14 | App | 5 | Browser | 30 s | Enrol in the 5 km walk |
| C15 | App | 5 | Browser | 25 s | My enrolments, three badge colours |
| C16 | App | 5 | Browser | 20 s | My results, 47th of 312 |
| C17 | App | 5 | Browser | 15 s | Profile picture upload |
| C18 | App | 6 | Browser | 20 s | Organiser dashboard |
| C19 | App | 6 | Browser | 30 s | Create event, empty submit, validation |
| C20 | App | 6 | Browser | 30 s | Create event with banner |
| C21 | App | 6 | Browser | 20 s | Add a category |
| C22 | App | 6 | Browser | 20 s | Enrolment list with the three status badges |
| C23 | App | 6 | Browser | 35 s | Event 1 results table, then capture one result |
| C24 | Azure | 7 | Browser | 20 s | Banner served from Azurite, blob listed |
| C25 | Ops | 8 | Terminal | 30 s | `docker compose ps`, MVC, Swagger |
| C26 | Ops | 8 | Browser | 35 s | Green GitHub Actions run |
| C27 | Close | 9 | Browser | 20 s | Home page or title card |

Sum of targets: about 12 minutes. The final length follows the voice-over, so
these are ceilings, not quotas. Trim dead air freely.

---

## 5. Browser beat sheet

All URLs are on the Docker stack. Passwords are `Password123!`.

| Step | URL | Action | What must be on screen |
| --- | --- | --- | --- |
| B1 | `http://localhost:8081/` | None | Hero, upcoming event cards with banners |
| B2 | `/Events` | Scroll one card row | Event grid |
| B3 | `/Events?search=trail&type=Run` | Type `trail` in search, pick Run | Filtered result: Winelands Trail Challenge |
| B4 | `/Events/Details/5` | Scroll to categories | Banner, date, location, 5 km walk entry |
| B5 | `/Account/Login` | `demo.runner@raceday.test` | Signed-in nav, no organiser links |
| B6 | `/Events/Details/5` | Click Enrol on the 5 km walk | Confirmation message |
| B7 | `/Enrolments` | None | Green Confirmed, amber Pending, red Cancelled |
| B8 | `/Results/Mine` | None | `47th of 312 finishers`, `02:36:50` |
| B9 | `/Profile` | Choose `raceday-upload.jpg`, Upload | New profile picture |
| B10 | Sign out, then `/Account/Login` | `demo.organiser@raceday.test` | Nav gains Dashboard and My events |
| B11 | `/Organiser` | None | 5 events, 4 upcoming (one event date is in the past), 433 enrolments |
| B12 | `/organiser/events/create` | Submit empty, then fill | Inline validation, values preserved |
| B13 | same form | Attach `raceday-upload.jpg`, save | New event card with uploaded banner |
| B14 | `/organiser/events/6/categories` | Add `10 km`, 10, 250 | New row in the category table |
| B15 | `/organiser/events/4/enrolments` | Scroll the list | Three status badges; the view is read-only |
| B16 | `/organiser/events/1/results` | Scroll the captured results | Rows reading `47th of 312` |
| B17 | `/organiser/events/2/results/capture/319` | `01:58:24`, position `1`, save | New row in the results table |
| B18 | `/organiser/events` | None | Banner grid, including the new event |

Values used in B12 if the plan is followed exactly:

- Name: `Garden Route Coastal Run`
- Description: `A coastal half marathon from Knysna to Brenton-on-Sea.`
- Date: `2027-03-14`
- Location: `Knysna, Western Cape`
- Distance: `21.1`
- Type: `Run`

**Ordering rule:** record B11 (dashboard) before B13 (create event). The
dashboard shows five events in the narration; after B13 there are six. Reseed
before any second take of B11.

**Badge colours live on event 4.** Event 2 has only Confirmed and Pending
enrolments. Durban Beachfront Cycle Tour (event 4) has 15 Confirmed, 10 Pending
and 1 Cancelled, so that is the shot that shows three colours. The participant's
own My Enrolments page (B7) also shows all three, drawn from events 1-4.

**Don't be clever in B17.** Enrolment 319 has no result yet. Position `1` is
deliberate: this event has only one captured result, so the table reads
`1st of 1`. The strong `47th of 312` shot is B16, from the Pretoria event. If the
capture page says there are no participants awaiting a result, the event already
has one — reseed or pick another enrolment.

---

## 6. Visual Studio cue sheet

Open `RaceDay.Api.slnx`. Set the editor zoom with **Ctrl + mouse wheel** until
code is comfortable at 1080p (about 130%). Close Test Explorer, the Error List
and Git Changes. Show one file at a time; use **Ctrl + G** to jump to a line.

Scroll at roughly two lines per second. Pause at the highlighted range; do not
read the code out loud — the narration explains it.

| Clip | File | Lines | Point at |
| --- | --- | --- | --- |
| C01 | Solution Explorer | — | `RaceDay.Api`, `RaceDay.Mvc`, `RaceDay.Api.test`; expand MVC once |
| C02 | `RaceDay.Mvc/RaceDay.Mvc.csproj` | 1-7 | No `PackageReference` at all |
| C02 | Global search | — | Ctrl + Shift + F, `DbContext`, scope Current Project, 0 results |
| C03 | `RaceDay.Mvc/Services/RaceDayApiClient.cs` | 14-38 | Class, constructor, `LoginAsync` at 37 |
| C03 | same | 60-90 | One method per endpoint, e.g. `GetEventsAsync` |
| C04 | same | 125-149 | `Set-Cookie` captured into session |
| C04 | same | 151-171 | Cookie attached, 401 throws |
| C05 | `RaceDay.Mvc/Program.cs` | 32-67 | Session, typed client, `UseCookies = false`, cookie auth |
| C05 | `RaceDay.Mvc/Services/ApiUnauthorizedFilter.cs` | 9-23 | Sign-out and redirect |
| C06 | `RaceDay.Api/Services/IFileStorage.cs` | 4-15 | The abstraction |
| C06 | `RaceDay.Api/Services/FileStorageRegistration.cs` | 12-24 | Provider switch |
| C07 | `RaceDay.Api/Services/AzureBlobFileStorage.cs` | 25-42 | Container, blob name, upload, public URL |
| C07 | `RaceDay.Api/appsettings.json` | 18-27 | `Storage` block |
| C08 | `docker-compose.yml` | 7-59 | Three services, API port 8080, MVC port 8081 |
| C08 | `Dockerfile`, `RaceDay.Mvc/Dockerfile` | all | Multi-stage build |
| C09 | `.github/workflows/dotnet-ci.yml` | 27-56 | Restore, build, test, publish, artifacts |

Two optional close-ups if you have spare time: `Results/Mine.cshtml` lines 64-67
(the `47th of 312` markup) and `Helpers/ViewHelpers.cs` lines 5-29 (ordinal and
badge helpers). They are small enough to record in one 15-second clip.

---

## 7. Azure Blob Storage demonstration

This is the one part that needs a deliberate environment switch. It was tested
end to end on 9 November 2026: the upload returned
`http://localhost:10000/devstoreaccount1/raceday/event-banners/<guid>.jpg` and
that URL served `200 image/jpeg`.

**Start the emulator and switch the API to the Azure provider:**

```powershell
docker compose -f docker-compose.yml -f docker-compose.azure.yml up -d
```

That override adds an `azurite` service on port 10000 and sets
`Storage__Provider=Azure` on the API. Wait until
`http://localhost:8080/swagger/index.html` returns 200 (about 20 seconds).

**On camera:**

1. Sign in to the MVC app as the organiser.
2. Open an event's edit page and replace the banner.
3. The card shows the new image, and the image URL starts with
   `http://localhost:10000/devstoreaccount1/raceday/`.
4. Open the blob listing at `http://localhost:10000/devstoreaccount1/raceday?comp=list`
   to show the object exists, or show the Azurite container in Docker Desktop.
5. Say the honest line from the narration: this is the official emulator, the
   same code path runs against a real account, only the connection string changes.

**After the take, restore the default state:**

```powershell
docker compose up -d api
docker compose -f docker-compose.yml -f docker-compose.azure.yml stop azurite
```

Then re-upload the event banner as the organiser so the image points back at the
API's local storage. The upload endpoint deletes the old blob when it is one of
ours; external URLs are ignored by design.

> The override contains the public Azurite development key. It is an emulator
> credential, not a secret, and is useless outside `localhost`.

---

## 8. Docker and CI beats

```powershell
docker compose ps
Start-Process http://localhost:8081
Start-Process http://localhost:8080/swagger
Start-Process https://github.com/Lusani-No1/RaceDay.Api/actions
```

- Show four healthy containers: `sql`, `api`, `mvc`, and optionally `azurite`.
- Show the MVC app loading on 8081 and Swagger on 8080.
- On GitHub, open the latest green run and expand **Build solution**,
  **Run unit tests** (85 passed) and **Upload MVC publish output**.
- Do not run `docker compose up --build` on camera. It takes minutes on this
  machine and would be the most boring part of the video.

---

## 9. Recording protocol

1. You say **go**. Assistant starts recording, waits three silent seconds, then
   performs the clip.
2. Two silent seconds at the end, then assistant stops.
3. Every clip becomes its own MP4 in `C:\Users\lusan\Videos`.
4. If a take goes wrong, say **again**. The file is renamed into
   `C:\Users\lusan\Videos\discard` and the clip is re-recorded from a clean state.
5. Assistant keeps a running log: clip ID, filename, duration, notes. That log
   is the edit decision list.

Clip naming after the fact:

```
C01_solution_explorer.mp4
C02_project_file_search.mp4
...
C16_results_47_of_312.mp4
```

Aim for clips of 15 to 50 seconds. Short clips are easier to re-record and
easier to cut.

---

## 10. CapCut assembly

The draft **RaceDay Part 3** in `%LOCALAPPDATA%\CapCut\User Data\Projects\com.lveditor.draft`
already contains this assembly — 36 clips, 204 captions, 12:11.640 — so the steps
below are only the manual equivalent.

> **Strip the OBS audio before importing.** OBS records an Opus track into its MKV
> files, and CapCut cannot demux Opus out of MP4: the project loads and then the
> application quits. The clips in `Videos\part3` have already been flattened to
> video only; the original takes with audio are in `Videos\part3\withaudio`.
>
> ```powershell
> ffmpeg -i in.mp4 -map 0:v -c:v copy -an out.mp4
> ```
>
> The screen recordings are silent anyway, so nothing is lost.

1. New project: **1920 x 1080, 30 fps**.
2. Import every MP4 and the nine voice files (`S1` to `S9`, one per narration
   section).
3. Place clips on the main track in numeric order. Each clip's narration section
   is in the clip list, so section boundaries map to clip boundaries.
4. Place voice on audio track 1 at the matching section start. Trim clip tails to
   the voice; keep 0.3 seconds of handle before each cut.
5. Add a 4-second title card at the start: your name, student number, module,
   "Part 3 — MVC web application".
6. Add an end card: repository URL and "Thank you for watching".
7. Captions are already written — 204 lines, timed to the clips, using the
   correct spelling of RaceDay, RaceDayApiClient, IFileStorage,
   AzureBlobFileStorage, Azurite, DbContext, QuickSync, GUID, `RaceDay.Session`
   and `RACEDAY-ORG-2026`. Only edit them if the spoken words change.
8. Audio: keep one microphone and one gain setting for all nine sections, so the
   voice does not jump between files. If you add music, keep it at -28 dB and
   duck under speech. The screen clips are silent, so there is nothing to clean.
9. Optional polish: a 1.15x scale keyframe on the code close-ups, held for four
   seconds, so line numbers are readable on a phone. Do not zoom on scrolling
   shots; the motion looks bad.
10. Export: H.264, 1080p, 30 fps, 12 Mbps, AAC 192 kbps.

### Section timings of the edit

These are the exact timings in the draft. Drop each voice file at its start time.

| Section | Narration | Starts | Length |
| --- | --- | --- | --- |
| 1 | Introduction | 00:00.000 | 00:40.900 |
| 2 | Solution structure | 00:40.900 | 01:03.770 |
| 3 | Consuming the API | 01:44.670 | 01:45.730 |
| 4 | Sign-in and role-based nav | 03:30.400 | 00:34.370 |
| 5 | Participant journey | 04:04.770 | 01:40.400 |
| 6 | Organiser journey | 05:45.170 | 02:01.570 |
| 7 | Azure Blob Storage | 07:46.740 | 01:48.540 |
| 8 | Docker and CI | 09:35.280 | 02:18.360 |
| 9 | Close | 11:53.640 | 00:18.000 |

---

## 11. Known risks and fixes

| Risk | Fix |
| --- | --- |
| OBS records its own window | Keep OBS minimised; the scene is a full display capture |
| Notification popup ruins a take | Do Not Disturb before every session |
| Dropped frames during builds | Never build, restore or pull images while recording |
| Low free RAM swaps and stutters | Close extra Edge windows; one tab per beat |
| Visual Studio IntelliSense popup covers code | Press Esc before the take starts |
| Demo numbers drift after a take | Reseed, then re-record the affected clips |
| Azure provider left on | Run the restore commands in section 7 |
| Code text too small on video | Ctrl + wheel zoom in VS; 110% browser zoom |
| Two events called "Garden Route" after retakes | Reseed; the seed clears events and identities |

---

## 12. After recording

1. Upload to YouTube as **unlisted**. Title: `RaceDay Part 3 — MVC Web Application Walkthrough`.
2. Replace `_[Insert link — unlisted YouTube]_` in `README.md` with the video URL.
3. Commit and push; confirm the CI run is green.
4. Replace the duplicated Part 2 video link at the top of the README — it
   currently repeats the Part 1 URL.
