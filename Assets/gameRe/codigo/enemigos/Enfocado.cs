using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class Enfocado : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Físicas")]
    [SerializeField, Min(0f)] private float desacel = 18f;
    [SerializeField, Min(0f)] private float rangoDet = 15f;

    [Header("Láser Gaster Blaster")]
    [SerializeField] private LayerMask capaJugador;
    [SerializeField, Min(0f)] private float cdAtaque = 2.5f;
    [SerializeField, Min(0f)] private float rangoLaser = 12f;
    [SerializeField, Min(0f)] private float fuerzaEmpujeLaser = 10f;

    [Space]
    [SerializeField, Min(0f)] private float tiempoApuntar = 1f;
    [SerializeField, Min(0f)] private float tiempoFijado = 0.35f;
    [SerializeField, Min(0f)] private float tiempoDisparo = 0.2f;

    [Header("Visuales")]
    [SerializeField] private Color colorMira = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField, Min(0f)] private float grosorMira = 0.05f;
    [SerializeField, Min(0f)] private float grosorLaser = 0.8f;

    [Space]
    [Tooltip("Sprite (bullet.png) que se estira a lo largo del rayo.")]
    [SerializeField] private Sprite spriteLaser;
    [SerializeField] private int ordenSpriteLaser = 10;

    public float RangoDeteccion => rangoDet;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private LineRenderer lr;
    private IndicadorAtaque indicador;
    private SpriteRenderer srLaser;

    private enum Estado { Esperando, Apuntando, Fijado, Disparando }
    private Estado estadoActual = Estado.Esperando;

    private float timerCd;
    private float timerEstado;
    private Vector2 dirRayo;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);
        TryGetComponent(out lr);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();

        ConfigurarLinea();
        CrearVisualLaser();
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

        if (player && TryGetComponent(out Collider2D col) && player.TryGetComponent(out Collider2D colP))
        {
            Physics2D.IgnoreCollision(col, colP, true);
        }
    }

    void ConfigurarLinea()
    {
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        if (lr.sharedMaterial == null)
        {
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s != null) lr.material = new Material(s);
        }
        lr.enabled = false;
    }

    // El sprite NO es hijo del enemigo para que su escala/rotación no se mezclen con las del padre
    void CrearVisualLaser()
    {
        if (!spriteLaser) return;

        GameObject go = new GameObject("VisualLaser");
        srLaser = go.AddComponent<SpriteRenderer>();
        srLaser.sprite = spriteLaser;
        srLaser.drawMode = SpriteDrawMode.Sliced; // las esquinas no se deforman, solo el centro se estira
        srLaser.sortingOrder = ordenSpriteLaser;
        srLaser.enabled = false;
    }

    void Update()
    {
        if (!player) return;

        if (Bloqueado() && estadoActual != Estado.Esperando)
        {
            estadoActual = Estado.Esperando;
            ApagarVisuales();
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
                    lr.enabled = true;
                }
                break;

            case Estado.Apuntando:
                timerEstado -= Time.deltaTime;
                dirRayo = (player.position - transform.position).normalized;
                DibujarLinea(colorMira, grosorMira);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Fijado;
                    timerEstado = tiempoFijado;
                    indicador.MostrarAviso(tiempoFijado);
                }
                break;

            case Estado.Fijado:
                timerEstado -= Time.deltaTime;
                DibujarLinea(colorMira, grosorMira);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Disparando;
                    timerEstado = tiempoDisparo;

                    if (srLaser != null)
                    {
                        lr.enabled = false;       // apaga la mira
                        srLaser.enabled = true;   // muestra el sprite estirado
                    }

                    EjecutarDanioRayo();
                }
                break;

            case Estado.Disparando:
                timerEstado -= Time.deltaTime;

                if (srLaser != null) DibujarSpriteLaser();

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Esperando;
                    timerCd = cdAtaque;
                    ApagarVisuales();
                }
                break;
        }
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

    void DibujarLinea(Color color, float grosor)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.widthMultiplier = grosor;
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, (Vector2)transform.position + dirRayo * rangoLaser);
    }

    // Mismo centro, ángulo y tamaño que el OverlapBoxAll: lo que se ve coincide con lo que daña
    void DibujarSpriteLaser()
    {
        Vector2 centro = (Vector2)transform.position + dirRayo * (rangoLaser * 0.5f);
        float angulo = Mathf.Atan2(dirRayo.y, dirRayo.x) * Mathf.Rad2Deg;

        srLaser.transform.SetPositionAndRotation(centro, Quaternion.Euler(0f, 0f, angulo));
        srLaser.size = new Vector2(rangoLaser, grosorLaser); // en modo Sliced, size reemplaza a la escala
    }

    void EjecutarDanioRayo()
    {
        Vector2 centroHitbox = (Vector2)transform.position + dirRayo * (rangoLaser * 0.5f);
        Vector2 tamHitbox = new Vector2(rangoLaser, grosorLaser);
        float angulo = Mathf.Atan2(dirRayo.y, dirRayo.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centroHitbox, tamHitbox, angulo, capaJugador);

        for (int i = 0; i < golpeados.Length; i++)
        {
            Collider2D c = golpeados[i];
            if (c.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) continue;

            if (c.TryGetComponent(out Vida vida))
            {
                if (vida.EsInvulnerable) continue;
                vida.RecibirDanio(1);
            }
            if (c.TryGetComponent(out Empuje e)) e.AplicarEmpuje(dirRayo, fuerzaEmpujeLaser);
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void ApagarVisuales()
    {
        if (lr != null) lr.enabled = false;
        if (srLaser != null) srLaser.enabled = false;
    }

    void OnDisable()
    {
        ApagarVisuales();
        if (indicador != null) indicador.CancelarAviso();
        estadoActual = Estado.Esperando;
    }

    void OnDestroy()
    {
        if (srLaser != null) Destroy(srLaser.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.2f, 0.9f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDet);
    }
}