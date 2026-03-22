using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for DialogicSignalParser — the pure signal-string parser.
/// No Godot runtime required.
/// </summary>
[TestFixture]
public sealed class DialogicSignalParserTests
{
    // ── flag: prefix ──────────────────────────────────────────────────────────

    [Test]
    public void Parse_FlagSignal_ReturnsTypFlag()
    {
        var (type, _) = DialogicSignalParser.Parse("flag:met_shizu");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeFlag));
    }

    [Test]
    public void Parse_FlagSignal_ExtractsFlagName()
    {
        var (_, arg) = DialogicSignalParser.Parse("flag:met_shizu");
        Assert.That(arg, Is.EqualTo("met_shizu"));
    }

    [TestCase("flag:met_shizu",      "met_shizu")]
    [TestCase("flag:talked_to_foran","talked_to_foran")]
    [TestCase("flag:chapter_1_done", "chapter_1_done")]
    public void Parse_FlagSignal_ExtractsCorrectName(string signal, string expected)
    {
        var (_, arg) = DialogicSignalParser.Parse(signal);
        Assert.That(arg, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_FlagPrefix_WithEmptyName_ReturnsEmptyArgument()
    {
        // Edge case: "flag:" with nothing after the colon
        var (type, arg) = DialogicSignalParser.Parse("flag:");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeFlag));
        Assert.That(arg,  Is.EqualTo(""));
    }

    // ── give_item: prefix ─────────────────────────────────────────────────────

    [Test]
    public void Parse_GiveItemSignal_ReturnsTypeGiveItem()
    {
        var (type, _) = DialogicSignalParser.Parse("give_item:res://resources/items/item_001.tres");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeGiveItem));
    }

    [Test]
    public void Parse_GiveItemSignal_ExtractsFullPath()
    {
        var (_, arg) = DialogicSignalParser.Parse("give_item:res://resources/items/item_001.tres");
        Assert.That(arg, Is.EqualTo("res://resources/items/item_001.tres"));
    }

    [TestCase("give_item:res://resources/items/item_001.tres", "res://resources/items/item_001.tres")]
    [TestCase("give_item:res://resources/items/item_002.tres", "res://resources/items/item_002.tres")]
    public void Parse_GiveItemSignal_ExtractsCorrectPath(string signal, string expected)
    {
        var (_, arg) = DialogicSignalParser.Parse(signal);
        Assert.That(arg, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_GiveItemPrefix_WithEmptyPath_ReturnsEmptyArgument()
    {
        var (type, arg) = DialogicSignalParser.Parse("give_item:");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeGiveItem));
        Assert.That(arg,  Is.EqualTo(""));
    }

    // ── custom signals ────────────────────────────────────────────────────────

    [Test]
    public void Parse_UnknownSignal_ReturnsTypeCustom()
    {
        var (type, _) = DialogicSignalParser.Parse("open_chest");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeCustom));
    }

    [Test]
    public void Parse_UnknownSignal_ReturnsFullStringAsArgument()
    {
        var (_, arg) = DialogicSignalParser.Parse("open_chest");
        Assert.That(arg, Is.EqualTo("open_chest"));
    }

    [TestCase("open_chest")]
    [TestCase("play_sound:fanfare")]
    [TestCase("quest_complete")]
    [TestCase("")]
    public void Parse_CustomSignal_PreservesOriginalString(string signal)
    {
        var (type, arg) = DialogicSignalParser.Parse(signal);
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeCustom));
        Assert.That(arg,  Is.EqualTo(signal));
    }

    // ── remove_gold: prefix ───────────────────────────────────────────────────

    [Test]
    public void Parse_RemoveGoldSignal_ReturnsTypeRemoveGold()
    {
        var (type, _) = DialogicSignalParser.Parse("remove_gold:10");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeRemoveGold));
    }

    [Test]
    public void Parse_RemoveGoldSignal_ExtractsAmount()
    {
        var (_, arg) = DialogicSignalParser.Parse("remove_gold:10");
        Assert.That(arg, Is.EqualTo("10"));
    }

    [TestCase("remove_gold:10",  "10")]
    [TestCase("remove_gold:50",  "50")]
    [TestCase("remove_gold:100", "100")]
    public void Parse_RemoveGoldSignal_ExtractsCorrectAmount(string signal, string expected)
    {
        var (_, arg) = DialogicSignalParser.Parse(signal);
        Assert.That(arg, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_RemoveGoldPrefix_WithEmptyAmount_ReturnsEmptyArgument()
    {
        var (type, arg) = DialogicSignalParser.Parse("remove_gold:");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeRemoveGold));
        Assert.That(arg,  Is.EqualTo(""));
    }

    [Test]
    public void Parse_StringContainingRemoveGoldNotAtStart_IsCustom()
    {
        var (type, _) = DialogicSignalParser.Parse("not_remove_gold:10");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeCustom));
    }

    // ── type constants ────────────────────────────────────────────────────────

    [Test]
    public void TypeConstants_AreAllDistinct()
    {
        string[] types = [
            DialogicSignalParser.TypeFlag,
            DialogicSignalParser.TypeGiveItem,
            DialogicSignalParser.TypeRemoveGold,
            DialogicSignalParser.TypeCustom,
        ];
        Assert.That(types, Is.Unique, "Each signal type constant must map to a distinct string.");
    }

    // ── no false-positive prefix matches ──────────────────────────────────────

    [Test]
    public void Parse_StringContainingFlagNotAtStart_IsCustom()
    {
        // "myflag:value" must not be treated as a flag signal
        var (type, _) = DialogicSignalParser.Parse("myflag:value");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeCustom));
    }

    [Test]
    public void Parse_StringContainingGiveItemNotAtStart_IsCustom()
    {
        var (type, _) = DialogicSignalParser.Parse("not_give_item:path");
        Assert.That(type, Is.EqualTo(DialogicSignalParser.TypeCustom));
    }
}
