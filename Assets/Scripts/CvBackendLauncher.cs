using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Starts the Python CV/OSC backend (main.py) when the game launches and
/// stops it on quit. Prefers a local venv interpreter if present; otherwise PATH.
/// </summary>

public class CvBackendLauncher : MonoBehaviour
{
    static Process _process;
    static bool _bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (_bootstrapped) return;
        _bootstrapped = true;

        var go = new GameObject("CvBackendLauncher");
        DontDestroyOnLoad(go);
        go.AddComponent<CvBackendLauncher>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartBackend();
    }

    void OnApplicationQuit()
    {
        StopBackend();
    }

    void OnDestroy()
    {
        StopBackend();
#if UNITY_EDITOR
        _bootstrapped = false;
#endif
    }

    static void StartBackend()
    {
        if (_process != null && !_process.HasExited)
            return;

        string repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            Debug.LogError(
                "CvBackendLauncher: could not find main.py. " +
                "Keep Builds/ inside the project clone, or run from the Editor.");
            return;
        }

        string mainPy = Path.Combine(repoRoot, "main.py");
        string python = FindPython(repoRoot);

        var startInfo = new ProcessStartInfo
        {
            FileName = python,
            Arguments = Quote(mainPy),
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                Debug.LogError("CvBackendLauncher: failed to start Python process.");
                return;
            }

            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log("[CV] " + e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogWarning("[CV] " + e.Data);
            };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Debug.Log($"CvBackendLauncher: started `{python} {mainPy}`");
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                "CvBackendLauncher: could not start backend. " +
                "Install deps with: pip install -r requirements.txt\n" + ex);
            _process = null;
        }
    }

    static void StopBackend()
    {
        if (_process == null)
            return;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit(3000);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CvBackendLauncher: error stopping backend: " + ex.Message);
        }
        finally
        {
            _process.Dispose();
            _process = null;
            Debug.Log("CvBackendLauncher: backend stopped.");
        }
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Application.dataPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "main.py")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static string FindPython(string repoRoot)
    {
        string venvUnix = Path.Combine(repoRoot, "venv", "bin", "python");
        string venvWin = Path.Combine(repoRoot, "venv", "Scripts", "python.exe");

        if (File.Exists(venvUnix))
            return venvUnix;
        if (File.Exists(venvWin))
            return venvWin;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        return "python3";
#else
        return "python";
#endif
    }

    static string Quote(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.Contains(" "))
            return "\"" + path + "\"";
        return path;
    }
}
