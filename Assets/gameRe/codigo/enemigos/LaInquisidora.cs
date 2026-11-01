using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Vida))]
public class LaInquisidora : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias Principales")]
    [SerializeField] private Transform player;
    [SerializeField] private Bullet prefabBullet;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private LayerMask capaJugador;
    [SerializeField] private Sala salaJefe;
    [SerializeField] private Collider2D limitesArena;
    [SerializeField, Min(0.1f)] private float tiempoBrilloAntesAtaque = 0.4f;

    [Header("Movimiento y Dash (Sin Invulnerabilidad)")]
    [SerializeField, Min(0f)] private float velocidadBase = 3.2f;
    [SerializeField, Min(0f)] private float aceleracion = 20f;
    [SerializeField, Min(0f)] private float desaceleracion = 25f;
    [SerializeField, Min(0f)] private float rangoDeteccion = 16f;
    [SerializeField, Min(0f)] private float velocidadDash = 14f;
    [SerializeField, Min(0f)] private float duracionDash = 0.22f;
    [SerializeField, Min(0f)] private float cdDash = 3f;
    [SerializeField, Min(0f)] private float pausaEntreAtaques = 1.1f;

    [Header("Fase 1: 1. Melee Grande (Hitbox)")]
    [SerializeField, Min(0f)] private float distAcercamientoMelee = 2f;
    [SerializeField, Min(0f)] private float tiempoQuietoAntesMelee = 0.45f;
    [SerializeField] private Vector2 tamHitboxMeleeGrande = new Vector2(3.2f, 2.8f);
    [SerializeField, Min(0f)] private float offsetMeleeGrande = 1.6f;
    [SerializeField, Min(1)] private int danioMeleeGrande = 2;
    [SerializeField, Min(0f)] private float empujeMeleeGrande = 10f;

    [Header("Fase 1: 2. Oleadas Radiales ZigZag")]
    [SerializeField, Min(1)] private int cantidadOleadasZigZag = 3;
    [SerializeField, Min(3)] private int balasPorOleadaZigZag = 12;
    [SerializeField, Min(0.05f)] private float intervaloOleadasZigZag = 0.45f;
    [SerializeField, Min(0f)] private float velBalaZigZag = 6.5f;
    [SerializeField, Min(0f)] private float frecuenciaZigZag = 10f;
    [SerializeField, Min(0f)] private float amplitudZigZag = 4.5f;

    [Header("Fase 1: 3. Metralleta Imprecisa")]
    [SerializeField, Min(1)] private int cantidadBalasMetralleta = 28;
    [SerializeField, Min(0.01f)] private float cadenciaMetralleta = 0.06f;
    [SerializeField, Min(0f)] private float anguloImprecision = 22f;
    [SerializeField, Min(0f)] private float velBalaMetralleta = 10.5f;

    [Header("Fase 1: 4. Super Ataque Melee Largo (Hitbox)")]
    [SerializeField, Min(0f)] private float velMovimientoLento = 0.85f;
    [SerializeField, Min(0f)] private float tiempoCargaSuperMelee = 1.1f;
    [SerializeField] private Vector2 tamHitboxSuperMelee = new Vector2(7.5f, 1.1f);
    [SerializeField, Min(0f)] private float offsetSuperMelee = 3.75f;
    [SerializeField, Min(1)] private int danioSuperMelee = 2;
    [SerializeField, Min(0f)] private float empujeSuperMelee = 13f;
    [SerializeField] private Color colorAvisoMeleeLargo = new Color(1f, 0.5f, 0f, 0.45f);
    [SerializeField] private Color colorImpactoMeleeLargo = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Fase 2: Transición y Columnas")]
    [SerializeField] private GameObject[] columnasSala;
    [SerializeField] private GameObject prefabEfectoDestruccionColumna;

    [Header("Fase 2: 1. Invocación e Invulnerabilidad")]
    [SerializeField, Min(0f)] private float duracionInvulnerableInvocacion = 5f;
    [SerializeField, Min(1)] private int cantidadEnemigosInvocar = 5;
    [SerializeField] private GameObject[] prefabsEnemigosRandom;
    [SerializeField] private Transform[] puntosSpawnEnemigos;
    [SerializeField] private Color colorInvulnerable = new Color(0.6f, 0.85f, 1f, 0.75f);

    [Header("Fase 2: 2. Patrones Touhou (2 Tipos)")]
    [SerializeField, Min(1)] private int oleadasTouhou = 16;
    [SerializeField, Min(3)] private int brazosTouhou = 6;
    [SerializeField, Min(0.02f)] private float intervaloTouhou = 0.12f;
    [SerializeField, Min(0f)] private float velBalaTouhou = 6f;

    [Header("Fase 2: 3. Cuadrícula de Láseres Estáticos")]
    [SerializeField, Min(1)] private int lineasVerticales = 4;
    [SerializeField, Min(1)] private int lineasHorizontales = 3;
    [SerializeField, Min(0f)] private float tamanoArenaFallback = 12f;
    [SerializeField, Min(0.1f)] private float tiempoAvisoCuadricula = 1.1f;
    [SerializeField, Min(0.1f)] private float duracionCuadricula = 0.65f;
    [SerializeField, Min(0.02f)] private float grosorAvisoLaser = 0.08f;
    [SerializeField, Min(0.1f)] private float grosorDisparoLaser = 0.55f;
    [SerializeField] private Color colorAvisoLaser = new Color(1f, 0f, 0.2f, 0.45f);
    [SerializeField] private Color colorDisparoLaser = new Color(1f, 0.1f, 0.3f, 1f);
    [SerializeField, Min(1)] private int danioLaserCuadricula = 1;

    [Header("Animaciones (Opcional)")]
    [SerializeField] private Animator anim;
    [SerializeField] private AnimationClip aIdle;
    [SerializeField] private AnimationClip aMover;
    [SerializeField] private AnimationClip aDash;
    [SerializeField] private AnimationClip aAtaqueMelee;
    [SerializeField] private AnimationClip aDisparo;
    [SerializeField] private AnimationClip aInvocar;

    public float RangoDeteccion => rangoDeteccion;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D miCol;
    private Vida vida;
    private Empuje empuje;
    private Aturdido aturdido;
    private IndicadorAtaque indicador;
    private LineRenderer lineaGuia;

    private bool barraJefeActivada;
    private bool enFase2;
    private bool ejecutandoAccion;
    private bool dasheando;
    private float timerCdDash;
    private float timerPausaAtaque;
    private Vector2 velObjetivo;
    private Vector2 dirDash;
    private Color colorBase;
    private string animActual;
    private int ultimoAtaqueF1 = -1;
    private int ultimoAtaqueF2 = -1;

    private readonly List<Vida> enemigosInvocados = new List<Vida>();

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out sprite);
        TryGetComponent(out miCol);
        TryGetComponent(out vida);
        TryGetComponent(out empuje);
        TryGetComponent(out aturdido);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
        if (!anim) TryGetComponent(out anim);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        colorBase = sprite.color;
        CrearLineaGuia();
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

        vida.AlRecibirDanio.AddListener(VerificarCambioDeFase);
        vida.AlMorir.AddListener(LimpiarAlMorir);
        timerCdDash = cdDash;
        timerPausaAtaque = 1f;
    }

    private void CrearLineaGuia()
    {
        GameObject objGuia = new GameObject("GuiaSuperMelee");
        objGuia.transform.SetParent(transform, false);
        lineaGuia = objGuia.AddComponent<LineRenderer>();
        lineaGuia.positionCount = 2;
        lineaGuia.useWorldSpace = true;
        Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s != null) lineaGuia.material = new Material(s);
        lineaGuia.sortingOrder = sprite.sortingOrder + 1;
        lineaGuia.enabled = false;
    }

    void Update()
    {
        if (!player) return;

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
            ReproducirAnim(aIdle);
            return;
        }

        if (!barraJefeActivada)
        {
            barraJefeActivada = true;
            if (UIBarraJefe.Instancia != null)
            {
                UIBarraJefe.Instancia.VincularJefe(vida);
            }
        }

        if (ejecutandoAccion || Bloqueado()) return;

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
            ReproducirAnim(velObjetivo.sqrMagnitude > 0.1f ? aMover : aIdle);
        }
    }

    void FixedUpdate()
    {
        if (aturdido != null && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desaceleracion * Time.fixedDeltaTime);
            return;
        }

        if (empuje != null && empuje.EstaEmpujado) return;

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
        if (enFase2 || vida.VidaActual <= 0) return;

        if (vida.VidaActual <= vida.VidaMaxima * 0.5f)
        {
            enFase2 = true;
            StopAllCoroutines();
            indicador.CancelarAviso();
            lineaGuia.enabled = false;
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
            int atk = ObtenerIndiceAleatorio(4, ref ultimoAtaqueF2);
            switch (atk)
            {
                case 0:
                    StartCoroutine(RutinaInvocarEnemigos());
                    break;
                case 1:
                    int atkF1 = Random.Range(0, 4);
                    EjecutarAtaqueFase1(atkF1);
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

    private IEnumerator RutinaDash()
    {
        ejecutandoAccion = true;
        dasheando = true;
        timerCdDash = cdDash;

        Vector2 haciaJugador = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 perpendicular = new Vector2(-haciaJugador.y, haciaJugador.x) * (Random.value > 0.5f ? 1f : -1f);
        dirDash = (haciaJugador * 0.65f + perpendicular * 0.35f).normalized;

        if (Mathf.Abs(dirDash.x) > 0.1f) sprite.flipX = dirDash.x < 0f;
        ReproducirAnim(aDash != null ? aDash : aMover);

        float t = duracionDash;
        while (t > 0f)
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
        ReproducirAnim(aMover);

        float tiempoMaxAcercamiento = 2.5f;
        while (tiempoMaxAcercamiento > 0f && player != null && Vector2.Distance(transform.position, player.position) > distAcercamientoMelee)
        {
            if (Bloqueado()) { FinalizarAccion(); yield break; }
            tiempoMaxAcercamiento -= Time.deltaTime;
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            velObjetivo = dir * (velocidadBase * 1.35f);
            yield return null;
        }

        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aIdle);

        Vector2 dirAtaque = player != null ? ((Vector2)player.position - (Vector2)transform.position).normalized : Vector2.down;
        if (dirAtaque == Vector2.zero) dirAtaque = Vector2.down;

        indicador.MostrarAviso(tiempoQuietoAntesMelee);
        yield return new WaitForSeconds(tiempoQuietoAntesMelee);
        if (Bloqueado()) { FinalizarAccion(); yield break; }

        ReproducirAnim(aAtaqueMelee);
        Vector2 centro = (Vector2)transform.position + dirAtaque * offsetMeleeGrande;
        float angulo = Mathf.Atan2(dirAtaque.y, dirAtaque.x) * Mathf.Rad2Deg;

        AplicarDanioCaja(centro, tamHitboxMeleeGrande, angulo, danioMeleeGrande, dirAtaque, empujeMeleeGrande);

        yield return new WaitForSeconds(0.35f);
        FinalizarAccion();
    }

    private IEnumerator RutinaRadialZigZag()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        ReproducirAnim(aDisparo);

        indicador.MostrarAviso(tiempoBrilloAntesAtaque);
        yield return new WaitForSeconds(tiempoBrilloAntesAtaque);

        for (int o = 0; o < cantidadOleadasZigZag; o++)
        {
            if (Bloqueado()) { FinalizarAccion(); yield break; }

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
        ReproducirAnim(aDisparo);

        indicador.MostrarAviso(tiempoBrilloAntesAtaque);
        yield return new WaitForSeconds(tiempoBrilloAntesAtaque);

        for (int i = 0; i < cantidadBalasMetralleta; i++)
        {
            if (Bloqueado() || player == null) { FinalizarAccion(); yield break; }

            Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
            Vector2 haciaJugador = ((Vector2)player.position - (Vector2)origen).normalized;
            float anguloBase = Mathf.Atan2(haciaJugador.y, haciaJugador.x) * Mathf.Rad2Deg;
            float anguloFinal = (anguloBase + Random.Range(-anguloImprecision, anguloImprecision)) * Mathf.Deg2Rad;

            Vector2 dirBala = new Vector2(Mathf.Cos(anguloFinal), Mathf.Sin(anguloFinal));
            InstanciarBala(dirBala, velBalaMetralleta, 1, 4f);

            yield return new WaitForSeconds(cadenciaMetralleta);
        }

        FinalizarAccion();
    }

    private IEnumerator RutinaSuperMeleeLargo()
    {
        ejecutandoAccion = true;
        ReproducirAnim(aAtaqueMelee);

        indicador.MostrarAviso(tiempoCargaSuperMelee);

        float t = tiempoCargaSuperMelee;
        Vector2 dirApuntado = Vector2.down;

        lineaGuia.enabled = true;
        lineaGuia.startColor = colorAvisoMeleeLargo;
        lineaGuia.endColor = colorAvisoMeleeLargo;
        lineaGuia.widthMultiplier = tamHitboxSuperMelee.y;

        while (t > 0f)
        {
            if (Bloqueado())
            {
                lineaGuia.enabled = false;
                indicador.CancelarAviso();
                FinalizarAccion();
                yield break;
            }

            t -= Time.deltaTime;
            if (player != null)
            {
                Vector2 hacia = (Vector2)player.position - (Vector2)transform.position;
                if (hacia.sqrMagnitude > 0.01f) dirApuntado = hacia.normalized;
            }

            velObjetivo = dirApuntado * velMovimientoLento;
            float distanciaVisual = offsetSuperMelee + (tamHitboxSuperMelee.x * 0.5f);
            lineaGuia.SetPosition(0, transform.position);
            lineaGuia.SetPosition(1, (Vector2)transform.position + dirApuntado * distanciaVisual);
            yield return null;
        }

        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        lineaGuia.startColor = colorImpactoMeleeLargo;
        lineaGuia.endColor = colorImpactoMeleeLargo;

        Vector2 centro = (Vector2)transform.position + dirApuntado * offsetSuperMelee;
        float angulo = Mathf.Atan2(dirApuntado.y, dirApuntado.x) * Mathf.Rad2Deg;
        AplicarDanioCaja(centro, tamHitboxSuperMelee, angulo, danioSuperMelee, dirApuntado, empujeSuperMelee);

        yield return new WaitForSeconds(0.2f);
        lineaGuia.enabled = false;
        yield return new WaitForSeconds(0.25f);
        FinalizarAccion();
    }

    private IEnumerator RutinaTransicionFase2()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        DestruirColumnasDeLaSala();
        yield return StartCoroutine(RutinaInvocarEnemigos());
    }

    private void DestruirColumnasDeLaSala()
    {
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

        GameObject[] porTag = GameObject.FindGameObjectsWithTag("Columna");
        for (int i = 0; i < porTag.Length; i++)
        {
            if (prefabEfectoDestruccionColumna != null)
            {
                Instantiate(prefabEfectoDestruccionColumna, porTag[i].transform.position, Quaternion.identity);
            }
            Destroy(porTag[i]);
        }
    }

    private IEnumerator RutinaInvocarEnemigos()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        ReproducirAnim(aInvocar != null ? aInvocar : aIdle);

        vida.ActivarInvulnerabilidad(duracionInvulnerableInvocacion);
        sprite.color = colorInvulnerable;

        float intervaloSpawn = duracionInvulnerableInvocacion / Mathf.Max(1, cantidadEnemigosInvocar + 1);

        for (int i = 0; i < cantidadEnemigosInvocar; i++)
        {
            yield return new WaitForSeconds(intervaloSpawn);
            SpawnearEnemigoAleatorio(i);
        }

        yield return new WaitForSeconds(intervaloSpawn);
        sprite.color = colorBase;
        FinalizarAccion();
    }

    private void SpawnearEnemigoAleatorio(int indice)
    {
        if (prefabsEnemigosRandom == null || prefabsEnemigosRandom.Length == 0) return;

        GameObject prefab = prefabsEnemigosRandom[Random.Range(0, prefabsEnemigosRandom.Length)];
        if (prefab == null) return;

        Vector2 posSpawn = ObtenerPosicionSpawn(indice);
        GameObject nuevo = Instantiate(prefab, posSpawn, Quaternion.identity);

        MonoBehaviour[] scripts = nuevo.GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null) scripts[i].enabled = true;
        }

        if (nuevo.TryGetComponent(out Vida vidaEnemigo))
        {
            enemigosInvocados.Add(vidaEnemigo);
        }
    }

    private Vector2 ObtenerPosicionSpawn(int indice)
    {
        if (puntosSpawnEnemigos != null && puntosSpawnEnemigos.Length > 0)
        {
            Transform pt = puntosSpawnEnemigos[indice % puntosSpawnEnemigos.Length];
            if (pt != null) return pt.position;
        }

        int capaEnemigo = LayerMask.NameToLayer("Enemy");
        for (int intento = 0; intento < 20; intento++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(2.2f, 4.5f);
            Vector2 cand = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (limitesArena != null && !limitesArena.bounds.Contains(cand)) continue;

            Collider2D[] hits = Physics2D.OverlapCircleAll(cand, 0.5f);
            bool ocupado = false;
            for (int j = 0; j < hits.Length; j++)
            {
                if (!hits[j].isTrigger && hits[j].gameObject.layer != capaEnemigo && !hits[j].CompareTag("Player"))
                {
                    ocupado = true;
                    break;
                }
            }
            if (!ocupado) return cand;
        }

        return (Vector2)transform.position + Random.insideUnitCircle * 2f;
    }

    private IEnumerator RutinaPatronesTouhou()
    {
        ejecutandoAccion = true;
        velObjetivo = Vector2.zero;
        ReproducirAnim(aDisparo);

        indicador.MostrarAviso(tiempoBrilloAntesAtaque);
        yield return new WaitForSeconds(tiempoBrilloAntesAtaque);

        int tipoPatron = Random.Range(0, 2);
        float anguloAcumulado = 0f;

        for (int o = 0; o < oleadasTouhou; o++)
        {
            if (Bloqueado()) { FinalizarAccion(); yield break; }

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
        ReproducirAnim(aInvocar != null ? aInvocar : aDisparo);

        indicador.MostrarAviso(tiempoAvisoCuadricula);

        Bounds b = ObtenerBoundsArena();
        float margen = 0.8f;
        float minX = b.min.x + margen;
        float maxX = b.max.x - margen;
        float minY = b.min.y + margen;
        float maxY = b.max.y - margen;

        float offsetAleatorioX = Random.Range(-0.6f, 0.6f);
        float offsetAleatorioY = Random.Range(-0.6f, 0.6f);

        for (int i = 1; i <= lineasVerticales; i++)
        {
            float t = i / (float)(lineasVerticales + 1);
            float x = Mathf.Lerp(minX, maxX, t) + offsetAleatorioX;
            CrearLineaCuadricula(new Vector2(x, b.min.y), new Vector2(x, b.max.y));
        }

        for (int j = 1; j <= lineasHorizontales; j++)
        {
            float t = j / (float)(lineasHorizontales + 1);
            float y = Mathf.Lerp(minY, maxY, t) + offsetAleatorioY;
            CrearLineaCuadricula(new Vector2(b.min.x, y), new Vector2(b.max.x, y));
        }

        yield return new WaitForSeconds(tiempoAvisoCuadricula + duracionCuadricula);
        FinalizarAccion();
    }

    private void CrearLineaCuadricula(Vector2 a, Vector2 b)
    {
        GameObject obj = new GameObject("LaserCuadricula");
        LaserCuadricula lc = obj.AddComponent<LaserCuadricula>();
        lc.Inicializar(
            a, b,
            tiempoAvisoCuadricula, duracionCuadricula,
            grosorAvisoLaser, grosorDisparoLaser,
            colorAvisoLaser, colorDisparoLaser,
            danioLaserCuadricula, empujeMeleeGrande * 0.6f,
            capaJugador
        );
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

    private bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    private void LimpiarAlMorir()
    {
        if (lineaGuia != null) lineaGuia.enabled = false;
        for (int i = 0; i < enemigosInvocados.Count; i++)
        {
            if (enemigosInvocados[i] != null)
            {
                enemigosInvocados[i].ForzarMuerte();
            }
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (indicador != null) indicador.CancelarAviso();
        if (lineaGuia != null) lineaGuia.enabled = false;
        if (sprite != null) sprite.color = colorBase;
        ejecutandoAccion = false;
        dasheando = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + Vector2.right * offsetMeleeGrande, tamHitboxMeleeGrande);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube((Vector2)transform.position + Vector2.right * offsetSuperMelee, tamHitboxSuperMelee);
    }
}
