using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Escudero : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 1.2f;
    [SerializeField, Min(0f)] private float acel = 10f;
    [SerializeField, Min(0f)] private float desacel = 18f;
    [SerializeField, Min(0f)] private float rangoDet = 9f;
    [SerializeField, Min(0f)] private float distCarga = 4.5f;

    [Header("Carga")]
    [SerializeField, Min(0f)] private float tiempoAviso = 0.6f;
    [SerializeField, Min(0f)] private float velCarga = 11f;
    [SerializeField, Min(0f)] private float durCarga = 0.7f;
    [SerializeField, Min(0f)] private float giroGradosSeg = 40f;
    [SerializeField, Min(0f)] private float tiempoRecuperacion = 1f;
    [SerializeField, Min(0f)] private float radioGolpe = 0.6f;
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 8f;
    [SerializeField] private LayerMask capaJugador;

    private enum Estado { Acercando, Aviso, Cargando, Recuperando }
    private Estado estado = Estado.Acercando;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private float timer;
    private Vector2 dirCarga;
    private bool golpeo;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player && TryGetComponent(out Collider2D c) && player.TryGetComponent(out Collider2D cp))
            Physics2D.IgnoreCollision(c, cp, true);
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void Update()
    {
        if (!player) return;

        if (Bloqueado() && estado != Estado.Acercando)
        {
            estado = Estado.Acercando;
            return;
        }

        timer -= Time.deltaTime;

        switch (estado)
        {
            case Estado.Acercando:
                float d = Vector2.Distance(transform.position, player.position);
                if (!Bloqueado() && d <= distCarga)
                {
                    estado = Estado.Aviso;
                    timer = tiempoAviso;
                }
                break;

            case Estado.Aviso:
                dirCarga = ((Vector2)player.position - (Vector2)transform.position).normalized;
                if (timer <= 0f)
                {
                    estado = Estado.Cargando;
                    timer = durCarga;
                    golpeo = false;
                }
                break;

            case Estado.Cargando:
                if (timer <= 0f) IniciarRecuperacion();
                break;

            case Estado.Recuperando:
                if (timer <= 0f) estado = Estado.Acercando;
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

        switch (estado)
        {
            case Estado.Acercando:
                Vector2 hacia = (Vector2)player.position - (Vector2)transform.position;
                bool avanzar = hacia.magnitude <= rangoDet && hacia.magnitude > distCarga;
                Vector2 obj = avanzar ? hacia.normalized * vel : Vector2.zero;
                float tasa = avanzar ? acel : desacel;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasa * Time.fixedDeltaTime);
                break;

            case Estado.Aviso:
            case Estado.Recuperando:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * 2f * Time.fixedDeltaTime);
                break;

            case Estado.Cargando:
                // Ligero desvío hacia el jugador
                Vector2 haciaP = ((Vector2)player.position - (Vector2)transform.position).normalized;
                float a = Mathf.MoveTowardsAngle(
                    Mathf.Atan2(dirCarga.y, dirCarga.x) * Mathf.Rad2Deg,
                    Mathf.Atan2(haciaP.y, haciaP.x) * Mathf.Rad2Deg,
                    giroGradosSeg * Time.fixedDeltaTime) * Mathf.Deg2Rad;
                dirCarga = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                rb.linearVelocity = dirCarga * velCarga;
                GolpearJugador();
                break;
        }
    }

    void GolpearJugador()
    {
        if (golpeo) return;
        var hit = Physics2D.OverlapCircle(transform.position, radioGolpe, capaJugador);
        if (!hit) return;

        if (hit.TryGetComponent(out Vida v))
        {
            if (v.EsInvulnerable) return;   // el dash lo atraviesa
            v.RecibirDanio(danio);
        }
        if (hit.TryGetComponent(out Empuje e)) e.AplicarEmpuje(dirCarga, fuerzaEmpuje);
        golpeo = true;
    }

    void IniciarRecuperacion()
    {
        estado = Estado.Recuperando;
        timer = tiempoRecuperacion;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (estado == Estado.Cargando && col.gameObject.CompareTag("Pared"))
            IniciarRecuperacion();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distCarga);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioGolpe);
    }
}
