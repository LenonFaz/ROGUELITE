using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float duracion = 0.5f;

    private bool transicionando;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;
    }

    private void Start()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    public void CargarEscena(string nombre)
    {
        if (!transicionando)
            StartCoroutine(Transicion(nombre));
    }

    private IEnumerator Transicion(string nombre)
    {
        transicionando = true;

        yield return Fade(0f, 1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(nombre);
        while (!op.isDone) yield return null;

        fadeGroup.alpha = 1f;
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.1f);

        yield return Fade(1f, 0f);

        transicionando = false;
    }

    private IEnumerator Fade(float desde, float hasta)
    {
        fadeGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < duracion)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            fadeGroup.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        fadeGroup.alpha = hasta;
        fadeGroup.blocksRaycasts = hasta > 0.5f;
    }
}