using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Espi : MonoBehaviour, IRangoDeteccion
{
    [Header("Referencias")]
    public Transform player;
    public Bullet prefabBullet;
    public Transform puntoDisparo;

    [Header("Físicas")]
    public float desacel = 18f;

    [Header("Ataque Espiral")]
    public float tiempoAvisoAtaque = 0.35f;
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

    [Header("Límites del mapa")]
    [Tooltip("Tilemap 'Limites del mapa' (tiles = zonas prohibidas). Si está vacío, no se validan límites.")]
    public Tilemap tilemapLimites;
    public float margenBorde = 0.5f;
    public int intentosMaximos = 30;

    [Header("Fade")]
    public float tiempoFadeOut = 0.3f;
    public float tiempoInvisible = 0.2f;
    public float tiempoFadeIn = 0.3f;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private AnimationClip aQuieto;
    [SerializeField] private AnimationClip aLanzarProyectiles;
    [SerializeField] private AnimationClip aTeleport;

    public float RangoDeteccion => rangoDet;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D miCol;
    private Collider2D colLimites;
    private Empuje empuje;
    private Aturdido aturdido;
    private IndicadorAtaque indicador;
    private float tTeleport;
    private bool ocupado;
    private string animActual;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out sprite);
        TryGetComponent(out miCol);
        TryGetComponent(out empuje);
        TryGetComponent(out aturdido);
        if (!TryGetComponent(out indicador)) indicador = gameObject.AddComponent<IndicadorAtaque>();
        if (!anim) TryGetComponent(out anim);

        if (tilemapLimites != null) tilemapLimites.TryGetComponent(out colLimites);

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
            if (Jugador.Instancia != null) player = Jugador.Instancia.transform;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
        }

        if (player != null && miCol != null && player.TryGetComponent(out Collider2D colP))
        {
            Physics2D.IgnoreCollision(miCol, colP, true);
        }

        tTeleport = cdTeleport;
        ReproducirAnim(aQuieto);
    }

    void Update()
    {
        if (player == null) return;

        if (!ocupado)
        {
            ReproducirAnim(aQuieto);
        }

        if (ocupado || Bloqueado()) return;

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
        ReproducirAnim(aLanzarProyectiles);

        indicador.MostrarAviso(tiempoAvisoAtaque);
        yield return new WaitForSeconds(tiempoAvisoAtaque);

        if (!Bloqueado())
        {
            yield return StartCoroutine(DispararEspiral());
        }

        ReproducirAnim(aTeleport);
        yield return StartCoroutine(HacerFade(1f, 0f, tiempoFadeOut));
        yield return new WaitForSeconds(tiempoInvisible);

        Vector2 nuevaPos = CalcularNuevaPosicion();
        transform.position = nuevaPos;
        rb.position = nuevaPos;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        yield return StartCoroutine(HacerFade(0f, 1f, tiempoFadeIn));

        RestablecerSprite();
        ReproducirAnim(aQuieto);
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

            if (tilemapLimites != null && !PosicionValida(candidata)) continue;

            Collider2D[] colisiones = Physics2D.OverlapCircleAll(candidata, margenBorde);
            bool chocaObstaculo = false;
            for (int j = 0; j < colisiones.Length; j++)
            {
                Collider2D col = colisiones[j];
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

    bool PosicionValida(Vector2 punto)
    {
        Vector2[] offsets = { Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3Int celda = tilemapLimites.WorldToCell(punto + offsets[i] * margenBorde);
            if (!tilemapLimites.HasTile(celda)) return false;
        }
        return true;
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

    private void ReproducirAnim(AnimationClip clip)
    {
        if (!anim || !clip) return;
        if (animActual == clip.name) return;
        anim.Play(clip.name);
        animActual = clip.name;
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
        animActual = string.Empty;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (indicador != null) indicador.CancelarAviso();
        RestablecerSprite();
        ocupado = false;
    }
}