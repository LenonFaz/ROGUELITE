using UnityEngine;
using UnityEngine.InputSystem;

public class AtaqueJugador : MonoBehaviour
{
    [Header("Hitbox y Ataque")]
    [SerializeField, Min(0f)] private float offsetHitbox = 1.2f;
    [SerializeField] private Vector2 tamHitbox = new Vector2(1.2f, 1f);
    [SerializeField, Min(0f)] private float cooldown = 0.3f;
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField, Min(0)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 8f;

    [Header("Animación")]
    [SerializeField] private Animator animJugador;
    [SerializeField] private SpriteRenderer spriteJugador;
    [SerializeField, Min(0f)] private float duracionAnimAtaque = 0.2f;
    [SerializeField] private AnimationClip aAtkUp;
    [SerializeField] private AnimationClip aAtkDown;
    [SerializeField] private AnimationClip aAtkRight;

    public bool EstaAtacando { get; private set; }

    private Jugador jugador;
    private float timerCooldown;
    private float timerAtaque;
    private Vector2 ultimaDireccion = Vector2.down;
    private Vector2 inputAtaque;
    private bool bloqueadoExternamente;

    void Awake()
    {
        TryGetComponent(out jugador);
        if (!animJugador) animJugador = GetComponentInChildren<Animator>();
        if (!spriteJugador) spriteJugador = GetComponentInChildren<SpriteRenderer>();
    }

    public void EstablecerBloqueoExterno(bool bloqueado)
    {
        bloqueadoExternamente = bloqueado;
        if (bloqueado)
        {
            EstaAtacando = false;
            timerAtaque = 0f;
        }
    }

    void OnLook(InputValue value)
    {
        inputAtaque = value.Get<Vector2>();
    }

    void Update()
    {
        if (bloqueadoExternamente || (jugador != null && jugador.EstaEncadenado))
        {
            EstaAtacando = false;
            return;
        }

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

        if (jugador != null)
        {
            jugador.EstablecerDireccion(dir);
        }

        ReproducirAnimacionAtaque(dir);

        Vector2 centro = (Vector2)transform.position + dir * offsetHitbox;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 dir = Application.isPlaying ? ultimaDireccion : Vector2.down;
        Vector2 centro = (Vector2)transform.position + dir * offsetHitbox;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.Euler(0f, 0f, angulo), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
