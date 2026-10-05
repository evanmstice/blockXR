#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// After a macOS build: write NSCameraUsageDescription, add the camera
/// entitlement, and ad-hoc re-sign so TCC sees a sealed Info.plist.
/// Without re-signing, macOS silently denies camera (no prompt, no Settings row).
/// </summary>
public static class MacCameraUsagePostBuild
{
    const string Description = "blockXR uses the platform's mounted webcam to detect the blocks you place down";

    const string EntitlementsXml =
        @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>com.apple.security.device.camera</key>
	<true/>
	<key>com.apple.security.get-task-allow</key>
	<true/>
</dict>
</plist>
";

    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.StandaloneOSX)
            return;

        if (Application.platform != RuntimePlatform.OSXEditor)
            return;

        string appPath = pathToBuiltProject;
        if (!appPath.EndsWith(".app"))
        {
            // Some build layouts pass the .app; others pass a folder containing it.
            string nested = Path.Combine(pathToBuiltProject, "blockXR.app");
            if (Directory.Exists(nested))
                appPath = nested;
        }

        string plistPath = Path.Combine(appPath, "Contents", "Info.plist");
        if (!File.Exists(plistPath))
        {
            Debug.LogWarning("MacCameraUsagePostBuild: Info.plist not found at " + plistPath);
            return;
        }

        try
        {
            EnsureCameraUsageDescription(plistPath);
            string entitlementsPath = WriteEntitlementsFile(appPath);
            ResignApp(appPath, entitlementsPath);
            Debug.Log("MacCameraUsagePostBuild: camera usage + entitlement + re-sign OK → " + appPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("MacCameraUsagePostBuild: failed — " + ex.Message);
        }
    }

    static void EnsureCameraUsageDescription(string plistPath)
    {
        RunTool("/usr/bin/plutil", "-convert xml1 " + Quote(plistPath));

        var doc = new XmlDocument();
        doc.Load(plistPath);

        XmlNode dict = doc.SelectSingleNode("/plist/dict");
        if (dict == null)
            throw new System.Exception("no root dict in Info.plist");

        XmlNode existingKey = null;
        foreach (XmlNode child in dict.ChildNodes)
        {
            if (child.Name == "key" && child.InnerText == "NSCameraUsageDescription")
            {
                existingKey = child;
                break;
            }
        }

        if (existingKey != null)
        {
            XmlNode valueNode = existingKey.NextSibling;
            while (valueNode != null && valueNode.NodeType != XmlNodeType.Element)
                valueNode = valueNode.NextSibling;
            if (valueNode != null && valueNode.Name == "string")
                valueNode.InnerText = Description;
        }
        else
        {
            XmlElement key = doc.CreateElement("key");
            key.InnerText = "NSCameraUsageDescription";
            XmlElement value = doc.CreateElement("string");
            value.InnerText = Description;
            dict.AppendChild(key);
            dict.AppendChild(value);
        }

        doc.Save(plistPath);

        string xml = File.ReadAllText(plistPath);
        xml = xml.Replace(
            "http://www.apple.com/DTDs/PropertyList-1.0.dtd\"[]>",
            "http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
        File.WriteAllText(plistPath, xml);
    }

    static string WriteEntitlementsFile(string appPath)
    {
        string path = Path.Combine(Path.GetTempPath(), "blockxr-camera.entitlements");
        File.WriteAllText(path, EntitlementsXml, Encoding.UTF8);
        return path;
    }

    static void ResignApp(string appPath, string entitlementsPath)
    {
        // Seal Info.plist + entitlements. Ad-hoc (-) is fine for local runs.
        // Do not use --deep: Unity already signs nested dylibs; re-signing the
        // outer app is enough to bind Info.plist for TCC.
        RunTool(
            "/usr/bin/codesign",
            "--force --sign - --entitlements " + Quote(entitlementsPath) + " " + Quote(appPath));
    }

    static void RunTool(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using (var p = Process.Start(psi))
        {
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(60000);
            if (p.ExitCode != 0)
                throw new System.Exception(fileName + " failed: " + stderr + stdout);
        }
    }

    static string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";
}
#endif
