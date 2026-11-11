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
    [SerializeField] private bool activarLuz = false;
    [SerializeField] private Light2D luzPrefab;
    [SerializeField] private Color colorLuz = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float radioLuz = 1.6f;
    [SerializeField] private float intensidadLuz = 1.2f;

    [Header("Partículas al Destruirse")]
    [SerializeField] private bool activarParticulas = true;
    [SerializeField, Min(1)] private int cantidadParticulas = 8;
    [SerializeField, Min(0.05f)] private float duracionParticulas = 0.25f;
    [SerializeField, Min(0.1f)] private float velocidadParticulas = 3.5f;
    [SerializeField, Min(0.01f)] private float tamanoParticulas = 0.12f;
    [SerializeField] private Color colorParticulas = new Color(1f, 0.35f, 0.35f, 1f);

    private static Material materialEfectosCompartido;

    private Rigidbody2D rb;
    private Collider2D miCollider;
    private SpriteRenderer sr;
    private Vector2 direccion;
    private bool direccionAsignada;
    private bool destruida;
    private int capaEnemigo;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out miCollider);
        TryGetComponent(out sr);

        capaEnemigo = LayerMask.NameToLayer("Enemy");

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        ConfigurarEfectosVisuales();
    }

    void Start()
    {
        Invoke(nameof(DestruirBala), tiempoVida);

        if (!direccionAsignada)
        {
            rb.linearVelocity = (Vector2)transform.right * velocidad;
        }
    }

    private void AsegurarMaterial()
    {
        if (materialEfectosCompartido != null) return;

        Shader shaderValido = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shaderValido != null)
        {
            materialEfectosCompartido = new Material(shaderValido);
        }
    }

    private void ConfigurarEfectosVisuales()
    {
        AsegurarMaterial();

        if (activarTrail)
        {
            if (!TryGetComponent(out TrailRenderer tr))
            {
                tr = gameObject.AddComponent<TrailRenderer>();
            }

            if (materialEfectosCompartido != null)
            {
                tr.sharedMaterial = materialEfectosCompartido;
            }

            tr.time = duracionTrail;
            tr.startWidth = anchoTrail;
            tr.endWidth = 0f;
            tr.startColor = colorTrailInicio;
            tr.endColor = colorTrailFin;
            tr.numCapVertices = 2;
            if (sr != null)
            {
                tr.sortingLayerID = sr.sortingLayerID;
                tr.sortingOrder = sr.sortingOrder - 1;
            }
        }

        if (!luzPrefab) TryGetComponent(out luzPrefab);
        if (luzPrefab != null)
        {
            luzPrefab.enabled = activarLuz;
            if (activarLuz && luzPrefab.lightType != Light2D.LightType.Global)
            {
                luzPrefab.color = colorLuz;
                luzPrefab.pointLightOuterRadius = radioLuz;
                luzPrefab.intensity = intensidadLuz;
                luzPrefab.shadowsEnabled = false;
            }
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

        CancelInvoke(nameof(DestruirBala));
        Invoke(nameof(DestruirBala), tiempoVida);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        ProcesarImpacto(col);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (EsTag(collision.gameObject, tagHueco) || EsAliadoOProyectil(collision.collider))
        {
            if (miCollider != null && collision.collider != null)
            {
                Physics2D.IgnoreCollision(miCollider, collision.collider, true);
            }
            rb.linearVelocity = (direccionAsignada ? direccion : (Vector2)transform.right) * velocidad;
            return;
        }

        ProcesarImpacto(collision.collider);
    }

    private void ProcesarImpacto(Collider2D col)
    {
        if (destruida || col == null) return;

        if (EsTag(col.gameObject, tagHueco) || EsAliadoOProyectil(col))
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

            DestruirBala();
            return;
        }

        if (EsTag(col.gameObject, tagPared) || !col.isTrigger)
        {
            DestruirBala();
        }
    }

    private bool EsAliadoOProyectil(Collider2D col)
    {
        if (col == null) return false;
        GameObject obj = col.gameObject;

        if (EsTag(obj, tagObjetivo)) return false;
        if (capaEnemigo != -1 && obj.layer == capaEnemigo) return true;
        if (EsTag(obj, "Enemy") || EsTag(obj, "Enemigo")) return true;
        if (col.TryGetComponent<IRangoDeteccion>(out _) || col.TryGetComponent<Bullet>(out _)) return true;
        if (col.GetComponentInParent<IRangoDeteccion>() != null) return true;

        return false;
    }

    private void DestruirBala()
    {
        if (destruida) return;
        destruida = true;

        if (activarParticulas && gameObject.scene.isLoaded)
        {
            GenerarParticulasImpacto();
        }

        Destroy(gameObject);
    }

    private void GenerarParticulasImpacto()
    {
        AsegurarMaterial();

        GameObject objParticulas = new GameObject("ParticulasBala");
        objParticulas.transform.position = transform.position;

        ParticleSystem ps = objParticulas.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = objParticulas.GetComponent<ParticleSystemRenderer>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (materialEfectosCompartido != null)
        {
            psr.sharedMaterial = materialEfectosCompartido;
        }

        if (sr != null)
        {
            psr.sortingLayerID = sr.sortingLayerID;
            psr.sortingOrder = sr.sortingOrder + 1;
        }

        var main = ps.main;
        main.duration = duracionParticulas;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(duracionParticulas * 0.6f, duracionParticulas);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadParticulas * 0.4f, velocidadParticulas);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoParticulas * 0.6f, tamanoParticulas);
        main.startColor = colorParticulas;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)cantidadParticulas) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ps.Play();
        Destroy(objParticulas, duracionParticulas + 0.2f);
    }

    private bool EsTag(GameObject obj, string tagBuscado)
    {
        return obj != null && !string.IsNullOrEmpty(tagBuscado) && string.Equals(obj.tag, tagBuscado, System.StringComparison.OrdinalIgnoreCase);
    }
}
