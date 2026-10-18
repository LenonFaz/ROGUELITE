using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Magg : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 2.5f;
    [SerializeField, Min(0f)] private float acel = 15f;
    [SerializeField, Min(0f)] private float desacel = 18f;
    [SerializeField, Min(0f)] private float rangoDet = 8f;
    [SerializeField, Min(0f)] private float distMax = 6f;
    [SerializeField, Min(0f)] private float distMin = 4f;

    [Header("Disparo")]
    [SerializeField] private Bullet prefabBullet;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField, Min(0f)] private float cdDisparo = 1.5f;
    [SerializeField, Min(0f)] private float tiempoAnticipacion = 0.5f;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aIdle;
    [SerializeField] private AnimationClip aMover;
    [SerializeField] private AnimationClip aAtacar;

    private Rigidbody2D rb;
    private Aturdido aturdido;
    private Empuje empuje;

    private float tDisparo;
    private float tPrep;
    private bool preparandose;
    private string animActual;

    void OnValidate()
    {
        if (distMin >= distMax) distMax = distMin + 1f;
    }

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        TryGetComponent(out aturdido);
        TryGetComponent(out empuje);

        if (!anim) TryGetComponent(out anim);
        if (!sprite) TryGetComponent(out sprite);

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (player)
        {
            if (TryGetComponent(out Collider2D col) && player.TryGetComponent(out Collider2D colP))
            {
                Physics2D.IgnoreCollision(col, colP, true);
            }
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

        if (aturdido != null && aturdido.EstaAturdido)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
            ActualizarAnim(Vector2.zero);
            return;
        }

        if (empuje != null && empuje.EstaEmpujado) return;

        Vector2 obj = Vector2.zero;

        if (preparandose)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, (desacel * 2f) * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 hacia = player.position - transform.position;
            float dist = hacia.magnitude;
            Vector2 dir = hacia != Vector2.zero ? hacia.normalized : Vector2.zero;

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
        else if (velObj != Vector2.zero || rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            clipObj = aMover;
        }

        if (clipObj && animActual != clipObj.name)
        {
            anim.Play(clipObj.name);
            animActual = clipObj.name;
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void Disparar()
    {
        if (!prefabBullet) return;

        Vector3 pos = puntoDisparo ? puntoDisparo.position : transform.position;
        Vector2 dirBala = (player.position - pos).normalized;

        Bullet b = Instantiate(prefabBullet, pos, Quaternion.identity);

        if (TryGetComponent(out Collider2D miCol) && b.TryGetComponent(out Collider2D balaCol))
        {
            Physics2D.IgnoreCollision(miCol, balaCol, true);
        }

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
