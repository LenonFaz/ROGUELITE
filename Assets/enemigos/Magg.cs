using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Magg : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public GameObject prefabProyectil;
    public Transform puntoDisparo;

    [Header("Movimiento")]
    public float velocidad = 2.5f;
    public float aceleracion = 15f;
    public float desaceleracion = 18f;
    public float rangoDeteccion = 7f;
    public float distanciaSegura = 4f; // si el jugador se acerca más que esto, huye

    [Header("Disparo")]
    public float cooldownDisparo = 1.5f;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;

    private float timerDisparo = 0f;
    private Vector2 direccion = Vector2.down;
    private float distancia;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        aturdido = GetComponent<Aturdido>();
        empuje = GetComponent<Empuje>();

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (jugador != null)
        {
            var colPropio = GetComponent<Collider2D>();
            var colJugador = jugador.GetComponent<Collider2D>();
            if (colPropio != null && colJugador != null)
                Physics2D.IgnoreCollision(colPropio, colJugador, true);
        }
    }

    void Update()
    {
        timerDisparo -= Time.deltaTime;
        if (jugador == null) return;

        Vector2 haciaJugador = (Vector2)(jugador.position - transform.position);
        distancia = haciaJugador.magnitude;
        if (distancia > 0.01f) direccion = haciaJugador.normalized;

        if (Bloqueado() || distancia > rangoDeteccion) return;

        if (timerDisparo <= 0f)
        {
            Disparar(direccion);
            timerDisparo = cooldownDisparo;
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
        if (distancia <= rangoDeteccion && distancia < distanciaSegura)
            objetivo = -direccion * velocidad; // huye en la dirección contraria

        float tasa = objetivo.sqrMagnitude > 0.01f ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
    }

    bool Bloqueado()
    {
        return (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);
    }

    void Disparar(Vector2 dir)
    {
        if (prefabProyectil == null) return;

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        GameObject proy = Instantiate(prefabProyectil, origen, Quaternion.identity);

        var p = proy.GetComponent<Proyectil>();
        if (p != null) p.Apuntar(dir);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.2f, 0.9f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, distanciaSegura);
    }
}