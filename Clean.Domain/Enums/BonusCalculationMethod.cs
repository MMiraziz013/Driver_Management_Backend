namespace Clean.Domain.Enums;

public enum BonusCalculationMethod
{
    QuantityBased = 0,      // Fixed per trip, varies by vehicle type (Transfer, Airport, Railway)
    DurationBased = 1,      // Time brackets (Customer Itinerary)
    RoundTripBased = 2,     // Fixed per trip, different rates (Round Trip)
    FieldTripBased = 3      // Duration brackets + daily rate (Field Trip)
}