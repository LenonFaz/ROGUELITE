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

    [Header("Ataque con Impulso")]
    [SerializeField, Min(0f)] private float rangoAtaque = 1.6f;
    [SerializeField, Min(0.05f)] private float tiempoQuietoAntes = 0.35f;
    [SerializeField, Min(0f)] private float velocidadImpulso = 7.5f;
    [SerializeField, Min(0.05f)] private float duracionImpulso = 0.15f;
    [SerializeField, Min(0.05f)] private float tiempoQuietoDespues = 0.4f;
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

    private enum Estado { Persiguiendo, Preparando, Impulsando, Recuperando }
    private Estado estadoActual = Estado.Persiguiendo;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private IndicadorAtaque indicador;

    private float timerCooldown;
    private float timerEstado;
    private Vector2 direccion = Vector2.down;
    private Vector2 dirAtaque = Vector2.down;
    private float distancia;
    private bool yaGolpeoEnImpulso;

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
            if (estadoActual != Estado.Persiguiendo)
            {
                estadoActual = Estado.Persiguiendo;
                indicador.CancelarAviso();
            }
            return;
        }

        Vector2 haciaJugador = jugador.position - transform.position;
        distancia = haciaJugador.magnitude;
        if (distancia > 0.001f) direccion = haciaJugador / distancia;

        switch (estadoActual)
        {
            case Estado.Persiguiendo:
                if (distancia <= rangoAtaque && timerCooldown <= 0f)
                {
                    estadoActual = Estado.Preparando;
                    timerEstado = tiempoQuietoAntes;
                    dirAtaque = direccion;
                    indicador.MostrarAviso(tiempoQuietoAntes);
                }
                break;

            case Estado.Preparando:
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f)
                {
                    dirAtaque = direccion;
                    estadoActual = Estado.Impulsando;
                    timerEstado = duracionImpulso;
                    yaGolpeoEnImpulso = false;
                    ChequearGolpe(dirAtaque);
                }
                break;

            case Estado.Impulsando:
                timerEstado -= Time.deltaTime;
                ChequearGolpe(dirAtaque);
                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Recuperando;
                    timerEstado = tiempoQuietoDespues;
                }
                break;

            case Estado.Recuperando:
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Persiguiendo;
                    timerCooldown = cooldown;
                }
                break;
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

        switch (estadoActual)
        {
            case Estado.Persiguiendo:
                bool avanzar = distancia <= rangoDeteccion && distancia > rangoAtaque;
                Vector2 objetivo = avanzar ? direccion * velocidad : Vector2.zero;
                float tasa = avanzar ? aceleracion : desaceleracion;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
                break;

            case Estado.Preparando:
            case Estado.Recuperando:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desaceleracion * 2.5f) * Time.fixedDeltaTime);
                break;

            case Estado.Impulsando:
                rb.linearVelocity = dirAtaque * velocidadImpulso;
                break;
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void ChequearGolpe(Vector2 dir)
    {
        if (yaGolpeoEnImpulso) return;

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
                yaGolpeoEnImpulso = true;
            }
            if (c.TryGetComponent(out Empuje e))
            {
                e.AplicarEmpuje(dir, fuerzaEmpuje);
                yaGolpeoEnImpulso = true;
            }
        }
    }

    void OnDisable()
    {
        estadoActual = Estado.Persiguiendo;
        if (indicador != null) indicador.CancelarAviso();
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo) return;

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = new Color(colorHitbox.r, colorHitbox.g, colorHitbox.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        Gizmos.color = colorHitbox;
        Vector2 dirGizmo = Application.isPlaying ? dirAtaque : Vector2.down;
        Vector2 centro = (Vector2)transform.position + dirGizmo * offsetHitbox;
        float angulo = Mathf.Atan2(dirGizmo.y, dirGizmo.x) * Mathf.Rad2Deg;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.Euler(0f, 0f, angulo), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
