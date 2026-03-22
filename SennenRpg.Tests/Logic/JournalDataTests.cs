using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for JournalData — the pure-data journal entry store.
/// No Godot runtime required.
/// </summary>
[TestFixture]
public sealed class JournalDataTests
{
    // ── Entry count ───────────────────────────────────────────────────────────

    [Test]
    public void Entries_HasSevenEntries()
        => Assert.That(JournalData.Entries, Has.Length.EqualTo(7));

    [Test]
    public void Entries_IsNotNull()
        => Assert.That(JournalData.Entries, Is.Not.Null);

    // ── No entry is empty ─────────────────────────────────────────────────────

    [Test]
    public void AllEntries_HaveNonEmptyDate()
    {
        foreach (var entry in JournalData.Entries)
            Assert.That(entry.Date, Is.Not.Null.And.Not.Empty,
                $"Entry titled '{entry.Title}' has an empty Date.");
    }

    [Test]
    public void AllEntries_HaveNonEmptyTitle()
    {
        foreach (var entry in JournalData.Entries)
            Assert.That(entry.Title, Is.Not.Null.And.Not.Empty,
                $"Entry dated '{entry.Date}' has an empty Title.");
    }

    [Test]
    public void AllEntries_HaveNonEmptyBody()
    {
        foreach (var entry in JournalData.Entries)
            Assert.That(entry.Body, Is.Not.Null.And.Not.Empty,
                $"Entry '{entry.Date} - {entry.Title}' has an empty Body.");
    }

    // ── All dates and titles are unique ───────────────────────────────────────

    [Test]
    public void AllEntries_HaveUniqueDates()
    {
        var dates = Array.ConvertAll(JournalData.Entries, e => e.Date);
        Assert.That(dates, Is.Unique, "Each journal entry must have a unique date.");
    }

    [Test]
    public void AllEntries_HaveUniqueTitles()
    {
        var titles = Array.ConvertAll(JournalData.Entries, e => e.Title);
        Assert.That(titles, Is.Unique, "Each journal entry must have a unique title.");
    }

    // ── Specific entry content ────────────────────────────────────────────────

    [Test]
    public void FirstEntry_IsChildsHandwriting()
    {
        var first = JournalData.Entries[0];
        Assert.That(first.Date,  Is.EqualTo("10/3/1160"));
        Assert.That(first.Title, Is.EqualTo("First entry"));
        Assert.That(first.Body,  Does.Contain("Aoife Sylzair"));
        Assert.That(first.Body,  Does.Contain("sevn years old"), "Child's misspelling must be preserved.");
        Assert.That(first.Body,  Does.Contain("- Effy"));
    }

    [Test]
    public void ArgyreEntry_ContainsShipAirBubble()
    {
        var entry = JournalData.Entries[1];
        Assert.That(entry.Date,  Is.EqualTo("5/10/1193"));
        Assert.That(entry.Title, Is.EqualTo("Arriving in Argyre"));
        Assert.That(entry.Body,  Does.Contain("air bubble"));
        Assert.That(entry.Body,  Does.Contain("Assholes"));
    }

    [Test]
    public void GravityEntry_MentionsRadovast()
    {
        var entry = JournalData.Entries[3];
        Assert.That(entry.Date,  Is.EqualTo("3/8/1207"));
        Assert.That(entry.Title, Is.EqualTo("The Gravity of the situation"));
        Assert.That(entry.Body,  Does.Contain("Radovast"));
    }

    [Test]
    public void LakeEntry_MentionsSaydlisLake()
    {
        var entry = JournalData.Entries[4];
        Assert.That(entry.Date,  Is.EqualTo("07/08/1210"));
        Assert.That(entry.Body,  Does.Contain("Saydlis Lake"));
        Assert.That(entry.Body,  Does.Contain("Rime Saydlis"));
    }

    [Test]
    public void VolcanoEntry_IsShort()
    {
        var entry = JournalData.Entries[5];
        Assert.That(entry.Date,  Is.EqualTo("07/25/1210"));
        Assert.That(entry.Title, Is.EqualTo("The Volcano"));
        Assert.That(entry.Body,  Does.Contain("lava creatures"));
        // Should be the shortest entry — confirm it's under 200 chars
        Assert.That(entry.Body.Length, Is.LessThan(200));
    }

    [Test]
    public void FinalEntry_ContainsMAPPWarning()
    {
        var entry = JournalData.Entries[6];
        Assert.That(entry.Date,  Is.EqualTo("12/5/1210"));
        Assert.That(entry.Title, Is.EqualTo("Final Entry"));
        Assert.That(entry.Body,  Does.Contain("MAPP expedition"));
        Assert.That(entry.Body,  Does.Contain("DO NOT SEND ANYONE"));
        Assert.That(entry.Body,  Does.Contain("THE DANGER LIES IN THE NORTH"));
        Assert.That(entry.Body,  Does.Contain("Aoife Sylzair"));
    }

    [Test]
    public void FinalEntry_ContainsRedactedGap()
    {
        // The gap of spaces before "but" is the intentional redaction effect.
        var entry = JournalData.Entries[6];
        Assert.That(entry.Body, Does.Contain("sending back the journal"));
        Assert.That(entry.Body, Does.Contain("but this is where the MAPP expedition"));
    }

    // ── JournalEntry record equality ──────────────────────────────────────────

    [Test]
    public void JournalEntry_RecordEquality_WorksByValue()
    {
        var a = new JournalData.JournalEntry("date", "title", "body");
        var b = new JournalData.JournalEntry("date", "title", "body");
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void JournalEntry_RecordEquality_DifferentBodyNotEqual()
    {
        var a = new JournalData.JournalEntry("date", "title", "body A");
        var b = new JournalData.JournalEntry("date", "title", "body B");
        Assert.That(a, Is.Not.EqualTo(b));
    }
}
