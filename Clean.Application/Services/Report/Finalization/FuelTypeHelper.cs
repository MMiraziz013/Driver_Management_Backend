namespace Clean.Application.Services.Report.Finalization;

public static class FuelTypeHelper
{
    // Map specific fuel types to generic categories for allocation
    private static readonly Dictionary<string, string> SpecificToGeneric = new(StringComparer.OrdinalIgnoreCase)
    {
        // Gasoline variants
        { "АИ-92", "Gasoline" },
        { "АИ-95", "Gasoline" },
        { "АИ-98", "Gasoline" },
        { "АИ-100", "Gasoline" },
        { "92", "Gasoline" },
        { "95", "Gasoline" },
        { "98", "Gasoline" },
        
        // Diesel variants
        { "ДТ", "Diesel" },
        { "Дизель", "Diesel" },
        { "Diesel", "Diesel" },
        { "ДТ-Л", "Diesel" },
        { "ДТ-З", "Diesel" },
        
        // Gas variants
        { "Газ", "Gas" },
        { "LPG", "Gas" },
        { "Пропан", "Gas" },
        { "Метан", "Gas" },
        { "CNG", "Gas" },
    };

    /// <summary>
    /// Get generic fuel type for allocation matching
    /// </summary>
    public static string GetGenericFuelType(string specificType)
    {
        if (string.IsNullOrWhiteSpace(specificType))
            return "Gasoline"; // Default
            
        // Try exact match
        if (SpecificToGeneric.TryGetValue(specificType.Trim(), out var generic))
            return generic;
            
        // Try contains match
        var lower = specificType.ToLower();
        if (lower.Contains("92") || lower.Contains("95") || lower.Contains("98") || lower.Contains("аи"))
            return "Gasoline";
        if (lower.Contains("дизель") || lower.Contains("дт") || lower.Contains("diesel"))
            return "Diesel";
        if (lower.Contains("газ") || lower.Contains("lpg") || lower.Contains("пропан"))
            return "Gas";
            
        return "Gasoline"; // Default fallback
    }
    
    /// <summary>
    /// Normalize specific fuel type for consistent display
    /// </summary>
    public static string NormalizeSpecificFuelType(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "АИ-92"; // Default
            
        var trimmed = input.Trim();
        
        // Normalize common variations
        return trimmed.ToUpper() switch
        {
            "92" or "АИ92" or "AI-92" or "AI92" => "АИ-92",
            "95" or "АИ95" or "AI-95" or "AI95" => "АИ-95",
            "98" or "АИ98" or "AI-98" or "AI98" => "АИ-98",
            "ДТ" or "DIESEL" or "ДИЗЕЛЬ" => "ДТ",
            "ГАЗ" or "LPG" or "GAS" => "Газ",
            _ => trimmed // Keep as-is if not recognized
        };
    }
}
