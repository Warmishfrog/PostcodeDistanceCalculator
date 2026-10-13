using System.Globalization;
using PostcodeDistanceCalculator.Extensions;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Tests;

public class ModelTests
{
    [Fact]
    public void Journey_totals_and_first_postcode_are_derived_from_legs()
    {
        var journey = new Journey("", "raw", [new Leg("A", "B", 1500), new Leg("B", "C", 2500)]);

        Assert.Equal(4.0, journey.TotalKm, precision: 9);
        Assert.Equal("A", journey.FirstPostcode);
    }

    [Fact]
    public void Batch_total_sums_its_journeys()
    {
        var batch = new JourneyBatch
        {
            CreatedAt = DateTime.Now,
            Journeys = [new Journey("", "a", [new Leg("A", "B", 1000)]), new Journey("", "b", [new Leg("C", "D", 3000)])]
        };

        Assert.Equal(4.0, batch.TotalKm, precision: 9);
    }

    [Fact]
    public void Unit_conversion_and_copy_format()
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");

        Assert.Equal(62.1371, DistanceUnit.Miles.FromKm(100), precision: 4);
        Assert.Equal(100, DistanceUnit.Kilometers.FromKm(100));

        // Clipboard text is just the number: no label, no unit
        Assert.Equal("62.1", DistanceUnit.Miles.FormatNumber(100));
        Assert.Equal("100.0", DistanceUnit.Kilometers.FormatNumber(100));
    }
}
