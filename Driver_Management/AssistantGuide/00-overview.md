# Overview: the monthly workflow

The system plans which driver and car do each trip in a report period, then works out fuel.
Main sidebar pages: Drivers, Vehicle Types, Cars, Service Types, Trips, Reports, Vacations,
Bonuses, Bonus Settings, Fuel Settings, Accounting, Companies.

Typical order of work for a period, on the **Reports** page (each period has its own row of buttons):

1. **Upload → Trip Report** — import the trips for the period.
2. **Upload → Gas Purchases** — import the fuel purchases.
3. **Assign** — automatic assignment of a driver and car to every trip.
   Trips that could not get a driver or car become *conflicts* (the period shows "Has conflicts").
4. **Resolve** — retries only the conflicting trips on other (fallback) vehicle types. See the conflicts guide.
5. **Reschedule** — moves trips that are still conflicts to another day of the period. See the conflicts guide.
6. **Fuel** — fuel allocation: shares the purchased fuel between the cars, based on their trips.
7. **Export** — download reports such as the Assignment Report or Waybill.
8. **Finalize** — locks the period. While a period is finalized, Assign, Resolve, Reschedule and
   Fuel are disabled. To change something, revert the finalization first.

If assignments change after fuel allocation (for example after Reschedule), run **Fuel** again.
