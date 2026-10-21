using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Espi : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Bullet prefabBullet;
    public Transform puntoDisparo;

    [Header("Físicas")]
    public float desacel = 18f;

    [Header("Ataque Espiral")]
    public int shootAmount = 10;
    public float shootInterval = 0.1f;
    public float velocidadBala = 6f;
    public int danioBala = 1;
    public float vidaBala = 5f;

    [Header("Detección y teletransporte")]
    public float rangoDet = 8f;
    public float cdTeleport = 2f;
    public float tpDistMin = 3f;
    public float tpDistMax = 5f;

    [Header("Límites del mapa (Opcional)")]
    public Collider2D limitesMapa;
    public float margenBorde = 0.5f;
    public int intentosMaximos = 30;

    [Header("Fade")]
    public float tiempoFadeOut = 0.3f;
    public float tiempoInvisible = 0.2f;
    public float tiempoFadeIn = 0.3f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D miCol;
    private Empuje empuje;
    private Aturdido aturdido;
    private float tTeleport;
    private bool ocupado;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out sprite);
        TryGetComponent(out miCol);
        TryGetComponent(out empuje);
        TryGetComponent(out aturdido);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        RestablecerSprite();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && miCol != null && player.TryGetComponent(out Collider2D colP))
        {
            Physics2D.IgnoreCollision(miCol, colP, true);
        }

        tTeleport = cdTeleport;
    }

    void Update()
    {
        if (player == null || ocupado || Bloqueado()) return;

        sprite.flipX = player.position.x < transform.position.x;

        if (Vector2.Distance(transform.position, player.position) > rangoDet) return;

        tTeleport -= Time.deltaTime;

        if (tTeleport <= 0f)
        {
            StartCoroutine(AtaqueYTeleport());
        }
    }

    void FixedUpdate()
    {
        if (empuje != null && empuje.EstaEmpujado) return;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desacel * Time.fixedDeltaTime);
    }

    bool Bloqueado() => (aturdido != null && aturdido.EstaAturdido) || (empuje != null && empuje.EstaEmpujado);

    IEnumerator AtaqueYTeleport()
    {
        ocupado = true;

        yield return StartCoroutine(DispararEspiral());
        yield return StartCoroutine(HacerFade(1f, 0f, tiempoFadeOut));
        yield return new WaitForSeconds(tiempoInvisible);

        Vector2 nuevaPos = CalcularNuevaPosicion();
        transform.position = nuevaPos;
        rb.position = nuevaPos;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        yield return StartCoroutine(HacerFade(0f, 1f, tiempoFadeIn));

        RestablecerSprite();
        tTeleport = cdTeleport;
        ocupado = false;
    }

    IEnumerator DispararEspiral()
    {
        for (int i = 0; i < shootAmount; i++)
        {
            if (Bloqueado()) yield break;

            float angulo = Mathf.Deg2Rad * (360f / shootAmount) * i;
            Vector2 direccion = new Vector2(Mathf.Sin(angulo), Mathf.Cos(angulo));

            if (prefabBullet != null)
            {
                Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
                Bullet b = Instantiate(prefabBullet, origen, Quaternion.identity);

                if (miCol != null && b.TryGetComponent(out Collider2D balaCol))
                {
                    Physics2D.IgnoreCollision(miCol, balaCol, true);
                }

                b.Init(direccion, velocidadBala, danioBala, vidaBala);
            }

            if (i < shootAmount - 1)
            {
                yield return new WaitForSeconds(shootInterval);
            }
        }
    }

    Vector2 CalcularNuevaPosicion()
    {
        if (player == null) return transform.position;

        int capaEnemigo = LayerMask.NameToLayer("Enemy");

        for (int i = 0; i < intentosMaximos; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(tpDistMin, tpDistMax);
            Vector2 candidata = (Vector2)player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (limitesMapa != null)
            {
                Bounds limites = limitesMapa.bounds;
                bool dentro =
                    candidata.x >= limites.min.x + margenBorde &&
                    candidata.x <= limites.max.x - margenBorde &&
                    candidata.y >= limites.min.y + margenBorde &&
                    candidata.y <= limites.max.y - margenBorde;

                if (!dentro) continue;
            }

            Collider2D[] colisiones = Physics2D.OverlapCircleAll(candidata, margenBorde);
            bool chocaObstaculo = false;
            foreach (var col in colisiones)
            {
                if (!col.isTrigger && col.gameObject != gameObject && !col.CompareTag("Player") && col.gameObject.layer != capaEnemigo)
                {
                    chocaObstaculo = true;
                    break;
                }
            }

            if (!chocaObstaculo) return candidata;
        }

        return transform.position;
    }

    IEnumerator HacerFade(float inicio, float fin, float duracion)
    {
        float t = 0f;
        Color c = sprite.color;

        while (t < duracion)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(inicio, fin, duracion <= 0f ? 1f : t / duracion);
            sprite.color = c;
            yield return null;
        }

        c.a = fin;
        sprite.color = c;
    }

    void RestablecerSprite()
    {
        if (sprite == null) return;
        Color c = sprite.color;
        c.a = 1f;
        sprite.color = c;
    }

    void OnEnable()
    {
        RestablecerSprite();
        ocupado = false;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        RestablecerSprite();
        ocupado = false;
    }
}
