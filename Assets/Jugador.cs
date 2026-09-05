using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6f;
    public float aceleracion = 45f;
    public float desaceleracion = 60f;

    [Header("Dash")]
    public float velocidadDash = 22f;
    public float duracionDash = 0.15f;
    public float cooldownDash = 0.6f;
    public float duracionInvencibilidad = 0.2f;

    private Rigidbody2D rb;
    private Empuje empuje;

    private Vector2 inputMovimiento;
    private Vector2 ultimaDireccion = Vector2.down;

    private bool dasheando = false;
    private bool puedeDashear = true;
    private float timerDash = 0f;
    private float timerCooldownDash = 0f;

    public bool EsInvencible { get; private set; }
    private float timerInvencibilidad = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        empuje = GetComponent<Empuje>();
    }

    void Update()
    {
        LeerInput();
        LeerDash();
        ActualizarTimers(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (empuje != null && empuje.EstaEmpujado) return;

        if (dasheando)
        {
            rb.linearVelocity = ultimaDireccion * velocidadDash;
            return;
        }

        Vector2 objetivo = inputMovimiento * velocidad;
        float tasa = inputMovimiento.sqrMagnitude > 0.01f ? aceleracion : desaceleracion;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
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

        inputMovimiento = new Vector2(x, y).normalized;
        if (inputMovimiento.sqrMagnitude > 0.01f)
            ultimaDireccion = inputMovimiento;
    }

    void LeerDash()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame && puedeDashear && !dasheando)
            Dashear();
    }

    void Dashear()
    {
        dasheando = true;
        puedeDashear = false;
        timerDash = duracionDash;
        timerCooldownDash = cooldownDash;

        EsInvencible = true;
        timerInvencibilidad = duracionInvencibilidad;
    }

    void ActualizarTimers(float dt)
    {
        if (dasheando)
        {
            timerDash -= dt;
            if (timerDash <= 0f) dasheando = false;
        }

        if (EsInvencible)
        {
            timerInvencibilidad -= dt;
            if (timerInvencibilidad <= 0f) EsInvencible = false;
        }

        if (!puedeDashear)
        {
            timerCooldownDash -= dt;
            if (timerCooldownDash <= 0f) puedeDashear = true;
        }
    }
}