using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string MacOutputDir = "Builds/macOS";
    const string WindowsOutputDir = "Builds/Windows";

    [MenuItem("Build/Build macOS")]
    public static void BuildMac()
    {
        string product = SanitizeFileName(PlayerSettings.productName);
        string output = Path.Combine(MacOutputDir, product + ".app");
        RunBuild(BuildTarget.StandaloneOSX, output);
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        string product = SanitizeFileName(PlayerSettings.productName);
        string output = Path.Combine(WindowsOutputDir, product + ".exe");
        RunBuild(BuildTarget.StandaloneWindows64, output);
    }

    static void RunBuild(BuildTarget target, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("BuildScript: no enabled scenes in Build Settings.");
            return;
        }

        string dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"BuildScript: succeeded → {outputPath} ({summary.totalSize} bytes)");
        else
            Debug.LogError($"BuildScript: failed ({summary.result})");
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "BlockXR" : name;
    }
}
