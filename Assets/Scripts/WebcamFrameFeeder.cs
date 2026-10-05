using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Optional: Unity captures webcam frames to a JPEG for Python.
/// On current macOS + Unity 6 this often never triggers a TCC prompt, so
/// CvBackendLauncher prefers the Python OpenCV path instead.
/// </summary>
public class WebcamFrameFeeder : MonoBehaviour
{
    public string FramePath { get; private set; }
    public bool HasFrame { get; private set; }
    public string Status { get; private set; } = "webcam: starting";

    WebCamTexture _webcam;
    Texture2D _scratch;
    float _nextWrite;
    const float WriteInterval = 0.2f;

    public static WebcamFrameFeeder Ensure(GameObject host, string framePath)
    {
        var feeder = host.GetComponent<WebcamFrameFeeder>();
        if (feeder == null)
            feeder = host.AddComponent<WebcamFrameFeeder>();
        feeder.FramePath = framePath;
        return feeder;
    }

    void SetStatus(string msg)
    {
        Status = msg;
        Debug.Log("WebcamFrameFeeder: " + msg);
    }

    public IEnumerator StartFeeding(float timeoutSeconds = 8f)
    {
        if (string.IsNullOrEmpty(FramePath))
        {
            SetStatus("webcam: no frame path");
            yield break;
        }

        Screen.fullScreen = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        yield return null;

        // Skip Application.RequestUserAuthorization — it hangs/no-ops on macOS.
        string deviceName = null;
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices != null && devices.Length > 0)
        {
            deviceName = devices[0].name;
            for (int i = 0; i < devices.Length; i++)
            {
                if (!devices[i].isFrontFacing)
                {
                    deviceName = devices[i].name;
                    break;
                }
            }
        }

        SetStatus(
            deviceName != null
                ? "webcam: Play " + deviceName
                : "webcam: Play default");

        try
        {
            _webcam = string.IsNullOrEmpty(deviceName)
                ? new WebCamTexture(1280, 720, 30)
                : new WebCamTexture(deviceName, 1280, 720, 30);
            _webcam.Play();
        }
        catch (System.Exception ex)
        {
            SetStatus("webcam: Play() threw — " + ex.Message);
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (_webcam != null && !_webcam.didUpdateThisFrame && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (_webcam == null || !_webcam.isPlaying || _webcam.width < 16 || !_webcam.didUpdateThisFrame)
        {
            SetStatus("webcam: Unity capture unavailable");
            if (_webcam != null)
            {
                if (_webcam.isPlaying)
                    _webcam.Stop();
                Destroy(_webcam);
                _webcam = null;
            }
            yield break;
        }

        SetStatus("webcam: feeding frames (" + _webcam.width + "x" + _webcam.height + ")");
        HasFrame = false;
        _nextWrite = 0f;
    }

    void Update()
    {
        if (_webcam == null || !_webcam.isPlaying || !_webcam.didUpdateThisFrame)
            return;
        if (Time.realtimeSinceStartup < _nextWrite)
            return;
        _nextWrite = Time.realtimeSinceStartup + WriteInterval;

        try
        {
            int w = _webcam.width;
            int h = _webcam.height;
            if (w < 16 || h < 16)
                return;

            if (_scratch == null || _scratch.width != w || _scratch.height != h)
            {
                if (_scratch != null)
                    Destroy(_scratch);
                _scratch = new Texture2D(w, h, TextureFormat.RGB24, false);
            }

            _scratch.SetPixels32(_webcam.GetPixels32());
            _scratch.Apply(false, false);

            string dir = Path.GetDirectoryName(FramePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string tmp = FramePath + ".tmp";
            File.WriteAllBytes(tmp, _scratch.EncodeToJPG(85));
            if (File.Exists(FramePath))
                File.Delete(FramePath);
            File.Move(tmp, FramePath);
            HasFrame = true;
        }
        catch (System.Exception ex)
        {
            SetStatus("webcam: write failed — " + ex.Message);
        }
    }

    void OnDestroy()
    {
        if (_webcam != null)
        {
            if (_webcam.isPlaying)
                _webcam.Stop();
            Destroy(_webcam);
            _webcam = null;
        }
        if (_scratch != null)
        {
            Destroy(_scratch);
            _scratch = null;
        }
    }
}
