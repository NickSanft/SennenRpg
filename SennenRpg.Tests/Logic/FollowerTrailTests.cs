using NUnit.Framework;
using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class FollowerTrailTests
{
    [Test]
    public void NewTrail_IsEmpty()
    {
        var trail = new FollowerTrail(8);
        Assert.That(trail.IsEmpty, Is.True);
        Assert.That(trail.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetTrailPosition_OnEmptyTrail_ReturnsFallback()
    {
        var trail    = new FollowerTrail(8);
        var fallback = new Vector2(123, 456);
        Assert.That(trail.GetTrailPosition(1, fallback), Is.EqualTo(fallback));
    }

    [Test]
    public void Push_RecordsLatest_GetTrailPosition_OneStepBackReturnsLatest()
    {
        var trail = new FollowerTrail(8);
        trail.Push(new Vector2(0, 0));
        trail.Push(new Vector2(16, 0));
        trail.Push(new Vector2(32, 0));

        Assert.That(trail.Count, Is.EqualTo(3));
        Assert.That(trail.GetTrailPosition(1, Vector2.Zero), Is.EqualTo(new Vector2(32, 0)));
        Assert.That(trail.GetTrailPosition(2, Vector2.Zero), Is.EqualTo(new Vector2(16, 0)));
        Assert.That(trail.GetTrailPosition(3, Vector2.Zero), Is.EqualTo(new Vector2(0, 0)));
    }

    [Test]
    public void GetTrailPosition_DeeperThanHistory_ReturnsOldestEntry()
    {
        var trail = new FollowerTrail(8);
        trail.Push(new Vector2(0, 0));
        trail.Push(new Vector2(16, 0));

        // Asking for "5 steps back" when only 2 exist returns the oldest entry.
        Assert.That(trail.GetTrailPosition(5, new Vector2(-1, -1)), Is.EqualTo(new Vector2(0, 0)));
    }

    [Test]
    public void Push_OverflowsCapacity_DropsOldest()
    {
        var trail = new FollowerTrail(capacity: 3);
        trail.Push(new Vector2(0, 0));
        trail.Push(new Vector2(16, 0));
        trail.Push(new Vector2(32, 0));
        trail.Push(new Vector2(48, 0)); // overflow — drops (0,0)

        Assert.That(trail.Count, Is.EqualTo(3));
        Assert.That(trail.GetTrailPosition(1, Vector2.Zero), Is.EqualTo(new Vector2(48, 0)));
        Assert.That(trail.GetTrailPosition(3, Vector2.Zero), Is.EqualTo(new Vector2(16, 0)));
        // Asking for 4 returns the oldest available (16,0), not the dropped (0,0).
        Assert.That(trail.GetTrailPosition(4, Vector2.Zero), Is.EqualTo(new Vector2(16, 0)));
    }

    [Test]
    public void Capacity_AlwaysAtLeastOne()
    {
        var trail = new FollowerTrail(capacity: 0);
        Assert.That(trail.Capacity, Is.EqualTo(1));
        trail.Push(new Vector2(1, 1));
        trail.Push(new Vector2(2, 2));
        Assert.That(trail.Count, Is.EqualTo(1));
        Assert.That(trail.GetTrailPosition(1, Vector2.Zero), Is.EqualTo(new Vector2(2, 2)));
    }

    [Test]
    public void Clear_EmptiesTrail()
    {
        var trail = new FollowerTrail(8);
        trail.Push(new Vector2(0, 0));
        trail.Push(new Vector2(16, 0));
        trail.Clear();
        Assert.That(trail.IsEmpty, Is.True);
        Assert.That(trail.GetTrailPosition(1, new Vector2(7, 7)), Is.EqualTo(new Vector2(7, 7)));
    }

    [Test]
    public void GetTrailPosition_StepsBackZero_TreatedAsOne()
    {
        var trail = new FollowerTrail(8);
        trail.Push(new Vector2(10, 0));
        trail.Push(new Vector2(20, 0));
        Assert.That(trail.GetTrailPosition(0, Vector2.Zero), Is.EqualTo(new Vector2(20, 0)));
    }

    [Test]
    public void Trail_SimulatesFiveFollowerChain()
    {
        // Simulate the leader walking right one tile at a time, and verify a chain of
        // 5 followers reads back the correct positions for any leader frame.
        var trail = new FollowerTrail(capacity: 8);
        for (int i = 0; i < 10; i++)
            trail.Push(new Vector2(i * 16, 0));

        // Followers spaced 1 step apart: follower n is n steps back.
        // After 10 pushes the leader is at (144, 0); follower 1 is at (128,0), etc.
        Assert.That(trail.GetTrailPosition(1, Vector2.Zero), Is.EqualTo(new Vector2(144, 0)));
        Assert.That(trail.GetTrailPosition(2, Vector2.Zero), Is.EqualTo(new Vector2(128, 0)));
        Assert.That(trail.GetTrailPosition(5, Vector2.Zero), Is.EqualTo(new Vector2(80, 0)));
    }
}
