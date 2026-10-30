# Conflicts: Resolve and Reschedule

After **Assign**, some trips may be *conflicts*: "No vehicles available" or "No qualified drivers available".
Two buttons on the Reports page fix them. Recommended order: **Assign → Resolve → Reschedule**.
Both only touch conflicting trips — trips that already have a driver and car are never changed — and both
are safe to run again. Neither works on a finalized period.

## Resolve
Retries each conflicting trip on its **fallback vehicle types**, in priority order. If a fallback type has a
free car and qualified driver, the trip's vehicle type is changed to it and the trip is assigned.

Example fallbacks: SUV → Standard Sedan → Hyundai Sonata → Hyundai Tucson; Minivan → Minibus → Luxury Minibus.

- **Field Trips are skipped** — fix their imported driver or car by hand.
- "No fallback vehicle types configured for <type>" means that type has no fallbacks yet.

### Setting fallback types
On the **Vehicle Types** page, edit a type's fallback list: add types, put them in order (first = tried first),
remove, and save. A type cannot be its own fallback.

## Reschedule
For trips that are still conflicts (usually days with more trips than free drivers), Reschedule moves each trip
to the **closest day in the period** that has a free driver and car: first 1 day earlier, then 1 day later,
then 2 days earlier, 2 days later, and so on up to the ends of the period. On each day it tries the trip's own
vehicle type first, then its fallback types.

- Only the date changes; garage-out and garage-in times stay the same.
- **Field Trips never move.**
- On the Trips page, moved trips show a "moved from <date>" hint.
- Exports, waybills and journeys use the new date automatically.
- If fuel allocation was already run for the period, run **Fuel** again afterwards.

## Results
Both show a results table: each trip's status is **Resolved**, **Unresolved** (with the reason) or **Skipped**.
