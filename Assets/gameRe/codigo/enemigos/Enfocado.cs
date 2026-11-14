using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Enfocado : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform puntoOrigenLaser;

    [Header("Físicas y Alcance (Detección = Láser)")]
    [SerializeField, Min(0f)] private float desacel = 18f;
    [SerializeField, Min(1f)] private float rangoDet = 15f;

    [Header("Láser y Bloqueo por Pared")]
    [SerializeField] private LayerMask capaJugador;
    [SerializeField] private string tagPared = "pared";
    [SerializeField, Min(0f)] private float cdAtaque = 2.5f;
    [SerializeField, Min(0f)] private float fuerzaEmpujeLaser = 10f;
    [SerializeField, Min(1)] private int danioLaser = 1;

    [Header("Tiempos del Ataque")]
    [SerializeField, Min(0.05f)] private float tiempoApuntar = 1f;
    [SerializeField, Min(0.05f)] private float tiempoFijado = 0.35f;
    [SerializeField, Min(0.05f)] private float tiempoDisparo = 0.45f;

    [Header("Visuales Pixel Art (Mira y Láser)")]
    [SerializeField, Min(8)] private int pixelesPorUnidad = 32;
    [SerializeField] private Color colorMira = new Color(1f, 0.15f, 0.15f, 0.9f);
    [SerializeField, Min(1)] private int grosorMiraPixeles = 1;
    [SerializeField, Min(1f)] private float frecuenciaParpadeoApuntando = 10f;
    [SerializeField, Min(1f)] private float frecuenciaParpadeoFijado = 24f;
    [SerializeField] private bool lineaMiraPunteada = true;

    [Space]
    [SerializeField] private Color colorLaserBorde = new Color(1f, 0.1f, 0.2f, 1f);
    [SerializeField] private Color colorLaserNucleo = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField, Min(1)] private int grosorBaseLaserPixeles = 24;
    [SerializeField, Min(0)] private int amplitudPulsoPixeles = 4;
    [SerializeField, Range(0.1f, 0.9f)] private float proporcionNucleo = 0.6f;
    [SerializeField, Min(1f)] private float velocidadPulsoLaser = 28f;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aCaminarArriba;
    [SerializeField] private AnimationClip aCaminarAbajo;
    [SerializeField] private AnimationClip aCastearLaserArriba;
    [SerializeField] private AnimationClip aCastearLaserAbajo;

    public float RangoDeteccion => rangoDet;

    private Rigidbody2D rb;
    private Collider2D miCol;
    private Aturdido aturdido;
    private Empuje empuje;
    private IndicadorAtaque indicador;

    private enum Estado { Esperando, Apuntando, Fijado, Disparando }
    private Estado estadoActual = Estado.Esperando;

    private float timerCd;
    private float timerEstado;
    private float timerTickDanio;
    private Vector2 dirRayo = Vector2.down;
    private float distanciaActualRayo;
    private bool chocaConPared;
    private string animActual;

    private GameObject objLienzo;
    private SpriteRenderer srLienzo;
    private Texture2D texLaser;
    private Color32[] bufferLimpio;
    private Color32[] bufferPixeles;
    private int ladoTex;
    private Vector2 esquinaMinLienzo;
    private static Material materialPixelCompartido;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out miCol);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
        if (!anim) TryGetComponent(out anim);
        if (!sprite) TryGetComponent(out sprite);

        if (TryGetComponent(out LineRenderer lrAntiguo))
        {
            lrAntiguo.enabled = false;
        }

        InicializarLienzoPixel();
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
    }

    private int CalcularLadoTexturaRequerido()
    {
        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int margenGrosorUnidades = Mathf.CeilToInt((grosorBaseLaserPixeles + amplitudPulsoPixeles) / (float)ppu) + 2;
        int radioUnidades = Mathf.CeilToInt(rangoDet) + margenGrosorUnidades;
        return radioUnidades * 2 * ppu;
    }

    private void InicializarLienzoPixel()
    {
        if (materialPixelCompartido == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) materialPixelCompartido = new Material(s);
        }

        int ppu = Mathf.Max(8, pixelesPorUnidad);
        ladoTex = CalcularLadoTexturaRequerido();

        texLaser = new Texture2D(ladoTex, ladoTex, TextureFormat.RGBA32, false);
        texLaser.filterMode = FilterMode.Point;
        texLaser.wrapMode = TextureWrapMode.Clamp;

        bufferLimpio = new Color32[ladoTex * ladoTex];
        bufferPixeles = new Color32[ladoTex * ladoTex];
        texLaser.SetPixels32(bufferLimpio);
        texLaser.Apply(false);

        objLienzo = new GameObject("LienzoPixelLaser");
        objLienzo.transform.SetParent(transform, false);
        srLienzo = objLienzo.AddComponent<SpriteRenderer>();
        if (materialPixelCompartido != null) srLienzo.sharedMaterial = materialPixelCompartido;

        if (sprite != null)
        {
            srLienzo.sortingLayerID = sprite.sortingLayerID;
            srLienzo.sortingOrder = sprite.sortingOrder + 2;
        }
        else
        {
            srLienzo.sortingOrder = 15;
        }

        srLienzo.sprite = Sprite.Create(texLaser, new Rect(0, 0, ladoTex, ladoTex), new Vector2(0.5f, 0.5f), ppu);
        srLienzo.enabled = false;
    }

    void Update()
    {
        if (!player) return;

        if (Bloqueado() && estadoActual != Estado.Esperando)
        {
            estadoActual = Estado.Esperando;
            if (srLienzo != null) srLienzo.enabled = false;
            indicador.CancelarAviso();
            timerCd = cdAtaque;
        }

        switch (estadoActual)
        {
            case Estado.Esperando:
                timerCd -= Time.deltaTime;
                float dist = Vector2.Distance(transform.position, player.position);

                if (timerCd <= 0f && dist <= rangoDet && !Bloqueado())
                {
                    estadoActual = Estado.Apuntando;
                    timerEstado = tiempoApuntar;
                    if (srLienzo != null) srLienzo.enabled = true;
                }
                break;

            case Estado.Apuntando:
                timerEstado -= Time.deltaTime;
                Vector2 origen = ObtenerPuntoOrigen();
                Vector2 hacia = (Vector2)player.position - origen;
                if (hacia.sqrMagnitude > 0.001f) dirRayo = hacia.normalized;

                CalcularDistanciaHastaPared(origen, dirRayo);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Fijado;
                    timerEstado = tiempoFijado;
                    indicador.MostrarAviso(tiempoFijado);
                }
                break;

            case Estado.Fijado:
                timerEstado -= Time.deltaTime;
                CalcularDistanciaHastaPared(ObtenerPuntoOrigen(), dirRayo);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Disparando;
                    timerEstado = tiempoDisparo;
                    timerTickDanio = 0f;
                }
                break;

            case Estado.Disparando:
                timerEstado -= Time.deltaTime;
                timerTickDanio -= Time.deltaTime;

                CalcularDistanciaHastaPared(ObtenerPuntoOrigen(), dirRayo);

                if (timerTickDanio <= 0f)
                {
                    EjecutarDanioRayo();
                    timerTickDanio = 0.1f;
                }

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Esperando;
                    timerCd = cdAtaque;
                    if (srLienzo != null) srLienzo.enabled = false;
                }
                break;
        }

        ActualizarAnimaciones();
    }

    void LateUpdate()
    {
        if (estadoActual == Estado.Esperando || srLienzo == null || !srLienzo.enabled) return;
        RenderizarPixelArt();
    }

    void FixedUpdate()
    {
        if (!player) return;

        if (aturdido != null && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            return;
        }

        if (empuje != null && empuje.EstaEmpujado) return;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
    }

    private Vector2 ObtenerPuntoOrigen()
    {
        return puntoOrigenLaser != null ? (Vector2)puntoOrigenLaser.position : (Vector2)transform.position;
    }

    private void CalcularDistanciaHastaPared(Vector2 origen, Vector2 direccion)
    {
        distanciaActualRayo = rangoDet;
        chocaConPared = false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, direccion, rangoDet);
        float menorDist = rangoDet;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null || col == miCol || col.isTrigger) continue;

            if (EsParedBloqueante(col))
            {
                if (hits[i].distance < menorDist)
                {
                    menorDist = hits[i].distance;
                    chocaConPared = true;
                }
            }
        }

        distanciaActualRayo = menorDist;
    }

    private bool EsParedBloqueante(Collider2D col)
    {
        if (!string.IsNullOrEmpty(tagPared) && string.Equals(col.gameObject.tag, tagPared, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(col.gameObject.tag, "Columna", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private void RenderizarPixelArt()
    {
        int ppu = Mathf.Max(8, pixelesPorUnidad);
        int ladoRequerido = CalcularLadoTexturaRequerido();

        if (ladoRequerido != ladoTex || texLaser == null)
        {
            if (texLaser != null) Destroy(texLaser);
            if (objLienzo != null) Destroy(objLienzo);
            InicializarLienzoPixel();
            srLienzo.enabled = true;
        }

        Vector2 origen = ObtenerPuntoOrigen();

        float cx = Mathf.Round(origen.x * ppu) / ppu;
        float cy = Mathf.Round(origen.y * ppu) / ppu;
        objLienzo.transform.position = new Vector3(cx, cy, 0f);

        float mitadUnidades = (ladoTex / (float)ppu) * 0.5f;
        esquinaMinLienzo = new Vector2(cx - mitadUnidades, cy - mitadUnidades);

        Array.Copy(bufferLimpio, bufferPixeles, bufferLimpio.Length);

        Vector2 puntoFinal = origen + dirRayo * distanciaActualRayo;

        int x0 = Mathf.RoundToInt((origen.x - esquinaMinLienzo.x) * ppu);
        int y0 = Mathf.RoundToInt((origen.y - esquinaMinLienzo.y) * ppu);
        int x1 = Mathf.RoundToInt((puntoFinal.x - esquinaMinLienzo.x) * ppu);
        int y1 = Mathf.RoundToInt((puntoFinal.y - esquinaMinLienzo.y) * ppu);

        if (estadoActual == Estado.Apuntando || estadoActual == Estado.Fijado)
        {
            float frec = estadoActual == Estado.Apuntando ? frecuenciaParpadeoApuntando : frecuenciaParpadeoFijado;
            bool visibleEnParpadeo = Mathf.Sin(Time.time * frec * Mathf.PI) >= -0.15f;

            if (visibleEnParpadeo)
            {
                Color32 colMira32 = colorMira;
                DibujarLineaPixel(x0, y0, x1, y1, grosorMiraPixeles, colMira32, lineaMiraPunteada);

                if (chocaConPared)
                {
                    DibujarImpactoParedPixel(x1, y1, grosorMiraPixeles + 1, colMira32);
                }
            }
        }
        else if (estadoActual == Estado.Disparando)
        {
            float onda = Mathf.Sin(Time.time * velocidadPulsoLaser);
            int grosorExterior = Mathf.Max(1, grosorBaseLaserPixeles + Mathf.RoundToInt(onda * amplitudPulsoPixeles));
            int grosorInterior = Mathf.Max(1, Mathf.RoundToInt(grosorExterior * proporcionNucleo));

            Color32 colBorde32 = colorLaserBorde;
            Color32 colNucleo32 = colorLaserNucleo;

            DibujarLineaPixel(x0, y0, x1, y1, grosorExterior, colBorde32, false);
            if (grosorExterior >= 3)
            {
                DibujarLineaPixel(x0, y0, x1, y1, grosorInterior, colNucleo32, false);
            }

            if (chocaConPared)
            {
                DibujarImpactoParedPixel(x1, y1, grosorExterior + 2, colNucleo32);
            }
        }

        texLaser.SetPixels32(bufferPixeles);
        texLaser.Apply(false);
    }

    private void DibujarLineaPixel(int x0, int y0, int x1, int y1, int grosor, Color32 color, bool punteada)
    {
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
                PintarCuadroPixel(x0, y0, grosor, color);
            }

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
            paso++;
        }
    }

    private void DibujarImpactoParedPixel(int cx, int cy, int radioPixeles, Color32 color)
    {
        PintarCuadroPixel(cx, cy, Mathf.Max(2, radioPixeles / 2), color);

        int cantidadChispas = Mathf.Clamp(radioPixeles / 2, 5, 16);
        for (int i = 0; i < cantidadChispas; i++)
        {
            int rx = cx + UnityEngine.Random.Range(-radioPixeles, radioPixeles + 1);
            int ry = cy + UnityEngine.Random.Range(-radioPixeles, radioPixeles + 1);
            PintarCuadroPixel(rx, ry, Mathf.Max(1, radioPixeles / 8), color);
        }
    }

    private void PintarCuadroPixel(int cx, int cy, int grosor, Color32 color)
    {
        int offsetInicio = -(grosor / 2);
        int offsetFin = offsetInicio + grosor;

        for (int ox = offsetInicio; ox < offsetFin; ox++)
        {
            for (int oy = offsetInicio; oy < offsetFin; oy++)
            {
                int px = cx + ox;
                int py = cy + oy;
                if (px >= 0 && px < ladoTex && py >= 0 && py < ladoTex)
                {
                    bufferPixeles[py * ladoTex + px] = color;
                }
            }
        }
    }

    private void EjecutarDanioRayo()
    {
        if (distanciaActualRayo <= 0.05f) return;

        int ppu = Mathf.Max(8, pixelesPorUnidad);
        float onda = Mathf.Sin(Time.time * velocidadPulsoLaser);
        int grosorActualPx = Mathf.Max(1, grosorBaseLaserPixeles + Mathf.RoundToInt(onda * amplitudPulsoPixeles));
        float anchoMundo = Mathf.Max(0.12f, grosorActualPx / (float)ppu);

        Vector2 origen = ObtenerPuntoOrigen();
        Vector2 centroHitbox = origen + dirRayo * (distanciaActualRayo * 0.5f);
        Vector2 tamHitbox = new Vector2(distanciaActualRayo, anchoMundo);
        float angulo = Mathf.Atan2(dirRayo.y, dirRayo.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centroHitbox, tamHitbox, angulo, capaJugador);

        for (int i = 0; i < golpeados.Length; i++)
        {
            Collider2D c = golpeados[i];
            if (c.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) continue;

            if (c.TryGetComponent(out Vida vida))
            {
                if (vida.EsInvulnerable) continue;
                vida.RecibirDanio(danioLaser);
            }
            if (c.TryGetComponent(out Empuje e))
            {
                e.AplicarEmpuje(dirRayo, fuerzaEmpujeLaser);
            }
        }
    }

    private void ActualizarAnimaciones()
    {
        if (!player) return;

        Vector2 dirRef = estadoActual == Estado.Esperando ? (Vector2)(player.position - transform.position) : dirRayo;

        if (sprite && Mathf.Abs(dirRef.x) > 0.05f)
        {
            sprite.flipX = dirRef.x < 0f;
        }

        if (!anim) return;

        bool mirandoArriba = dirRef.y > 0f;
        AnimationClip clip;

        if (estadoActual != Estado.Esperando)
        {
            clip = mirandoArriba ? aCastearLaserArriba : aCastearLaserAbajo;
        }
        else
        {
            clip = mirandoArriba ? aCaminarArriba : aCaminarAbajo;
        }

        if (clip != null && animActual != clip.name)
        {
            anim.Play(clip.name);
            animActual = clip.name;
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void OnDisable()
    {
        if (srLienzo != null) srLienzo.enabled = false;
        if (indicador != null) indicador.CancelarAviso();
        estadoActual = Estado.Esperando;
    }

    void OnDestroy()
    {
        if (texLaser != null) Destroy(texLaser);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.2f, 0.9f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDet);
    }
}
