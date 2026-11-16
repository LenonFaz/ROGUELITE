using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PortalSalida : MonoBehaviour
{
    [Header("Configuración de Escena Final")]
    [SerializeField] private string nombreSiguienteEscena;
    [SerializeField] private int indiceSiguienteEscena = -1;

    [Header("Visuales del Portal del Jefe")]
    [SerializeField] private SpriteRenderer spritePortal;
    [SerializeField] private Sprite spriteApagado;
    [SerializeField] private Sprite spriteEncendido;
    [SerializeField] private Animator animPortal;
    [SerializeField] private AnimationClip clipPortalApagado;
    [SerializeField] private AnimationClip clipPortalEncendido;

    [Header("Luz del Portal (URP 2D)")]
    [SerializeField] private Light2D luzPortal;
    [SerializeField, Min(0f)] private float intensidadLuzEncendido = 2f;
    [SerializeField, Min(0f)] private float intensidadLuzAlViajar = 3.8f;
    [SerializeField, Min(0.05f)] private float velocidadTransicionLuz = 8f;

    [Header("Icono Tecla Espacio (Debajo del Portal)")]
    [SerializeField] private GameObject objetoIconoEspacio;
    [SerializeField] private Vector2 offsetIconoDebajo = new Vector2(0f, -0.85f);

    [Header("Interacción y Secuencia del Jugador")]
    [SerializeField, Min(0.2f)] private float radioDeteccionJugador = 0.85f;
    [SerializeField] private Transform puntoCentroPortal;
    [SerializeField, Min(0.5f)] private float velocidadCentradoJugador = 4.5f;
    [SerializeField] private AnimationClip clipJugadorTeletransporte;
    [SerializeField, Min(0.1f)] private float duracionExtraAntesDeCambiar = 0.35f;
    [SerializeField] private bool hacerFadeNegroAlFinal = true;
    [SerializeField, Min(0.1f)] private float duracionFadeNegro = 0.45f;

    private Collider2D colPortal;
    private bool portalEncendido;
    private bool jugadorEncima;
    private bool secuenciaEnCurso;
    private float intensidadObjetivoLuz;
    private static Sprite spriteTelonCompartido;

    void Awake()
    {
        TryGetComponent(out colPortal);
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }

        if (!spritePortal) TryGetComponent(out spritePortal);
        if (!animPortal) TryGetComponent(out animPortal);
        if (!luzPortal) luzPortal = GetComponentInChildren<Light2D>();

        if (objetoIconoEspacio != null)
        {
            objetoIconoEspacio.transform.position = transform.position + (Vector3)offsetIconoDebajo;
            objetoIconoEspacio.SetActive(false);
        }

        AplicarEstadoVisual(false, true);
    }

    public void EncenderPortal()
    {
        if (portalEncendido) return;
        gameObject.SetActive(true);
        AplicarEstadoVisual(true, false);
    }

    void Update()
    {
        ActualizarSuavizadoLuz();

        if (!portalEncendido || secuenciaEnCurso) return;

        jugadorEncima = ComprobarJugadorEncima();
        ActualizarIconoEspacio();

        if (jugadorEncima && PresionoTeclaActivar())
        {
            StartCoroutine(RutinaViajarPorPortal());
        }
    }

    private bool ComprobarJugadorEncima()
    {
        Jugador jug = Jugador.Instancia;
        if (jug == null) return false;

        Vector2 posJug = jug.transform.position;

        if (colPortal != null && colPortal.enabled && colPortal.OverlapPoint(posJug))
        {
            return true;
        }

        return Vector2.Distance(transform.position, posJug) <= radioDeteccionJugador;
    }

    private bool PresionoTeclaActivar()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
        {
            return true;
        }

        var gp = Gamepad.current;
        if (gp != null && gp.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private void ActualizarSuavizadoLuz()
    {
        if (luzPortal == null || luzPortal.lightType == Light2D.LightType.Global) return;

        luzPortal.intensity = Mathf.MoveTowards(luzPortal.intensity, intensidadObjetivoLuz, velocidadTransicionLuz * Time.deltaTime);
        luzPortal.enabled = luzPortal.intensity > 0.01f;
    }

    private void AplicarEstadoVisual(bool encendido, bool instantaneo)
    {
        portalEncendido = encendido;

        if (animPortal != null && (clipPortalEncendido != null || clipPortalApagado != null))
        {
            AnimationClip clip = encendido ? clipPortalEncendido : clipPortalApagado;
            if (clip != null)
            {
                animPortal.enabled = true;
                animPortal.Play(clip.name, -1, 0f);
            }
        }
        else if (spritePortal != null)
        {
            if (animPortal != null) animPortal.enabled = false;
            Sprite spr = encendido ? spriteEncendido : spriteApagado;
            if (spr != null) spritePortal.sprite = spr;
        }

        intensidadObjetivoLuz = encendido ? intensidadLuzEncendido : 0f;
        if (instantaneo && luzPortal != null && luzPortal.lightType != Light2D.LightType.Global)
        {
            luzPortal.intensity = intensidadObjetivoLuz;
            luzPortal.enabled = encendido;
        }

        ActualizarIconoEspacio();
    }

    private void ActualizarIconoEspacio()
    {
        if (objetoIconoEspacio == null) return;
        objetoIconoEspacio.transform.position = transform.position + (Vector3)offsetIconoDebajo;
        objetoIconoEspacio.SetActive(portalEncendido && jugadorEncima && !secuenciaEnCurso);
    }

    private IEnumerator RutinaViajarPorPortal()
    {
        secuenciaEnCurso = true;
        ActualizarIconoEspacio();

        Jugador jug = Jugador.Instancia;
        if (jug == null)
        {
            CargarSiguienteEscena();
            yield break;
        }

        AtaqueJugador atkJug = jug.GetComponent<AtaqueJugador>();
        Rigidbody2D rbJug = jug.GetComponent<Rigidbody2D>();
        Animator animJug = jug.GetComponentInChildren<Animator>();
        PlayerInput pInput = jug.GetComponent<PlayerInput>();

        if (pInput != null) pInput.DeactivateInput();
        if (atkJug != null) atkJug.EstablecerBloqueoExterno(true);
        jug.EstablecerEstadoEncadenado(true, null, null);
        jug.enabled = false;

        if (rbJug != null) rbJug.linearVelocity = Vector2.zero;

        Vector3 destinoCentro = puntoCentroPortal != null ? puntoCentroPortal.position : transform.position;
        destinoCentro.z = jug.transform.position.z;

        while (Vector2.Distance(jug.transform.position, destinoCentro) > 0.02f)
        {
            Vector3 nuevaPos = Vector3.MoveTowards(jug.transform.position, destinoCentro, velocidadCentradoJugador * Time.deltaTime);
            jug.transform.position = nuevaPos;
            if (rbJug != null) rbJug.position = nuevaPos;
            yield return null;
        }

        jug.transform.position = destinoCentro;
        if (rbJug != null) rbJug.position = destinoCentro;

        intensidadObjetivoLuz = intensidadLuzAlViajar;

        float duracionAnim = 0.8f;
        if (clipJugadorTeletransporte != null && animJug != null)
        {
            animJug.enabled = true;
            animJug.Play(clipJugadorTeletransporte.name, -1, 0f);
            duracionAnim = clipJugadorTeletransporte.length;
        }

        yield return new WaitForSeconds(duracionAnim + duracionExtraAntesDeCambiar);

        if (hacerFadeNegroAlFinal)
        {
            SpriteRenderer telon = CrearTelonFadeNegro();
            float t = 0f;
            float d = Mathf.Max(0.05f, duracionFadeNegro);
            while (t < d)
            {
                t += Time.deltaTime;
                telon.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / d));
                yield return null;
            }
        }

        CargarSiguienteEscena();
    }

    private SpriteRenderer CrearTelonFadeNegro()
    {
        if (spriteTelonCompartido == null)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pix = new Color[16];
            for (int i = 0; i < 16; i++) pix[i] = Color.white;
            tex.SetPixels(pix);
            tex.Apply();
            spriteTelonCompartido = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        GameObject obj = new GameObject("FadePortalSalida");
        Vector3 posCam = Camera.main != null ? Camera.main.transform.position : transform.position;
        posCam.z = 0f;
        obj.transform.position = posCam;
        obj.transform.localScale = new Vector3(500f, 500f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spriteTelonCompartido;

        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
        {
            sr.sortingLayerID = layers[layers.Length - 1].id;
        }
        sr.sortingOrder = 30010;
        sr.color = new Color(0f, 0f, 0f, 0f);

        return sr;
    }

    private void CargarSiguienteEscena()
    {
        GestorProgresoSalas.ReiniciarTodoElProgreso();

        if (!string.IsNullOrEmpty(nombreSiguienteEscena))
        {
            SceneManager.LoadScene(nombreSiguienteEscena);
        }
        else if (indiceSiguienteEscena >= 0)
        {
            SceneManager.LoadScene(indiceSiguienteEscena);
        }
        else
        {
            int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
            if (siguiente < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(siguiente);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeteccionJugador);

        Gizmos.color = Color.cyan;
        Vector3 posIcono = transform.position + (Vector3)offsetIconoDebajo;
        Gizmos.DrawWireCube(posIcono, new Vector3(0.8f, 0.35f, 0.1f));

        Vector3 centro = puntoCentroPortal != null ? puntoCentroPortal.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centro, 0.15f);
    }
}
