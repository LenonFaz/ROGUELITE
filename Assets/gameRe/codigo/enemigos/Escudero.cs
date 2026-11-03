using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Escudero : MonoBehaviour, IRangoDeteccion
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

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aCaminarArriba;
    [SerializeField] private AnimationClip aCaminarAbajo;
    [SerializeField] private AnimationClip aPrepararCarga;
    [SerializeField] private AnimationClip aCargaArriba;
    [SerializeField] private AnimationClip aCargaAbajo;
    [SerializeField] private AnimationClip aCargaDerecha;

    public float RangoDeteccion => rangoDet;

    private enum Estado { Acercando, Aviso, Cargando, Recuperando }
    private Estado estado = Estado.Acercando;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private IndicadorAtaque indicador;
    private float timer;
    private Vector2 dirCarga;
    private bool golpeo;
    private string animActual;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
        if (!anim) TryGetComponent(out anim);
        if (!sprite) TryGetComponent(out sprite);
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

        if (player && TryGetComponent(out Collider2D c) && player.TryGetComponent(out Collider2D cp))
        {
            Physics2D.IgnoreCollision(c, cp, true);
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void Update()
    {
        if (!player) return;

        if (Bloqueado() && estado != Estado.Acercando)
        {
            estado = Estado.Acercando;
            indicador.CancelarAviso();
            ActualizarAnimaciones();
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
                    indicador.MostrarAviso(tiempoAviso);
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

        ActualizarAnimaciones();
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
                float dist = hacia.magnitude;
                bool avanzar = dist <= rangoDet && dist > distCarga;
                Vector2 obj = avanzar ? (hacia / dist) * vel : Vector2.zero;
                float tasa = avanzar ? acel : desacel;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasa * Time.fixedDeltaTime);
                break;

            case Estado.Aviso:
            case Estado.Recuperando:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * 2f * Time.fixedDeltaTime);
                break;

            case Estado.Cargando:
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

    private void ActualizarAnimaciones()
    {
        if (!sprite || !player) return;

        Vector2 haciaJugador = (Vector2)player.position - (Vector2)transform.position;
        AnimationClip clip = null;

        switch (estado)
        {
            case Estado.Aviso:
                if (Mathf.Abs(dirCarga.x) > 0.05f) sprite.flipX = dirCarga.x < 0f;
                clip = aPrepararCarga;
                break;

            case Estado.Cargando:
                if (Mathf.Abs(dirCarga.x) >= Mathf.Abs(dirCarga.y))
                {
                    sprite.flipX = dirCarga.x < 0f;
                    clip = aCargaDerecha;
                }
                else
                {
                    sprite.flipX = false;
                    clip = dirCarga.y > 0f ? aCargaArriba : aCargaAbajo;
                }
                break;

            case Estado.Acercando:
            case Estado.Recuperando:
                if (Mathf.Abs(haciaJugador.x) > 0.05f) sprite.flipX = haciaJugador.x < 0f;
                bool mirandoArriba = rb.linearVelocity.sqrMagnitude > 0.05f ? rb.linearVelocity.y > 0f : haciaJugador.y > 0f;
                clip = mirandoArriba ? aCaminarArriba : aCaminarAbajo;
                break;
        }

        if (anim != null && clip != null && animActual != clip.name)
        {
            anim.Play(clip.name);
            animActual = clip.name;
        }
    }

    void GolpearJugador()
    {
        if (golpeo) return;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, radioGolpe, capaJugador);
        if (!hit) return;

        if (hit.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) return;

        if (hit.TryGetComponent(out Vida v))
        {
            if (v.EsInvulnerable) return;
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
        if (estado == Estado.Cargando && (string.Equals(col.gameObject.tag, "pared", System.StringComparison.OrdinalIgnoreCase) || string.Equals(col.gameObject.tag, "hueco", System.StringComparison.OrdinalIgnoreCase)))
        {
            IniciarRecuperacion();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distCarga);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioGolpe);
    }
}
