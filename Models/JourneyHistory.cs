namespace PostcodeDistanceCalculator.Models;

/// <summary>Saved batches, oldest first, and the edits the history panel makes to them.</summary>
public sealed class JourneyHistory(IEnumerable<JourneyBatch> batches)
{
    private List<JourneyBatch> batches = [.. batches];

    // Only the most recent removal can be undone; any other change makes this copy stale, so it is dropped
    private List<JourneyBatch>? beforeLastRemoval;

    public IReadOnlyList<JourneyBatch> Batches => batches;
    public double TotalKm => batches.Sum(b => b.TotalKm);
    public int JourneyCount => batches.Sum(b => b.Journeys.Count);
    public bool CanUndoRemove => beforeLastRemoval != null;

    public void Add(JourneyBatch batch)
    {
        batches.Add(batch);
        beforeLastRemoval = null;
    }

    /// <summary>Removes whole batches and single journeys. A batch left with no journeys is removed too.</summary>
    public void Remove(IEnumerable<JourneyBatch> removedBatches, IEnumerable<(JourneyBatch Batch, Journey Journey)> removedJourneys)
    {
        beforeLastRemoval = Copy(batches);
        foreach (var batch in removedBatches)
            batches.Remove(batch);
        foreach (var (batch, journey) in removedJourneys)
        {
            batch.Journeys.Remove(journey);
            if (batch.Journeys.Count == 0) batches.Remove(batch);
        }
    }

    public void Clear()
    {
        beforeLastRemoval = Copy(batches);
        batches.Clear();
    }

    public void UndoRemove()
    {
        if (beforeLastRemoval == null) return;
        batches = beforeLastRemoval;
        beforeLastRemoval = null;
    }

    /// <summary>Swaps <paramref name="old"/> (matched by reference) for <paramref name="updated"/>. Returns true if it was found.</summary>
    public bool ReplaceJourney(Journey old, Journey updated)
    {
        bool replaced = false;
        foreach (var batch in batches)
        {
            int index = batch.Journeys.FindIndex(j => ReferenceEquals(j, old));
            if (index < 0) continue;
            batch.Journeys[index] = updated;
            replaced = true;
        }
        if (replaced) beforeLastRemoval = null;
        return replaced;
    }

    /// <summary>
    /// Adds <paramref name="imported"/> batches, skipping any with the same date and time as one already here
    /// (e.g. importing the same file twice). Returns how many were added.
    /// </summary>
    public int Import(IEnumerable<JourneyBatch> imported)
    {
        var existing = batches.Select(b => b.CreatedAt).ToHashSet();
        var added = imported.Where(b => existing.Add(b.CreatedAt)).ToList();
        if (added.Count == 0) return 0;

        batches.AddRange(added);
        batches.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
        beforeLastRemoval = null;
        return added.Count;
    }

    // Batches' journey lists are mutable, so copy them too or later removals would change the copy
    private static List<JourneyBatch> Copy(IEnumerable<JourneyBatch> source)
        => source.Select(b => new JourneyBatch { CreatedAt = b.CreatedAt, Journeys = [.. b.Journeys] }).ToList();
}
