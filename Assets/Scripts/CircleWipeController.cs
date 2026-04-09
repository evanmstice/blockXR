using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CircleWipeController : MonoBehaviour
{
    public static CircleWipeController Instance;

    [SerializeField] private Material wipeMaterial;
    [SerializeField] private Canvas canvas;

    private void Awake()
{
    if (Instance == null) {
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        wipeMaterial.SetFloat("_Progress", 0f);
    } else {
        Destroy(gameObject);
    }
    canvas = GetComponent<Canvas>();
}

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (canvas != null) {
            canvas.enabled = false;
            canvas.enabled = true;
        }
    }

    public IEnumerator WipeOut()
    {
        yield return StartCoroutine(AnimateWipe(0f, 1f));
    }

    public IEnumerator WipeIn()
    {
        yield return StartCoroutine(AnimateWipe(1f, 0f));
    }

    private IEnumerator AnimateWipe(float from, float to, float duration = 0.6f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            wipeMaterial.SetFloat("_Progress", Mathf.Lerp(from, to, t));
            yield return null;
        }
        wipeMaterial.SetFloat("_Progress", to);
    }
}