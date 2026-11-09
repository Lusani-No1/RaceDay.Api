# Part 3 — what to record

Everything below is recorded by hand in Edge, one clip per beat. The Visual
Studio group is already done (`V01`-`V11` in `C:\Users\lusan\Videos`), so do not
re-record any code.

Total target: about 10 minutes of footage across 20 clips.

---

## 1. Setup, once per session

1. Docker stack running. Both of these must answer before you start:

   ```
   http://localhost:8081          -> the app
   http://localhost:8080/swagger  -> the API
   ```

2. Turn on **Do Not Disturb** (Win + N), or a notification will land mid-take.
3. Leave **OBS minimised**. The scene captures the whole display, so an open OBS
   window records itself.
4. In Edge: **F11** full screen, zoom to **110%** (Ctrl + plus twice), hide the
   favourites bar with Ctrl + Shift + B.
5. Sign out of the app so the first clip starts from a known state.
6. Point OBS at the ingest folder so nothing needs renaming later:
   **Settings > Output > Recording Path** = `C:\Users\lusan\Videos\part3`.

The upload image is already waiting at
`C:\Users\lusan\Videos\part3\raceday-upload.jpg`. Use it for both the profile
picture (B09) and the new event banner (B13).

---

## 2. The clips, in order

Passwords are `Password123!` throughout. Keep each clip **15-45 seconds**. Start
each take with two seconds of stillness and end with two.

| # | File name | Go to | Do this | What must be visible |
|---|---|---|---|---|
| B01 | `B01_home` | `http://localhost:8081/` | Sit still, no scrolling | Hero section, upcoming event cards with banners |
| B02 | `B02_events` | `/Events` | Scroll slowly down one row of cards | Event grid, five events |
| B03 | `B03_filter` | `/Events` | Type `trail` in Search, choose `Run` in Type, click Filter | One result: Winelands Trail Challenge |
| B04 | `B04_event_detail` | `/Events/Details/5` | Scroll to the categories table | Banner, date, location, 5 km Walk category |
| B05 | `B05_login_participant` | `/Account/Login` | Sign in as `demo.runner@raceday.test` | Nav after sign-in: **no** Dashboard or My events links |
| B06 | `B06_enrol` | `/Events/Details/5` | Click **Enrol** on the 5 km Walk, wait for the page | Success message, badge changes to Enrolled |
| B07 | `B07_my_enrolments` | `/Enrolments` | Scroll the list | Green Confirmed, amber Pending, red Cancelled |
| B08 | `B08_my_results` | `/Results/Mine` | Sit on the Pretoria result | `47th of 312 finishers`, time `02:36:50` |
| B09 | `B09_profile_upload` | `/Profile` | Choose `raceday-upload.jpg`, click Upload | New profile picture on the page and in the nav |
| B10 | `B10_login_organiser` | Sign out, then `/Account/Login` | Sign in as `demo.organiser@raceday.test` | Nav now shows Dashboard and My events |
| B11 | `B11_dashboard` | `/Organiser` | Sit still, one slow scroll | **5 events, 4 upcoming, 433 enrolments** |
| B12 | `B12_create_validation` | `/organiser/events/create` | Click Save with the form **empty**, pause on the errors, then fill it in (values below) | Red validation messages, then values preserved |
| B13 | `B13_create_event` | same page, still filled | Attach `raceday-upload.jpg` as Banner, click Save | New event saved with your banner |
| B14 | `B14_categories` | `/organiser/events/6/categories` | Add `10 km`, distance `10`, price `250`, save | New row in the category table |
| B15 | `B15_enrolments` | `/organiser/events/4/enrolments` | Scroll the list slowly | Three badge colours: 15 Confirmed, 10 Pending, 1 Cancelled. Read-only, no approve button |
| B16 | `B16_results` | `/organiser/events/1/results` | Scroll the results table | Rows reading `47th of 312` and similar |
| B17 | `B17_capture_result` | `/organiser/events/2/results/capture/319` | Finish time `01:58:24`, position `1`, Save | New row appears in the results table |
| B18 | `B18_organiser_events` | `/organiser/events` | Sit still, one slow scroll | Banner grid, six events including the new one |
| B19 | `B19_ops` | Terminal, then browser | Run `docker compose ps`, then open 8081 and 8080/swagger | Three containers Up, both pages loading |
| B20 | `B20_ci` | GitHub Actions | Open the latest green run, expand **Build solution**, **Run unit tests**, **Upload MVC publish output** | 85 tests passed, artifacts listed |

Values for B12:

- Name: `Garden Route Coastal Run`
- Description: `A coastal half marathon from Knysna to Brenton-on-Sea.`
- Date: `2027-03-14`
- Location: `Knysna, Western Cape`
- Distance: `21.1`
- Type: `Run`

---

## 3. Rules that stop a retake

- **B11 before B13.** The dashboard says five events in the narration. Creating
  the new event makes it six, so the dashboard shot must come first.
- **Do not re-record B11** after B13 unless the database is reseeded; otherwise
  the numbers contradict the voice-over.
- **B12 and B13 are one form.** Leave the form filled at the end of B12 and start
  B13 on the same page — the values carry over.
- **B15 is event 4, not event 2.** Event 2 has no cancelled enrolments, so it
  only shows two colours.
- **B17 is deliberately `1st of 1`.** That event has no other results. The strong
  `47th of 312` shot is B16, from the Pretoria event.
- Type at a normal speed. Do not narrate — the voice-over is recorded separately.
- If a take goes wrong, keep the file and tell me which one. The demo data can be
  reseeded and only the affected clips re-recorded.

---

## 4. Optional but worth it

**Azure Blob Storage (B21).** The assignment expects Azure to be visible. The
honest demonstration uses the official Azurite emulator. The setup is one
command, and I can switch it on for you and switch it back afterwards:

```powershell
docker compose -f docker-compose.yml -f docker-compose.azure.yml up -d
```

Then, as the organiser, replace an event banner. The image URL will start with
`http://localhost:10000/devstoreaccount1/raceday/`, which proves the upload went
to blob storage rather than the local disk. Say the line from the narration: it
is the official emulator, the same code path runs against a real account, only
the connection string changes.

**Voice-over.** Nine sections, one audio file each, script in
`docs/Part3_Narration.md` (about 10:45 spoken). Record section by section so a
fluffed line costs seconds, not minutes.

---

## 5. When you are done

Tell me the files are in `C:\Users\lusan\Videos\part3`. I will:

1. Check each clip's duration and read a frame from it to confirm the right screen
   is on it.
2. Map every clip to its narration section.
3. Build the timeline as a real CapCut draft in your **RaceDay2** project, with
   title cards, in the right order.
4. You then open RaceDay2 in CapCut, drop the voice-over on the audio track and
   do the sound.
