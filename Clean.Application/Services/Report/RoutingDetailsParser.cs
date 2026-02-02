using System.Text.RegularExpressions;
using Clean.Application.Dtos.Mapbox;

namespace Clean.Application.Services.Report;

public static class RoutingDetailsParser
{
    // Hardcoded airport coordinates - extend this dictionary as needed
    //TODO: Add all airport coordinates
    private static readonly Dictionary<string, (double Lat, double Lon)> KnownAirports = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Tashkent International Airport", (41.262959, 69.267004) },
        { "Tashkent Terminal 3", (41.249759, 69.264930)},
        { "Samarkand International Airport", (39.700547, 66.983829) },
        { "Bukhara International Airport", (39.775000, 64.483333) },
        { "Fergana International Airport", (40.376518, 71.752928)},
        // Add more airports here as you collect their coordinates
        // { "Airport Name", (latitude, longitude) },
    };

    public static ParsedRoutingDetailsDto Parse(string routingDetails)
    {
        var result = new ParsedRoutingDetailsDto();
        
        if (string.IsNullOrWhiteSpace(routingDetails))
            return result;

        var segments = routingDetails.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            var trimmedSegment = segment.Trim();
            
            if (trimmedSegment.StartsWith("PU:", StringComparison.OrdinalIgnoreCase))
            {
                result.PickUp = ExtractLocationWithCoordinates(trimmedSegment, "PU:");
            }
            else if (trimmedSegment.StartsWith("DO:", StringComparison.OrdinalIgnoreCase))
            {
                result.DropOff = ExtractLocationWithCoordinates(trimmedSegment, "DO:");
            }
            else if (trimmedSegment.StartsWith("ST:", StringComparison.OrdinalIgnoreCase))
            {
                var stop = ExtractLocationWithCoordinates(trimmedSegment, "ST:");
                if (stop != null && !string.IsNullOrWhiteSpace(stop.Address))
                {
                    result.Stops.Add(stop);
                }
            }
        }

        return result;
    }

    private static LocationWithCoordinates ExtractLocationWithCoordinates(string segment, string prefix)
    {
        // Remove prefix
        var withoutPrefix = segment.Substring(prefix.Length).Trim();
        
        // Pattern to match coordinates anywhere in the string: (lat, lon)
        var coordPattern = @"\((-?\d+\.?\d*),\s*(-?\d+\.?\d*)\)";
        var match = Regex.Match(withoutPrefix, coordPattern);
        
        string address;
        double? lat = null;
        double? lon = null;
        
        if (match.Success)
        {
            // Extract coordinates
            lat = double.Parse(match.Groups[1].Value);
            lon = double.Parse(match.Groups[2].Value);
            
            // Remove the coordinates from the string to get clean address
            address = withoutPrefix.Remove(match.Index, match.Length).Trim();
            
            // Remove any trailing commas or extra whitespace
            address = address.TrimEnd(',', ' ');
        }
        else
        {
            // No coordinates found in the string
            address = withoutPrefix;
        }
        
        // Clean the address - split by comma and take the first part
        var parts = address.Split(',');
        var mainLocation = parts[0].Trim();
        
        // Remove common noise patterns (flight info, terminal, etc.)
        mainLocation = RemoveFlightInfo(mainLocation);
        
        // Check if this is a known airport and use hardcoded coordinates if no coords were provided
        if (!lat.HasValue && !lon.HasValue)
        {
            var airportCoords = GetAirportCoordinates(mainLocation);
            if (airportCoords.HasValue)
            {
                lat = airportCoords.Value.Lat;
                lon = airportCoords.Value.Lon;
            }
        }
        
        return new LocationWithCoordinates
        {
            Address = mainLocation,
            Latitude = lat,
            Longitude = lon
        };
    }
    
    /// <summary>
    /// Get hardcoded coordinates for known airports
    /// </summary>
    private static (double Lat, double Lon)? GetAirportCoordinates(string locationName)
    {
        // Try exact match first
        if (KnownAirports.TryGetValue(locationName, out var coords))
        {
            return coords;
        }
        
        // Try partial match (e.g., "Tashkent Airport" should match "Tashkent International Airport")
        var normalizedInput = NormalizeForComparison(locationName);
        
        foreach (var airport in KnownAirports)
        {
            var normalizedAirport = NormalizeForComparison(airport.Key);
            
            // Check if the normalized names are similar enough
            if (normalizedAirport.Contains(normalizedInput) || normalizedInput.Contains(normalizedAirport))
            {
                return airport.Value;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Normalize airport names for comparison
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        // Remove common words and normalize
        var normalized = text.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\b(international|airport|intl)\b", "");
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    /// <summary>
    /// Remove common noise patterns like flight numbers, terminal info
    /// </summary>
    private static string RemoveFlightInfo(string location)
    {
        // Remove patterns like "Flight HY123", "Terminal A", "Gate 5", etc.
        var patterns = new[]
        {
            @"\bFlight\s+[A-Z0-9]+\b",
            @"\bTerminal\s+[A-Z0-9]+\b",
            @"\bGate\s+[A-Z0-9]+\b",
            @"\bDeparture\s+[A-Z0-9]+\b",
            @"\bArrival\s+[A-Z0-9]+\b",
            @"\bTerm/Gate:\s*\d+\b",
            @"\bFlt#:\s*[A-Z0-9]+\b",
            @"\bFrom/To:\s*[A-Z]+\b"
        };

        foreach (var pattern in patterns)
        {
            location = Regex.Replace(location, pattern, "", RegexOptions.IgnoreCase).Trim();
        }

        // Clean up multiple spaces and commas
        location = Regex.Replace(location, @"\s+", " ");
        location = Regex.Replace(location, @",+", ",");
        location = location.Trim(',', ' ');

        return location;
    }

    /// <summary>
    /// Check if two addresses are the same after normalization
    /// </summary>
    public static bool AreSameLocation(string address1, string address2)
    {
        if (string.IsNullOrWhiteSpace(address1) || string.IsNullOrWhiteSpace(address2))
            return false;

        var normalized1 = NormalizeAddress(address1);
        var normalized2 = NormalizeAddress(address2);

        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return true;

        var similarity = CalculateSimilarity(normalized1, normalized2);
        return similarity >= 0.8;
    }

    private static string NormalizeAddress(string address)
    {
        var normalized = address.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            return 1.0;
        
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;

        var maxLength = Math.Max(s1.Length, s2.Length);
        if (maxLength == 0)
            return 1.0;

        var distance = LevenshteinDistance(s1, s2);
        return 1.0 - (distance / (double)maxLength);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var matrix = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            matrix[i, 0] = i;
        
        for (int j = 0; j <= len2; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[len1, len2];
    }
}