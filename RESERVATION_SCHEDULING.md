# Reservation Scheduling and Availability (Functionality #2)

This implementation follows the Group 3 proposal for the Mapua MCL CLIR Reservation System.

## What was added

- **Student date restriction:** new student reservations are accepted only for the current day.
- **Faculty date scheduling:** faculty can reserve today or a future date; past dates are rejected.
- **First-come, first-served processing:** reservation submission is serialized through the existing linked queue. The selected location and time are checked again when the request is processed, so two requests cannot both claim an overlapping slot.
- **Overlap-aware availability:** only Active and Occupied reservations hold a seat or room. Adjacent time slots are allowed, while intersecting time ranges are rejected.
- **Live availability in the reservation form:** the available-location list is refreshed every two seconds and immediately after a reservation is submitted. Locations already booked for the selected date/time are removed from the dropdown.
- **Five-minute no-show grace period:** an Active reservation becomes Expired at `scheduled start + 5 minutes` if it has not been verified as Occupied.
- **Automatic release:** Expired reservations no longer hold their location, making it available to another reservation.
- **Staff notification:** expiration is written to the existing activity log and a notification is shown to staff when the staff screen is open. Notices remain queued for the next time staff opens the screen.
- **Automatic completion:** Occupied reservations become Completed once their scheduled end time is reached.
- **Periodic monitor:** the main menu runs the scheduling monitor every five seconds while the application is open.

## Main files

- `MMCL_CLIRRS/MMCL_CLIRRS/Services/ReservationManagementService.cs` - scheduling rules, FCFS processing, availability, and lifecycle monitor.
- `MMCL_CLIRRS/MMCL_CLIRRS/Models/ReservationExpirationNotice.cs` - staff notification data.
- `MMCL_CLIRRS/MMCL_CLIRRS/Forms/MakeReservationForm.cs` - available-space filtering and periodic refresh.
- `MMCL_CLIRRS/MMCL_CLIRRS/Forms/MainMenuForm.cs` - application-wide scheduling timer.
- `MMCL_CLIRRS/MMCL_CLIRRS/Forms/ReservationSearchForm.cs` - staff expiration notifications.
- `.github/workflows/build-windows.yml` - Windows/.NET 10 restore and build check.

## Manual verification checklist

Run the WinForms application on Windows and verify:

1. A student can submit a reservation for today but receives a validation message for tomorrow.
2. A faculty member can reserve for today and an upcoming date, but not a past date.
3. Two reservations for the same location with overlapping times cannot both be confirmed.
4. A reservation ending at 10:00 and another starting at 10:00 can both be confirmed.
5. A location is removed from the available-location dropdown after its reservation is confirmed; cancellation makes it available again.
6. Verify/occupy a reservation before its five-minute deadline; it must not become Expired at the grace deadline.
7. Leave an Active reservation unverified. At five minutes after its start time, it must become Expired, its location must become available, and staff must receive a notification.
8. Let an Occupied reservation reach its end time. It must become Completed and no longer hold the location.
9. Allow a no-show to expire while the staff form is closed; opening the staff form must display the pending notification.

## Build and verification status

The source files were checked for balanced C# delimiters, terminated strings, conflict markers, and trailing whitespace, and the project file was parsed as XML. The .NET SDK is not installed in the editing environment, so a local C# compilation could not be run. The included GitHub Actions workflow is configured to run `dotnet restore` and `dotnet build` on `windows-latest` with .NET 10 when this branch is pushed or a pull request targets `main`.

The current reservation repository is the project's in-memory linked list with mock seed data. Availability refreshes therefore reflect changes made within the running application's shared service instance; synchronizing separate computers/processes would require a shared persistent database or API, which is outside the data storage currently present in this repository.
