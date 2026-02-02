using Clean.Application.Services.Report;

namespace Clean.Application.Tests;

public class RoutingDetailsParserTests
{
    [Fact]
    public void Parse_SimpleRoute_ReturnsCorrectData()
    {
        // Arrange
        var input = "PU: Hyatt Regency Tashkent; DO: Tashkent International Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.Equal("Hyatt Regency Tashkent", result.PickUp?.Address);
        Assert.Equal("Tashkent International Airport", result.DropOff?.Address);
        Assert.Empty(result.Stops);
        
        // Airport should have hardcoded coordinates
        Assert.NotNull(result.DropOff?.Latitude);
        Assert.NotNull(result.DropOff?.Longitude);
        Assert.Equal(41.262959, result.DropOff.Latitude.Value, 6);
        Assert.Equal(69.267004, result.DropOff.Longitude.Value, 6);
    }
    
    [Fact]
    public void Parse_RouteWithStops_ReturnsCorrectData()
    {
        // Arrange
        var input = "PU: Hotel A; ST: Stop 1; ST: Stop 2; DO: Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.Equal("Hotel A", result.PickUp?.Address);
        Assert.Equal("Airport", result.DropOff?.Address);
        Assert.Equal(2, result.Stops.Count);
        Assert.Contains(result.Stops, s => s.Address == "Stop 1");
        Assert.Contains(result.Stops, s => s.Address == "Stop 2");
    }
    
    [Fact]
    public void Parse_WithExtraDetails_CleansAddresses()
    {
        // Arrange
        var input = "PU: Hyatt Regency, Navoiy street, Tashkent; DO: Airport, Terminal 2";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.Equal("Hyatt Regency", result.PickUp?.Address);
        Assert.Equal("Airport", result.DropOff?.Address);
    }
    
    [Fact]
    public void Parse_WithCoordinatesAtEnd_ExtractsCorrectly()
    {
        // Arrange
        var input = "PU: Tashkent International Airport; DO: Courtyard by Marriott, 126, Kichik Beshyogoch (41.279719, 69.269330), Tashkent";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.PickUp);
        Assert.NotNull(result.DropOff);
        
        Assert.Equal("Tashkent International Airport", result.PickUp.Address);
        Assert.Equal("Courtyard by Marriott", result.DropOff.Address);
        
        // Airport should have hardcoded coordinates
        Assert.NotNull(result.PickUp.Latitude);
        Assert.NotNull(result.PickUp.Longitude);
        Assert.Equal(41.262959, result.PickUp.Latitude.Value, 6);
        Assert.Equal(69.267004, result.PickUp.Longitude.Value, 6);
        
        // Hotel should have embedded coordinates
        Assert.NotNull(result.DropOff.Latitude);
        Assert.NotNull(result.DropOff.Longitude);
        Assert.Equal(41.279719, result.DropOff.Latitude.Value, 6);
        Assert.Equal(69.269330, result.DropOff.Longitude.Value, 6);
    }
    
    [Fact]
    public void Parse_WithCoordinatesInMiddle_ExtractsCorrectly()
    {
        // Arrange
        var input = "PU: InterContinental (41.311234, 69.279876) Hotel, Isqtiqlol st; DO: Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.PickUp);
        Assert.Equal("InterContinental Hotel", result.PickUp.Address);
        Assert.NotNull(result.PickUp.Latitude);
        Assert.NotNull(result.PickUp.Longitude);
        Assert.Equal(41.311234, result.PickUp.Latitude.Value, 6);
        Assert.Equal(69.279876, result.PickUp.Longitude.Value, 6);
    }
    
    [Fact]
    public void Parse_WithCoordinatesAtBeginning_ExtractsCorrectly()
    {
        // Arrange
        var input = "PU: (41.300000, 69.250000) Hilton Hotel, Downtown; DO: Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.PickUp);
        Assert.Equal("Hilton Hotel", result.PickUp.Address);
        Assert.NotNull(result.PickUp.Latitude);
        Assert.NotNull(result.PickUp.Longitude);
        Assert.Equal(41.300000, result.PickUp.Latitude.Value, 6);
        Assert.Equal(69.250000, result.PickUp.Longitude.Value, 6);
    }
    
    [Fact]
    public void Parse_AirportWithoutCoordinates_UsesHardcodedValues()
    {
        // Arrange
        var input = "PU: Hyatt Regency; DO: Tashkent International Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.DropOff);
        Assert.Equal("Tashkent International Airport", result.DropOff.Address);
        Assert.NotNull(result.DropOff.Latitude);
        Assert.NotNull(result.DropOff.Longitude);
        Assert.Equal(41.262959, result.DropOff.Latitude.Value, 6);
        Assert.Equal(69.267004, result.DropOff.Longitude.Value, 6);
    }
    
    [Fact]
    public void Parse_AirportPartialName_MatchesHardcodedValues()
    {
        // Arrange
        var input = "PU: Hotel; DO: Tashkent Airport"; // Shortened name
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.DropOff);
        Assert.Equal("Tashkent Airport", result.DropOff.Address);
        Assert.NotNull(result.DropOff.Latitude);
        Assert.NotNull(result.DropOff.Longitude);
        // Should still match "Tashkent International Airport"
        Assert.Equal(41.262959, result.DropOff.Latitude.Value, 6);
        Assert.Equal(69.267004, result.DropOff.Longitude.Value, 6);
    }
    
    [Fact]
    public void AreSameLocation_IdenticalAddresses_ReturnsTrue()
    {
        // Arrange
        var addr1 = "Hyatt Regency Tashkent";
        var addr2 = "Hyatt Regency Tashkent";
        
        // Act
        var result = RoutingDetailsParser.AreSameLocation(addr1, addr2);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreSameLocation_SimilarAddresses_ReturnsTrue()
    {
        // Arrange
        var addr1 = "Hyatt Regency Tashkent";
        var addr2 = "Hyatt-Regency, Tashkent";
        
        // Act
        var result = RoutingDetailsParser.AreSameLocation(addr1, addr2);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void AreSameLocation_DifferentAddresses_ReturnsFalse()
    {
        // Arrange
        var addr1 = "Hyatt Regency Tashkent";
        var addr2 = "Tashkent International Airport";
        
        // Act
        var result = RoutingDetailsParser.AreSameLocation(addr1, addr2);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void Parse_RemovesFlightInfo()
    {
        // Arrange
        var input = "PU: Hyatt Regency Flight HY123 Terminal A; DO: Tashkent International Airport, Turkish Airlines, From/To: IST, Term/Gate: 2, Flt#: 371";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.Equal("Hyatt Regency", result.PickUp?.Address);
        Assert.Equal("Tashkent International Airport", result.DropOff?.Address);
    }
    
    [Fact]
    public void Parse_MultipleStopsWithMixedCoordinates_ParsesCorrectly()
    {
        // Arrange
        var input = "PU: Hotel A (41.300000, 69.250000); ST: Stop 1; ST: Stop 2 (41.310000, 69.260000); DO: Tashkent International Airport";
        
        // Act
        var result = RoutingDetailsParser.Parse(input);
        
        // Assert
        Assert.NotNull(result.PickUp);
        Assert.NotNull(result.DropOff);
        
        Assert.Equal("Hotel A", result.PickUp.Address);
        Assert.NotNull(result.PickUp.Latitude);
        Assert.NotNull(result.PickUp.Longitude);
        Assert.Equal(41.300000, result.PickUp.Latitude.Value, 6);
        Assert.Equal(69.250000, result.PickUp.Longitude.Value, 6);
        
        Assert.Equal(2, result.Stops.Count);
        Assert.Equal("Stop 1", result.Stops[0].Address);
        Assert.Null(result.Stops[0].Latitude); // No coordinates
        
        Assert.Equal("Stop 2", result.Stops[1].Address);
        Assert.NotNull(result.Stops[1].Latitude);
        Assert.NotNull(result.Stops[1].Longitude);
        Assert.Equal(41.310000, result.Stops[1].Latitude.Value, 6);
        Assert.Equal(69.260000, result.Stops[1].Longitude.Value, 6);
        
        // Airport has hardcoded coordinates
        Assert.NotNull(result.DropOff.Latitude);
        Assert.NotNull(result.DropOff.Longitude);
        Assert.Equal(41.262959, result.DropOff.Latitude.Value, 6);
        Assert.Equal(69.267004, result.DropOff.Longitude.Value, 6);
    }
}