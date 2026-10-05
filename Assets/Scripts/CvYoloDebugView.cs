using System.IO;
using UnityEngine;

/// <summary>
/// Bottom-left toggle that shows Python's YOLO-annotated webcam JPEG.
/// </summary>
public class CvYoloDebugView : MonoBehaviour
{
    string _debugPath;
    bool _open;
    Texture2D _tex;
    float _nextReload;
    long _lastWriteTicks;

    const float PanelWidth = 360f;
    const float PanelHeight = 270f;
    const float Margin = 12f;
    const float ButtonWidth = 110f;
    const float ButtonHeight = 32f;

    public static CvYoloDebugView Ensure(GameObject host, string debugPath)
    {
        var view = host.GetComponent<CvYoloDebugView>();
        if (view == null)
            view = host.AddComponent<CvYoloDebugView>();
        view._debugPath = debugPath;
        return view;
    }

    void OnGUI()
    {
        float btnX = Margin;
        float btnY = Screen.height - Margin - ButtonHeight;
        var btnRect = new Rect(btnX, btnY, ButtonWidth, ButtonHeight);

        string label = _open ? "Hide CV" : "CV Debug";
        if (GUI.Button(btnRect, label))
            _open = !_open;

        if (!_open)
            return;

        float panelY = btnY - Margin - PanelHeight;
        var panel = new Rect(Margin, panelY, PanelWidth, PanelHeight);
        GUI.Box(panel, GUIContent.none);

        TryReloadTexture();

        var content = new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, panel.height - 12f);
        if (_tex != null)
            GUI.DrawTexture(content, _tex, ScaleMode.ScaleToFit, false);
        else
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            GUI.Label(content, "Waiting for YOLO frames…", style);
        }
    }

    void TryReloadTexture()
    {
        if (string.IsNullOrEmpty(_debugPath) || !File.Exists(_debugPath))
            return;
        if (Time.unscaledTime < _nextReload)
            return;
        _nextReload = Time.unscaledTime + 0.15f;

        try
        {
            long ticks = File.GetLastWriteTimeUtc(_debugPath).Ticks;
            if (ticks == _lastWriteTicks && _tex != null)
                return;
            _lastWriteTicks = ticks;

            byte[] bytes = File.ReadAllBytes(_debugPath);
            if (bytes == null || bytes.Length < 32)
                return;

            if (_tex == null)
                _tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            _tex.LoadImage(bytes, false);
        }
        catch
        {
            // File may be mid-write; try again next tick.
        }
    }

    void OnDestroy()
    {
        if (_tex != null)
        {
            Destroy(_tex);
            _tex = null;
        }
    }
}
