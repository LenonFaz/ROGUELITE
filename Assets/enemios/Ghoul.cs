using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class Ghoul : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Persecución")]
    public float velocidad = 2f;
    public float aceleracion = 15f;
    public float desaceleracion = 18f;
    public float rangoDeteccion = 5f;

    [Header("Ataque")]
    public float rangoAtaque = 1.3f;
    public Vector2 tamHitbox = new Vector2(1f, 1f);
    public float offsetHitbox = 0.8f;
    public float cooldown = 1f;
    public int danio = 1;
    public float fuerzaEmpuje = 6f;
    public LayerMask capaJugador;

    [Header("Debug - Scene view")]
    public bool mostrarGizmo = true;
    public Color colorHitbox = new Color(1f, 0.4f, 0f);

    [Header("Debug - Game view")]
    public bool mostrarHitboxJuego = true;
    public float tiempoVisible = 0.15f;
    public float grosorLinea = 0.05f;

    private static readonly Vector2[] direccionesValidas =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;
    private LineRenderer lr;
    private Coroutine rutinaOcultar;

    private float timerCooldown = 0f;
    private bool atacando = false;
    private Vector2 direccion = Vector2.down;
    private float distancia;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        aturdido = GetComponent<Aturdido>();
        empuje = GetComponent<Empuje>();

        lr = GetComponent<LineRenderer>();
        ConfigurarLinea();

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        // evita que el choque físico entre jugador y ghoul los separe solos,
        // sin tocar la matriz de layers (eso rompería las queries del golpe)
        if (jugador != null)
        {
            var colPropio = GetComponent<Collider2D>();
            var colJugador = jugador.GetComponent<Collider2D>();
            if (colPropio != null && colJugador != null)
                Physics2D.IgnoreCollision(colPropio, colJugador, true);
        }
    }

    void ConfigurarLinea()
    {
        lr.positionCount = 5;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.widthMultiplier = grosorLinea;
        lr.startColor = colorHitbox;
        lr.endColor = colorHitbox;
        if (lr.material == null)
            lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.enabled = false;
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;
        if (jugador == null) return;

        Vector2 haciaJugador = (Vector2)(jugador.position - transform.position);
        distancia = haciaJugador.magnitude;
        if (distancia > 0.01f) direccion = haciaJugador.normalized;

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
            objetivo = direccion * velocidad;

        float tasa = objetivo.sqrMagnitude > 0.01f ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
    }

    bool Bloqueado()
    {
        return (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);
    }

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
            // c.GetComponent<VidaJugador>()?.RecibirDanio(danio);

            var empujeJugador = c.GetComponent<Empuje>();
            if (empujeJugador != null) empujeJugador.AplicarEmpuje(dir, fuerzaEmpuje);
        }

        if (mostrarHitboxJuego) MostrarHitbox(centro, angulo);
    }

    void MostrarHitbox(Vector2 centro, float angulo)
    {
        Vector3[] esquinas = ObtenerEsquinas(centro, tamHitbox, angulo);
        lr.SetPosition(0, esquinas[0]);
        lr.SetPosition(1, esquinas[1]);
        lr.SetPosition(2, esquinas[2]);
        lr.SetPosition(3, esquinas[3]);
        lr.SetPosition(4, esquinas[0]);
        lr.startColor = colorHitbox;
        lr.endColor = colorHitbox;
        lr.widthMultiplier = grosorLinea;
        lr.enabled = true;

        if (rutinaOcultar != null) StopCoroutine(rutinaOcultar);
        rutinaOcultar = StartCoroutine(OcultarLuego(tiempoVisible));
    }

    IEnumerator OcultarLuego(float t)
    {
        yield return new WaitForSeconds(t);
        lr.enabled = false;
    }

    Vector3[] ObtenerEsquinas(Vector2 centro, Vector2 tam, float angulo)
    {
        Vector2 mitad = tam * 0.5f;
        Vector2[] local = {
            new Vector2(-mitad.x, -mitad.y),
            new Vector2(mitad.x, -mitad.y),
            new Vector2(mitad.x, mitad.y),
            new Vector2(-mitad.x, mitad.y),
        };

        Quaternion rot = Quaternion.Euler(0f, 0f, angulo);
        Vector3[] mundo = new Vector3[4];
        for (int i = 0; i < 4; i++)
            mundo[i] = new Vector3(centro.x, centro.y, 0f) + rot * new Vector3(local[i].x, local[i].y, 0f);
        return mundo;
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