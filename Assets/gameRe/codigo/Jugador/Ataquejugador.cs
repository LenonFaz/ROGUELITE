using UnityEngine;
using UnityEngine.InputSystem;

public class AtaqueJugador : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField, Min(0f)] private float rango = 1.2f;
    [SerializeField] private Vector2 tamHitbox = new Vector2(1.2f, 1f);
    [SerializeField, Min(0f)] private float cooldown = 0.3f;
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField, Min(0)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 8f;

    [Header("Animación y Efectos")]
    [SerializeField] private Animator animJugador;
    [SerializeField] private SpriteRenderer spriteJugador;
    [SerializeField, Min(0f)] private float duracionAnimAtaque = 0.2f;
    [SerializeField] private AnimationClip aAtkUp;
    [SerializeField] private AnimationClip aAtkDown;
    [SerializeField] private AnimationClip aAtkRight;
    [SerializeField] private GameObject prefabEfectoGolpe;

    public bool EstaAtacando { get; private set; }

    private float timerCooldown;
    private float timerAtaque;
    private Vector2 ultimaDireccion = Vector2.down;
    private Vector2 inputAtaque;

    void Awake()
    {
        if (!animJugador) animJugador = GetComponentInChildren<Animator>();
        if (!spriteJugador) spriteJugador = GetComponentInChildren<SpriteRenderer>();
    }

    void OnLook(InputValue value)
    {
        inputAtaque = value.Get<Vector2>();
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;

        if (EstaAtacando)
        {
            timerAtaque -= Time.deltaTime;
            if (timerAtaque <= 0f) EstaAtacando = false;
        }

        if (inputAtaque != Vector2.zero && timerCooldown <= 0f)
        {
            Vector2 dirRigida = inputAtaque.normalized;
            if (Mathf.Abs(dirRigida.x) > Mathf.Abs(dirRigida.y))
                dirRigida = dirRigida.x > 0f ? Vector2.right : Vector2.left;
            else
                dirRigida = dirRigida.y > 0f ? Vector2.up : Vector2.down;

            Atacar(dirRigida);
            timerCooldown = cooldown;
        }
    }

    void Atacar(Vector2 dir)
    {
        EstaAtacando = true;
        timerAtaque = duracionAnimAtaque;
        ultimaDireccion = dir;

        Vector2 centro = (Vector2)transform.position + dir * rango;

        ReproducirAnimacionAtaque(dir);
        GenerarEfectoVisual(centro, dir);

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centro, tamHitbox, angulo, capaEnemigos);

        for (int i = 0; i < golpeados.Length; i++)
        {
            Collider2D c = golpeados[i];
            if (c.TryGetComponent(out Vida vida)) vida.RecibirDanio(danio);
            if (c.TryGetComponent(out Empuje empuje)) empuje.AplicarEmpuje(dir, fuerzaEmpuje);
            if (c.TryGetComponent(out Aturdido aturdido)) aturdido.Aturdir();
        }
    }

    void ReproducirAnimacionAtaque(Vector2 dir)
    {
        if (!animJugador || !spriteJugador) return;

        AnimationClip clipAtk = aAtkDown;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            clipAtk = aAtkRight;
            spriteJugador.flipX = dir.x < 0f;
        }
        else
        {
            clipAtk = dir.y > 0f ? aAtkUp : aAtkDown;
            spriteJugador.flipX = false;
        }

        if (clipAtk)
        {
            animJugador.Play(clipAtk.name, -1, 0f);
        }
    }

    void GenerarEfectoVisual(Vector2 pos, Vector2 dir)
    {
        if (!prefabEfectoGolpe) return;

        float anguloEfecto = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(prefabEfectoGolpe, pos, Quaternion.Euler(0f, 0f, anguloEfecto));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 centro = Application.isPlaying ? (Vector2)transform.position + ultimaDireccion * rango : (Vector2)transform.position + Vector2.down * rango;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
