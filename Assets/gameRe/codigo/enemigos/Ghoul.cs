using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ghoul : MonoBehaviour
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
    [SerializeField] private Vector2 tamHitbox = new Vector2(1f, 1f);
    [SerializeField] private float offsetHitbox = 0.8f;
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(0)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 6f;
    [SerializeField] private LayerMask capaJugador;

    [Header("Debug")]
    [SerializeField] private bool mostrarGizmo = true;
    [SerializeField] private Color colorHitbox = new Color(1f, 0.4f, 0f);

    private static readonly Vector2[] direccionesValidas =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;

    private float timerCooldown = 0f;
    private bool atacando = false;
    private Vector2 direccion = Vector2.down;
    private float distancia;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        // Ignorar colisiones físicas
        if (jugador != null)
        {
            if (TryGetComponent(out Collider2D colPropio) && jugador.TryGetComponent(out Collider2D colJugador))
            {
                Physics2D.IgnoreCollision(colPropio, colJugador, true);
            }
        }
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;
        if (jugador == null) return;

        Vector2 haciaJugador = jugador.position - transform.position;
        distancia = haciaJugador.magnitude;
        if (haciaJugador != Vector2.zero) direccion = haciaJugador.normalized;

        if (Bloqueado() || distancia > rangoDeteccion)
        {
            atacando = false;
            return;
        }

        if (distancia <= rangoAtaque)
        {
            atacando = true;
            if (timerCooldown <= 0f)
            {
                Atacar(SnapDireccion(direccion));
                timerCooldown = cooldown;
            }
        }
        else
        {
            atacando = false;
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

        Vector2 objetivo = Vector2.zero;
        if (!atacando && distancia <= rangoDeteccion)
        {
            objetivo = direccion * velocidad;
        }

        float tasa = objetivo != Vector2.zero ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    Vector2 SnapDireccion(Vector2 dir)
    {
        Vector2 mejor = direccionesValidas[0];
        float mejorDot = -2f;
        foreach (var d in direccionesValidas)
        {
            float dot = Vector2.Dot(dir, d);
            if (dot > mejorDot) { mejorDot = dot; mejor = d; }
        }
        return mejor;
    }

    void Atacar(Vector2 dir)
    {
        Vector2 centro = (Vector2)transform.position + dir * offsetHitbox;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centro, tamHitbox, angulo, capaJugador);
        foreach (var c in golpeados)
        {
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
