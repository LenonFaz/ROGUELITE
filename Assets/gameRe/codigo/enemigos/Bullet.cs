using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Estadísticas del Proyectil")]
    [SerializeField] private float velocidad = 10f;
    [SerializeField] private int danio = 1;
    [SerializeField] private float tiempoVida = 4f;
    [SerializeField] private float fuerzaEmpuje = 4f;

    [Header("Objetivos y Terreno")]
    [SerializeField] private string tagObjetivo = "Player";
    [SerializeField] private string tagPared = "pared";
    [SerializeField] private string tagHueco = "hueco";

    [Header("Efectos Visuales (Trail y Luz)")]
    [SerializeField] private bool activarTrail = true;
    [SerializeField] private float duracionTrail = 0.18f;
    [SerializeField] private float anchoTrail = 0.22f;
    [SerializeField] private Color colorTrailInicio = new Color(1f, 0.35f, 0.35f, 0.85f);
    [SerializeField] private Color colorTrailFin = new Color(1f, 0.1f, 0.1f, 0f);
    [SerializeField] private bool activarLuz = true;
    [SerializeField] private Color colorLuz = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float radioLuz = 1.6f;
    [SerializeField] private float intensidadLuz = 1.2f;

    private static Material materialTrailCompartido;

    private Rigidbody2D rb;
    private Collider2D miCollider;
    private Vector2 direccion;
    private bool direccionAsignada;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out miCollider);

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        ConfigurarEfectosVisuales();
    }

    void Start()
    {
        Destroy(gameObject, tiempoVida);

        if (!direccionAsignada)
        {
            rb.linearVelocity = (Vector2)transform.right * velocidad;
        }
    }

    private void ConfigurarEfectosVisuales()
    {
        if (TryGetComponent(out SpriteRenderer sr) && sr.sharedMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ?? Shader.Find("Sprites/Default");
            if (s != null) sr.sharedMaterial = new Material(s);
        }

        if (activarTrail)
        {
            if (!TryGetComponent(out TrailRenderer tr))
            {
                tr = gameObject.AddComponent<TrailRenderer>();
            }

            if (materialTrailCompartido == null)
            {
                Shader shaderValido = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shaderValido != null)
                {
                    materialTrailCompartido = new Material(shaderValido);
                }
            }

            if (tr.sharedMaterial == null || tr.sharedMaterial.shader == null || tr.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                tr.sharedMaterial = materialTrailCompartido;
            }

            tr.time = duracionTrail;
            tr.startWidth = anchoTrail;
            tr.endWidth = 0f;
            tr.startColor = colorTrailInicio;
            tr.endColor = colorTrailFin;
            tr.numCapVertices = 2;
            tr.sortingOrder = sr != null ? sr.sortingOrder - 1 : 0;
        }

        if (activarLuz)
        {
            if (!TryGetComponent(out Light2D luz))
            {
                luz = gameObject.AddComponent<Light2D>();
            }
            luz.lightType = Light2D.LightType.Point;
            luz.color = colorLuz;
            luz.pointLightOuterRadius = radioLuz;
            luz.intensity = intensidadLuz;
            luz.shadowsEnabled = false;
        }
    }

    public void Init(Vector2 dir) => ConfigurarBala(dir, velocidad, danio, tiempoVida);
    public void Init(Vector2 dir, float vel) => ConfigurarBala(dir, vel, danio, tiempoVida);
    public void Init(Vector2 dir, float vel, int dmg) => ConfigurarBala(dir, vel, dmg, tiempoVida);
    public void Init(Vector2 dir, float vel, float vida) => ConfigurarBala(dir, vel, danio, vida);
    public void Init(Vector2 dir, float vel, int dmg, float vida) => ConfigurarBala(dir, vel, dmg, vida);

    public void Inicializar(Vector2 dir, float vel = -1f, int dmg = -1)
    {
        ConfigurarBala(dir, vel > 0f ? vel : velocidad, dmg >= 0 ? dmg : danio, tiempoVida);
    }

    private void ConfigurarBala(Vector2 dir, float vel, int dmg, float vida)
    {
        direccion = dir.normalized;
        velocidad = vel;
        danio = dmg;
        tiempoVida = vida;
        direccionAsignada = true;

        if (!rb) TryGetComponent(out rb);
        rb.linearVelocity = direccion * velocidad;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        ProcesarImpacto(col);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (EsTag(collision.gameObject, tagHueco))
        {
            Physics2D.IgnoreCollision(miCollider, collision.collider, true);
            rb.linearVelocity = (direccionAsignada ? direccion : (Vector2)transform.right) * velocidad;
            return;
        }

        ProcesarImpacto(collision.collider);
    }

    private void ProcesarImpacto(Collider2D col)
    {
        if (col == null) return;

        if (EsTag(col.gameObject, tagHueco))
        {
            if (!col.isTrigger && miCollider != null)
            {
                Physics2D.IgnoreCollision(miCollider, col, true);
            }
            return;
        }

        if (col.isTrigger && !EsTag(col.gameObject, tagObjetivo)) return;

        if (EsTag(col.gameObject, tagObjetivo))
        {
            if (col.TryGetComponent(out Jugador jug) && jug.EsInvulnerablePorDash) return;
            if (col.TryGetComponent(out Vida vidaObj))
            {
                if (vidaObj.EsInvulnerable) return;
                vidaObj.RecibirDanio(danio);
            }
            if (fuerzaEmpuje > 0f && col.TryGetComponent(out Empuje empujeObj))
            {
                Vector2 dirEmpuje = direccionAsignada ? direccion : (Vector2)(col.transform.position - transform.position).normalized;
                empujeObj.AplicarEmpuje(dirEmpuje, fuerzaEmpuje);
            }

            Destroy(gameObject);
            return;
        }

        if (EsTag(col.gameObject, tagPared) || !col.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    private bool EsTag(GameObject obj, string tagBuscado)
    {
        return obj != null && !string.IsNullOrEmpty(tagBuscado) && string.Equals(obj.tag, tagBuscado, System.StringComparison.OrdinalIgnoreCase);
    }
}
