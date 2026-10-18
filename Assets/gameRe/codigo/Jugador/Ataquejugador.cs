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
    [SerializeField, Min(0f)] private float duracionAnimAtaque = 0.2f; // Lo que dura el jugador en pose de ataque
    [SerializeField] private AnimationClip aAtkUp;
    [SerializeField] private AnimationClip aAtkDown;
    [SerializeField] private AnimationClip aAtkRight;
    [SerializeField] private GameObject prefabEfectoGolpe;

    public bool EstaAtacando { get; private set; }

    private float timerCooldown = 0f;
    private float timerAtaque = 0f;
    private Vector2 ultimaDireccion = Vector2.down;
    private Vector2 inputAtaque;

    void Awake()
    {
        if (!animJugador) TryGetComponent(out animJugador);
        if (!spriteJugador) TryGetComponent(out spriteJugador);
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
            // Forzar el vector a 4 direcciones exactas para la animación
            Vector2 dirRígida = inputAtaque.normalized;
            if (Mathf.Abs(dirRígida.x) > Mathf.Abs(dirRígida.y))
                dirRígida = dirRígida.x > 0 ? Vector2.right : Vector2.left;
            else
                dirRígida = dirRígida.y > 0 ? Vector2.up : Vector2.down;

            Atacar(dirRígida);
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

        foreach (var c in golpeados)
        {
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
            spriteJugador.flipX = dir.x < 0; // Si ataca a la izquierda, voltea el sprite
        }
        else
        {
            clipAtk = dir.y > 0 ? aAtkUp : aAtkDown;
            spriteJugador.flipX = false;
        }

        if (clipAtk)
        {
            // El 0f al final fuerza a que la animación inicie desde el frame 0 siempre
            animJugador.Play(clipAtk.name, -1, 0f);
        }
    }

    void GenerarEfectoVisual(Vector2 pos, Vector2 dir)
    {
        if (!prefabEfectoGolpe) return;

        // Rota el efecto visual para que mire hacia donde atacaste
        float anguloEfecto = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(prefabEfectoGolpe, pos, Quaternion.Euler(0, 0, anguloEfecto));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 centro = Application.isPlaying ? (Vector2)transform.position + ultimaDireccion * rango : (Vector2)transform.position + Vector2.down * rango;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
