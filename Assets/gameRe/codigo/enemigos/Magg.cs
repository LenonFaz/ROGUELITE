using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Magg : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Movimiento")]
    public float vel = 2.5f;
    public float acel = 15f;
    public float desacel = 18f;
    public float rangoDet = 8f;
    public float distMax = 6f;
    public float distMin = 4f;

    [Header("Disparo")]
    public Bullet prefabBullet;
    public Transform puntoDisparo;
    public float cdDisparo = 1.5f;
    public float tiempoAnticipacion = 0.5f;

    [Header("Animaciones")]
    public Animator anim;
    public SpriteRenderer sprite;
    public AnimationClip aIdle;
    public AnimationClip aMover;
    public AnimationClip aAtacar;

    Rigidbody2D rb;
    Aturdido aturdido;
    Empuje empuje;

    float tDisparo;
    float tPrep;
    bool preparandose;
    string animActual;

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

        if (!anim) anim = GetComponent<Animator>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();

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

    void Update()
    {
        if (!player) return;

        if (preparandose)
        {
            tPrep -= Time.deltaTime;
            if (tPrep <= 0f)
            {
                Disparar();
                preparandose = false;
                tDisparo = cdDisparo;
            }
            return;
        }

        tDisparo -= Time.deltaTime;

        if (tDisparo <= 0f && Vector2.Distance(transform.position, player.position) <= rangoDet)
        {
            if (!Bloqueado())
            {
                preparandose = true;
                tPrep = tiempoAnticipacion;
            }
        }
    }

    void FixedUpdate()
    {
        if (!player) return;

        if (aturdido && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            ActualizarAnim(Vector2.zero);
            return;
        }

        if (empuje && empuje.EstaEmpujado) return;

        Vector2 obj = Vector2.zero;

        if (preparandose)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desacel * 2f) * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 hacia = player.position - transform.position;
            float dist = hacia.magnitude;
            Vector2 dir = dist > 0.01f ? hacia.normalized : Vector2.zero;

            if (dist <= rangoDet)
            {
                bool enZonaIdeal = (dist >= distMin && dist <= distMax);

                if (enZonaIdeal)
                {
                    rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desacel * 2f) * Time.fixedDeltaTime);
                }
                else
                {
                    if (dist > distMax) obj = dir * vel;
                    else if (dist < distMin) obj = -dir * vel;

                    rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, acel * Time.fixedDeltaTime);
                }
            }
            else
            {
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            }
        }

        ActualizarAnim(obj);
    }

    void ActualizarAnim(Vector2 velObj)
    {
        if (!anim || !sprite) return;

        AnimationClip clipObj = aIdle;

        if (preparandose)
        {
            clipObj = aAtacar;
        }
        else if (velObj.sqrMagnitude > 0.01f || rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            clipObj = aMover;
        }

        if (clipObj && animActual != clipObj.name)
        {
            anim.Play(clipObj.name);
            animActual = clipObj.name;
        }
    }

    bool Bloqueado() => (aturdido && aturdido.EstaAturdido) || (empuje && empuje.EstaEmpujado);

    void Disparar()
    {
        if (!prefabBullet) return;

        Vector3 pos = puntoDisparo ? puntoDisparo.position : transform.position;
        Vector2 dirBala = (player.position - pos).normalized;

        Bullet b = Instantiate(prefabBullet, pos, Quaternion.identity);

        var miCol = GetComponent<Collider2D>();
        var balaCol = b.GetComponent<Collider2D>();
        if (miCol && balaCol) Physics2D.IgnoreCollision(miCol, balaCol, true);

        b.Init(dirBala);
    }

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
