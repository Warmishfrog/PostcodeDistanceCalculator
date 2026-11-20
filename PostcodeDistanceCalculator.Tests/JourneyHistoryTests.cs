using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.Tests;

public class JourneyHistoryTests
{
    private static Journey Trip(string from, string to) => new("", [new Leg(from, to, 1000)]);

    private static JourneyBatch Batch(int day, params Journey[] journeys)
        => new() { CreatedAt = new DateTime(2026, 11, day), Journeys = [.. journeys] };

    [Fact]
    public void Removing_the_last_journey_removes_its_batch()
    {
        var trip = Trip("A", "B");
        var batch = Batch(1, trip);
        var history = new JourneyHistory([batch]);

        history.Remove([], [(batch, trip)]);

        Assert.Empty(history.Batches);
    }

    [Fact]
    public void Undo_restores_removed_batches_and_journeys()
    {
        var (a, b) = (Trip("A", "B"), Trip("C", "D"));
        var first = Batch(1, a, b);
        var second = Batch(2, Trip("E", "F"));
        var history = new JourneyHistory([first, second]);

        history.Remove([second], [(first, a)]);
        Assert.Equal(1, history.JourneyCount);

        history.UndoRemove();

        Assert.Equal(3, history.JourneyCount);
        Assert.Equal([a, b], history.Batches[0].Journeys);
        Assert.False(history.CanUndoRemove);
    }

    [Fact]
    public void Undo_restores_a_cleared_history()
    {
        var history = new JourneyHistory([Batch(1, Trip("A", "B"))]);

        history.Clear();
        history.UndoRemove();

        Assert.Single(history.Batches);
    }

    [Fact]
    public void Other_changes_drop_the_undo()
    {
        var history = new JourneyHistory([Batch(1, Trip("A", "B")), Batch(2, Trip("C", "D"))]);
        history.Remove([history.Batches[0]], []);

        history.Add(Batch(3, Trip("E", "F")));

        Assert.False(history.CanUndoRemove);
    }

    [Fact]
    public void Replace_matches_by_reference()
    {
        var trip = Trip("A", "B");
        var lookalike = Trip("A", "B");
        var history = new JourneyHistory([Batch(1, trip)]);
        var verified = trip with { GoogleMapsKm = 2 };

        Assert.False(history.ReplaceJourney(lookalike, verified));
        Assert.True(history.ReplaceJourney(trip, verified));
        Assert.Same(verified, history.Batches[0].Journeys[0]);
    }

    [Fact]
    public void Import_skips_batches_already_present_and_keeps_date_order()
    {
        var history = new JourneyHistory([Batch(5, Trip("A", "B"))]);

        int added = history.Import([Batch(5, Trip("X", "Y")), Batch(1, Trip("C", "D"))]);

        Assert.Equal(1, added);
        Assert.Equal([1, 5], history.Batches.Select(b => b.CreatedAt.Day));
    }
}
