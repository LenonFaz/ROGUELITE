using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 6f;
    [SerializeField, Min(0f)] private float acel = 45f;
    [SerializeField, Min(0f)] private float desacel = 60f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float velDash = 22f;
    [SerializeField, Min(0f)] private float durDash = 0.15f;
    [SerializeField, Min(0f)] private float cdDash = 0.6f;
    [SerializeField, Min(0f)] private float durInv = 0.2f;

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aUp;
    [SerializeField] private AnimationClip aDown;
    [SerializeField] private AnimationClip aRight;
    [SerializeField] private AnimationClip aIdleUp;
    [SerializeField] private AnimationClip aIdleDown;
    [SerializeField] private AnimationClip aIdleRight;
    [SerializeField] private AnimationClip aDash;

    private Rigidbody2D rb;
    private Empuje empuje;

    private Vector2 inputMov;
    private Vector2 ultimaDir = Vector2.down;

    private bool dasheando;
    private bool puedeDashear = true;
    private float tDash;
    private float tCdDash;

    public bool EsInvencible { get; private set; }
    private float tInv;

    private string animActual;

    void Awake()
    {
        TryGetComponent(out rb);
        rb.gravityScale = 0f;

        TryGetComponent(out empuje);
        if (!anim) TryGetComponent(out anim);
        if (!sprite) TryGetComponent(out sprite);
    }

    void OnMove(InputValue value)
    {
        inputMov = value.Get<Vector2>();

        if (inputMov != Vector2.zero)
        {
            ultimaDir = inputMov.normalized;
        }
    }

    void OnDash(InputValue value)
    {
        if (value.isPressed && puedeDashear && !dasheando)
        {
            dasheando = true;
            puedeDashear = false;
            tDash = durDash;
            tCdDash = cdDash;
            EsInvencible = true;
            tInv = durInv;
        }
    }

    void Update()
    {
        ActualizarTimers(Time.deltaTime);
        ActualizarAnim();
    }

    void FixedUpdate()
    {
        if (empuje != null && empuje.EstaEmpujado) return;

        if (dasheando)
        {
            rb.linearVelocity = ultimaDir * velDash;
            return;
        }

        Vector2 obj = inputMov * vel;
        float tasa = inputMov != Vector2.zero ? acel : desacel;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasa * Time.fixedDeltaTime);
    }

    void ActualizarTimers(float dt)
    {
        if (dasheando)
        {
            tDash -= dt;
            if (tDash <= 0f) dasheando = false;
        }

        if (EsInvencible)
        {
            tInv -= dt;
            if (tInv <= 0f) EsInvencible = false;
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

        AnimationClip clipObj = null;

        if (dasheando)
        {
            clipObj = aDash;
        }
        else
        {
            if (Mathf.Abs(ultimaDir.x) > Mathf.Abs(ultimaDir.y))
            {
                clipObj = inputMov != Vector2.zero ? aRight : aIdleRight;
                sprite.flipX = ultimaDir.x < 0;
            }
            else
            {
                if (ultimaDir.y > 0)
                {
                    clipObj = inputMov != Vector2.zero ? aUp : aIdleUp;
                }
                else
                {
                    clipObj = inputMov != Vector2.zero ? aDown : aIdleDown;
                }
                sprite.flipX = false;
            }
        }

        if (clipObj && animActual != clipObj.name)
        {
            anim.Play(clipObj.name);
            animActual = clipObj.name;
        }
    }
}
