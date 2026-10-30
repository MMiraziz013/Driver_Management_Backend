# Recalculate fuel (Trips page)

After fuel allocation has run, you may change a car's consumption or a trip's distance. Then that car's
allocated fuel no longer matches: its tank can go **over capacity** (consumption lowered) or **below zero**
(consumption raised).

Each trip on the **Trips** page has a **Recalculate fuel** button (fuel pump icon). It redoes the automatic
fuel allocation of **the car assigned to that trip, for the whole period** — not only for that one trip.

How it works:
1. Press the button. A preview opens; nothing is saved yet. It shows the car's liters, cost, ending tank
   level and problem days (over capacity / deficit) before → after, plus any warnings.
2. Press **Apply** to save.

Rules:
- Other cars' fuel is never changed.
- The car can only get fuel that is still free in the purchases (the fuel it gave back plus fuel that was
  already unallocated). Fuel that no longer fits stays unallocated and is shown in the warnings.
- Manual fuel allocations of the car are kept.
- The button is disabled when the trip has no assigned car.

Typical use: fix the trip's distance (calculator button) or the car's consumption on the Cars page,
then press Recalculate fuel on one of that car's trips.

Common messages:
- "Fuel is finalized for this period. Revert fuel finalization first."
- "No fuel allocations found for this period. Run fuel allocation first." — run **Fuel** on the Reports page.
- "Vehicle(s) not part of fuel allocation in this period" — for example an electric or inactive car.

Running **Fuel** on the Reports page again re-shares fuel for all cars, not just one.
