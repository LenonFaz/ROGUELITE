using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Jugador : MonoBehaviour
{
    public static Jugador Instancia { get; private set; }

    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 6f;
    [SerializeField, Min(0f)] private float acel = 45f;
    [SerializeField, Min(0f)] private float desacel = 60f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float velDash = 14.9f;
    [SerializeField, Min(0f)] private float durDash = 0.12f;
    [SerializeField, Min(0f)] private float cdDash = 0.6f;
    [SerializeField] private bool invulnerableEnDash = true;

    [Header("Efecto Dash (Imagen Residual)")]
    [SerializeField] private bool usarImagenResidual = true;
    [SerializeField, Min(0.01f)] private float intervaloResidual = 0.03f;
    [SerializeField, Min(0.05f)] private float duracionResidual = 0.2f;
    [SerializeField] private Color colorResidual = new Color(0.5f, 0.8f, 1f, 0.6f);

    [Header("Animaciones")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AnimationClip aIdleDown;
    [SerializeField] private AnimationClip aIdleUp;
    [SerializeField] private AnimationClip aIdleRight;
    [SerializeField] private AnimationClip aMoveDown;
    [SerializeField] private AnimationClip aMoveUp;
    [SerializeField] private AnimationClip aMoveRight;
    [SerializeField] private AnimationClip aDashDown;
    [SerializeField] private AnimationClip aDashUp;
    [SerializeField] private AnimationClip aDashRight;

    [Header("Referencias")]
    [SerializeField] private Vida vida;
    [SerializeField] private Empuje empuje;
    [SerializeField] private AtaqueJugador ataque;

    private Rigidbody2D rb;
    private Vector2 inputMov;
    private Vector2 ultimaDir = Vector2.down;

    private float velBase;
    private bool dasheando;
    private bool puedeDashear = true;
    private float tDash;
    private float tCdDash;
    private float tResidual;
    private string animActual;

    public bool EstaDasheando => dasheando;
    public bool EsInvulnerablePorDash => dasheando && invulnerableEnDash;
    public Vector2 UltimaDireccion => ultimaDir;

    void Awake()
    {
        Instancia = this;
        TryGetComponent(out rb);
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!vida) TryGetComponent(out vida);
        if (!empuje) TryGetComponent(out empuje);
        if (!ataque) TryGetComponent(out ataque);

        velBase = vel;
    }

    public void CambiarVelocidad(float nuevaVel)
    {
        vel = nuevaVel;
    }

    public void RestaurarVelocidad()
    {
        vel = velBase;
    }

    void OnMove(InputValue value)
    {
        inputMov = value.Get<Vector2>();
        if (inputMov.sqrMagnitude > 0.01f && !dasheando)
        {
            ultimaDir = inputMov.normalized;
        }
    }

    void OnDash(InputValue value)
    {
        if (value.isPressed && puedeDashear && !dasheando)
        {
            IniciarDash();
        }
    }

    private void IniciarDash()
    {
        dasheando = true;
        puedeDashear = false;
        tDash = durDash;
        tCdDash = cdDash;
        tResidual = 0f;

        if (inputMov.sqrMagnitude > 0.01f)
        {
            ultimaDir = inputMov.normalized;
        }

        if (invulnerableEnDash && vida != null)
        {
            vida.ActivarInvulnerabilidad(durDash);
        }
    }

    void Update()
    {
        ActualizarTimers(Time.deltaTime);
        ActualizarVolteoSprite();
        ActualizarAnimaciones();
    }

    void FixedUpdate()
    {
        if (empuje != null && empuje.EstaEmpujado && !dasheando) return;

        if (dasheando)
        {
            rb.linearVelocity = ultimaDir * velDash;
            return;
        }

        Vector2 obj = inputMov * vel;
        float tasa = inputMov.sqrMagnitude > 0.01f ? acel : desacel;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, obj, tasa * Time.fixedDeltaTime);
    }

    private void ActualizarTimers(float dt)
    {
        if (dasheando)
        {
            tDash -= dt;
            if (usarImagenResidual)
            {
                tResidual -= dt;
                if (tResidual <= 0f)
                {
                    CrearImagenResidual();
                    tResidual = intervaloResidual;
                }
            }

            if (tDash <= 0f)
            {
                dasheando = false;
            }
        }

        if (!puedeDashear)
        {
            tCdDash -= dt;
            if (tCdDash <= 0f)
            {
                puedeDashear = true;
            }
        }
    }

    private void CrearImagenResidual()
    {
        if (!sprite || !sprite.sprite) return;

        GameObject obj = new GameObject("Residual");
        obj.transform.SetPositionAndRotation(sprite.transform.position, sprite.transform.rotation);
        obj.transform.localScale = sprite.transform.lossyScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite.sprite;
        sr.flipX = sprite.flipX;
        sr.sortingLayerID = sprite.sortingLayerID;
        sr.sortingOrder = sprite.sortingOrder - 1;
        if (sprite.sharedMaterial != null) sr.sharedMaterial = sprite.sharedMaterial;

        ImagenResidual ir = obj.AddComponent<ImagenResidual>();
        ir.Configurar(duracionResidual, colorResidual);
    }

    private void ActualizarVolteoSprite()
    {
        if (!sprite || dasheando) return;
        if (ataque != null && ataque.EstaAtacando) return;

        if (Mathf.Abs(ultimaDir.x) > 0.1f)
        {
            sprite.flipX = ultimaDir.x < 0f;
        }
        else if (Mathf.Abs(ultimaDir.y) > 0.1f)
        {
            sprite.flipX = false;
        }
    }

    private void ActualizarAnimaciones()
    {
        if (!anim) return;
        if (ataque != null && ataque.EstaAtacando)
        {
            animActual = string.Empty;
            return;
        }

        bool moviendo = inputMov.sqrMagnitude > 0.01f;
        bool esHorizontal = Mathf.Abs(ultimaDir.x) >= Mathf.Abs(ultimaDir.y);

        AnimationClip clip = null;

        if (dasheando)
        {
            if (esHorizontal) clip = aDashRight != null ? aDashRight : aMoveRight;
            else clip = ultimaDir.y > 0f ? (aDashUp != null ? aDashUp : aMoveUp) : (aDashDown != null ? aDashDown : aMoveDown);
        }
        else if (moviendo)
        {
            if (esHorizontal) clip = aMoveRight;
            else clip = ultimaDir.y > 0f ? aMoveUp : aMoveDown;
        }
        else
        {
            if (esHorizontal) clip = aIdleRight;
            else clip = ultimaDir.y > 0f ? aIdleUp : aIdleDown;
        }

        if (clip != null && animActual != clip.name)
        {
            anim.Play(clip.name);
            animActual = clip.name;
        }
    }
}
