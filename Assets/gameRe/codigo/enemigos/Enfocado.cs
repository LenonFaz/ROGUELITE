using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class Enfocado : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Movimiento")]
    public float vel = 2f;
    public float acel = 15f;
    public float desacel = 18f;
    public float rangoDet = 9f;
    public float distMax = 7f;
    public float distMin = 5f;

    [Header("Láser Gaster Blaster")]
    public LayerMask capaJugador; // Asegúrate de asignar la capa de tu jugador en el inspector
    public float cdAtaque = 2.5f;
    public float rangoLaser = 12f;
    public float fuerzaEmpujeLaser = 10f;

    [Space]
    public float tiempoApuntar = 1f;   // El láser sigue al jugador
    public float tiempoFijado = 0.35f; // El láser se bloquea en su posición como advertencia final
    public float tiempoDisparo = 0.2f; // Lo que dura el rayo grueso en pantalla

    [Header("Visuales")]
    public Color colorMira = new Color(1f, 0f, 0f, 0.4f);
    public float grosorMira = 0.05f;
    public Color colorLaser = Color.red;
    public float grosorLaser = 0.8f;

    Rigidbody2D rb;
    Aturdido aturdido;
    Empuje empuje;
    LineRenderer lr;

    enum Estado { Moviendose, Apuntando, Fijado, Disparando }
    Estado estadoActual = Estado.Moviendose;

    float timerCd;
    float timerEstado;
    Vector2 dirRayo;

    void OnValidate()
    {
        if (distMin >= distMax) distMax = distMin + 1f;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        aturdido = GetComponent<Aturdido>();
        empuje = GetComponent<Empuje>();
        lr = GetComponent<LineRenderer>();

        ConfigurarLinea();

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (player)
        {
            var col = GetComponent<Collider2D>();
            var colP = player.GetComponent<Collider2D>();
            if (col && colP) Physics2D.IgnoreCollision(col, colP, true);
        }
    }

    void ConfigurarLinea()
    {
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        if (lr.material == null)
            lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.enabled = false;
    }

    void Update()
    {
        if (!player) return;

        // Si es aturdido o empujado, cancela el ataque inmediatamente
        if (Bloqueado() && estadoActual != Estado.Moviendose)
        {
            estadoActual = Estado.Moviendose;
            lr.enabled = false;
            timerCd = cdAtaque; // Lo forzamos a esperar de nuevo
        }

        switch (estadoActual)
        {
            case Estado.Moviendose:
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

                // El rayo persigue la posición del jugador
                dirRayo = (player.position - transform.position).normalized;
                DibujarLinea(colorMira, grosorMira);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Fijado;
                    timerEstado = tiempoFijado;
                }
                break;

            case Estado.Fijado:
                timerEstado -= Time.deltaTime;

                // dirRayo YA NO se actualiza, el láser se queda congelado en el sitio
                DibujarLinea(colorMira, grosorMira);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Disparando;
                    timerEstado = tiempoDisparo;
                    EjecutarDanioRayo(); // Hace el daño en el frame 1 del disparo
                }
                break;

            case Estado.Disparando:
                timerEstado -= Time.deltaTime;

                // Láser grueso final
                DibujarLinea(colorLaser, grosorLaser);

                if (timerEstado <= 0f)
                {
                    estadoActual = Estado.Moviendose;
                    timerCd = cdAtaque;
                    lr.enabled = false;
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (!player) return;

        if (aturdido && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            return;
        }

        if (empuje && empuje.EstaEmpujado) return;

        // Si está en cualquier estado que no sea moverse, se clava en el piso
        if (estadoActual != Estado.Moviendose)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desacel * 2f) * Time.fixedDeltaTime);
            return;
        }

        // Movimiento normal buscando su anillo ideal de distancia
        Vector2 hacia = player.position - transform.position;
        float dist = hacia.magnitude;
        Vector2 dir = dist > 0.01f ? hacia.normalized : Vector2.zero;

        if (dist > rangoDet)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            return;
        }

        bool enZonaIdeal = (dist >= distMin && dist <= distMax);

        if (enZonaIdeal)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desacel * 2f) * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 obj = Vector2.zero;
            if (dist > distMax) obj = dir * vel;
            else if (dist < distMin) obj = -dir * vel;

            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, acel * Time.fixedDeltaTime);
        }
    }

    void DibujarLinea(Color color, float grosor)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.widthMultiplier = grosor;
        lr.SetPosition(0, transform.position);
        lr.SetPosition(1, (Vector2)transform.position + dirRayo * rangoLaser);
    }

    void EjecutarDanioRayo()
    {
        // Creamos una caja de colisión a lo largo de todo el rayo
        Vector2 centroHitbox = (Vector2)transform.position + dirRayo * (rangoLaser / 2f);
        Vector2 tamañoHitbox = new Vector2(rangoLaser, grosorLaser);
        float angulo = Mathf.Atan2(dirRayo.y, dirRayo.x) * Mathf.Rad2Deg;

        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centroHitbox, tamañoHitbox, angulo, capaJugador);

        foreach (var c in golpeados)
        {
            // c.GetComponent<Vida>()?.RecibirDanio(1);

            var empujeJugador = c.GetComponent<Empuje>();
            if (empujeJugador != null)
            {
                empujeJugador.AplicarEmpuje(dirRayo, fuerzaEmpujeLaser);
            }
        }
    }

    bool Bloqueado() => (aturdido && aturdido.EstaAturdido) || (empuje && empuje.EstaEmpujado);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.7f, 0.2f, 0.9f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, rangoDet);

        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, distMax);

        Gizmos.color = new Color(0.9f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, distMin);
    }
}
