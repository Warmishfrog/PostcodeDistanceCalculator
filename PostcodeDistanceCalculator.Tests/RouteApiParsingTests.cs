using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.Tests;

/// <summary>Parsing of real (trimmed) postcodes.io and OSRM responses. No network calls.</summary>
public class RouteApiParsingTests
{
    [Fact]
    public void Bulk_lookup_separates_found_unknown_and_no_location_postcodes()
    {
        const string json = """
            {"status":200,"result":[
              {"query":"SW1A1AA","result":{"postcode":"SW1A 1AA","longitude":-0.141563,"latitude":51.50101}},
              {"query":"ZZ99ZZ","result":null},
              {"query":"BX11LT","result":{"postcode":"BX1 1LT","longitude":null,"latitude":null}}
            ]}
            """;

        var results = RouteApi.ParseBulkLookup(json).ToDictionary(r => r.Postcode);

        Assert.Equal(("SW1A1AA", true, new PostcodeLocation(51.50101, -0.141563)), results["SW1A1AA"]);
        Assert.Equal(("ZZ99ZZ", false, (PostcodeLocation?)null), results["ZZ99ZZ"]);
        Assert.Equal(("BX11LT", true, (PostcodeLocation?)null), results["BX11LT"]);
    }

    [Fact]
    public void Route_response_gives_one_distance_per_leg()
    {
        const string json = """
            {"code":"Ok","routes":[{"distance":1314636.4,"legs":[
              {"distance":320453.1,"duration":14664.2},
              {"distance":343905.7,"duration":15744.6},
              {"distance":650277.6,"duration":28440.3}
            ]}],"waypoints":[]}
            """;

        Assert.Equal([320453.1, 343905.7, 650277.6], RouteApi.ParseRoute(json, expectedLegs: 3));
    }

    [Fact]
    public void No_route_gives_a_readable_error()
    {
        var ex = Assert.Throws<RouteLookupException>(() => RouteApi.ParseRoute("""{"code":"NoRoute","message":"Impossible route"}""", 1));
        Assert.Contains("No drivable route", ex.Message);
    }

    [Fact]
    public void Osrm_error_message_is_included()
    {
        var ex = Assert.Throws<RouteLookupException>(() =>
            RouteApi.ParseRoute("""{"code":"InvalidQuery","message":"Query string malformed close to position 12"}""", 1));

        Assert.Contains("InvalidQuery", ex.Message);
        Assert.Contains("Query string malformed close to position 12", ex.Message);
    }

    [Fact]
    public void Wrong_number_of_legs_is_rejected()
    {
        const string json = """{"code":"Ok","routes":[{"legs":[{"distance":1000}]}]}""";

        Assert.Throws<RouteLookupException>(() => RouteApi.ParseRoute(json, expectedLegs: 2));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"code":"Ok"}""")]
    [InlineData("""{"code":"Ok","routes":[]}""")]
    public void Malformed_route_responses_become_lookup_errors(string json)
    {
        Assert.Throws<RouteLookupException>(() => RouteApi.ParseRoute(json, 1));
    }

    [Fact]
    public void Malformed_bulk_lookup_becomes_a_lookup_error()
    {
        Assert.Throws<RouteLookupException>(() => RouteApi.ParseBulkLookup("""{"status":200}"""));
    }
}
