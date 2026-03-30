using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Scans every .cs file in the project for hard-coded res:// scene paths and asserts
/// the corresponding .tscn files actually exist on disk.
///
/// This catches "Cannot open file 'res://...tscn'" runtime errors before you ever run
/// the game. No maintenance required: any new GoToAsync / ChangeSceneToFile call is
/// automatically covered the next time the tests run.
///
/// Pattern matched: any "res://..." string ending in .tscn (including sub-paths).
/// </summary>
[TestFixture]
public sealed class SceneReferenceTests
{
    // Relative path segments used to locate the project root from the test binary.
    // The test binary lives in SennenRpg.Tests/bin/<Config>/net*/
    // Walking up 4 levels lands at the repo root which contains project.godot.
    private static string? _projectRoot;

    [OneTimeSetUp]
    public void FindProjectRoot()
    {
        string dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "project.godot")))
            {
                _projectRoot = dir;
                return;
            }
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        Assert.Fail($"Could not locate project.godot — traversed up from {TestContext.CurrentContext.TestDirectory}");
    }

    /// <summary>
    /// Returns every unique res:// .tscn path found in any .cs file under the project,
    /// along with the file it was found in (for test naming / failure messages).
    /// </summary>
    private static IEnumerable<TestCaseData> AllSceneReferences()
    {
        string root = FindProjectRootStatic();
        if (root == null) yield break;

        // Exclude addons (third-party) and the test project itself.
        var csFiles = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories);
        var scenePathPattern = new Regex(@"""(res://[^""]+\.tscn)""", RegexOptions.Compiled);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (string csFile in csFiles)
        {
            // Normalise separator for comparison
            string relative = csFile.Replace('\\', '/');
            if (relative.Contains("/addons/"))          continue;
            if (relative.Contains("/SennenRpg.Tests/")) continue;
            if (relative.Contains("/tests/"))           continue; // GdUnit test data — fake paths
            if (relative.Contains("/test/"))            continue; // in-game test runner — fake paths
            if (relative.Contains("/bin/"))             continue;
            if (relative.Contains("/obj/"))             continue;

            string source;
            try { source = File.ReadAllText(csFile); }
            catch { continue; }

            foreach (Match m in scenePathPattern.Matches(source))
            {
                string scenePath = m.Groups[1].Value;
                if (!seen.Add(scenePath)) continue; // only yield each path once

                string displayName = $"{Path.GetFileName(csFile)} → {scenePath}";
                yield return new TestCaseData(root, scenePath).SetName(displayName);
            }
        }
    }

    /// <summary>Static version of root search (called from TestCaseSource before SetUp runs).</summary>
    private static string FindProjectRootStatic()
    {
        string dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "project.godot"))) return dir;
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return string.Empty;
    }

    [TestCaseSource(nameof(AllSceneReferences))]
    public void SceneFile_Exists(string projectRoot, string resPath)
    {
        // Convert res://foo/bar.tscn → <projectRoot>/foo/bar.tscn
        string relative = resPath.Replace("res://", "").Replace('/', Path.DirectorySeparatorChar);
        string absolute = Path.Combine(projectRoot, relative);

        Assert.That(File.Exists(absolute), Is.True,
            $"Scene file not found on disk: {resPath}\n" +
            $"Expected at: {absolute}\n" +
            "Add the file or remove the reference from your C# code.");
    }
}
