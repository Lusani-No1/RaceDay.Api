RaceDay — API Endpoint Plan

This document lists every API endpoint the RaceDay system will expose, planned before any application code is written (Part 2). It is derived directly from the ERD (Users, UserProfiles, Events, Categories, Enrolments, Results) and the two system roles (Organiser, Participant).

Role Required key: None = public, Any = any logged-in user, Organiser / Participant = that role only.

1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. | None | { fullName, email, password, role } | 201 Created – new user record (no password hash returned) <br> 400 Bad Request – missing/invalid fields <br> 409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and issues an access token. | None | { email, password } | 200 OK – JWT token + user role <br> 401 Unauthorized – invalid credentials |

2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's account and profile details. | Any | None | 200 OK – user + profile object <br> 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's extended profile information. | Any | { phoneNumber, dateOfBirth, emergencyContact, profileImageUrl } | 200 OK – updated profile <br> 400 Bad Request – invalid fields |

3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events, so Participants can browse upcoming races. | None | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns full details for a single event. | None | None | 200 OK – event object <br> 404 Not Found – event does not exist |
| POST | /api/events | Creates a new event, owned by the logged-in Organiser. | Organiser | { name, description, eventDate, location } | 201 Created – new event <br> 400 Bad Request – invalid fields |
| PUT | /api/events/{id} | Updates an event. Only the Organiser who created it may edit it. | Organiser | { name, description, eventDate, location } | 200 OK – updated event <br> 403 Forbidden – not the owning organiser <br> 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event and its categories/enrolments. | Organiser | None | 204 No Content <br> 403 Forbidden – not the owning organiser <br> 404 Not Found |

4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories (e.g. 10km, 21km) for a given event. | None | None | 200 OK – array of categories <br> 404 Not Found – event does not exist |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. | Organiser | { name, distanceKm, price } | 201 Created – new category <br> 403 Forbidden – not the owning organiser <br> 404 Not Found – event does not exist |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser | None | 204 No Content <br> 403 Forbidden <br> 404 Not Found |

5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrol | Enrols the logged-in Participant into a category for an event. | Participant | None (participant identified from token) | 201 Created – enrolment record <br> 404 Not Found – category does not exist <br> 409 Conflict – already enrolled in this category |
| GET | /api/enrolments/me | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK – array of the participant's enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event, across all categories, for the owning Organiser. | Organiser | None | 200 OK – array of enrolments <br> 403 Forbidden – not the owning organiser <br> 404 Not Found – event does not exist |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant | None | 204 No Content <br> 403 Forbidden – not the owning participant <br> 404 Not Found |

6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/result | Captures a result for a completed enrolment. | Organiser | { finishTime, position } | 201 Created – result record <br> 403 Forbidden – not the owning organiser <br> 404 Not Found – enrolment does not exist <br> 409 Conflict – result already captured |
| GET | /api/results/me | Lists the logged-in Participant's personal result history. | Participant | None | 200 OK – array of the participant's results |
| GET | /api/events/{eventId}/results | Lists all results for an event (e.g. for a results/leaderboard page). | None | None | 200 OK – array of results <br> 404 Not Found – event does not exist |

