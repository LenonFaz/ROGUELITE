using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Escudero : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugador;

    [Header("Persecución")]
    [SerializeField, Min(0f)] private float velPersecucion = 2.5f;
    [SerializeField, Min(0f)] private float aceleracion = 15f;
    [SerializeField, Min(0f)] private float desaceleracion = 18f;
    [SerializeField, Min(0f)] private float rangoDeteccion = 10f;
    [SerializeField, Min(0f)] private float rangoCarga = 6f;

    [Header("Carga (Embestida)")]
    [SerializeField, Min(0f)] private float velCarga = 18f;
    [SerializeField, Min(0f)] private float tiempoPreparacion = 0.4f;
    [SerializeField, Min(0f)] private float tiempoCarga = 0.35f;
    [SerializeField, Min(0f)] private float tiempoEnfriamiento = 1.2f;
    [Tooltip("Capacidad de girar hacia el jugador mientras embiste. 0 = Línea recta. 2 a 4 = Curva ligera.")]
    [SerializeField, Min(0f)] private float factorCurva = 2.5f;

    [Header("Daño de Impacto")]
    [SerializeField] private Vector2 tamHitbox = new Vector2(1.2f, 1.2f);
    [SerializeField] private float offsetHitbox = 0.5f;
    [SerializeField, Min(0)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 12f;
    [SerializeField] private LayerMask capaJugador;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;

    private enum Estado { Persiguiendo, Preparando, Cargando, Enfriamiento }
    private Estado estadoActual = Estado.Persiguiendo;

    private float timerEstado;
    private Vector2 direccionCarga;
    private Vector2 direccionActual = Vector2.down;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

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
        if (jugador == null) return;

        if (Bloqueado() && estadoActual != Estado.Persiguiendo && estadoActual != Estado.Enfriamiento)
        {
            CambiarEstado(Estado.Enfriamiento, tiempoEnfriamiento);
        }

        switch (estadoActual)
        {
            case Estado.Persiguiendo:
                float dist = Vector2.Distance(transform.position, jugador.position);
                if (dist <= rangoCarga && !Bloqueado())
                {
                    CambiarEstado(Estado.Preparando, tiempoPreparacion);
                }
                break;

            case Estado.Preparando:
                timerEstado -= Time.deltaTime;
                direccionCarga = (jugador.position - transform.position).normalized;
                direccionActual = direccionCarga;

                if (timerEstado <= 0f)
                {
                    CambiarEstado(Estado.Cargando, tiempoCarga);
                }
                break;

            case Estado.Cargando:
                timerEstado -= Time.deltaTime;

                DetectarImpacto();

                if (timerEstado <= 0f)
                {
                    CambiarEstado(Estado.Enfriamiento, tiempoEnfriamiento);
                }
                break;

            case Estado.Enfriamiento:
                timerEstado -= Time.deltaTime;
                if (timerEstado <= 0f && !Bloqueado())
                {
                    CambiarEstado(Estado.Persiguiendo, 0f);
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

        Vector2 objetivo = Vector2.zero;

        switch (estadoActual)
        {
            case Estado.Persiguiendo:
                float dist = Vector2.Distance(transform.position, jugador.position);
                if (dist <= rangoDeteccion)
                {
                    Vector2 hacia = (jugador.position - transform.position).normalized;
                    objetivo = hacia * velPersecucion;
                    direccionActual = hacia;
                }
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, aceleracion * Time.fixedDeltaTime);
                break;

            case Estado.Preparando:
            case Estado.Enfriamiento:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desaceleracion * 2f) * Time.fixedDeltaTime);
                break;

            case Estado.Cargando:
                // Recalcula la dirección hacia el jugador en tiempo real
                Vector2 direccionObjetivo = (jugador.position - transform.position).normalized;

                // Rota gradualmente la trayectoria de carga hacia el jugador
                direccionCarga = Vector2.Lerp(direccionCarga, direccionObjetivo, factorCurva * Time.fixedDeltaTime).normalized;
                direccionActual = direccionCarga; // Actualiza visualmente la rotación del Gizmo/hitbox

                // Aplica la velocidad en la nueva dirección curva
                rb.linearVelocity = direccionCarga * velCarga;
                break;
        }
    }

    void CambiarEstado(Estado nuevoEstado, float tiempo)
    {
        estadoActual = nuevoEstado;
        timerEstado = tiempo;
    }

    void DetectarImpacto()
    {
        Vector2 centroHitbox = (Vector2)transform.position + direccionCarga * offsetHitbox;
        float angulo = Mathf.Atan2(direccionCarga.y, direccionCarga.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centroHitbox, tamHitbox, angulo, capaJugador);

        foreach (var c in golpeados)
        {
            if (c.TryGetComponent(out Vida vidaJugador)) vidaJugador.RecibirDanio(danio);
            if (c.TryGetComponent(out Empuje empujeJugador))
            {
                empujeJugador.AplicarEmpuje(direccionCarga, fuerzaEmpuje);
            }

            CambiarEstado(Estado.Enfriamiento, tiempoEnfriamiento);
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, rangoCarga);

        if (estadoActual == Estado.Cargando || estadoActual == Estado.Preparando)
        {
            Gizmos.color = Color.red;
            Vector2 centro = (Vector2)transform.position + direccionActual * offsetHitbox;
            Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
        }
    }
}
