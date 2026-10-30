# How automatic assignment (Assign) chooses drivers and cars

Pressing **Assign** on the Reports page assigns a driver and a car to every trip of the period.
Running it again deletes the period's previous assignments and starts over. It does not work on a finalized period.

The system does NOT use driver ratings, experience, bonuses or company preferences. It only uses the rules below.

## Which trips are assigned
- Trips are processed in time order: by pickup date, then garage-out time.
- **Cash trips** and **Samarkand trips** are never assigned.
- **Field Trips** are handled first. They are not chosen automatically: they get exactly the driver and car
  written in the uploaded trip report. If that driver or car is not found (or the car is unavailable that day),
  the Field Trip becomes a conflict ("Driver Not Found", "Vehicle Not Found or Unavailable"). Fix the name or
  plate in the report.

## Step 1 — which cars can take the trip
A car can take the trip only if:
- it is active, and available on that date (after its "active from" date and not in an unavailable period);
- it is of the trip's vehicle type;
- it has no other trip at the same time.

The car with the **fewest trips so far in the period** is tried first (ties: by plate number).
If no car fits, the trip is a conflict: **"No vehicles available"**.

## Step 2 — which drivers can drive that car
A driver is allowed only if ALL of these are true:
1. **Category**: the driver's licence category is high enough for the car (B = car, C = truck, D = bus;
   a D driver can also drive C and B cars).
2. **Not on leave**: no vacation or day off on that date (Vacations page).
3. **No overlap**: no other trip at the same time.
4. **Rest**: at least 10 hours of rest since the previous shift, including the last trip of the previous period.
   Trips less than 10 hours apart count as one shift.
5. **Max shift 20 hours**: one shift (from its first trip start to its last trip end) cannot be longer than 20 hours.
6. **Weekly limits**: not more than 6 working days in a row, and not more than 60 hours in any 7 days.
7. **Field Trips**: the driver is not on a Field Trip that day, and the trip does not overlap or come within
   1 hour of one of the driver's Field Trips.
8. **1 hour to switch cars**: if the driver's previous trip was in a different car, there must be at least
   1 hour between the trips. The same applies to a car changing drivers.
9. **Max 4 drivers per car** in a period.

If no driver passes for any of the suitable cars, the trip is a conflict: **"No qualified drivers available"**.

## Step 3 — which allowed driver is chosen
Among the allowed drivers, the first one in this order wins:
1. **Regular drivers before backup drivers.** Backup drivers are used only when no regular driver fits.
2. **Same car**: a driver who already drives this car in the period.
3. **Already working today**: a driver who already has a trip that day (keeps trips in one shift).
4. **Fewest trips** in the period (fair distribution).
5. **Longest since last worked.**
6. **Shortest shift so far today.**

## After Assign
Trips that got no driver or car are conflicts. Use **Resolve** (try fallback vehicle types) and then
**Reschedule** (move to another day). See the conflicts guide.
