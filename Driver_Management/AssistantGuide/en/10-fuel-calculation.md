# How trip fuel is calculated

Fuel for a trip = **driving fuel + idle fuel**.

- **Driving fuel** = distance (km) × the car's consumption (L/100 km) ÷ 100.
- **Idle fuel** = idle hours × the car's idle consumption (L/h).
- **Idle hours** = time between garage-out and garage-in, minus the driving time of the route.
  It is never below 0.

Example: a trip with garage-out 08:00 and garage-in 14:00 (6 h), route driving time 2 h, car idle rate 1.2 L/h
→ 4 idle hours → 4.8 L of idle fuel, on top of the driving fuel.

## Where the numbers come from
- **Distance and driving time** are calculated from the trip's addresses with a map routing service.
- **Consumption (L/100km)** and **Idle consumption (L/h)** are set per car on the **Cars** page (add or edit a car).
  Idle consumption of 0 means the car has no idle fuel. Typical values: about 0.8–1.5 L/h for a sedan,
  2–3 L/h for a minibus. When editing a car, leaving the idle field empty keeps the old value; type 0 to clear it.

## Why a trip shows "—" for idle hours
The Trips page has an **Idle** column. It shows "—" (no data, not zero) when the trip has no routed driving time:
- the trip was uploaded before idle fuel existed and its distance was never recalculated;
- the distance was estimated from the trip's duration (routing found nothing, or a Custom Itinerary trip);
- the distance was typed in by hand.
Those trips get no idle fuel. To get idle hours, press the **Recalculate distance** (calculator) button on the trip.

## Distance Fallback Speed (Fuel Settings page)
When routing cannot find a distance, the distance is estimated from the trip's duration using an assumed
average speed — the **Distance Fallback Speed** (default 15 km/h). For Custom Itinerary trips this estimate is
also used as a minimum distance.
- Change it on **Fuel Settings**. It must be more than 0 and at most 150 km/h.
- Only users with fuel management permission can change it; others can only view it.
- A change only affects distances calculated afterwards. Existing trips keep their distance until they are
  recalculated or uploaded again.

## Exports
The fuel Excel export and the detailed CSV export include "Idle Rate (L/h)" and "Idle Hours" columns.
In the Waybill, fuel consumed already includes idle fuel.
