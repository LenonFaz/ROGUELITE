using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Vida))]
public class SecuenciaMuerteJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SpriteRenderer spriteJugador;
    [SerializeField] private Animator animJugador;
    [SerializeField] private AnimationClip clipMuerte;

    [Header("Tiempos Estilo Hades")]
    [SerializeField, Min(0f)] private float pausaInicialAntesAnim = 0.25f;
    [SerializeField, Min(0.1f)] private float duracionExtraPostAnim = 0.6f;
    [SerializeField, Min(0.1f)] private float duracionFadeFinalNegro = 0.85f;
    [SerializeField] private Color colorFondoMuerte = Color.black;

    [Header("Transición de Escena")]
    [SerializeField] private string nombreEscenaAlMorir;

    private Vida vida;
    private Jugador jugador;
    private AtaqueJugador ataque;
    private Rigidbody2D rb;
    private bool secuenciaIniciada;

    private static Texture2D texBlanca;
    private static Material matUnlit;

    void Awake()
    {
        TryGetComponent(out vida);
        TryGetComponent(out jugador);
        TryGetComponent(out ataque);
        TryGetComponent(out rb);

        if (!spriteJugador) spriteJugador = GetComponentInChildren<SpriteRenderer>();
        if (!animJugador) animJugador = GetComponentInChildren<Animator>();

        vida.EstablecerConservarAlMorir(true);
        vida.AlMorir.AddListener(IniciarSecuenciaMuerte);
    }

    void Start()
    {
        if (GestorProgresoSalas.IntentarObtenerCheckpointEnEscenaActual(out Vector3 posCheckpoint))
        {
            transform.position = posCheckpoint;
            if (rb != null) rb.position = posCheckpoint;
        }
    }

    private void IniciarSecuenciaMuerte()
    {
        if (secuenciaIniciada) return;
        secuenciaIniciada = true;

        StartCoroutine(RutinaMuerteHades());
    }

    private IEnumerator RutinaMuerteHades()
    {
        if (jugador != null) jugador.enabled = false;
        if (ataque != null) ataque.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
        }

        Canvas[] todosLosCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < todosLosCanvas.Length; i++)
        {
            if (todosLosCanvas[i] != null) todosLosCanvas[i].enabled = false;
        }

        Light2D[] todasLasLuces = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        for (int i = 0; i < todasLasLuces.Length; i++)
        {
            if (todasLasLuces[i] != null)
            {
                if (todasLasLuces[i].lightType == Light2D.LightType.Global)
                {
                    todasLasLuces[i].color = Color.white;
                    todasLasLuces[i].intensity = 1f;
                }
                else
                {
                    todasLasLuces[i].enabled = false;
                }
            }
        }

        Time.timeScale = 0f;

        AsegurarRecursosVisuales();

        int capaMasAlta = ObtenerSortingLayerMasAlta();
        SpriteRenderer telonFondo = CrearTelonPantalla("TelonNegroFondoHades", capaMasAlta, 30000, colorFondoMuerte);

        if (spriteJugador != null)
        {
            spriteJugador.sortingLayerID = capaMasAlta;
            spriteJugador.sortingOrder = 30005;
            spriteJugador.color = Color.white;
            if (matUnlit != null) spriteJugador.sharedMaterial = matUnlit;
        }

        float tPausa = pausaInicialAntesAnim;
        while (tPausa > 0f)
        {
            tPausa -= Time.unscaledDeltaTime;
            yield return null;
        }

        float duracionAnim = 1.2f;
        if (animJugador != null)
        {
            animJugador.enabled = true;
            animJugador.updateMode = AnimatorUpdateMode.UnscaledTime;

            if (clipMuerte != null)
            {
                animJugador.Play(clipMuerte.name, -1, 0f);
                duracionAnim = clipMuerte.length;
            }
        }

        float tEspera = duracionAnim + duracionExtraPostAnim;
        while (tEspera > 0f)
        {
            tEspera -= Time.unscaledDeltaTime;
            yield return null;
        }

        Color colorInicialFade = new Color(0f, 0f, 0f, 0f);
        SpriteRenderer telonFrente = CrearTelonPantalla("TelonFadeFinalHades", capaMasAlta, 30010, colorInicialFade);

        float tFade = 0f;
        float dFade = Mathf.Max(0.1f, duracionFadeFinalNegro);
        while (tFade < dFade)
        {
            tFade += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(tFade / dFade);
            telonFrente.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nombreEscenaAlMorir))
        {
            SceneManager.LoadScene(nombreEscenaAlMorir);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void AsegurarRecursosVisuales()
    {
        if (texBlanca == null)
        {
            texBlanca = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texBlanca.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
            texBlanca.Apply();
        }

        if (matUnlit == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) matUnlit = new Material(s);
        }
    }

    private int ObtenerSortingLayerMasAlta()
    {
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
        {
            return layers[layers.Length - 1].id;
        }
        return spriteJugador != null ? spriteJugador.sortingLayerID : 0;
    }

    private SpriteRenderer CrearTelonPantalla(string nombre, int sortingLayerId, int sortingOrder, Color colorInicial)
    {
        GameObject obj = new GameObject(nombre);
        Vector3 posCentro = Camera.main != null ? Camera.main.transform.position : transform.position;
        posCentro.z = 0f;
        obj.transform.position = posCentro;
        obj.transform.localScale = new Vector3(400f, 400f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texBlanca, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        if (matUnlit != null) sr.sharedMaterial = matUnlit;
        sr.sortingLayerID = sortingLayerId;
        sr.sortingOrder = sortingOrder;
        sr.color = colorInicial;

        return sr;
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
