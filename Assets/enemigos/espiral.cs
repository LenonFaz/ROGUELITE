using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Espiral : MonoBehaviour
{
    public Transform player;
    public ProyectilEspiral prefabProyectil;
    public Transform puntoDisparo;

    public float rangoDet = 8f;
    public float cdTeleport = 2f;
    public float tpDistMin = 3f;
    public float tpDistMax = 5f;

    [Header("Fade")]
    public float tiempoFadeOut = 0.3f;
    public float tiempoInvisible = 0.2f;
    public float tiempoFadeIn = 0.3f;

    Rigidbody2D rb;
    SpriteRenderer sprite;
    float tTeleport;
    bool ocupado;
    Vector2 posicionFija;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    void Start()
    {
        RestablecerSprite();
        posicionFija = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        tTeleport = cdTeleport;
    }

    void Update()
    {
        if (player == null || ocupado) return;

        sprite.flipX = player.position.x < transform.position.x;

        if (Vector2.Distance(transform.position, player.position) > rangoDet) return;

        tTeleport -= Time.deltaTime;
        if (tTeleport <= 0f)
            StartCoroutine(AtaqueYTeleport());
    }
    void LateUpdate()
    {
        transform.position = posicionFija;
        rb.position = posicionFija;
        rb.linearVelocity = Vector2.zero;
    }

    IEnumerator AtaqueYTeleport()
    {
        ocupado = true;

        DispararUno();
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(HacerFade(1f, 0f, tiempoFadeOut));
        yield return new WaitForSeconds(tiempoInvisible);

        posicionFija = CalcularNuevaPosicion();
        transform.position = posicionFija;
        rb.position = posicionFija;
        Physics2D.SyncTransforms();

        yield return StartCoroutine(HacerFade(0f, 1f, tiempoFadeIn));

        RestablecerSprite();
        tTeleport = cdTeleport;
        ocupado = false;
    }

    Vector2 CalcularNuevaPosicion()
    {
        if (player == null) return transform.position;

        float ang = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(tpDistMin, tpDistMax);
        return (Vector2)player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
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

    void DispararUno()
    {
        if (prefabProyectil == null || player == null) return;

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)origen).normalized;

        ProyectilEspiral p = Instantiate(prefabProyectil, origen, Quaternion.identity);
        p.Iniciar(dir);
    }

    void OnEnable()
    {
        RestablecerSprite();
        posicionFija = transform.position;
        ocupado = false;
    }

    void OnDisable()
    {
        RestablecerSprite();
        ocupado = false;
    }
}