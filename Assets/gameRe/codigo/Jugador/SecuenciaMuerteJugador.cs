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
    private Material materialOriginalJugador;
    private bool secuenciaIniciada;

    private static Sprite spriteTelonCompartido;

    void Awake()
    {
        TryGetComponent(out vida);
        TryGetComponent(out jugador);
        TryGetComponent(out ataque);
        TryGetComponent(out rb);

        if (!animJugador) animJugador = GetComponentInChildren<Animator>();

        if (!spriteJugador)
        {
            if (animJugador != null && animJugador.TryGetComponent(out SpriteRenderer srAnim))
            {
                spriteJugador = srAnim;
            }
            else
            {
                TryGetComponent(out spriteJugador);
                if (!spriteJugador) spriteJugador = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (spriteJugador != null)
        {
            materialOriginalJugador = spriteJugador.sharedMaterial;
        }

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

        if (TryGetComponent(out ImpactoJugador impacto))
        {
            impacto.StopAllCoroutines();
            impacto.enabled = false;
        }

        if (TryGetComponent(out IndicadorAtaque ind))
        {
            ind.CancelarAviso();
            ind.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
        }

        SpriteRenderer[] srsHijos = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srsHijos.Length; i++)
        {
            if (srsHijos[i] != null && srsHijos[i] != spriteJugador)
            {
                srsHijos[i].enabled = false;
            }
        }

        Canvas[] todosLosCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < todosLosCanvas.Length; i++)
        {
            if (todosLosCanvas[i] != null) todosLosCanvas[i].enabled = false;
        }

        Light2D[] todasLasLuces = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        bool existeLuzGlobal = false;
        for (int i = 0; i < todasLasLuces.Length; i++)
        {
            if (todasLasLuces[i] == null) continue;

            if (todasLasLuces[i].lightType == Light2D.LightType.Global)
            {
                todasLasLuces[i].enabled = true;
                todasLasLuces[i].color = Color.white;
                todasLasLuces[i].intensity = 1f;
                existeLuzGlobal = true;
            }
            else
            {
                todasLasLuces[i].enabled = false;
            }
        }

        if (!existeLuzGlobal)
        {
            GameObject objLuzGlobal = new GameObject("LuzGlobalMuerteHades");
            Light2D nuevaGlobal = objLuzGlobal.AddComponent<Light2D>();
            nuevaGlobal.lightType = Light2D.LightType.Global;
            nuevaGlobal.color = Color.white;
            nuevaGlobal.intensity = 1f;
        }

        Time.timeScale = 0f;

        AsegurarSpriteTelon();

        int capaMasAlta = ObtenerSortingLayerMasAlta();
        CrearTelonPantalla("TelonNegroFondoHades", capaMasAlta, 30000, colorFondoMuerte);

        if (spriteJugador != null)
        {
            spriteJugador.enabled = true;
            if (materialOriginalJugador != null)
            {
                spriteJugador.sharedMaterial = materialOriginalJugador;
            }
            spriteJugador.color = Color.white;
            spriteJugador.sortingLayerID = capaMasAlta;
            spriteJugador.sortingOrder = 30005;
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

    private void AsegurarSpriteTelon()
    {
        if (spriteTelonCompartido != null) return;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] pix = new Color[16];
        for (int i = 0; i < 16; i++) pix[i] = Color.white;
        tex.SetPixels(pix);
        tex.Apply();

        spriteTelonCompartido = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
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
        obj.transform.localScale = new Vector3(500f, 500f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spriteTelonCompartido;
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
