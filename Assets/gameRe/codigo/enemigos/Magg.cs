using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Magg : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 2.5f;
    [SerializeField, Min(0f)] private float velLentaAtaque = 0.65f;
    [SerializeField, Min(0f)] private float acel = 15f;
    [SerializeField, Min(0f)] private float desacel = 18f;
    [SerializeField, Min(0f)] private float rangoDet = 8f;
    [SerializeField, Min(0f)] private float distMax = 6f;
    [SerializeField, Min(0f)] private float distMin = 4f;

    [Header("Disparo Múltiple")]
    [SerializeField] private Bullet prefabBullet;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField, Min(0f)] private float cdDisparo = 1.5f;
    [SerializeField, Min(0f)] private float tiempoAnticipacion = 0.5f;
    [SerializeField, Min(1)] private int cantidadBalas = 5;
    [SerializeField, Min(0f)] private float intervaloEntreBalas = 0.08f;
    [SerializeField, Min(0f)] private float anguloImprecision = 16f;
    [SerializeField, Min(0f)] private float velocidadBala = 8f;
    [SerializeField, Min(1)] private int danioBala = 1;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aQuieta;
    [SerializeField] private AnimationClip aCaminarArriba;
    [SerializeField] private AnimationClip aCaminarAbajo;
    [SerializeField] private AnimationClip aPrepararArriba;
    [SerializeField] private AnimationClip aPrepararAbajo;

    public float RangoDeteccion => rangoDet;

    private Rigidbody2D rb;
    private Collider2D miCol;
    private Aturdido aturdido;
    private Empuje empuje;
    private IndicadorAtaque indicador;

    private float tDisparo;
    private bool preparandose;
    private bool atacando;
    private string animActual;
    private Coroutine rutinaAtaque;

    void OnValidate()
    {
        if (distMin >= distMax) distMax = distMin + 1f;
    }

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out miCol);
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

        if (player && miCol && player.TryGetComponent(out Collider2D colP))
        {
            Physics2D.IgnoreCollision(miCol, colP, true);
        }
    }

    void Update()
    {
        if (!player) return;

        if (Mathf.Abs(player.position.x - transform.position.x) > 0.05f)
        {
            sprite.flipX = player.position.x < transform.position.x;
        }

        if (Bloqueado())
        {
            CancelarAtaqueActual();
            return;
        }

        if (preparandose || atacando) return;

        tDisparo -= Time.deltaTime;

        if (tDisparo <= 0f && Vector2.Distance(transform.position, player.position) <= rangoDet)
        {
            rutinaAtaque = StartCoroutine(SecuenciaAtaque());
        }
    }

    private IEnumerator SecuenciaAtaque()
    {
        preparandose = true;
        indicador.MostrarAviso(tiempoAnticipacion);

        yield return new WaitForSeconds(tiempoAnticipacion);

        if (Bloqueado())
        {
            CancelarAtaqueActual();
            yield break;
        }

        preparandose = false;
        atacando = true;

        for (int i = 0; i < cantidadBalas; i++)
        {
            if (Bloqueado() || !player)
            {
                CancelarAtaqueActual();
                yield break;
            }

            DispararBalaImprecisa();

            if (i < cantidadBalas - 1 && intervaloEntreBalas > 0f)
            {
                yield return new WaitForSeconds(intervaloEntreBalas);
            }
        }

        atacando = false;
        tDisparo = cdDisparo;
        rutinaAtaque = null;
    }

    private void CancelarAtaqueActual()
    {
        if (rutinaAtaque != null)
        {
            StopCoroutine(rutinaAtaque);
            rutinaAtaque = null;
        }
        if (preparandose || atacando)
        {
            preparandose = false;
            atacando = false;
            indicador.CancelarAviso();
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
        Vector2 hacia = player.position - transform.position;
        float dist = hacia.magnitude;
        Vector2 dir = dist > 0.001f ? hacia / dist : Vector2.zero;

        bool enAccionAtaque = preparandose || atacando;
        float velActual = enAccionAtaque ? velLentaAtaque : vel;

        if (dist <= rangoDet)
        {
            if (dist >= distMin && dist <= distMax)
            {
                obj = enAccionAtaque ? dir * (velLentaAtaque * 0.5f) : Vector2.zero;
                float tasaFreno = enAccionAtaque ? acel : desacel * 2f;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasaFreno * Time.fixedDeltaTime);
            }
            else
            {
                obj = dist > distMax ? dir * velActual : -dir * velActual;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, acel * Time.fixedDeltaTime);
            }
        }
        else
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
        }

        ActualizarAnim(obj);
    }

    void ActualizarAnim(Vector2 velObj)
    {
        if (!anim || !sprite || !player) return;

        bool mirandoArriba = player.position.y > transform.position.y;
        AnimationClip clipObj = aQuieta;

        if (preparandose || atacando)
        {
            clipObj = mirandoArriba ? aPrepararArriba : aPrepararAbajo;
        }
        else if (velObj != Vector2.zero || rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            bool movArriba = rb.linearVelocity.sqrMagnitude > 0.05f ? rb.linearVelocity.y > 0f : mirandoArriba;
            clipObj = movArriba ? aCaminarArriba : aCaminarAbajo;
        }

        if (clipObj && animActual != clipObj.name)
        {
            anim.Play(clipObj.name);
            animActual = clipObj.name;
        }
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    void DispararBalaImprecisa()
    {
        if (!prefabBullet || !player) return;

        Vector3 pos = puntoDisparo ? puntoDisparo.position : transform.position;
        Vector2 haciaJugador = ((Vector2)player.position - (Vector2)pos).normalized;
        float anguloBase = Mathf.Atan2(haciaJugador.y, haciaJugador.x) * Mathf.Rad2Deg;
        float anguloDesviado = (anguloBase + Random.Range(-anguloImprecision, anguloImprecision)) * Mathf.Deg2Rad;
        Vector2 dirBala = new Vector2(Mathf.Cos(anguloDesviado), Mathf.Sin(anguloDesviado));

        Bullet b = Instantiate(prefabBullet, pos, Quaternion.identity);

        if (miCol != null && b.TryGetComponent(out Collider2D balaCol))
        {
            Physics2D.IgnoreCollision(miCol, balaCol, true);
        }

        b.Init(dirBala, velocidadBala, danioBala, 5f);
    }

    void OnDisable()
    {
        CancelarAtaqueActual();
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
