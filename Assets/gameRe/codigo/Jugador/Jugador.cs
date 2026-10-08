using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float vel = 6f;
    public float acel = 45f;
    public float desacel = 60f;

    [Header("Dash")]
    public float velDash = 22f;
    public float durDash = 0.15f;
    public float cdDash = 0.6f;
    public float durInv = 0.2f;

    [Header("Animaciones")]
    public Animator anim;
    public SpriteRenderer sprite;
    public AnimationClip aUp;
    public AnimationClip aDown;
    public AnimationClip aRight; // Se usará para derecha e izquierda
    public AnimationClip aIdle;
    public AnimationClip aDash;

    Rigidbody2D rb;
    Empuje empuje;

    Vector2 input;
    Vector2 ultimaDir = Vector2.down;

    bool dasheando;
    bool puedeDashear = true;
    float tDash;
    float tCdDash;

    public bool invencible { get; private set; }
    float tInv;

    string nombreAnimActual;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        empuje = GetComponent<Empuje>();

        if (!anim) anim = GetComponent<Animator>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        LeerInput();
        LeerDash();
        ActualizarTimers(Time.deltaTime);
        ActualizarAnim();
    }

    void FixedUpdate()
    {
        if (empuje && empuje.EstaEmpujado) return;

        if (dasheando)
        {
            rb.linearVelocity = ultimaDir * velDash;
            return;
        }

        Vector2 obj = input * vel;
        float tasa = input.sqrMagnitude > 0.01f ? acel : desacel;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasa * Time.fixedDeltaTime);
    }

    void LeerInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float x = 0f, y = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.wKey.isPressed) y += 1f;
        if (kb.sKey.isPressed) y -= 1f;

        input = new Vector2(x, y).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                ultimaDir = input.x > 0 ? Vector2.right : Vector2.left;
            else
                ultimaDir = input.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    void LeerDash()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && puedeDashear && !dasheando)
        {
            dasheando = true;
            puedeDashear = false;
            tDash = durDash;
            tCdDash = cdDash;
            invencible = true;
            tInv = durInv;
        }
    }

    void ActualizarTimers(float dt)
    {
        if (dasheando)
        {
            tDash -= dt;
            if (tDash <= 0f) dasheando = false;
        }

        if (invencible)
        {
            tInv -= dt;
            if (tInv <= 0f) invencible = false;
        }

        if (!puedeDashear)
        {
            tCdDash -= dt;
            if (tCdDash <= 0f) puedeDashear = true;
        }
    }

    void ActualizarAnim()
    {
        if (!anim || !sprite) return;

        AnimationClip clipObj = aIdle;

        if (dasheando)
        {
            clipObj = aDash;
        }
        else if (input.sqrMagnitude > 0.01f)
        {
            if (ultimaDir == Vector2.up)
            {
                clipObj = aUp;
                sprite.flipX = false; // Restablecer flip al ir arriba
            }
            else if (ultimaDir == Vector2.down)
            {
                clipObj = aDown;
                sprite.flipX = false; // Restablecer flip al ir abajo
            }
            else if (ultimaDir == Vector2.right || ultimaDir == Vector2.left)
            {
                clipObj = aRight;
                // Si va a la izquierda se voltea (true), si va a la derecha se mantiene normal (false)
                sprite.flipX = (ultimaDir == Vector2.left);
            }
        }

        if (clipObj && nombreAnimActual != clipObj.name)
        {
            anim.Play(clipObj.name);
            nombreAnimActual = clipObj.name;
        }
    }
}
