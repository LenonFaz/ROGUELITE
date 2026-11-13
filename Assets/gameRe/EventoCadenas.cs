using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class EventoCadenas : MonoBehaviour
{
    [Serializable]
    public class CadenaFisica
    {
        public string nombre;
        public Transform puntoAnclajeEscena;
        public Vector2 offsetAnclajePorDefecto;
        public Vector2 offsetEnJugador;

        [HideInInspector] public Vector2[] nodosActuales;
        [HideInInspector] public Vector2[] nodosPrevios;
        [HideInInspector] public float longitudSegmento;
        [HideInInspector] public bool activa;
        [HideInInspector] public bool debilitada;
        [HideInInspector] public float timerLatigazo;
    }

    [Serializable]
    public class LineaDialogo
    {
        public string nombreHablante;
        public Sprite ilustracionHablante;
        [TextArea(2, 4)] public string texto;
    }

    [Header("1. Cinemática Previa y Diálogo")]
    [SerializeField] private bool activarCinematicaPrevia = true;
    [SerializeField] private Transform npcTransform;
    [SerializeField] private Vector2 offsetEntradaArriba = new Vector2(0f, 7f);
    [SerializeField] private Vector2 offsetParadaFrenteJugador = new Vector2(0f, 2.2f);
    [SerializeField, Min(0.1f)] private float velocidadMovimientoNPC = 5f;
    [SerializeField] private Animator animNPC;
    [SerializeField] private AnimationClip clipNPCMover;
    [SerializeField] private AnimationClip clipNPCHablar;

    [Header("UI de Diálogo e Ilustración")]
    [SerializeField] private GameObject panelDialogo;
    [SerializeField] private Image imagenRetratoUI;
    [SerializeField] private TMP_Text textoNombreUI;
    [SerializeField] private TMP_Text textoContenidoUI;
    [SerializeField, Min(0.005f)] private float tiempoPorLetra = 0.03f;
    [SerializeField] private LineaDialogo[] lineasDialogo = new LineaDialogo[]
    {
        new LineaDialogo { nombreHablante = "???", texto = "..." },
        new LineaDialogo { nombreHablante = "Protagonista", texto = "..." },
        new LineaDialogo { nombreHablante = "???", texto = "..." }
    };

    [Header("2. Sprites Animados de Tutorial (Aseprite)")]
    [SerializeField] private GameObject objetoAvisoMover;
    [SerializeField] private GameObject objetoAvisoAtacar;
    [SerializeField] private GameObject objetoAvisoDash;
    [SerializeField, Min(0.1f)] private float duracionFadeAviso = 1.2f;
    [SerializeField, Min(0.1f)] private float tiempoVisibleAviso = 2.4f;
    [SerializeField, Min(0f)] private float pausaEntreAvisos = 0.35f;

    [Header("3. Cadenas Pixel Art y Física Verlet")]
    [SerializeField, Min(8)] private int pixelesPorUnidad = 32;
    [SerializeField, Range(1, 3)] private int grosorEnPixeles = 1;
    [SerializeField, Min(4)] private int segmentosPorCadena = 14;
    [SerializeField, Min(1)] private int iteracionesRigidez = 10;
    [SerializeField] private Color colorCadenaIntacta = new Color(0.85f, 0.1f, 0.15f, 1f);
    [SerializeField] private Color colorCadenaDebilitada = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private Vector2 gravedadCadena = new Vector2(0f, -9.5f);
    [SerializeField, Range(0.8f, 0.99f)] private float amortiguacion = 0.94f;
    [SerializeField, Range(0.95f, 1.4f)] private float factorHolguraInicial = 1.06f;
    [SerializeField, Range(1.05f, 1.6f)] private float factorHolguraDebilitada = 1.22f;
    [SerializeField] private int sortingOrderCadenas = 15;

    [Header("Las 9 Cadenas y sus Anclajes")]
    [SerializeField] private CadenaFisica[] cadenas = new CadenaFisica[9]
    {
        new CadenaFisica { nombre = "0. Mov Arriba (W)", offsetAnclajePorDefecto = new Vector2(-0.6f, 2.2f), offsetEnJugador = new Vector2(-0.1f, 0.2f) },
        new CadenaFisica { nombre = "1. Mov Abajo (S)", offsetAnclajePorDefecto = new Vector2(-0.6f, -2.2f), offsetEnJugador = new Vector2(-0.1f, -0.2f) },
        new CadenaFisica { nombre = "2. Mov Izquierda (A)", offsetAnclajePorDefecto = new Vector2(-2.5f, 0.6f), offsetEnJugador = new Vector2(-0.2f, 0.1f) },
        new CadenaFisica { nombre = "3. Mov Derecha (D)", offsetAnclajePorDefecto = new Vector2(2.5f, 0.6f), offsetEnJugador = new Vector2(0.2f, 0.1f) },
        new CadenaFisica { nombre = "4. Atk Arriba (Flecha Arriba)", offsetAnclajePorDefecto = new Vector2(0.6f, 2.2f), offsetEnJugador = new Vector2(0.1f, 0.2f) },
        new CadenaFisica { nombre = "5. Atk Abajo (Flecha Abajo)", offsetAnclajePorDefecto = new Vector2(0.6f, -2.2f), offsetEnJugador = new Vector2(0.1f, -0.2f) },
        new CadenaFisica { nombre = "6. Atk Izquierda (Flecha Izq)", offsetAnclajePorDefecto = new Vector2(-2.5f, -0.6f), offsetEnJugador = new Vector2(-0.2f, -0.1f) },
        new CadenaFisica { nombre = "7. Atk Derecha (Flecha Der)", offsetAnclajePorDefecto = new Vector2(2.5f, -0.6f), offsetEnJugador = new Vector2(0.2f, -0.1f) },
        new CadenaFisica { nombre = "8. Dash Final (Espacio)", offsetAnclajePorDefecto = new Vector2(0f, 2.8f), offsetEnJugador = Vector2.zero }
    };

    [Header("4. Configuración del QTE y Jugador")]
    [SerializeField, Min(1)] private int pulsosPorDireccion = 2;
    [SerializeField] private bool centrarJugadorEnTrigger = false;
    [SerializeField] private bool impulsarConDashAlLiberarse = true;
    [SerializeField] private Sprite spriteJugadorEncadenado;
    [SerializeField] private AnimationClip animJugadorEncadenado;
    [SerializeField, Min(0f)] private float distanciaTiron = 0.25f;
    [SerializeField, Min(0.05f)] private float duracionTiron = 0.16f;
    [SerializeField, Min(0f)] private float intensidadVibracion = 0.04f;

    [Header("Partículas al Romper")]
    [SerializeField] private bool emitirParticulas = true;

    [Header("Eventos (Opcional)")]
    public UnityEvent AlIniciarCinematica;
    public UnityEvent AlIniciarQTECadenas;
    public UnityEvent AlDebilitarUnaCadena;
    public UnityEvent AlRomperUnaCadena;
    public UnityEvent AlLiberarsePorCompleto;

    private bool yaSeActivo;
    private bool cadenasSimulando;
    private bool qteActivo;
    private bool avisoDashMostrado;

    private readonly int[] progresoMov = new int[4];
    private readonly int[] progresoAtk = new int[4];
    private int cadenasDireccionalesRotas;

    private Jugador jugador;
    private AtaqueJugador ataqueJugador;
    private Rigidbody2D rbJugador;
    private SpriteRenderer srJugador;
    private PlayerInput playerInput;
    private InputAction accionMover;
    private InputAction accionAtacar;
    private InputAction accionDash;

    private Vector3 posicionAnclada;
    private Vector2Int ultimaDirMovLeida;
    private Vector2Int ultimaDirAtkLeida;
    private bool esperandoSoltarDash;

    private GameObject objLienzoPixel;
    private SpriteRenderer srLienzoPixel;
    private Texture2D texturaPixel;
    private Color32[] bufferLimpieza;
    private Color32[] bufferTrabajo;
    private int anchoTex;
    private int altoTex;
    private Vector2 esquinaMinLienzo;

    private Coroutine rutinaForcejeo;
    private Coroutine rutinaTutorialVisual;
    private static Material materialCompartido;

    void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }

        AsegurarMaterial();

        if (panelDialogo != null) panelDialogo.SetActive(false);
        if (npcTransform != null) npcTransform.gameObject.SetActive(false);

        ConfigurarAlphaObjeto(objetoAvisoMover, 0f, false);
        ConfigurarAlphaObjeto(objetoAvisoAtacar, 0f, false);
        ConfigurarAlphaObjeto(objetoAvisoDash, 0f, false);
    }

    private void AsegurarMaterial()
    {
        if (materialCompartido != null) return;
        Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s != null) materialCompartido = new Material(s);
    }

    private void InicializarLienzoPixelArt()
    {
        float maxDist = 4f;
        Vector2 centro = posicionAnclada;

        if (cadenas != null)
        {
            for (int i = 0; i < cadenas.Length; i++)
            {
                if (cadenas[i] == null) continue;
                Vector2 anclaje = ObtenerPosicionAnclajeEscena(cadenas[i]);
                float dx = Mathf.Abs(anclaje.x - centro.x) + 1.5f;
                float dy = Mathf.Abs(anclaje.y - centro.y) + 1.5f;
                if (dx > maxDist) maxDist = dx;
                if (dy > maxDist) maxDist = dy;
            }
        }

        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int mitadAnchoUnidades = Mathf.CeilToInt(maxDist);
        anchoTex = mitadAnchoUnidades * 2 * ppu;
        altoTex = mitadAnchoUnidades * 2 * ppu;

        float centroAlineadoX = Mathf.Round(centro.x * ppu) / ppu;
        float centroAlineadoY = Mathf.Round(centro.y * ppu) / ppu;
        esquinaMinLienzo = new Vector2(centroAlineadoX - mitadAnchoUnidades, centroAlineadoY - mitadAnchoUnidades);

        if (texturaPixel != null) Destroy(texturaPixel);

        texturaPixel = new Texture2D(anchoTex, altoTex, TextureFormat.RGBA32, false);
        texturaPixel.filterMode = FilterMode.Point;
        texturaPixel.wrapMode = TextureWrapMode.Clamp;

        bufferLimpieza = new Color32[anchoTex * altoTex];
        bufferTrabajo = new Color32[anchoTex * altoTex];
        texturaPixel.SetPixels32(bufferLimpieza);
        texturaPixel.Apply(false);

        if (objLienzoPixel == null)
        {
            objLienzoPixel = new GameObject("LienzoPixelCadenas");
            objLienzoPixel.transform.SetParent(transform, false);
            srLienzoPixel = objLienzoPixel.AddComponent<SpriteRenderer>();
            if (materialCompartido != null) srLienzoPixel.sharedMaterial = materialCompartido;
        }

        objLienzoPixel.transform.position = new Vector3(centroAlineadoX, centroAlineadoY, 0f);
        if (srJugador != null) srLienzoPixel.sortingLayerID = srJugador.sortingLayerID;
        srLienzoPixel.sortingOrder = sortingOrderCadenas;

        Sprite spr = Sprite.Create(texturaPixel, new Rect(0, 0, anchoTex, altoTex), new Vector2(0.5f, 0.5f), ppu);
        srLienzoPixel.sprite = spr;
        srLienzoPixel.enabled = true;
    }

    private void InicializarCadenasFisicas()
    {
        InicializarLienzoPixelArt();

        if (cadenas == null) return;
        int numNodos = Mathf.Max(4, segmentosPorCadena);

        for (int i = 0; i < cadenas.Length; i++)
        {
            CadenaFisica c = cadenas[i];
            if (c == null) continue;

            c.nodosActuales = new Vector2[numNodos];
            c.nodosPrevios = new Vector2[numNodos];

            Vector2 inicio = ObtenerPosicionAnclajeEscena(c);
            Vector2 fin = (Vector2)jugador.transform.position + c.offsetEnJugador;

            float distTotal = Vector2.Distance(inicio, fin) * factorHolguraInicial;
            c.longitudSegmento = distTotal / (numNodos - 1);

            for (int n = 0; n < numNodos; n++)
            {
                float t = n / (float)(numNodos - 1);
                Vector2 p = Vector2.Lerp(inicio, fin, t);
                c.nodosActuales[n] = p;
                c.nodosPrevios[n] = p;
            }

            c.activa = true;
            c.debilitada = false;
            c.timerLatigazo = 0f;
        }

        cadenasSimulando = true;
    }

    private Vector2 ObtenerPosicionAnclajeEscena(CadenaFisica c)
    {
        if (c.puntoAnclajeEscena != null)
        {
            return c.puntoAnclajeEscena.position;
        }
        Vector2 basePos = Application.isPlaying && yaSeActivo ? (Vector2)posicionAnclada : (Vector2)transform.position;
        return basePos + c.offsetAnclajePorDefecto;
    }

    private void ApagarTodasLasCadenas()
    {
        cadenasSimulando = false;
        if (srLienzoPixel != null) srLienzoPixel.enabled = false;

        if (cadenas == null) return;
        for (int i = 0; i < cadenas.Length; i++)
        {
            if (cadenas[i] != null) cadenas[i].activa = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (yaSeActivo || !other.CompareTag("Player")) return;

        if (!other.TryGetComponent(out jugador))
        {
            jugador = Jugador.Instancia;
        }

        if (jugador == null) return;

        yaSeActivo = true;
        StartCoroutine(SecuenciaCompleta());
    }

    private IEnumerator SecuenciaCompleta()
    {
        BloquearYEncadenarJugador();

        if (activarCinematicaPrevia)
        {
            AlIniciarCinematica?.Invoke();
            yield return StartCoroutine(RutinaCinematicaYDialogo());
        }

        IniciarFaseQTE();
    }

    private void BloquearYEncadenarJugador()
    {
        cadenasDireccionalesRotas = 0;
        avisoDashMostrado = false;

        for (int i = 0; i < 4; i++)
        {
            progresoMov[i] = 0;
            progresoAtk[i] = 0;
        }

        jugador.TryGetComponent(out rbJugador);
        srJugador = jugador.GetComponentInChildren<SpriteRenderer>();

        if (centrarJugadorEnTrigger)
        {
            posicionAnclada = transform.position;
            jugador.transform.position = posicionAnclada;
            if (rbJugador != null) rbJugador.position = posicionAnclada;
        }
        else
        {
            posicionAnclada = jugador.transform.position;
        }

        jugador.EstablecerEstadoEncadenado(true, spriteJugadorEncadenado, animJugadorEncadenado);

        if (jugador.TryGetComponent(out ataqueJugador))
        {
            ataqueJugador.EstablecerBloqueoExterno(true);
        }

        if (jugador.TryGetComponent(out playerInput) && playerInput.actions != null)
        {
            accionMover = playerInput.actions.FindAction("Move");
            accionAtacar = playerInput.actions.FindAction("Look");
            accionDash = playerInput.actions.FindAction("Dash");
        }

        InicializarCadenasFisicas();
    }

    private IEnumerator RutinaCinematicaYDialogo()
    {
        Vector3 posInicioNPC = posicionAnclada + (Vector3)offsetEntradaArriba;
        Vector3 posParadaNPC = posicionAnclada + (Vector3)offsetParadaFrenteJugador;

        if (npcTransform != null)
        {
            npcTransform.position = posInicioNPC;
            npcTransform.gameObject.SetActive(true);
            ReproducirAnimNPC(clipNPCMover);

            while (Vector3.Distance(npcTransform.position, posParadaNPC) > 0.03f)
            {
                npcTransform.position = Vector3.MoveTowards(npcTransform.position, posParadaNPC, velocidadMovimientoNPC * Time.deltaTime);
                yield return null;
            }

            npcTransform.position = posParadaNPC;
            ReproducirAnimNPC(clipNPCHablar);
        }

        if (lineasDialogo != null && lineasDialogo.Length > 0 && panelDialogo != null)
        {
            panelDialogo.SetActive(true);

            for (int i = 0; i < lineasDialogo.Length; i++)
            {
                LineaDialogo actual = lineasDialogo[i];

                if (textoNombreUI != null) textoNombreUI.text = actual.nombreHablante;
                if (textoContenidoUI != null) textoContenidoUI.text = string.Empty;

                if (imagenRetratoUI != null)
                {
                    if (actual.ilustracionHablante != null)
                    {
                        imagenRetratoUI.sprite = actual.ilustracionHablante;
                        imagenRetratoUI.enabled = true;
                    }
                    else
                    {
                        imagenRetratoUI.enabled = false;
                    }
                }

                string linea = actual.texto;
                yield return null;

                for (int c = 0; c < linea.Length; c++)
                {
                    if (textoContenidoUI != null) textoContenidoUI.text += linea[c];

                    float t = tiempoPorLetra;
                    bool saltar = false;
                    while (t > 0f)
                    {
                        t -= Time.deltaTime;
                        if (PresionoAvanzarDialogo())
                        {
                            saltar = true;
                            break;
                        }
                        yield return null;
                    }

                    if (saltar)
                    {
                        if (textoContenidoUI != null) textoContenidoUI.text = linea;
                        break;
                    }
                }

                yield return null;
                while (!PresionoAvanzarDialogo())
                {
                    yield return null;
                }
            }

            panelDialogo.SetActive(false);
        }

        if (npcTransform != null)
        {
            ReproducirAnimNPC(clipNPCMover);

            while (Vector3.Distance(npcTransform.position, posInicioNPC) > 0.03f)
            {
                npcTransform.position = Vector3.MoveTowards(npcTransform.position, posInicioNPC, velocidadMovimientoNPC * Time.deltaTime);
                yield return null;
            }

            npcTransform.gameObject.SetActive(false);
        }
    }

    private void ReproducirAnimNPC(AnimationClip clip)
    {
        if (animNPC != null && clip != null)
        {
            animNPC.Play(clip.name);
        }
    }

    private bool PresionoAvanzarDialogo()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame))
        {
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }

    private void IniciarFaseQTE()
    {
        ultimaDirMovLeida = LeerDireccionMover();
        ultimaDirAtkLeida = LeerDireccionAtacar();
        esperandoSoltarDash = LeerInputDash();

        qteActivo = true;
        AlIniciarQTECadenas?.Invoke();

        if (rutinaTutorialVisual != null) StopCoroutine(rutinaTutorialVisual);
        rutinaTutorialVisual = StartCoroutine(RutinaSecuenciaAvisosIniciales());
    }

    private IEnumerator RutinaSecuenciaAvisosIniciales()
    {
        if (objetoAvisoMover != null && cadenasDireccionalesRotas < 8 && qteActivo)
        {
            yield return StartCoroutine(RutinaMostrarYDesvanecerAviso(objetoAvisoMover));
        }

        if (pausaEntreAvisos > 0f && cadenasDireccionalesRotas < 8 && qteActivo)
        {
            yield return new WaitForSeconds(pausaEntreAvisos);
        }

        if (objetoAvisoAtacar != null && cadenasDireccionalesRotas < 8 && qteActivo)
        {
            yield return StartCoroutine(RutinaMostrarYDesvanecerAviso(objetoAvisoAtacar));
        }
    }

    private IEnumerator RutinaAvisoDashFinal()
    {
        ConfigurarAlphaObjeto(objetoAvisoMover, 0f, false);
        ConfigurarAlphaObjeto(objetoAvisoAtacar, 0f, false);

        if (objetoAvisoDash == null) yield break;

        ConfigurarAlphaObjeto(objetoAvisoDash, 0f, true);
        yield return StartCoroutine(InterpolarAlphaObjeto(objetoAvisoDash, 0f, 1f, duracionFadeAviso));

        float t = tiempoVisibleAviso;
        while (t > 0f && qteActivo)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (objetoAvisoDash != null && objetoAvisoDash.activeSelf)
        {
            yield return StartCoroutine(InterpolarAlphaObjeto(objetoAvisoDash, 1f, 0f, duracionFadeAviso));
            ConfigurarAlphaObjeto(objetoAvisoDash, 0f, false);
        }
    }

    private IEnumerator RutinaMostrarYDesvanecerAviso(GameObject objAviso)
    {
        if (objAviso == null) yield break;

        ConfigurarAlphaObjeto(objAviso, 0f, true);
        yield return StartCoroutine(InterpolarAlphaObjeto(objAviso, 0f, 1f, duracionFadeAviso));

        float t = tiempoVisibleAviso;
        while (t > 0f && qteActivo && cadenasDireccionalesRotas < 8)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(InterpolarAlphaObjeto(objAviso, ObtenerAlphaObjeto(objAviso), 0f, duracionFadeAviso));
        ConfigurarAlphaObjeto(objAviso, 0f, false);
    }

    private IEnumerator InterpolarAlphaObjeto(GameObject obj, float inicio, float fin, float duracion)
    {
        if (obj == null) yield break;

        float t = 0f;
        float d = Mathf.Max(0.05f, duracion);

        while (t < d && obj != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(inicio, fin, t / d);
            AplicarAlphaComponentes(obj, a);
            yield return null;
        }

        if (obj != null) AplicarAlphaComponentes(obj, fin);
    }

    private void ConfigurarAlphaObjeto(GameObject obj, float alpha, bool activo)
    {
        if (obj == null) return;
        obj.SetActive(activo);
        AplicarAlphaComponentes(obj, alpha);
    }

    private void AplicarAlphaComponentes(GameObject obj, float alpha)
    {
        if (obj == null) return;

        if (obj.TryGetComponent(out CanvasGroup cg))
        {
            cg.alpha = alpha;
        }

        SpriteRenderer[] srs = obj.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            Color c = srs[i].color;
            c.a = alpha;
            srs[i].color = c;
        }

        Graphic[] graficos = obj.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graficos.Length; i++)
        {
            Color c = graficos[i].color;
            c.a = alpha;
            graficos[i].color = c;
        }
    }

    private float ObtenerAlphaObjeto(GameObject obj)
    {
        if (obj == null) return 0f;
        if (obj.TryGetComponent(out CanvasGroup cg)) return cg.alpha;
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) return sr.color.a;
        Graphic g = obj.GetComponentInChildren<Graphic>(true);
        if (g != null) return g.color.a;
        return 1f;
    }

    void Update()
    {
        if (!qteActivo || jugador == null) return;

        Vector2Int dirMovActual = LeerDireccionMover();
        Vector2Int dirAtkActual = LeerDireccionAtacar();
        bool presionandoDash = LeerInputDash();

        bool nuevoPulsoMov = dirMovActual != Vector2Int.zero && dirMovActual != ultimaDirMovLeida;
        bool nuevoPulsoAtk = dirAtkActual != Vector2Int.zero && dirAtkActual != ultimaDirAtkLeida;

        ultimaDirMovLeida = dirMovActual;
        ultimaDirAtkLeida = dirAtkActual;
        if (!presionandoDash) esperandoSoltarDash = false;

        if (cadenasDireccionalesRotas < 8)
        {
            if (nuevoPulsoMov)
            {
                ProcesarIntentoDireccional(progresoMov, dirMovActual, 0);
            }

            if (nuevoPulsoAtk)
            {
                ProcesarIntentoDireccional(progresoAtk, dirAtkActual, 4);
            }

            if (cadenasDireccionalesRotas >= 8 && !avisoDashMostrado)
            {
                avisoDashMostrado = true;
                if (rutinaTutorialVisual != null) StopCoroutine(rutinaTutorialVisual);
                rutinaTutorialVisual = StartCoroutine(RutinaAvisoDashFinal());
            }
        }
        else
        {
            if (presionandoDash && !esperandoSoltarDash)
            {
                esperandoSoltarDash = true;
                Vector2 dirFinal = dirMovActual != Vector2Int.zero ? (Vector2)dirMovActual : jugador.UltimaDireccion;
                RomperCadenaEspecifica(8, dirFinal);
                LiberarJugador(dirFinal);
            }
        }
    }

    void FixedUpdate()
    {
        if (!cadenasSimulando || jugador == null) return;
        SimularFisicaVerlet(Time.fixedDeltaTime);
    }

    void LateUpdate()
    {
        if (!cadenasSimulando || cadenas == null || texturaPixel == null) return;
        RenderizarCadenasPixelArt();
    }

    private void RenderizarCadenasPixelArt()
    {
        Array.Copy(bufferLimpieza, bufferTrabajo, bufferLimpieza.Length);

        Color32 colIntacta = colorCadenaIntacta;
        Color32 colDebil = colorCadenaDebilitada;
        int ppu = Mathf.Max(8, pixelesPorUnidad);

        for (int i = 0; i < cadenas.Length; i++)
        {
            CadenaFisica c = cadenas[i];
            if (c == null || !c.activa || c.nodosActuales == null) continue;

            Color32 colorActual = c.debilitada ? colDebil : colIntacta;
            int totalNodos = c.nodosActuales.Length;

            for (int n = 0; n < totalNodos - 1; n++)
            {
                Vector2 pA = c.nodosActuales[n];
                Vector2 pB = c.nodosActuales[n + 1];

                if (c.debilitada)
                {
                    if (n > 0) pA += UnityEngine.Random.insideUnitCircle * 0.015f;
                    if (n + 1 < totalNodos - 1) pB += UnityEngine.Random.insideUnitCircle * 0.015f;
                }

                int x0 = Mathf.RoundToInt((pA.x - esquinaMinLienzo.x) * ppu);
                int y0 = Mathf.RoundToInt((pA.y - esquinaMinLienzo.y) * ppu);
                int x1 = Mathf.RoundToInt((pB.x - esquinaMinLienzo.x) * ppu);
                int y1 = Mathf.RoundToInt((pB.y - esquinaMinLienzo.y) * ppu);

                DibujarLineaBresenham(x0, y0, x1, y1, colorActual);
            }
        }

        texturaPixel.SetPixels32(bufferTrabajo);
        texturaPixel.Apply(false);
    }

    private void DibujarLineaBresenham(int x0, int y0, int x1, int y1, Color32 color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            PintarPixelConGrosor(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void PintarPixelConGrosor(int x, int y, Color32 color)
    {
        if (grosorEnPixeles <= 1)
        {
            if (x >= 0 && x < anchoTex && y >= 0 && y < altoTex)
            {
                bufferTrabajo[y * anchoTex + x] = color;
            }
            return;
        }

        for (int ox = 0; ox < grosorEnPixeles; ox++)
        {
            for (int oy = 0; oy < grosorEnPixeles; oy++)
            {
                int px = x + ox;
                int py = y + oy;
                if (px >= 0 && px < anchoTex && py >= 0 && py < altoTex)
                {
                    bufferTrabajo[py * anchoTex + px] = color;
                }
            }
        }
    }

    private void SimularFisicaVerlet(float dt)
    {
        if (cadenas == null) return;
        float dtCuadrado = dt * dt;

        for (int i = 0; i < cadenas.Length; i++)
        {
            CadenaFisica c = cadenas[i];
            if (c == null || !c.activa || c.nodosActuales == null) continue;

            int numNodos = c.nodosActuales.Length;
            Vector2 posInicio = ObtenerPosicionAnclajeEscena(c);
            Vector2 posFin = (Vector2)jugador.transform.position + c.offsetEnJugador;

            for (int n = 1; n < numNodos - 1; n++)
            {
                Vector2 vel = (c.nodosActuales[n] - c.nodosPrevios[n]) * amortiguacion;
                c.nodosPrevios[n] = c.nodosActuales[n];
                c.nodosActuales[n] += vel + gravedadCadena * dtCuadrado;

                if (c.timerLatigazo > 0f)
                {
                    c.nodosActuales[n] += UnityEngine.Random.insideUnitCircle * 0.045f;
                }
            }

            if (c.timerLatigazo > 0f)
            {
                c.timerLatigazo -= dt;
            }

            for (int iter = 0; iter < iteracionesRigidez; iter++)
            {
                c.nodosActuales[0] = posInicio;
                c.nodosActuales[numNodos - 1] = posFin;

                for (int n = 0; n < numNodos - 1; n++)
                {
                    Vector2 pA = c.nodosActuales[n];
                    Vector2 pB = c.nodosActuales[n + 1];
                    Vector2 delta = pB - pA;
                    float dist = delta.magnitude;

                    if (dist <= 0.0001f) continue;

                    float diferencia = (dist - c.longitudSegmento) / dist;
                    Vector2 correccion = delta * (0.5f * diferencia);

                    if (n == 0)
                    {
                        c.nodosActuales[n + 1] -= correccion * 2f;
                    }
                    else if (n + 1 == numNodos - 1)
                    {
                        c.nodosActuales[n] += correccion * 2f;
                    }
                    else
                    {
                        c.nodosActuales[n] += correccion;
                        c.nodosActuales[n + 1] -= correccion;
                    }
                }
            }
        }
    }

    private void ProcesarIntentoDireccional(int[] arregloProgreso, Vector2Int dir, int offsetIndiceCadena)
    {
        int idxCardinal = ObtenerIndiceCardinal(dir);
        if (idxCardinal < 0) return;

        int indiceCadena = offsetIndiceCadena + idxCardinal;

        if (arregloProgreso[idxCardinal] >= pulsosPorDireccion)
        {
            SacudirTodasLasCadenasActivas(0.08f, dir);
            IniciarAnimacionTiron((Vector2)dir * 0.4f);
            return;
        }

        arregloProgreso[idxCardinal]++;

        if (arregloProgreso[idxCardinal] >= pulsosPorDireccion)
        {
            cadenasDireccionalesRotas++;
            RomperCadenaEspecifica(indiceCadena, dir);
        }
        else
        {
            DebilitarCadenaEspecifica(indiceCadena, dir);
        }
    }

    private void DebilitarCadenaEspecifica(int indiceCadena, Vector2 direccionIntento)
    {
        if (cadenas != null && indiceCadena >= 0 && indiceCadena < cadenas.Length)
        {
            CadenaFisica c = cadenas[indiceCadena];
            if (c != null && c.activa)
            {
                c.debilitada = true;
                c.timerLatigazo = 0.22f;

                Vector2 inicio = ObtenerPosicionAnclajeEscena(c);
                Vector2 fin = (Vector2)posicionAnclada + c.offsetEnJugador;
                float distTotal = Vector2.Distance(inicio, fin) * factorHolguraDebilitada;
                c.longitudSegmento = distTotal / Mathf.Max(1, c.nodosActuales.Length - 1);

                if (emitirParticulas && c.nodosActuales != null && c.nodosActuales.Length > 2)
                {
                    Vector2 puntoMedio = c.nodosActuales[c.nodosActuales.Length / 2];
                    GenerarParticulas(puntoMedio, 5, 0.75f, colorCadenaDebilitada);
                }
            }
        }

        SacudirTodasLasCadenasActivas(0.12f, direccionIntento);
        IniciarAnimacionTiron(direccionIntento * 0.65f);
        AlDebilitarUnaCadena?.Invoke();
    }

    private void RomperCadenaEspecifica(int indiceCadena, Vector2 direccionIntento)
    {
        if (cadenas != null && indiceCadena >= 0 && indiceCadena < cadenas.Length)
        {
            CadenaFisica c = cadenas[indiceCadena];
            if (c != null && c.activa)
            {
                c.activa = false;

                if (emitirParticulas && c.nodosActuales != null)
                {
                    for (int n = 1; n < c.nodosActuales.Length - 1; n += 2)
                    {
                        GenerarParticulas(c.nodosActuales[n], 4, 1f, colorCadenaIntacta);
                    }
                }
            }
        }

        SacudirTodasLasCadenasActivas(0.18f, direccionIntento);
        IniciarAnimacionTiron(direccionIntento);
        AlRomperUnaCadena?.Invoke();
    }

    private void SacudirTodasLasCadenasActivas(float fuerza, Vector2 dirTiron)
    {
        if (cadenas == null) return;
        Vector2 perpendicular = new Vector2(-dirTiron.y, dirTiron.x);

        for (int i = 0; i < cadenas.Length; i++)
        {
            CadenaFisica c = cadenas[i];
            if (c == null || !c.activa || c.nodosActuales == null) continue;

            for (int n = 1; n < c.nodosActuales.Length - 1; n++)
            {
                float onda = Mathf.Sin((n / (float)c.nodosActuales.Length) * Mathf.PI);
                c.nodosActuales[n] += (perpendicular * UnityEngine.Random.Range(-1f, 1f) + dirTiron * 0.5f) * (fuerza * onda);
            }
        }
    }

    private int ObtenerIndiceCardinal(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 0;
        if (dir == Vector2Int.down) return 1;
        if (dir == Vector2Int.left) return 2;
        if (dir == Vector2Int.right) return 3;
        return -1;
    }

    private Vector2Int LeerDireccionMover()
    {
        Vector2 v = Vector2.zero;

        if (accionMover != null)
        {
            v = accionMover.ReadValue<Vector2>();
        }

        if (v.sqrMagnitude < 0.25f)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
            }

            var gp = Gamepad.current;
            if (v.sqrMagnitude < 0.25f && gp != null)
            {
                v = gp.leftStick.ReadValue();
            }
        }

        return ConvertirACardinal(v);
    }

    private Vector2Int LeerDireccionAtacar()
    {
        Vector2 v = Vector2.zero;

        if (accionAtacar != null)
        {
            v = accionAtacar.ReadValue<Vector2>();
        }

        if (v.sqrMagnitude < 0.25f)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.isPressed) v.y += 1f;
                if (kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.leftArrowKey.isPressed) v.x -= 1f;
            }

            var gp = Gamepad.current;
            if (v.sqrMagnitude < 0.25f && gp != null)
            {
                v = gp.rightStick.ReadValue();
            }
        }

        return ConvertirACardinal(v);
    }

    private Vector2Int ConvertirACardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 0.25f) return Vector2Int.zero;

        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
        {
            return v.x > 0f ? Vector2Int.right : Vector2Int.left;
        }
        return v.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    private bool LeerInputDash()
    {
        if (accionDash != null && accionDash.WasPressedThisFrame()) return true;

        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame))
        {
            return true;
        }

        var gp = Gamepad.current;
        if (gp != null && (gp.rightShoulder.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }

    private void IniciarAnimacionTiron(Vector2 direccion)
    {
        if (rutinaForcejeo != null) StopCoroutine(rutinaForcejeo);
        rutinaForcejeo = StartCoroutine(RutinaTironDireccional(direccion));
    }

    private IEnumerator RutinaTironDireccional(Vector2 direccion)
    {
        if (jugador == null) yield break;

        if (srJugador != null && Mathf.Abs(direccion.x) > 0.1f)
        {
            srJugador.flipX = direccion.x < 0f;
        }

        Vector3 dir3D = new Vector3(direccion.x, direccion.y, 0f);
        float t = 0f;
        float d = Mathf.Max(0.04f, duracionTiron);

        while (t < d && qteActivo && jugador != null)
        {
            t += Time.deltaTime;
            float progreso = t / d;
            float curvaTiron = Mathf.Sin(progreso * Mathf.PI);

            Vector3 desplazamiento = dir3D * (distanciaTiron * curvaTiron);
            Vector3 vibracion = (Vector3)(UnityEngine.Random.insideUnitCircle * (intensidadVibracion * (1f - progreso)));
            Vector3 posFinal = posicionAnclada + desplazamiento + vibracion;

            jugador.transform.position = posFinal;
            if (rbJugador != null) rbJugador.position = posFinal;

            yield return null;
        }

        if (qteActivo && jugador != null)
        {
            jugador.transform.position = posicionAnclada;
            if (rbJugador != null) rbJugador.position = posicionAnclada;
        }

        rutinaForcejeo = null;
    }

    private void LiberarJugador(Vector2 dirSalida)
    {
        qteActivo = false;

        if (rutinaForcejeo != null)
        {
            StopCoroutine(rutinaForcejeo);
            rutinaForcejeo = null;
        }

        if (rutinaTutorialVisual != null)
        {
            StopCoroutine(rutinaTutorialVisual);
            rutinaTutorialVisual = null;
        }

        ConfigurarAlphaObjeto(objetoAvisoMover, 0f, false);
        ConfigurarAlphaObjeto(objetoAvisoAtacar, 0f, false);
        ConfigurarAlphaObjeto(objetoAvisoDash, 0f, false);

        ApagarTodasLasCadenas();

        jugador.transform.position = posicionAnclada;
        if (rbJugador != null) rbJugador.position = posicionAnclada;

        if (ataqueJugador != null)
        {
            ataqueJugador.EstablecerBloqueoExterno(false);
        }

        jugador.EstablecerEstadoEncadenado(false, null, null);

        if (dirSalida.sqrMagnitude > 0.01f)
        {
            jugador.EstablecerDireccion(dirSalida);
        }

        if (impulsarConDashAlLiberarse)
        {
            jugador.ForzarDashInmediato();
        }

        AlLiberarsePorCompleto?.Invoke();
    }

    private void GenerarParticulas(Vector3 posicion, int cantidad, float factorEscala, Color colorParticula)
    {
        AsegurarMaterial();

        GameObject obj = new GameObject("ParticulasCadena");
        obj.transform.position = posicion;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = obj.GetComponent<ParticleSystemRenderer>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (materialCompartido != null) psr.sharedMaterial = materialCompartido;
        psr.sortingOrder = sortingOrderCadenas + 2;

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f * factorEscala, 4.2f * factorEscala);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f * factorEscala, 0.1f * factorEscala);
        main.startColor = colorParticula;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        ps.Play();
        Destroy(obj, 0.5f);
    }

    void OnDestroy()
    {
        if (texturaPixel != null) Destroy(texturaPixel);
    }

    void OnDrawGizmosSelected()
    {
        if (cadenas == null) return;

        Vector2 centroRef = Application.isPlaying && yaSeActivo ? (Vector2)posicionAnclada : (Vector2)transform.position;

        for (int i = 0; i < cadenas.Length; i++)
        {
            CadenaFisica c = cadenas[i];
            if (c == null) continue;

            Vector2 anclaje = c.puntoAnclajeEscena != null ? (Vector2)c.puntoAnclajeEscena.position : centroRef + c.offsetAnclajePorDefecto;
            Vector2 enJugador = centroRef + c.offsetEnJugador;

            Gizmos.color = colorCadenaIntacta;
            Gizmos.DrawLine(anclaje, enJugador);
            Gizmos.DrawWireSphere(anclaje, 0.12f);
        }
    }
}
