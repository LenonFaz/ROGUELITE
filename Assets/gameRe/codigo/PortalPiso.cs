using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PortalPiso : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nombreSiguienteEscena;
    [SerializeField] private int indiceSiguienteEscena = -1;
    [SerializeField] private bool requiereActivacionExterna = false;
    [SerializeField] private bool apagarAlSalir = true;

    [Header("Visuales del Portal")]
    [SerializeField] private SpriteRenderer spritePortal;
    [SerializeField] private Sprite spriteApagado;
    [SerializeField] private Sprite spriteEncendido;
    [SerializeField] private Animator animPortal;
    [SerializeField] private AnimationClip clipPortalApagado;
    [SerializeField] private AnimationClip clipPortalEncendido;

    [Header("Luz del Portal (URP 2D)")]
    [SerializeField] private Light2D luzPortal;
    [SerializeField, Min(0f)] private float intensidadLuzEncendido = 1.8f;
    [SerializeField, Min(0f)] private float intensidadLuzAlViajar = 3.5f;
    [SerializeField, Min(0.05f)] private float velocidadTransicionLuz = 8f;

    [Header("Icono Tecla Espacio (Debajo del Portal)")]
    [SerializeField] private GameObject objetoIconoEspacio;
    [SerializeField] private Vector2 offsetIconoDebajo = new Vector2(0f, -0.85f);

    [Header("Secuencia del Jugador al Presionar Espacio")]
    [SerializeField] private Transform puntoCentroPortal;
    [SerializeField, Min(0.5f)] private float velocidadCentradoJugador = 4.5f;
    [SerializeField] private AnimationClip clipJugadorTeletransporte;
    [SerializeField, Min(0.1f)] private float duracionExtraAntesDeCambiar = 0.35f;
    [SerializeField] private bool hacerFadeNegroAlFinal = true;
    [SerializeField, Min(0.1f)] private float duracionFadeNegro = 0.45f;

    private bool habilitadoParaUsar;
    private bool portalEncendido;
    private bool jugadorEncima;
    private bool secuenciaEnCurso;
    private float intensidadObjetivoLuz;

    private Jugador jugadorRef;
    private static Sprite spriteTelonCompartido;

    void Awake()
    {
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

        habilitadoParaUsar = !requiereActivacionExterna;
        EstablecerEstadoVisualPortal(false, true);
    }

    public void DesbloquearPortalExternamente()
    {
        habilitadoParaUsar = true;
        if (jugadorEncima && !portalEncendido)
        {
            EstablecerEstadoVisualPortal(true, false);
        }
    }

    void Update()
    {
        ActualizarSuavizadoLuz();

        if (!habilitadoParaUsar || !jugadorEncima || !portalEncendido || secuenciaEnCurso) return;

        if (PresionoEspacio())
        {
            StartCoroutine(RutinaViajarPorPortal());
        }
    }

    private bool PresionoEspacio()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

        var gp = Gamepad.current;
        if (gp != null && gp.buttonSouth.wasPressedThisFrame) return true;

        return false;
    }

    private void ActualizarSuavizadoLuz()
    {
        if (luzPortal == null || luzPortal.lightType == Light2D.LightType.Global) return;

        luzPortal.intensity = Mathf.MoveTowards(luzPortal.intensity, intensidadObjetivoLuz, velocidadTransicionLuz * Time.deltaTime);
        luzPortal.enabled = luzPortal.intensity > 0.01f;
    }

    private void EstablecerEstadoVisualPortal(bool encendido, bool instantaneo)
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

        if (objetoIconoEspacio != null)
        {
            objetoIconoEspacio.transform.position = transform.position + (Vector3)offsetIconoDebajo;
            objetoIconoEspacio.SetActive(encendido && !secuenciaEnCurso);
        }
    }

    private IEnumerator RutinaViajarPorPortal()
    {
        secuenciaEnCurso = true;

        if (objetoIconoEspacio != null)
        {
            objetoIconoEspacio.SetActive(false);
        }

        if (jugadorRef == null) jugadorRef = Jugador.Instancia;
        if (jugadorRef == null)
        {
            CargarSiguienteEscena();
            yield break;
        }

        AtaqueJugador atkJug = jugadorRef.GetComponent<AtaqueJugador>();
        Rigidbody2D rbJug = jugadorRef.GetComponent<Rigidbody2D>();
        Animator animJug = jugadorRef.GetComponentInChildren<Animator>();

        if (atkJug != null) atkJug.EstablecerBloqueoExterno(true);
        jugadorRef.EstablecerEstadoEncadenado(true, null, null);
        jugadorRef.enabled = false;

        if (rbJug != null)
        {
            rbJug.linearVelocity = Vector2.zero;
        }

        Vector3 destinoCentro = puntoCentroPortal != null ? puntoCentroPortal.position : transform.position;
        destinoCentro.z = jugadorRef.transform.position.z;

        while (Vector2.Distance(jugadorRef.transform.position, destinoCentro) > 0.02f)
        {
            Vector3 nuevaPos = Vector3.MoveTowards(jugadorRef.transform.position, destinoCentro, velocidadCentradoJugador * Time.deltaTime);
            jugadorRef.transform.position = nuevaPos;
            if (rbJug != null) rbJug.position = nuevaPos;
            yield return null;
        }

        jugadorRef.transform.position = destinoCentro;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (secuenciaEnCurso || !other.CompareTag("Player")) return;

        jugadorEncima = true;
        if (!other.TryGetComponent(out jugadorRef))
        {
            jugadorRef = Jugador.Instancia;
        }

        if (habilitadoParaUsar)
        {
            EstablecerEstadoVisualPortal(true, false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (secuenciaEnCurso || !other.CompareTag("Player")) return;

        jugadorEncima = false;

        if (apagarAlSalir)
        {
            EstablecerEstadoVisualPortal(false, false);
        }
        else if (objetoIconoEspacio != null)
        {
            objetoIconoEspacio.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 posIcono = transform.position + (Vector3)offsetIconoDebajo;
        Gizmos.DrawWireCube(posIcono, new Vector3(0.8f, 0.35f, 0.1f));

        Vector3 centro = puntoCentroPortal != null ? puntoCentroPortal.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centro, 0.15f);
    }
}
