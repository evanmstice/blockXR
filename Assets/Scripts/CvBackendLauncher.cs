using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

/// <summary>
/// Starts the Python CV/OSC backend (main.py) when the game launches and
/// stops it on quit.
/// </summary>
public class CvBackendLauncher : MonoBehaviour
{
    static Process _process;
    static bool _bootstrapped;
    static readonly StringBuilder _stderr = new StringBuilder();
    static readonly StringBuilder _recentOut = new StringBuilder();

    string _status = "CV backend: starting...";
    Color _statusColor = Color.yellow;
    float _statusHideAt = -1f; // realtimeSinceStartup when green status should clear
    bool _statusHiddenByUser;
    string _pythonHint;
    bool _didPromptForFolder;
    bool _retryAfterPrompt;
    bool _pendingFolderPrompt;
    bool _restoreFullscreenWhenReady;
    string _framePath;

    InputAction _quitAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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

        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _quitAction = new InputAction("Quit", InputActionType.Button);
        _quitAction.AddBinding("<Keyboard>/escape");
        _quitAction.performed += _ => QuitApp();
        _quitAction.Enable();

        StartCoroutine(BootBackend());
    }

    System.Collections.IEnumerator BootBackend()
    {
        yield return null;
        PrepareProjectAccess();

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        _restoreFullscreenWhenReady = true;
        Screen.fullScreen = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        yield return null;

        SetStatus("Starting camera via Python — click Allow if prompted...", Color.yellow);
        StartBackend(null);
#else
        SetStatus("Starting CV backend...", Color.yellow);
        StartBackend(null);
#endif
        yield break;
    }

    void PrepareProjectAccess()
    {
        string root = FindRepoRoot();
        string mainPy = root != null ? Path.Combine(root, "main.py") : null;
        if (mainPy != null && CanReadFile(mainPy))
            return;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        SetStatus("Select your blockXR project folder (contains main.py)...", Color.yellow);
        if (TryPromptForProjectFolder())
            return;
        SetStatus("Folder permission cancelled — cannot start CV backend", Color.red);
#else
        SetStatus("main.py not readable — keep Builds/ inside the project folder", Color.red);
#endif
    }

    static bool CanReadFile(string path)
    {
        try
        {
            using (File.OpenRead(path))
                return true;
        }
        catch
        {
            return false;
        }
    }

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    bool TryPromptForProjectFolder()
    {
        if (_didPromptForFolder)
            return false;
        _didPromptForFolder = true;

        // System folder dialog must not sit behind exclusive fullscreen.
        bool full = Screen.fullScreen;
        Screen.fullScreen = false;

        string chosen = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments =
                    "-e \"POSIX path of (choose folder with prompt " +
                    "\\\"Select your blockXR project folder (the one that contains main.py)\\\" " +
                    "default location (path to home folder))\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var p = Process.Start(psi))
            {
                chosen = p.StandardOutput.ReadToEnd().Trim().TrimEnd('/');
                p.WaitForExit(300000);
                if (p.ExitCode != 0)
                    chosen = null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CvBackendLauncher: folder prompt failed — " + ex.Message);
            chosen = null;
        }

        Screen.fullScreen = full;
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

        if (string.IsNullOrEmpty(chosen))
            return false;

        string mainPy = Path.Combine(chosen, "main.py");
        if (!File.Exists(mainPy))
        {
            SetStatus("That folder has no main.py — pick the blockXR project root", Color.red);
            return false;
        }

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavedProjectRootPath(), chosen);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CvBackendLauncher: could not save project path — " + ex.Message);
        }

        Debug.Log("CvBackendLauncher: project folder granted → " + chosen);
        return true;
    }
#endif

    static string SavedProjectRootPath() =>
        Path.Combine(Application.persistentDataPath, "project_root.txt");

    void OnEnable()
    {
        if (_quitAction != null)
            _quitAction.Enable();
    }

    void OnDisable()
    {
        if (_quitAction != null)
            _quitAction.Disable();
    }

    static void QuitApp()
    {
        StopBackend();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Update()
    {
        if (_statusHideAt > 0f && Time.realtimeSinceStartup >= _statusHideAt)
        {
            _status = null;
            _statusHideAt = -1f;
        }

        if (_retryAfterPrompt)
        {
            _retryAfterPrompt = false;
            StartBackend(_framePath);
        }

        if (_process != null && _process.HasExited)
        {
            string detail = FirstNonEmpty(
                LastLines(_recentOut, 240),
                LastLines(_stderr, 240),
                "exit code " + _process.ExitCode);

            bool accessDenied = detail.Contains("Operation not permitted") ||
                                detail.Contains("can't open file");
            bool cameraDenied = detail.Contains("camera:") ||
                                detail.Contains("CAMERA") ||
                                detail.Contains("not authorized to capture video");
            int code = _process.ExitCode;
            _process.Dispose();
            _process = null;
            _restoreFullscreenWhenReady = false;

            if (accessDenied)
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                SetStatus("macOS blocked file access — pick your project folder...", Color.yellow);
                if (!_didPromptForFolder && TryPromptForProjectFolder())
                {
                    _retryAfterPrompt = true;
                    return;
                }
#endif
                SetStatus(
                    "macOS blocked project files. Choose the project folder when prompted, or enable Files and Folders → Documents for blockXR",
                    Color.red);
                return;
            }

            if (cameraDenied)
            {
                SetStatus(
                    "Camera blocked. System Settings → Privacy & Security → Camera → enable blockXR, then relaunch",
                    Color.red);
                return;
            }

            SetStatus("CV backend died: " + detail + " (code " + code + ")", Color.red);
        }
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(_status))
            return;

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        float width = Mathf.Min(1000f, Screen.width - 40f);
        float height = _statusColor == Color.red ? 110f : 64f;
        var rect = new Rect((Screen.width - width) * 0.5f, 20f, width, height);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = _statusColor;
        GUI.Box(rect, _status, style);
        GUI.backgroundColor = prev;

        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
        };
        float btnW = 72f;
        float btnH = 28f;
        var btnRect = new Rect(rect.xMax - btnW - 10f, rect.y + 8f, btnW, btnH);
        if (GUI.Button(btnRect, "Hide", btnStyle))
            HideStatus();
    }

    void HideStatus()
    {
        _status = null;
        _statusHideAt = -1f;
        _statusHiddenByUser = true;
    }

    void OnApplicationQuit()
    {
        StopBackend();
    }

    void OnDestroy()
    {
        if (_quitAction != null)
        {
            _quitAction.Disable();
            _quitAction.Dispose();
            _quitAction = null;
        }
        StopBackend();
#if UNITY_EDITOR
        _bootstrapped = false;
#endif
    }

    void StartBackend(string framePath)
    {
        _framePath = framePath;

        if (_process != null && !_process.HasExited)
        {
            SetStatus("CV backend already running", Color.green);
            return;
        }

        string repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            SetStatus("main.py not found — keep Builds/ inside the project folder", Color.red);
            return;
        }

        WarnIfWrongOsVenv(repoRoot);

        string mainPy = Path.Combine(repoRoot, "main.py");
        string python;
        string pythonPath;
        try
        {
            if (!ResolvePythonLaunch(repoRoot, out python, out pythonPath, out _pythonHint))
            {
                SetStatus("no Python found — pip install -r requirements.txt", Color.red);
                return;
            }
        }
        catch (System.UnauthorizedAccessException ex)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (!_didPromptForFolder)
            {
                SetStatus("macOS blocked file access — pick your project folder...", Color.yellow);
                if (TryPromptForProjectFolder())
                {
                    _retryAfterPrompt = true;
                    return;
                }
            }
#endif
            SetStatus(
                "macOS blocked project files. Choose the folder when prompted, or enable Files and Folders → Documents for blockXR",
                Color.red);
            Debug.LogError("CvBackendLauncher: " + ex);
            return;
        }
        catch (System.Exception ex)
        {
            SetStatus("Python resolve failed — " + ex.Message, Color.red);
            Debug.LogError("CvBackendLauncher: " + ex);
            return;
        }

        _stderr.Clear();
        _recentOut.Clear();
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

        if (!string.IsNullOrEmpty(pythonPath))
            startInfo.EnvironmentVariables["PYTHONPATH"] = pythonPath;

        if (startInfo.EnvironmentVariables.ContainsKey("__PYVENV_LAUNCHER__"))
            startInfo.EnvironmentVariables.Remove("__PYVENV_LAUNCHER__");

        // Force Python stdout/stderr unbuffered so Unity sees CV_BACKEND_* lines promptly.
        startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

        if (!string.IsNullOrEmpty(framePath))
            startInfo.EnvironmentVariables["BLOCKXR_FRAME_PATH"] = framePath;

        string debugPath = Path.Combine(repoRoot, ".blockxr_yolo_debug.jpg");
        startInfo.EnvironmentVariables["BLOCKXR_DEBUG_FRAME"] = debugPath;
        CvYoloDebugView.Ensure(gameObject, debugPath);

        // Drop stale backends that keep the webcam LED on / hold OSC 31415.
        KillStrayBackends(repoRoot);

        try
        {
            _process = Process.Start(startInfo);
            if (_process == null)
            {
                SetStatus("Process.Start returned null", Color.red);
                return;
            }

            _process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                Debug.Log("[CV] " + e.Data);
                AppendRecent(e.Data);
                HandleBackendLine(e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                Debug.LogWarning("[CV] " + e.Data);
                if (_stderr.Length < 4000)
                    _stderr.AppendLine(e.Data);
                if (e.Data.Contains("CV_BACKEND_ERROR") || e.Data.Contains("Permission") ||
                    e.Data.Contains("NSCamera") || e.Data.Contains("TCC"))
                    HandleBackendLine(e.Data);
            };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            SetStatus("starting main.py (" + _pythonHint + ")...", Color.yellow);
            Debug.Log($"CvBackendLauncher: started `{python} {mainPy}` PYTHONPATH={pythonPath} FRAME={framePath}");
        }
        catch (System.Exception ex)
        {
            SetStatus("failed to start — " + ex.Message, Color.red);
            Debug.LogError("CvBackendLauncher: could not start backend.\n" + ex);
            _process = null;
        }
    }

    void HandleBackendLine(string line)
    {
        lock (_lineQueue)
        {
            _lineQueue.Enqueue(line);
        }
    }

    readonly System.Collections.Generic.Queue<string> _lineQueue = new System.Collections.Generic.Queue<string>();

    void LateUpdate()
    {
        while (true)
        {
            string line;
            lock (_lineQueue)
            {
                if (_lineQueue.Count == 0)
                    break;
                line = _lineQueue.Dequeue();
            }

            if (line.StartsWith("CV_BACKEND_STARTING"))
                SetStatus("main.py started — requesting camera...", Color.yellow);
            else if (line.StartsWith("CV_BACKEND_FRAME_SOURCE"))
                SetStatus("Python reading Unity webcam frames...", Color.yellow);
            else if (line.StartsWith("CV_BACKEND_CAMERA_PROMPT"))
                SetStatus("Allow camera access in the macOS popup...", Color.yellow);
            else if (line.StartsWith("CV_BACKEND_CAMERA_GRANTED"))
            {
                SetStatus("camera granted — opening webcam...", Color.yellow);
                MaybeRestoreFullscreen();
            }
            else if (line.StartsWith("CV_BACKEND_CAMERA_OK"))
                SetStatus("webcam / frames OK — starting OSC...", Color.yellow);
            else if (line.StartsWith("CV_BACKEND_READY"))
            {
                SetStatus("main.py running (camera + OSC ready)", Color.green, hideAfterSeconds: 2.5f);
                MaybeRestoreFullscreen();
            }
            else if (line.Contains("not authorized to capture video") ||
                     line.Contains("camera failed to properly") ||
                     line.Contains("camera access has been denied") ||
                     line.Contains("camera: denied") ||
                     line.Contains("user denied the prompt"))
            {
                MaybeRestoreFullscreen();
                SetStatus(
                    "Camera denied. System Settings → Privacy & Security → Camera → enable blockXR, then relaunch",
                    Color.red);
            }
            else if (line.Contains("CV_BACKEND_ERROR") || line.Contains("Operation not permitted") ||
                     line.Contains("NSCameraUsageDescription") || line.Contains("TCC") ||
                     line.Contains("can't open file"))
            {
                if (line.Contains("Operation not permitted") || line.Contains("can't open file"))
                {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                    if (!_didPromptForFolder)
                    {
                        SetStatus("macOS blocked file access — pick your project folder...", Color.yellow);
                        _pendingFolderPrompt = true;
                        break;
                    }
#endif
                    SetStatus(
                        "macOS blocked project files. Choose the folder when prompted, or enable Files and Folders → Documents for blockXR",
                        Color.red);
                }
                else
                    SetStatus(line, Color.red);
            }
        }

        if (_pendingFolderPrompt)
        {
            _pendingFolderPrompt = false;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (TryPromptForProjectFolder())
                _retryAfterPrompt = true;
            else
                SetStatus("Folder permission cancelled — cannot start CV backend", Color.red);
#endif
        }
    }

    void MaybeRestoreFullscreen()
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        if (!_restoreFullscreenWhenReady)
            return;
        _restoreFullscreenWhenReady = false;
        Screen.fullScreen = true;
#endif
    }

    static void AppendRecent(string line)
    {
        lock (_recentOut)
        {
            if (_recentOut.Length > 4000)
                _recentOut.Remove(0, _recentOut.Length - 2000);
            _recentOut.AppendLine(line);
        }
    }

    static string LastLines(StringBuilder sb, int maxChars)
    {
        string text;
        lock (sb)
            text = sb.ToString().Trim();
        if (string.IsNullOrEmpty(text))
            return null;
        if (text.Length > maxChars)
            text = text.Substring(text.Length - maxChars);
        return text;
    }

    static string FirstNonEmpty(params string[] parts)
    {
        foreach (string p in parts)
        {
            if (!string.IsNullOrEmpty(p))
                return p;
        }
        return "";
    }

    void SetStatus(string message, Color color, float hideAfterSeconds = -1f)
    {
        // After Hide, skip info/progress banners; still show errors.
        bool isError = color.r > 0.9f && color.g < 0.35f && color.b < 0.35f;
        if (_statusHiddenByUser && !isError)
            return;
        if (isError)
            _statusHiddenByUser = false;

        _status = message;
        _statusColor = color;
        _statusHideAt = hideAfterSeconds > 0f
            ? Time.realtimeSinceStartup + hideAfterSeconds
            : -1f;
    }

    static void StopBackend()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill();
                    _process.WaitForExit(3000);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("CvBackendLauncher: error killing backend: " + ex.Message);
                }
            }
        }
        finally
        {
            if (_process != null)
            {
                try { _process.Dispose(); } catch { /* ignore */ }
                _process = null;
            }

            // Always clear orphans — Escape/quit previously left Python holding the camera.
            KillStrayBackends(FindRepoRoot());
            Debug.Log("CvBackendLauncher: backend stopped.");
        }
    }

    static void KillStrayBackends(string repoRoot)
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        try
        {
            var script = new StringBuilder();
            script.Append("pids=$(lsof -tiUDP:31415 -tiTCP:31415 2>/dev/null); ");
            script.Append("if [ -n \"$pids\" ]; then kill -9 $pids 2>/dev/null; fi; ");
            if (!string.IsNullOrEmpty(repoRoot))
            {
                string mainPy = Path.Combine(repoRoot, "main.py").Replace("'", "");
                script.Append("pkill -9 -f '");
                script.Append(mainPy);
                script.Append("' 2>/dev/null || true");
            }
            RunQuiet("/bin/bash", "-lc " + Quote(script.ToString()));
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CvBackendLauncher: stray backend cleanup failed — " + ex.Message);
        }
#endif
    }

    static void RunQuiet(string fileName, string arguments)
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
            if (p == null)
                return;
            p.WaitForExit(5000);
        }
    }

    static string FindRepoRoot()
    {
        try
        {
            string saved = SavedProjectRootPath();
            if (File.Exists(saved))
            {
                string root = File.ReadAllText(saved).Trim();
                if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, "main.py")))
                    return root;
            }
        }
        catch
        {
            // ignore bad saved path
        }

        var dir = new DirectoryInfo(Application.dataPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "main.py")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static void WarnIfWrongOsVenv(string repoRoot)
    {
#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
        string windowsPython = Path.Combine(repoRoot, "venv", "Scripts", "python.exe");
        if (File.Exists(windowsPython))
        {
            Debug.LogWarning(
                "CvBackendLauncher: found a Windows venv (venv/Scripts/python.exe) on macOS — ignoring it.");
        }
#endif
    }

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    /// <summary>
    /// OpenCV runs inside Homebrew's Python.app. Without NSCameraUsageDescription on
    /// that app, macOS refuses camera access and shows no prompt.
    /// </summary>
    static void EnsurePythonCameraUsageDescription(string pythonExe)
    {
        const string description =
            "blockXR uses the platform's mounted webcam to detect the blocks you place down";

        try
        {
            string plistPath = FindPythonAppInfoPlist(pythonExe);
            if (plistPath == null || !File.Exists(plistPath))
            {
                Debug.LogWarning("CvBackendLauncher: could not find Python.app Info.plist for " + pythonExe);
                return;
            }

            string xml = File.ReadAllText(plistPath);
            if (xml.Contains("NSCameraUsageDescription"))
                return;

            // Insert before the closing </dict></plist>
            const string keyXml =
                "\t<key>NSCameraUsageDescription</key>\n" +
                "\t<string>" + description + "</string>\n";
            int insertAt = xml.LastIndexOf("</dict>", System.StringComparison.Ordinal);
            if (insertAt < 0)
                return;

            xml = xml.Insert(insertAt, keyXml);
            File.WriteAllText(plistPath, xml);
            Debug.Log("CvBackendLauncher: added NSCameraUsageDescription to " + plistPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CvBackendLauncher: could not patch Python.app Info.plist — " + ex.Message);
        }
    }

    static string FindPythonAppInfoPlist(string pythonExe)
    {
        string[] known =
        {
            "/opt/homebrew/Cellar/python@3.13/3.13.1/Frameworks/Python.framework/Versions/3.13/Resources/Python.app/Contents/Info.plist",
            "/usr/local/Cellar/python@3.13/3.13.1/Frameworks/Python.framework/Versions/3.13/Resources/Python.app/Contents/Info.plist",
            "/Library/Frameworks/Python.framework/Versions/3.13/Resources/Python.app/Contents/Info.plist",
            "/Library/Frameworks/Python.framework/Versions/3.12/Resources/Python.app/Contents/Info.plist",
        };
        foreach (string k in known)
        {
            if (File.Exists(k))
                return k;
        }

        string path = ResolveSymlink(pythonExe) ?? pythonExe;

        var dir = new DirectoryInfo(Path.GetDirectoryName(path) ?? path);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Resources", "Python.app", "Contents", "Info.plist");
            if (File.Exists(candidate))
                return candidate;
            if (dir.Name == "MacOS" && dir.Parent != null && dir.Parent.Name == "Contents")
            {
                string plist = Path.Combine(dir.Parent.FullName, "Info.plist");
                if (File.Exists(plist))
                    return plist;
            }
            dir = dir.Parent;
        }
        return null;
    }

    static string ResolveSymlink(string path)
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/python3",
                Arguments = "-c \"import os,sys; print(os.path.realpath(sys.argv[1]))\" " + Quote(path),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);
                if (p.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    return output;
            }
        }
        catch
        {
            // fall through
        }
#endif
        return path;
    }
#endif

    static bool ResolvePythonLaunch(
        string repoRoot,
        out string python,
        out string pythonPath,
        out string hint)
    {
        python = null;
        pythonPath = null;
        hint = null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string[] winCandidates =
        {
            Path.Combine(repoRoot, ".venv", "Scripts", "python.exe"),
            Path.Combine(repoRoot, "venv", "Scripts", "python.exe"),
        };
        foreach (string path in winCandidates)
        {
            if (File.Exists(path))
            {
                python = path;
                hint = path;
                return true;
            }
        }
        python = "python";
        hint = "python (PATH)";
        return true;
#else
        string[] venvRoots =
        {
            Path.Combine(repoRoot, ".venv"),
            Path.Combine(repoRoot, "venv"),
        };

        foreach (string venvRoot in venvRoots)
        {
            if (!Directory.Exists(venvRoot))
                continue;

            if (File.Exists(Path.Combine(venvRoot, "Scripts", "python.exe")) &&
                !Directory.Exists(Path.Combine(venvRoot, "bin")))
                continue;

            string sitePackages = FindSitePackages(venvRoot);
            string basePython = null;
            if (!string.IsNullOrEmpty(sitePackages))
                basePython = PythonForSitePackages(sitePackages);
            if (string.IsNullOrEmpty(basePython) || !File.Exists(basePython))
                basePython = ReadVenvExecutable(venvRoot);
            if (string.IsNullOrEmpty(basePython) || !File.Exists(basePython))
                basePython = FindSystemPython();

            if (!string.IsNullOrEmpty(basePython) && !string.IsNullOrEmpty(sitePackages))
            {
                python = basePython;
                pythonPath = sitePackages;
                hint = Path.GetFileName(basePython) + " + venv packages";
                return true;
            }

            string shim = Path.Combine(venvRoot, "bin", "python");
            if (File.Exists(shim))
            {
                python = shim;
                hint = shim;
                return true;
            }
        }

        string fallback = FindSystemPython();
        if (!string.IsNullOrEmpty(fallback))
        {
            python = fallback;
            hint = fallback;
            return true;
        }

        python = "python3";
        hint = "python3 (PATH)";
        return true;
#endif
    }

#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
    static string FindSitePackages(string venvRoot)
    {
        // Do NOT Directory.GetDirectories() here — macOS TCC often denies listing
        // Documents/.venv/lib from a Unity player. Probe known paths instead.
        string[] candidates =
        {
            Path.Combine(venvRoot, "lib", "python3.13", "site-packages"),
            Path.Combine(venvRoot, "lib", "python3.12", "site-packages"),
            Path.Combine(venvRoot, "lib", "python3.11", "site-packages"),
            Path.Combine(venvRoot, "lib", "python3.10", "site-packages"),
        };

        string fallback = null;
        foreach (string sp in candidates)
        {
            try
            {
                if (!Directory.Exists(sp))
                    continue;
                if (Directory.Exists(Path.Combine(sp, "ultralytics")))
                    return sp;
                fallback ??= sp;
            }
            catch (System.UnauthorizedAccessException)
            {
                // Keep trying other candidates; caller may still show a TCC message.
            }
        }
        return fallback;
    }

    static string PythonForSitePackages(string sitePackages)
    {
        string versionDir = Path.GetFileName(Path.GetDirectoryName(sitePackages));
        if (!string.IsNullOrEmpty(versionDir) && versionDir.StartsWith("python"))
        {
            string ver = versionDir.Substring("python".Length);
            string[] versioned =
            {
                "/opt/homebrew/bin/python" + ver,
                "/opt/homebrew/opt/python@" + ver + "/bin/python" + ver,
                "/usr/local/bin/python" + ver,
                "/Library/Frameworks/Python.framework/Versions/" + ver + "/bin/python" + ver,
                "/Library/Frameworks/Python.framework/Versions/" + ver + "/bin/python3",
            };
            foreach (string path in versioned)
            {
                if (File.Exists(path))
                    return path;
            }
        }
        return null;
    }

    static string ReadVenvExecutable(string venvRoot)
    {
        string cfg = Path.Combine(venvRoot, "pyvenv.cfg");
        if (!File.Exists(cfg))
            return null;

        try
        {
            string executable = null;
            string home = null;
            foreach (string raw in File.ReadAllLines(cfg))
            {
                string line = raw.Trim();
                if (line.StartsWith("executable", System.StringComparison.OrdinalIgnoreCase))
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                        executable = line.Substring(eq + 1).Trim();
                }
                else if (line.StartsWith("home", System.StringComparison.OrdinalIgnoreCase))
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                        home = line.Substring(eq + 1).Trim();
                }
            }

            if (!string.IsNullOrEmpty(executable) && File.Exists(executable))
                return executable;

            if (!string.IsNullOrEmpty(home))
            {
                string[] names = { "python3", "python3.13", "python3.12", "python" };
                foreach (string name in names)
                {
                    string candidate = Path.Combine(home, name);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("CvBackendLauncher: could not read pyvenv.cfg: " + ex.Message);
        }

        return null;
    }

    static string FindSystemPython()
    {
        string[] fallbacks =
        {
            "/opt/homebrew/bin/python3.13",
            "/opt/homebrew/bin/python3",
            "/Library/Frameworks/Python.framework/Versions/3.12/bin/python3.12",
            "/Library/Frameworks/Python.framework/Versions/3.12/bin/python3",
            "/usr/local/bin/python3",
            "/usr/bin/python3",
        };
        foreach (string path in fallbacks)
        {
            if (File.Exists(path))
                return path;
        }
        return null;
    }
#endif

    static string Quote(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.Contains(" "))
            return "\"" + path + "\"";
        return path;
    }
}
