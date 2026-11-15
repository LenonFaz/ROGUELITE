using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Vida))]
public class LaInquisidora : MonoBehaviour, IRangoDeteccion
{
    [Serializable]
    public class LineaDialogoJefe
    {
        public string nombreHablante;
        public Sprite ilustracionHablante;
        public bool voltearIlustracionX;
        public Vector2 offsetIlustracion;
        [TextArea(2, 4)] public string texto;
    }

    [Header("Referencias Principales")]
    [SerializeField] private Transform player;
    [SerializeField] private Bullet prefabBullet;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private LayerMask capaJugador;
    [SerializeField] private Sala salaJefe;
    [SerializeField] private Collider2D limitesArena;
    [SerializeField, Min(8)] private int pixelesPorUnidad = 32;
    [SerializeField, Min(0.1f)] private float tiempoAvisoAntesAtaque = 0.45f;
    [SerializeField] private Color colorDestelloJefe = new Color(1f, 0.15f, 0.35f, 0.95f);

    [Header("Cinemática y Diálogo de Inicio")]
    [SerializeField] private bool esperarTriggerDialogoInicial = true;
    [SerializeField] private LineaDialogoJefe[] dialogosInicio = new LineaDialogoJefe[]
    {
        new LineaDialogoJefe { nombreHablante = "La Inquisidora", texto = "..." },
        new LineaDialogoJefe { nombreHablante = "Protagonista", texto = "..." }
    };

    [Header("Movimiento y Firmeza")]
    [SerializeField] private bool inmuneAEmpujeSiempre = false;
    [SerializeField, Min(0f)] private float velocidadBase = 3.2f;
    [SerializeField, Min(0f)] private float aceleracion = 20f;
    [SerializeField, Min(0f)] private float desaceleracion = 25f;
    [SerializeField, Min(0f)] private float rangoDeteccion = 16f;
    [SerializeField, Min(0f)] private float velocidadDash = 14f;
    [SerializeField, Min(0f)] private float duracionDash = 0.22f;
    [SerializeField, Min(0f)] private float cdDash = 3.2f;
    [SerializeField, Min(0f)] private float pausaEntreAtaques = 1.05f;

    [Header("Fase 1: 1. Melee Grande (Con Indicador Pixel)")]
    [SerializeField, Min(0f)] private float distAcercamientoMelee = 2.2f;
    [SerializeField, Min(0.1f)] private float tiempoQuietoAntesMelee = 0.5f;
    [SerializeField] private Vector2 tamHitboxMeleeGrande = new Vector2(3.2f, 2.6f);
    [SerializeField, Min(0f)] private float offsetMeleeGrande = 1.6f;
    [SerializeField, Min(1)] private int danioMeleeGrande = 2;
    [SerializeField, Min(0f)] private float empujeMeleeGrande = 10f;

    [Header("Fase 1: 2. Oleadas Radiales ZigZag")]
    [SerializeField, Min(1)] private int cantidadOleadasZigZag = 3;
    [SerializeField, Min(3)] private int balasPorOleadaZigZag = 12;
    [SerializeField, Min(0.05f)] private float intervaloOleadasZigZag = 0.45f;
    [SerializeField, Min(0f)] private float velBalaZigZag = 6.5f;
    [SerializeField, Min(0f)] private float frecuenciaZigZag = 10f;
    [SerializeField, Min(0f)] private float amplitudZigZag = 4.2f;

    [Header("Fase 1: 3. Metralleta en Barrido")]
    [SerializeField, Min(1)] private int cantidadBalasMetralleta = 28;
    [SerializeField, Min(0.01f)] private float cadenciaMetralleta = 0.055f;
    [SerializeField, Min(0f)] private float arcoBarridoGrados = 32f;
    [SerializeField, Min(0f)] private float anguloImprecision = 6f;
    [SerializeField, Min(0f)] private float velBalaMetralleta = 10.5f;

    [Header("Fase 1: 4. Super Ataque Melee Largo (Carril Pixel)")]
    [SerializeField, Min(0f)] private float velMovimientoLento = 0.85f;
    [SerializeField, Min(0.2f)] private float tiempoCargaSuperMelee = 1.15f;
    [SerializeField] private Vector2 tamHitboxSuperMelee = new Vector2(8f, 1.25f);
    [SerializeField, Min(0f)] private float offsetSuperMelee = 4f;
    [SerializeField, Min(1)] private int danioSuperMelee = 2;
    [SerializeField, Min(0f)] private float empujeSuperMelee = 13f;
    [SerializeField] private Color colorAvisoPixel = new Color(1f, 0.2f, 0.25f, 0.85f);
    [SerializeField] private Color colorImpactoPixel = new Color(1f, 0.95f, 0.8f, 1f);

    [Header("Fase 2: Transición y Columnas (Tilemap)")]
    [SerializeField] private Tilemap tilemapColumnas;
    [SerializeField] private bool desactivarTilemapCompleto = true;
    [SerializeField] private GameObject[] columnasSala;
    [SerializeField] private GameObject prefabEfectoDestruccionColumna;

    [Header("Fase 2: 1. Invocación de 3 Ghouls (Izq, Der, Abajo)")]
    [SerializeField] private GameObject prefabGhoul;
    [SerializeField, Min(0.5f)] private float distanciaSpawnGhoul = 2.2f;
    [SerializeField, Min(0.1f)] private float tiempoAparicionGhouls = 0.6f;
    [SerializeField, Min(0.5f)] private float duracionInvulnerableInvocacion = 4f;
    [SerializeField] private Color colorInvulnerable = new Color(0.6f, 0.85f, 1f, 0.8f);

    [Header("Fase 2: 2. Patrones Touhou")]
    [SerializeField, Min(1)] private int oleadasTouhou = 16;
    [SerializeField, Min(3)] private int brazosTouhou = 6;
    [SerializeField, Min(0.02f)] private float intervaloTouhou = 0.12f;
    [SerializeField, Min(0f)] private float velBalaTouhou = 6f;

    [Header("Fase 2: 3. Cuadrícula de Láseres (Independiente)")]
    [SerializeField, Min(1)] private int lineasVerticales = 4;
    [SerializeField, Min(1)] private int lineasHorizontales = 3;
    [SerializeField, Min(2f)] private float tamanoArenaFallback = 12f;
    [SerializeField, Min(0.1f)] private float tiempoCasteoCuadricula = 0.45f;
    [SerializeField, Min(0.1f)] private float tiempoAvisoCuadricula = 1.15f;
    [SerializeField, Min(0.1f)] private float duracionCuadricula = 0.65f;
    [SerializeField, Min(1)] private int grosorLaserCuadriculaPx = 14;
    [SerializeField, Min(1)] private int danioLaserCuadricula = 1;

    [Header("UI de Diálogo, Derrota y Portal")]
    [SerializeField, Min(0.2f)] private float esperaAntesDeDialogoFinal = 1.4f;
    [SerializeField] private GameObject panelDialogoUI;
    [SerializeField] private Image imagenRetratoUI;
    [SerializeField] private TMP_Text textoNombreUI;
    [SerializeField] private TMP_Text textoContenidoUI;
    [SerializeField] private GameObject iconoContinuarEspacioUI;
    [SerializeField] private bool mostrarIconoSoloAlTerminarLinea = false;
    [SerializeField, Min(0.005f)] private float tiempoPorLetra = 0.03f;
    [SerializeField] private LineaDialogoJefe[] dialogosDerrota = new LineaDialogoJefe[]
    {
        new LineaDialogoJefe { nombreHablante = "La Inquisidora", texto = "..." },
        new LineaDialogoJefe { nombreHablante = "Protagonista", texto = "..." }
    };
    [SerializeField] private PortalSalida portalSalida;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private AnimationClip aCaminarAbajo;
    [SerializeField] private AnimationClip aPrepararMeleeGrande;
    [SerializeField] private AnimationClip aLanzarMeleeGrande;
    [SerializeField] private AnimationClip aPrepararRadialZigZag;
    [SerializeField] private AnimationClip aLanzarMetralleta;
    [SerializeField] private AnimationClip aCargarMeleeLargo;
    [SerializeField] private AnimationClip aLanzarMeleeLargo;
    [SerializeField] private AnimationClip aModoInvulnerable;
    [SerializeField] private AnimationClip aLanzarPatronesTouhou;
    [SerializeField] private AnimationClip aLanzarCuadriculaLaser;
    [SerializeField] private AnimationClip aVencida;

    public float RangoDeteccion => esperarTriggerDialogoInicial && !peleaIniciada ? 0f : rangoDeteccion;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D miCol;
    private Vida vida;
    private Empuje empuje;
    private IndicadorAtaque indicador;

    private Vector2 posBaseRetratoUI;
    private bool posBaseRetratoGuardada;

    private bool peleaIniciada;
    private bool introEnCurso;
    private bool barraJefeActivada;
    private bool enFase2;
    private bool ejecutandoAccion;
    private bool cuadriculaEnCurso;
    private bool dasheando;
    private bool vencida;
    private float timerCdDash;
    private float timerPausaAtaque;
    private Vector2 velObjetivo;
    private Vector2 dirDash;
    private Color colorBase;
    private string animActual;
    private int ultimoAtaqueF1 = -1;
    private int ultimoAtaqueF2 = -1;

    private readonly List<Vida> enemigosInvocados = new List<Vida>();

    private GameObject objLienzo;
    private SpriteRenderer srLienzo;
    private Texture2D texLienzo;
    private Color32[] bufferLimpio;
    private Color32[] bufferTrabajo;

    private GameObject objLienzoCuadricula;
    private SpriteRenderer srLienzoCuadricula;
    private Texture2D texLienzoCuadricula;
    private Color32[] bufferTrabajoCuadricula;

    private int anchoTex;
    private int altoTex;
    private Vector2 esquinaMinLienzo;
    private static Material materialPixelCompartido;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out sprite);
        TryGetComponent(out miCol);
        TryGetComponent(out vida);
        TryGetComponent(out empuje);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
        if (!anim) TryGetComponent(out anim);

        indicador.ConfigurarColorAviso(colorDestelloJefe);

        if (TryGetComponent(out Aturdido aturdidoComp))
        {
            aturdidoComp.enabled = false;
        }

        if (empuje != null)
        {
            empuje.InmuneAEmpuje = inmuneAEmpujeSiempre;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        colorBase = sprite.color;
        vida.EstablecerConservarAlMorir(true);

        if (imagenRetratoUI != null)
        {
            posBaseRetratoUI = imagenRetratoUI.rectTransform.anchoredPosition;
            posBaseRetratoGuardada = true;
            imagenRetratoUI.gameObject.SetActive(false);
        }

        if (iconoContinuarEspacioUI != null) iconoContinuarEspacioUI.SetActive(false);
        if (panelDialogoUI != null) panelDialogoUI.SetActive(false);

        peleaIniciada = !esperarTriggerDialogoInicial;
    }

    void Start()
    {
        if (!player)
        {
            if (Jugador.Instancia != null) player = Jugador.Instancia.transform;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p) player = p.transform;
            }
        }

        if (player && miCol && player.TryGetComponent(out Collider2D colP))
        {
            Physics2D.IgnoreCollision(miCol, colP, true);
        }

        InicializarLienzosPixelArena();

        vida.AlRecibirDanio.AddListener(VerificarCambioDeFase);
        vida.AlMorir.AddListener(EjecutarDerrota);
        timerCdDash = cdDash;
        timerPausaAtaque = 1f;
    }

    public void IniciarSecuenciaIntro()
    {
        if (peleaIniciada || introEnCurso || vencida) return;
        if (!enabled) enabled = true;
        StartCoroutine(RutinaIntroYDialogo());
    }

    private IEnumerator RutinaIntroYDialogo()
    {
        introEnCurso = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aCaminarAbajo);

        BloquearControlesJugadorParaDialogo(true);

        yield return StartCoroutine(ReproducirLineasDialogo(dialogosInicio));

        BloquearControlesJugadorParaDialogo(false);

        introEnCurso = false;
        peleaIniciada = true;
        timerPausaAtaque = 0.6f;

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(true);
        }

        ActivarBarraDeVida();
    }

    private void BloquearControlesJugadorParaDialogo(bool bloquear)
    {
        Jugador jug = Jugador.Instancia;
        if (jug == null) return;

        AtaqueJugador atkJug = jug.GetComponent<AtaqueJugador>();
        PlayerInput pInput = jug.GetComponent<PlayerInput>();

        if (bloquear)
        {
            if (pInput != null) pInput.DeactivateInput();
            if (atkJug != null) atkJug.EstablecerBloqueoExterno(true);
            jug.EstablecerEstadoEncadenado(true, null, null);
        }
        else
        {
            jug.EstablecerEstadoEncadenado(false, null, null);
            if (atkJug != null) atkJug.EstablecerBloqueoExterno(false);
            if (pInput != null) pInput.ActivateInput();
        }
    }

    private void ActivarBarraDeVida()
    {
        if (barraJefeActivada) return;
        barraJefeActivada = true;
        if (UIBarraJefe.Instancia != null)
        {
            UIBarraJefe.Instancia.VincularJefe(vida);
        }
    }

    private void InicializarLienzosPixelArena()
    {
        if (materialPixelCompartido == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) materialPixelCompartido = new Material(s);
        }

        Bounds b = ObtenerBoundsArena();
        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int wUnidades = Mathf.CeilToInt(b.size.x) + 6;
        int hUnidades = Mathf.CeilToInt(b.size.y) + 6;

        anchoTex = wUnidades * ppu;
        altoTex = hUnidades * ppu;

        float cx = Mathf.Round(b.center.x * ppu) / ppu;
        float cy = Mathf.Round(b.center.y * ppu) / ppu;
        esquinaMinLienzo = new Vector2(cx - wUnidades * 0.5f, cy - hUnidades * 0.5f);

        bufferLimpio = new Color32[anchoTex * altoTex];
        bufferTrabajo = new Color32[anchoTex * altoTex];
        bufferTrabajoCuadricula = new Color32[anchoTex * altoTex];

        CrearLienzoIndividual(ref objLienzo, ref srLienzo, ref texLienzo, "LienzoPixelInquisidora", cx, cy, ppu, -1);
        CrearLienzoIndividual(ref objLienzoCuadricula, ref srLienzoCuadricula, ref texLienzoCuadricula, "LienzoPixelCuadricula", cx, cy, ppu, -2);
    }

    private void CrearLienzoIndividual(ref GameObject obj, ref SpriteRenderer sr, ref Texture2D tex, string nombre, float cx, float cy, int ppu, int offsetOrden)
    {
        if (tex != null) Destroy(tex);
        tex = new Texture2D(anchoTex, altoTex, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(bufferLimpio);
        tex.Apply(false);

        if (obj == null)
        {
            obj = new GameObject(nombre);
            sr = obj.AddComponent<SpriteRenderer>();
            if (materialPixelCompartido != null) sr.sharedMaterial = materialPixelCompartido;
        }

        obj.transform.position = new Vector3(cx, cy, 0f);
        if (sprite != null)
        {
            sr.sortingLayerID = sprite.sortingLayerID;
            sr.sortingOrder = sprite.sortingOrder + offsetOrden;
        }
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, anchoTex, altoTex), new Vector2(0.5f, 0.5f), ppu);
        sr.enabled = false;
    }

    private void LimpiarYApagarLienzoPrincipal()
    {
        if (srLienzo != null) srLienzo.enabled = false;
    }

    private void LimpiarYApagarLienzoCuadricula()
    {
        cuadriculaEnCurso = false;
        if (srLienzoCuadricula != null) srLienzoCuadricula.enabled = false;
    }

    void Update()
    {
        if (vencida || !player || !peleaIniciada || introEnCurso) return;

        timerCdDash -= Time.deltaTime;
        if (!ejecutandoAccion) timerPausaAtaque -= Time.deltaTime;

        if (!dasheando && Mathf.Abs(player.position.x - transform.position.x) > 0.1f)
        {
            sprite.flipX = player.position.x < transform.position.x;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > rangoDeteccion && !ejecutandoAccion)
        {
            velObjetivo = Vector2.zero;
            ReproducirAnim(aCaminarAbajo);
            return;
        }

        ActivarBarraDeVida();

        if (ejecutandoAccion) return;

        if (timerCdDash <= 0f)
        {
            StartCoroutine(RutinaDash());
            return;
        }

        if (timerPausaAtaque <= 0f)
        {
            ElegirSiguienteAtaque();
        }
        else
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            velObjetivo = dist > 3.5f ? dir * velocidadBase : Vector2.zero;
            ReproducirAnim(aCaminarAbajo);
        }
    }

    void FixedUpdate()
    {
        if (vencida || !peleaIniciada || introEnCurso)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (vida.EsInvulnerable && empuje != null && empuje.EstaEmpujado)
        {
            empuje.CancelarEmpuje();
        }

        if (!inmuneAEmpujeSiempre && !vida.EsInvulnerable && !ejecutandoAccion && empuje != null && empuje.EstaEmpujado)
        {
            return;
        }

        if (dasheando)
        {
            rb.linearVelocity = dirDash * velocidadDash;
            return;
        }

        float tasa = velObjetivo.sqrMagnitude > 0.01f ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, velObjetivo, tasa * Time.fixedDeltaTime);
    }

    private void VerificarCambioDeFase()
    {
        if (!peleaIniciada && !introEnCurso)
        {
            peleaIniciada = true;
            ActivarBarraDeVida();
        }

        if (enFase2 || vencida || vida.VidaActual <= 0) return;

        if (vida.VidaActual <= vida.VidaMaxima * 0.5f)
        {
            enFase2 = true;
            StopAllCoroutines();
            indicador.CancelarAviso();
            LimpiarYApagarLienzoPrincipal();
            LimpiarYApagarLienzoCuadricula();
            dasheando = false;
            sprite.color = colorBase;
            StartCoroutine(RutinaTransicionFase2());
        }
    }

    private void ElegirSiguienteAtaque()
    {
        if (!enFase2)
        {
            int atk = ObtenerIndiceAleatorio(4, ref ultimoAtaqueF1);
            EjecutarAtaqueFase1(atk);
        }
        else
        {
            LimpiarEnemigosMuertos();
            int atk = ObtenerIndiceAleatorio(4, ref ultimoAtaqueF2);

            if (atk == 0 && enemigosInvocados.Count >= 3)
            {
                atk = Random.Range(1, 4);
            }

            if (atk == 3 && cuadriculaEnCurso)
            {
                atk = Random.Range(1, 3);
            }

            switch (atk)
            {
                case 0:
                    StartCoroutine(RutinaInvocarTresGhouls());
                    break;
                case 1:
                    EjecutarAtaqueFase1(Random.Range(0, 4));
                    break;
                case 2:
                    StartCoroutine(RutinaPatronesTouhou());
                    break;
                case 3:
                    StartCoroutine(RutinaCuadriculaLaser());
                    break;
            }
        }
    }

    private void EjecutarAtaqueFase1(int indice)
    {
        switch (indice)
        {
            case 0:
                StartCoroutine(RutinaMeleeGrande());
                break;
            case 1:
                StartCoroutine(RutinaRadialZigZag());
                break;
            case 2:
                StartCoroutine(RutinaMetralleta());
                break;
            case 3:
                StartCoroutine(RutinaSuperMeleeLargo());
                break;
        }
    }

    private int ObtenerIndiceAleatorio(int total, ref int ultimo)
    {
        int nuevo = Random.Range(0, total);
        if (nuevo == ultimo) nuevo = (nuevo + 1) % total;
        ultimo = nuevo;
        return nuevo;
    }

    private void MostrarDestelloJefe(float duracion)
    {
        if (indicador != null)
        {
            indicador.MostrarAviso(duracion, colorDestelloJefe);
        }
    }

    private IEnumerator RutinaDash()
    {
        ejecutandoAccion = true;
        dasheando = true;
        timerCdDash = cdDash;

        Vector2 haciaJugador = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 perpendicular = new Vector2(-haciaJugador.y, haciaJugador.x) * (Random.value > 0.5f ? 1f : -1f);
        dirDash = (haciaJugador * 0.6f + perpendicular * 0.4f).normalized;

        if (Mathf.Abs(dirDash.x) > 0.1f) sprite.flipX = dirDash.x < 0f;
        ReproducirAnim(aCaminarAbajo);

        float t = duracionDash;
        while (t > 0f && !vencida)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        dasheando = false;
        velObjetivo = Vector2.zero;
        ejecutandoAccion = false;
    }

    private IEnumerator RutinaMeleeGrande()
    {
        ejecutandoAccion = true;
        ReproducirAnim(aCaminarAbajo);

        float tiempoMaxAcercamiento = 2.2f;
        while (tiempoMaxAcercamiento > 0f && player != null && Vector2.Distance(transform.position, player.position) > distAcercamientoMelee)
        {
            if (vencida) { FinalizarAccion(); yield break; }
            tiempoMaxAcercamiento -= Time.deltaTime;
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            velObjetivo = dir * (velocidadBase * 1.35f);
            yield return null;
        }

        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aPrepararMeleeGrande);

        Vector2 dirAtaque = player != null ? ((Vector2)player.position - (Vector2)transform.position).normalized : Vector2.down;
        if (dirAtaque == Vector2.zero) dirAtaque = Vector2.down;
        float angulo = Mathf.Atan2(dirAtaque.y, dirAtaque.x) * Mathf.Rad2Deg;

        MostrarDestelloJefe(tiempoQuietoAntesMelee);
        srLienzo.enabled = true;

        float t = 0f;
        while (t < tiempoQuietoAntesMelee)
        {
            if (vencida) { LimpiarYApagarLienzoPrincipal(); FinalizarAccion(); yield break; }
            t += Time.deltaTime;

            Array.Copy(bufferLimpio, bufferTrabajo, bufferLimpio.Length);
            if (Mathf.Sin(t * 28f) >= -0.2f)
            {
                Vector2 centroAviso = (Vector2)transform.position + dirAtaque * offsetMeleeGrande;
                DibujarCajaRotadaPixel(bufferTrabajo, centroAviso, tamHitboxMeleeGrande, angulo, colorAvisoPixel, false);
            }
            AplicarLienzoPrincipal();
            yield return null;
        }

        ReproducirAnim(aLanzarMeleeGrande);
        rb.linearVelocity = dirAtaque * 5f;

        Vector2 centro = (Vector2)transform.position + dirAtaque * offsetMeleeGrande;
        Array.Copy(bufferLimpio, bufferTrabajo, bufferLimpio.Length);
        DibujarCajaRotadaPixel(bufferTrabajo, centro, tamHitboxMeleeGrande, angulo, colorImpactoPixel, true);
        AplicarLienzoPrincipal();

        AplicarDanioCaja(centro, tamHitboxMeleeGrande, angulo, danioMeleeGrande, dirAtaque, empujeMeleeGrande);

        yield return new WaitForSeconds(0.14f);
        rb.linearVelocity = Vector2.zero;
        LimpiarYApagarLienzoPrincipal();
        yield return new WaitForSeconds(0.22f);
        FinalizarAccion();
    }

    private IEnumerator RutinaRadialZigZag()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        ReproducirAnim(aPrepararRadialZigZag);

        MostrarDestelloJefe(tiempoAvisoAntesAtaque);
        yield return new WaitForSeconds(tiempoAvisoAntesAtaque);

        for (int o = 0; o < cantidadOleadasZigZag; o++)
        {
            if (vencida) { FinalizarAccion(); yield break; }

            float desfase = o * (180f / balasPorOleadaZigZag);
            for (int i = 0; i < balasPorOleadaZigZag; i++)
            {
                float angulo = (i * (360f / balasPorOleadaZigZag) + desfase) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));

                Bullet b = InstanciarBala(dir, velBalaZigZag, 1, 5f);
                if (b != null)
                {
                    BalaZigZag zz = b.gameObject.AddComponent<BalaZigZag>();
                    float signo = (i % 2 == 0) ? 1f : -1f;
                    zz.Configurar(dir, velBalaZigZag, frecuenciaZigZag, amplitudZigZag * signo);
                }
            }

            yield return new WaitForSeconds(intervaloOleadasZigZag);
        }

        FinalizarAccion();
    }

    private IEnumerator RutinaMetralleta()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        ReproducirAnim(aLanzarMetralleta);

        MostrarDestelloJefe(tiempoAvisoAntesAtaque);
        yield return new WaitForSeconds(tiempoAvisoAntesAtaque);

        for (int i = 0; i < cantidadBalasMetralleta; i++)
        {
            if (vencida || player == null) { FinalizarAccion(); yield break; }

            Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
            Vector2 haciaJugador = ((Vector2)player.position - (Vector2)origen).normalized;
            float anguloBase = Mathf.Atan2(haciaJugador.y, haciaJugador.x) * Mathf.Rad2Deg;

            float abanico = Mathf.Sin(i * 0.45f) * arcoBarridoGrados;
            float ruido = Random.Range(-anguloImprecision, anguloImprecision);
            float anguloFinal = (anguloBase + abanico + ruido) * Mathf.Deg2Rad;

            Vector2 dirBala = new Vector2(Mathf.Cos(anguloFinal), Mathf.Sin(anguloFinal));
            InstanciarBala(dirBala, velBalaMetralleta, 1, 4f);

            yield return new WaitForSeconds(cadenciaMetralleta);
        }

        FinalizarAccion();
    }

    private IEnumerator RutinaSuperMeleeLargo()
    {
        ejecutandoAccion = true;
        ReproducirAnim(aCargarMeleeLargo);

        MostrarDestelloJefe(tiempoCargaSuperMelee);
        srLienzo.enabled = true;

        float t = 0f;
        Vector2 dirApuntado = Vector2.down;
        float tiempoFijar = Mathf.Max(0.15f, tiempoCargaSuperMelee - 0.25f);

        while (t < tiempoCargaSuperMelee)
        {
            if (vencida)
            {
                LimpiarYApagarLienzoPrincipal();
                indicador.CancelarAviso();
                FinalizarAccion();
                yield break;
            }

            t += Time.deltaTime;

            if (t < tiempoFijar && player != null)
            {
                Vector2 hacia = (Vector2)player.position - (Vector2)transform.position;
                if (hacia.sqrMagnitude > 0.01f) dirApuntado = hacia.normalized;
                velObjetivo = dirApuntado * velMovimientoLento;
            }
            else
            {
                velObjetivo = Vector2.zero;
            }

            float frec = t < tiempoFijar ? 18f : 38f;
            Array.Copy(bufferLimpio, bufferTrabajo, bufferLimpio.Length);

            if (Mathf.Sin(t * frec) >= -0.15f)
            {
                Vector2 centroCarril = (Vector2)transform.position + dirApuntado * offsetSuperMelee;
                float ang = Mathf.Atan2(dirApuntado.y, dirApuntado.x) * Mathf.Rad2Deg;
                DibujarCajaRotadaPixel(bufferTrabajo, centroCarril, tamHitboxSuperMelee, ang, colorAvisoPixel, false);
            }

            AplicarLienzoPrincipal();
            yield return null;
        }

        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aLanzarMeleeLargo);

        Vector2 centroImpacto = (Vector2)transform.position + dirApuntado * offsetSuperMelee;
        float anguloFinal = Mathf.Atan2(dirApuntado.y, dirApuntado.x) * Mathf.Rad2Deg;

        Array.Copy(bufferLimpio, bufferTrabajo, bufferLimpio.Length);
        DibujarCajaRotadaPixel(bufferTrabajo, centroImpacto, tamHitboxSuperMelee, anguloFinal, colorImpactoPixel, true);
        AplicarLienzoPrincipal();

        AplicarDanioCaja(centroImpacto, tamHitboxSuperMelee, anguloFinal, danioSuperMelee, dirApuntado, empujeSuperMelee);

        yield return new WaitForSeconds(0.18f);
        LimpiarYApagarLienzoPrincipal();
        yield return new WaitForSeconds(0.25f);
        FinalizarAccion();
    }

    private IEnumerator RutinaTransicionFase2()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        DestruirColumnasDeLaSala();
        yield return StartCoroutine(RutinaInvocarTresGhouls());
    }

    private void DestruirColumnasDeLaSala()
    {
        if (tilemapColumnas != null)
        {
            Bounds bArena = ObtenerBoundsArena();
            Vector3Int minCell = tilemapColumnas.WorldToCell(bArena.min);
            Vector3Int maxCell = tilemapColumnas.WorldToCell(bArena.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    Vector3Int posCelda = new Vector3Int(x, y, 0);
                    if (tilemapColumnas.HasTile(posCelda))
                    {
                        if (prefabEfectoDestruccionColumna != null)
                        {
                            Vector3 posMundo = tilemapColumnas.GetCellCenterWorld(posCelda);
                            Instantiate(prefabEfectoDestruccionColumna, posMundo, Quaternion.identity);
                        }
                        tilemapColumnas.SetTile(posCelda, null);
                    }
                }
            }

            if (desactivarTilemapCompleto)
            {
                tilemapColumnas.gameObject.SetActive(false);
            }
            else if (tilemapColumnas.TryGetComponent(out CompositeCollider2D comp))
            {
                comp.GenerateGeometry();
                for (int i = tilemapColumnas.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(tilemapColumnas.transform.GetChild(i).gameObject);
                }
            }
        }

        if (columnasSala != null && columnasSala.Length > 0)
        {
            for (int i = 0; i < columnasSala.Length; i++)
            {
                if (columnasSala[i] != null)
                {
                    if (prefabEfectoDestruccionColumna != null)
                    {
                        Instantiate(prefabEfectoDestruccionColumna, columnasSala[i].transform.position, Quaternion.identity);
                    }
                    Destroy(columnasSala[i]);
                }
            }
        }
    }

    private IEnumerator RutinaInvocarTresGhouls()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aModoInvulnerable);

        vida.ActivarInvulnerabilidad(duracionInvulnerableInvocacion);
        if (empuje != null)
        {
            empuje.CancelarEmpuje();
            empuje.InmuneAEmpuje = true;
        }
        sprite.color = colorInvulnerable;

        Vector2 centroJefe = transform.position;
        Vector2[] posicionesSpawn = new Vector2[3]
        {
            AjustarPuntoDentroDeArena(centroJefe + Vector2.left * distanciaSpawnGhoul),
            AjustarPuntoDentroDeArena(centroJefe + Vector2.right * distanciaSpawnGhoul),
            AjustarPuntoDentroDeArena(centroJefe + Vector2.down * distanciaSpawnGhoul)
        };

        srLienzo.enabled = true;
        float tiempoSellos = Mathf.Min(tiempoAparicionGhouls, duracionInvulnerableInvocacion);
        float t = 0f;

        while (t < tiempoSellos && !vencida)
        {
            t += Time.deltaTime;
            Array.Copy(bufferLimpio, bufferTrabajo, bufferLimpio.Length);

            if (Mathf.Sin(t * 28f) >= -0.2f)
            {
                for (int i = 0; i < 3; i++)
                {
                    DibujarCirculoPixel(bufferTrabajo, posicionesSpawn[i], 0.55f, colorAvisoPixel);
                    DibujarCirculoPixel(bufferTrabajo, posicionesSpawn[i], 0.25f, colorImpactoPixel);
                }
            }

            AplicarLienzoPrincipal();
            yield return null;
        }

        LimpiarYApagarLienzoPrincipal();

        if (!vencida)
        {
            for (int i = 0; i < 3; i++)
            {
                SpawnearGhoulEnPosicion(posicionesSpawn[i]);
            }
        }

        float restoInvulnerable = Mathf.Max(0f, duracionInvulnerableInvocacion - tiempoSellos);
        if (restoInvulnerable > 0f && !vencida)
        {
            yield return new WaitForSeconds(restoInvulnerable);
        }

        if (empuje != null)
        {
            empuje.InmuneAEmpuje = inmuneAEmpujeSiempre;
        }
        sprite.color = colorBase;
        FinalizarAccion();
    }

    private Vector2 AjustarPuntoDentroDeArena(Vector2 puntoDeseado)
    {
        Bounds b = ObtenerBoundsArena();
        float margen = 0.7f;
        float x = Mathf.Clamp(puntoDeseado.x, b.min.x + margen, b.max.x - margen);
        float y = Mathf.Clamp(puntoDeseado.y, b.min.y + margen, b.max.y - margen);
        return new Vector2(x, y);
    }

    private void SpawnearGhoulEnPosicion(Vector2 posSpawn)
    {
        if (prefabGhoul == null) return;

        GameObject nuevo = Instantiate(prefabGhoul, posSpawn, Quaternion.identity);

        MonoBehaviour[] scripts = nuevo.GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null) scripts[i].enabled = true;
        }

        if (nuevo.TryGetComponent(out Vida vidaGhoul))
        {
            enemigosInvocados.Add(vidaGhoul);
        }
    }

    private void LimpiarEnemigosMuertos()
    {
        enemigosInvocados.RemoveAll(e => e == null);
    }

    private IEnumerator RutinaPatronesTouhou()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        ReproducirAnim(aLanzarPatronesTouhou);

        MostrarDestelloJefe(tiempoAvisoAntesAtaque);
        yield return new WaitForSeconds(tiempoAvisoAntesAtaque);

        int tipoPatron = Random.Range(0, 2);
        float anguloAcumulado = 0f;

        for (int o = 0; o < oleadasTouhou; o++)
        {
            if (vencida) { FinalizarAccion(); yield break; }

            if (tipoPatron == 0)
            {
                anguloAcumulado += 11f;
                for (int b = 0; b < brazosTouhou; b++)
                {
                    float angBase = (b * (360f / brazosTouhou));
                    float ang1 = (angBase + anguloAcumulado) * Mathf.Deg2Rad;
                    float ang2 = (angBase - anguloAcumulado) * Mathf.Deg2Rad;

                    InstanciarBala(new Vector2(Mathf.Cos(ang1), Mathf.Sin(ang1)), velBalaTouhou, 1, 6f);
                    InstanciarBala(new Vector2(Mathf.Cos(ang2), Mathf.Sin(ang2)), velBalaTouhou * 0.85f, 1, 6f);
                }
            }
            else
            {
                int petalos = brazosTouhou * 2;
                float desfase = Mathf.Sin(o * 0.45f) * 25f;
                for (int p = 0; p < petalos; p++)
                {
                    float ang = (p * (360f / petalos) + desfase) * Mathf.Deg2Rad;
                    float modVelocidad = 1f + 0.35f * Mathf.Sin(p * 2f + o * 0.5f);
                    InstanciarBala(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)), velBalaTouhou * modVelocidad, 1, 6f);
                }
            }

            yield return new WaitForSeconds(intervaloTouhou);
        }

        FinalizarAccion();
    }

    private IEnumerator RutinaCuadriculaLaser()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aLanzarCuadriculaLaser);

        MostrarDestelloJefe(tiempoCasteoCuadricula);
        StartCoroutine(RutinaCicloVidaCuadricula());

        yield return new WaitForSeconds(tiempoCasteoCuadricula);
        FinalizarAccion();
    }

    private IEnumerator RutinaCicloVidaCuadricula()
    {
        cuadriculaEnCurso = true;
        srLienzoCuadricula.enabled = true;

        Bounds b = ObtenerBoundsArena();
        float margen = 0.8f;
        float minX = b.min.x + margen;
        float maxX = b.max.x - margen;
        float minY = b.min.y + margen;
        float maxY = b.max.y - margen;

        float offsetX = Random.Range(-0.5f, 0.5f);
        float offsetY = Random.Range(-0.5f, 0.5f);

        float[] coordsX = new float[lineasVerticales];
        float[] coordsY = new float[lineasHorizontales];

        for (int i = 0; i < lineasVerticales; i++)
        {
            float t = (i + 1) / (float)(lineasVerticales + 1);
            coordsX[i] = Mathf.Lerp(minX, maxX, t) + offsetX;
        }

        for (int j = 0; j < lineasHorizontales; j++)
        {
            float t = (j + 1) / (float)(lineasHorizontales + 1);
            coordsY[j] = Mathf.Lerp(minY, maxY, t) + offsetY;
        }

        float timer = 0f;
        while (timer < tiempoAvisoCuadricula)
        {
            if (vencida) { LimpiarYApagarLienzoCuadricula(); yield break; }
            timer += Time.deltaTime;
            float frec = timer < tiempoAvisoCuadricula * 0.7f ? 16f : 34f;

            Array.Copy(bufferLimpio, bufferTrabajoCuadricula, bufferLimpio.Length);
            if (Mathf.Sin(timer * frec) >= -0.15f)
            {
                for (int i = 0; i < lineasVerticales; i++)
                {
                    DibujarLineaMundoPixel(bufferTrabajoCuadricula, new Vector2(coordsX[i], b.min.y), new Vector2(coordsX[i], b.max.y), 1, colorAvisoPixel, true);
                }
                for (int j = 0; j < lineasHorizontales; j++)
                {
                    DibujarLineaMundoPixel(bufferTrabajoCuadricula, new Vector2(b.min.x, coordsY[j]), new Vector2(b.max.x, coordsY[j]), 1, colorAvisoPixel, true);
                }
            }
            AplicarLienzoCuadricula();
            yield return null;
        }

        timer = 0f;
        float timerDanio = 0f;
        int ppu = Mathf.Max(8, pixelesPorUnidad);

        while (timer < duracionCuadricula)
        {
            if (vencida) { LimpiarYApagarLienzoCuadricula(); yield break; }
            timer += Time.deltaTime;
            timerDanio -= Time.deltaTime;

            int pulso = Mathf.RoundToInt(Mathf.Sin(Time.time * 32f) * 2f);
            int grosorExt = Mathf.Max(2, grosorLaserCuadriculaPx + pulso);
            int grosorInt = Mathf.Max(1, Mathf.RoundToInt(grosorExt * 0.55f));

            Array.Copy(bufferLimpio, bufferTrabajoCuadricula, bufferLimpio.Length);

            for (int i = 0; i < lineasVerticales; i++)
            {
                Vector2 a = new Vector2(coordsX[i], b.min.y);
                Vector2 fin = new Vector2(coordsX[i], b.max.y);
                DibujarLineaMundoPixel(bufferTrabajoCuadricula, a, fin, grosorExt, colorAvisoPixel, false);
                DibujarLineaMundoPixel(bufferTrabajoCuadricula, a, fin, grosorInt, colorImpactoPixel, false);
            }

            for (int j = 0; j < lineasHorizontales; j++)
            {
                Vector2 a = new Vector2(b.min.x, coordsY[j]);
                Vector2 fin = new Vector2(b.max.x, coordsY[j]);
                DibujarLineaMundoPixel(bufferTrabajoCuadricula, a, fin, grosorExt, colorAvisoPixel, false);
                DibujarLineaMundoPixel(bufferTrabajoCuadricula, a, fin, grosorInt, colorImpactoPixel, false);
            }

            AplicarLienzoCuadricula();

            if (timerDanio <= 0f)
            {
                timerDanio = 0.12f;
                float anchoMundo = grosorExt / (float)ppu;

                for (int i = 0; i < lineasVerticales; i++)
                {
                    Vector2 c = new Vector2(coordsX[i], b.center.y);
                    AplicarDanioCaja(c, new Vector2(anchoMundo, b.size.y), 0f, danioLaserCuadricula, Vector2.right, 5f);
                }
                for (int j = 0; j < lineasHorizontales; j++)
                {
                    Vector2 c = new Vector2(b.center.x, coordsY[j]);
                    AplicarDanioCaja(c, new Vector2(b.size.x, anchoMundo), 0f, danioLaserCuadricula, Vector2.up, 5f);
                }
            }

            yield return null;
        }

        LimpiarYApagarLienzoCuadricula();
    }

    private void DibujarCajaRotadaPixel(Color32[] buffer, Vector2 centroMundo, Vector2 tamano, float anguloGrados, Color32 color, bool rellenar)
    {
        float rad = anguloGrados * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 extX = dir * (tamano.x * 0.5f);
        Vector2 extY = perp * (tamano.y * 0.5f);

        Vector2 c1 = centroMundo - extX - extY;
        Vector2 c2 = centroMundo + extX - extY;
        Vector2 c3 = centroMundo + extX + extY;
        Vector2 c4 = centroMundo - extX + extY;

        DibujarLineaMundoPixel(buffer, c1, c2, 1, color, !rellenar);
        DibujarLineaMundoPixel(buffer, c2, c3, 1, color, !rellenar);
        DibujarLineaMundoPixel(buffer, c3, c4, 1, color, !rellenar);
        DibujarLineaMundoPixel(buffer, c4, c1, 1, color, !rellenar);

        if (rellenar)
        {
            int pasos = Mathf.CeilToInt(tamano.y * pixelesPorUnidad);
            for (int i = -pasos / 2; i <= pasos / 2; i += 2)
            {
                float offset = i / (float)pixelesPorUnidad;
                Vector2 pInicio = centroMundo - extX + perp * offset;
                Vector2 pFin = centroMundo + extX + perp * offset;
                DibujarLineaMundoPixel(buffer, pInicio, pFin, 2, color, false);
            }
        }
    }

    private void DibujarCirculoPixel(Color32[] buffer, Vector2 centroMundo, float radioMundo, Color32 color)
    {
        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int cx = Mathf.RoundToInt((centroMundo.x - esquinaMinLienzo.x) * ppu);
        int cy = Mathf.RoundToInt((centroMundo.y - esquinaMinLienzo.y) * ppu);
        int r = Mathf.RoundToInt(radioMundo * ppu);

        int x = r;
        int y = 0;
        int err = 0;

        while (x >= y)
        {
            PintarPixelSeguro(buffer, cx + x, cy + y, color);
            PintarPixelSeguro(buffer, cx + y, cy + x, color);
            PintarPixelSeguro(buffer, cx - y, cy + x, color);
            PintarPixelSeguro(buffer, cx - x, cy + y, color);
            PintarPixelSeguro(buffer, cx - x, cy - y, color);
            PintarPixelSeguro(buffer, cx - y, cy - x, color);
            PintarPixelSeguro(buffer, cx + y, cy - x, color);
            PintarPixelSeguro(buffer, cx + x, cy - y, color);

            y += 1;
            err += 1 + 2 * y;
            if (2 * (err - x) + 1 > 0)
            {
                x -= 1;
                err += 1 - 2 * x;
            }
        }
    }

    private void DibujarLineaMundoPixel(Color32[] buffer, Vector2 aMundo, Vector2 bMundo, int grosorPx, Color32 color, bool punteada)
    {
        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int x0 = Mathf.RoundToInt((aMundo.x - esquinaMinLienzo.x) * ppu);
        int y0 = Mathf.RoundToInt((aMundo.y - esquinaMinLienzo.y) * ppu);
        int x1 = Mathf.RoundToInt((bMundo.x - esquinaMinLienzo.x) * ppu);
        int y1 = Mathf.RoundToInt((bMundo.y - esquinaMinLienzo.y) * ppu);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int paso = 0;

        while (true)
        {
            if (!punteada || (paso % 4 != 3))
            {
                PintarCuadroPixel(buffer, x0, y0, grosorPx, color);
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
            paso++;
        }
    }

    private void PintarCuadroPixel(Color32[] buffer, int cx, int cy, int grosor, Color32 color)
    {
        int inicio = -(grosor / 2);
        int fin = inicio + grosor;

        for (int ox = inicio; ox < fin; ox++)
        {
            for (int oy = inicio; oy < fin; oy++)
            {
                PintarPixelSeguro(buffer, cx + ox, cy + oy, color);
            }
        }
    }

    private void PintarPixelSeguro(Color32[] buffer, int x, int y, Color32 color)
    {
        if (x >= 0 && x < anchoTex && y >= 0 && y < altoTex)
        {
            buffer[y * anchoTex + x] = color;
        }
    }

    private void AplicarLienzoPrincipal()
    {
        if (texLienzo == null) return;
        texLienzo.SetPixels32(bufferTrabajo);
        texLienzo.Apply(false);
    }

    private void AplicarLienzoCuadricula()
    {
        if (texLienzoCuadricula == null) return;
        texLienzoCuadricula.SetPixels32(bufferTrabajoCuadricula);
        texLienzoCuadricula.Apply(false);
    }

    private Bounds ObtenerBoundsArena()
    {
        if (limitesArena != null) return limitesArena.bounds;
        if (salaJefe != null && salaJefe.TryGetComponent(out Collider2D colSala)) return colSala.bounds;
        return new Bounds(transform.position, new Vector3(tamanoArenaFallback * 2f, tamanoArenaFallback * 2f, 1f));
    }

    private Bullet InstanciarBala(Vector2 direccion, float velocidad, int danio, float tiempoVidaBala)
    {
        if (prefabBullet == null) return null;

        Vector3 pos = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Bullet b = Instantiate(prefabBullet, pos, Quaternion.identity);

        if (miCol != null && b.TryGetComponent(out Collider2D colBala))
        {
            Physics2D.IgnoreCollision(miCol, colBala, true);
        }

        b.Init(direccion, velocidad, danio, tiempoVidaBala);
        return b;
    }

    private void AplicarDanioCaja(Vector2 centro, Vector2 tamano, float anguloGrados, int danio, Vector2 dirEmpuje, float fEmpuje)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(centro, tamano, anguloGrados, capaJugador);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) continue;

            if (c.TryGetComponent(out Vida v))
            {
                if (v.EsInvulnerable) continue;
                v.RecibirDanio(danio);
            }
            if (c.TryGetComponent(out Empuje emp))
            {
                emp.AplicarEmpuje(dirEmpuje, fEmpuje);
            }
        }
    }

    private void FinalizarAccion()
    {
        ejecutandoAccion = false;
        velObjetivo = Vector2.zero;
        timerPausaAtaque = enFase2 ? pausaEntreAtaques * 0.75f : pausaEntreAtaques;
    }

    private void ReproducirAnim(AnimationClip clip)
    {
        if (anim == null || clip == null) return;
        if (animActual == clip.name) return;
        anim.Play(clip.name);
        animActual = clip.name;
    }

    private void EjecutarDerrota()
    {
        if (vencida) return;
        vencida = true;

        StopAllCoroutines();
        if (indicador != null) indicador.CancelarAviso();
        LimpiarYApagarLienzoPrincipal();
        LimpiarYApagarLienzoCuadricula();
        if (sprite != null) sprite.color = colorBase;

        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aVencida);

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
            GestorLuz.Instancia.ReiniciarLuz();
        }

        for (int i = 0; i < enemigosInvocados.Count; i++)
        {
            if (enemigosInvocados[i] != null)
            {
                enemigosInvocados[i].ForzarMuerte();
            }
        }

        StartCoroutine(SecuenciaPostDerrota());
    }

    private IEnumerator SecuenciaPostDerrota()
    {
        float espera = aVencida != null ? Mathf.Max(aVencida.length, esperaAntesDeDialogoFinal) : esperaAntesDeDialogoFinal;
        yield return new WaitForSeconds(espera);

        BloquearControlesJugadorParaDialogo(true);

        yield return StartCoroutine(ReproducirLineasDialogo(dialogosDerrota));

        BloquearControlesJugadorParaDialogo(false);

        if (portalSalida != null)
        {
            portalSalida.EncenderPortal();
        }
    }

    private IEnumerator ReproducirLineasDialogo(LineaDialogoJefe[] lineas)
    {
        if (lineas == null || lineas.Length == 0 || panelDialogoUI == null) yield break;

        panelDialogoUI.SetActive(true);

        for (int i = 0; i < lineas.Length; i++)
        {
            LineaDialogoJefe actual = lineas[i];

            if (textoNombreUI != null) textoNombreUI.text = actual.nombreHablante;
            if (textoContenidoUI != null) textoContenidoUI.text = string.Empty;

            if (iconoContinuarEspacioUI != null)
            {
                iconoContinuarEspacioUI.SetActive(!mostrarIconoSoloAlTerminarLinea);
            }

            if (imagenRetratoUI != null)
            {
                if (!posBaseRetratoGuardada)
                {
                    posBaseRetratoUI = imagenRetratoUI.rectTransform.anchoredPosition;
                    posBaseRetratoGuardada = true;
                }

                if (actual.ilustracionHablante != null)
                {
                    imagenRetratoUI.sprite = actual.ilustracionHablante;
                    imagenRetratoUI.preserveAspect = true;
                    imagenRetratoUI.rectTransform.anchoredPosition = posBaseRetratoUI + actual.offsetIlustracion;

                    Vector3 escala = imagenRetratoUI.rectTransform.localScale;
                    escala.x = Mathf.Abs(escala.x) * (actual.voltearIlustracionX ? -1f : 1f);
                    imagenRetratoUI.rectTransform.localScale = escala;

                    imagenRetratoUI.enabled = true;
                    imagenRetratoUI.gameObject.SetActive(true);
                }
                else
                {
                    imagenRetratoUI.gameObject.SetActive(false);
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

            if (iconoContinuarEspacioUI != null)
            {
                iconoContinuarEspacioUI.SetActive(true);
            }

            yield return null;
            while (!PresionoAvanzarDialogo())
            {
                yield return null;
            }
        }

        if (iconoContinuarEspacioUI != null) iconoContinuarEspacioUI.SetActive(false);
        if (imagenRetratoUI != null)
        {
            imagenRetratoUI.rectTransform.anchoredPosition = posBaseRetratoUI;
            imagenRetratoUI.gameObject.SetActive(false);
        }
        panelDialogoUI.SetActive(false);
    }

    private bool PresionoAvanzarDialogo()
    {
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
    }

    void OnDisable()
    {
        if (vencida) return;
        StopAllCoroutines();
        if (indicador != null) indicador.CancelarAviso();
        LimpiarYApagarLienzoPrincipal();
        LimpiarYApagarLienzoCuadricula();
        if (sprite != null) sprite.color = colorBase;
        ejecutandoAccion = false;
        dasheando = false;
    }

    void OnDestroy()
    {
        if (texLienzo != null) Destroy(texLienzo);
        if (objLienzo != null) Destroy(objLienzo);
        if (texLienzoCuadricula != null) Destroy(texLienzoCuadricula);
        if (objLienzoCuadricula != null) Destroy(objLienzoCuadricula);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.left * distanciaSpawnGhoul, 0.4f);
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.right * distanciaSpawnGhoul, 0.4f);
        Gizmos.DrawWireSphere((Vector2)transform.position + Vector2.down * distanciaSpawnGhoul, 0.4f);
    }
}
