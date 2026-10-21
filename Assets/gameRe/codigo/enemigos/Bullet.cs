using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TrailRenderer))]
[RequireComponent(typeof(Light2D))]
public class Bullet : MonoBehaviour
{
    [Header("Trail (Estela)")]
    [SerializeField, Min(0f)] private float duracionTrail = 0.2f;
    [SerializeField, Min(0f)] private float grosorInicial = 0.25f;
    [SerializeField] private Color colorTrail = new Color(1f, 0.6f, 0.1f, 0.8f);

    [Header("Brillo y Luz")]
    [SerializeField] private Color colorLuz = new Color(1f, 0.7f, 0.2f, 0.75f);
    [SerializeField, Min(0f)] private float intensidadLuz = 3f;
    [SerializeField, Min(0f)] private float radioLuz = 1.8f;

    private int danio;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private TrailRenderer tr;
    private Light2D luz;

    private static Sprite spriteHalo;
    private static Material matAditivo;
    private static Material matUnlit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        sr = GetComponent<SpriteRenderer>();

        if (!TryGetComponent(out tr)) tr = gameObject.AddComponent<TrailRenderer>();
        if (!TryGetComponent(out luz)) luz = gameObject.AddComponent<Light2D>();

        InicializarRecursosBrillo();

        sr.sharedMaterial = matUnlit;

        tr.time = duracionTrail;
        tr.startWidth = grosorInicial;
        tr.endWidth = 0f;
        tr.startColor = colorTrail;
        tr.endColor = new Color(colorTrail.r, colorTrail.g, colorTrail.b, 0f);
        tr.sharedMaterial = matAditivo;
        tr.sortingLayerID = sr.sortingLayerID;
        tr.sortingOrder = sr.sortingOrder - 1;

        luz.lightType = Light2D.LightType.Point;
        luz.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, 1f);
        luz.intensity = intensidadLuz;
        luz.pointLightInnerRadius = 0f;
        luz.pointLightOuterRadius = radioLuz;
        luz.overlapOperation = Light2D.OverlapOperation.Additive;

        CrearHaloVisual();
    }

    private void InicializarRecursosBrillo()
    {
        if (matUnlit == null)
        {
            Shader sUnlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sUnlit == null) sUnlit = Shader.Find("Sprites/Default");
            matUnlit = new Material(sUnlit);
        }

        if (matAditivo == null)
        {
            Shader s = Shader.Find("Legacy Shaders/Particles/Additive");
            if (s == null) s = Shader.Find("Sprites/Default");
            matAditivo = new Material(s);
        }

        if (spriteHalo == null)
        {
            int res = 64;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 centro = new Vector2(res * 0.5f, res * 0.5f);
            float maxDist = res * 0.5f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centro) / maxDist;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            spriteHalo = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 32f);
        }
    }

    private void CrearHaloVisual()
    {
        GameObject halo = new GameObject("HaloBrillo");
        halo.transform.SetParent(transform, false);
        halo.transform.localPosition = Vector3.zero;
        halo.transform.localScale = Vector3.one * radioLuz;

        SpriteRenderer srHalo = halo.AddComponent<SpriteRenderer>();
        srHalo.sprite = spriteHalo;
        srHalo.color = colorLuz;
        srHalo.sharedMaterial = matAditivo;
        srHalo.sortingLayerID = sr.sortingLayerID;
        srHalo.sortingOrder = sr.sortingOrder - 1;
    }

    public void Init(Vector2 direccion, float velocidad, int danioBala, float tiempoDeVida = 5f)
    {
        this.danio = danioBala;
        rb.linearVelocity = direccion.normalized * velocidad;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        Destroy(gameObject, tiempoDeVida);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (col.TryGetComponent(out Vida vidaJugador))
            {
                if (vidaJugador.EsInvulnerable) return;
                vidaJugador.RecibirDanio(danio);
            }
            Destroy(gameObject);
        }
        else if (!col.isTrigger && col.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
