using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ghoul : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias")]
    [SerializeField] private Transform jugador;

    [Header("Persecución")]
    [SerializeField, Min(0f)] private float velocidad = 2f;
    [SerializeField, Min(0f)] private float aceleracion = 15f;
    [SerializeField, Min(0f)] private float desaceleracion = 18f;
    [SerializeField, Min(0f)] private float rangoDeteccion = 5f;

    [Header("Ataque")]
    [SerializeField, Min(0f)] private float rangoAtaque = 1.3f;
    [SerializeField, Min(0.05f)] private float tiempoAnticipacion = 0.25f;
    [SerializeField] private Vector2 tamHitbox = new Vector2(1f, 1f);
    [SerializeField] private float offsetHitbox = 0.8f;
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(0)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 6f;
    [SerializeField] private LayerMask capaJugador;

    [Header("Debug")]
    [SerializeField] private bool mostrarGizmo = true;
    [SerializeField] private Color colorHitbox = new Color(1f, 0.4f, 0f);

    public float RangoDeteccion => rangoDeteccion;

    private static readonly Vector2[] direccionesValidas =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private IndicadorAtaque indicador;

    private float timerCooldown;
    private float timerPrep;
    private bool preparandose;
    private Vector2 direccion = Vector2.down;
    private Vector2 dirFijada = Vector2.down;
    private float distancia;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
    }

    void Start()
    {
        if (jugador == null)
        {
            if (Jugador.Instancia != null) jugador = Jugador.Instancia.transform;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) jugador = p.transform;
            }
        }

        if (jugador != null && TryGetComponent(out Collider2D colPropio) && jugador.TryGetComponent(out Collider2D colJugador))
        {
            Physics2D.IgnoreCollision(colPropio, colJugador, true);
        }
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;
        if (jugador == null) return;

        if (Bloqueado())
        {
            if (preparandose)
            {
                preparandose = false;
                indicador.CancelarAviso();
            }
            return;
        }

        Vector2 haciaJugador = jugador.position - transform.position;
        distancia = haciaJugador.magnitude;
        if (distancia > 0.001f) direccion = haciaJugador / distancia;

        if (preparandose)
        {
            timerPrep -= Time.deltaTime;
            if (timerPrep <= 0f)
            {
                preparandose = false;
                Atacar(dirFijada);
                timerCooldown = cooldown;
            }
            return;
        }

        if (distancia <= rangoAtaque && timerCooldown <= 0f)
        {
            preparandose = true;
            timerPrep = tiempoAnticipacion;
            dirFijada = SnapDireccion(direccion);
            indicador.MostrarAviso(tiempoAnticipacion);
        }
    }

    void FixedUpdate()
    {
        if (jugador == null) return;

        if (aturdido != null && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desaceleracion * Time.fixedDeltaTime);
            return;
        }

        if (empuje != null && empuje.EstaEmpujado) return;

        bool quietoPorAtaque = preparandose || distancia <= rangoAtaque;
        Vector2 objetivo = (!quietoPorAtaque && distancia <= rangoDeteccion) ? direccion * velocidad : Vector2.zero;
        float tasa = objetivo != Vector2.zero ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    Vector2 SnapDireccion(Vector2 dir)
    {
        Vector2 mejor = direccionesValidas[0];
        float mejorDot = -2f;
        for (int i = 0; i < direccionesValidas.Length; i++)
        {
            float dot = Vector2.Dot(dir, direccionesValidas[i]);
            if (dot > mejorDot)
            {
                mejorDot = dot;
                mejor = direccionesValidas[i];
            }
        }
        return mejor;
    }

    void Atacar(Vector2 dir)
    {
        Vector2 centro = (Vector2)transform.position + dir * offsetHitbox;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centro, tamHitbox, angulo, capaJugador);
        for (int i = 0; i < golpeados.Length; i++)
        {
            Collider2D c = golpeados[i];
            if (c.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) continue;

            if (c.TryGetComponent(out Vida vida))
            {
                if (vida.EsInvulnerable) continue;
                vida.RecibirDanio(danio);
            }
            if (c.TryGetComponent(out Empuje e)) e.AplicarEmpuje(dir, fuerzaEmpuje);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo) return;

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = new Color(colorHitbox.r, colorHitbox.g, colorHitbox.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        Gizmos.color = colorHitbox;
        Vector2 centro = (Vector2)transform.position + direccion * offsetHitbox;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
