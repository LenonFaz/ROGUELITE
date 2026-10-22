using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField, Min(0f)] private float vel = 6f;
    private float velOriginal;

    public void CambiarVelocidad(float nuevaVelocidad)
    {
        vel = nuevaVelocidad;
    }
    [SerializeField, Min(0f)] private float acel = 45f;
    [SerializeField, Min(0f)] private float desacel = 60f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float velDash = 22f;
    [SerializeField, Min(0f)] private float durDash = 0.15f;
    [SerializeField, Min(0f)] private float cdDash = 0.6f;
    [SerializeField, Min(0f)] private float durInv = 0.2f;
    private Vida vida;

    [Header("Efecto de Imagen Residual (Sin Prefab)")]
    [Tooltip("Cuánto tiempo en total estará soltando sombras después de pulsar Dash")]
    [SerializeField, Min(0f)] private float duracionGeneracionResidual = 0.2f;
    [Tooltip("Cada cuánto tiempo suelta una sombra (más bajo = más juntas)")]
    [SerializeField, Min(0.01f)] private float intervaloResidual = 0.015f;
    [Tooltip("Cuánto tarda cada sombra individual en desaparecer")]
    [SerializeField, Min(0.1f)] private float duracionResidual = 0.4f;
    [SerializeField] private Color colorResidual = new Color(0f, 0.5f, 1f, 0.6f);

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
    private AtaqueJugador ataque;

    private Vector2 inputMov;
    private Vector2 ultimaDir = Vector2.down;

    private bool dasheando;
    private bool puedeDashear = true;
    private float tDash;
    private float tCdDash;

    private float tGeneracionResidual;
    private float tSiguienteResidual;

    public bool EsInvencible { get; private set; }
    private float tInv;

    private string animActual;

    void Awake()
    {
        velOriginal = vel;
        TryGetComponent(out vida);
        TryGetComponent(out rb);
        rb.gravityScale = 0f;

        TryGetComponent(out empuje);
        TryGetComponent(out ataque);

        if (!anim) TryGetComponent(out anim);
        if (!sprite) TryGetComponent(out sprite);
    }

    public void RestaurarVelocidad()
    {
        vel = velOriginal;
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
            EsInvencible = true; if (vida != null) vida.ActivarInvulnerabilidad(durInv);
            tInv = durInv;

            tGeneracionResidual = duracionGeneracionResidual;
            tSiguienteResidual = 0f;
        }
    }

    void Update()
    {
        ActualizarTimers(Time.deltaTime);
        ActualizarAnim();

        if (tGeneracionResidual > 0f)
        {
            ManejarImagenesResiduales(Time.deltaTime);
        }
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

        if (tGeneracionResidual > 0f)
        {
            tGeneracionResidual -= dt;
        }
    }

    void ManejarImagenesResiduales(float dt)
    {
        if (!sprite) return;

        tSiguienteResidual -= dt;

        if (tSiguienteResidual <= 0f)
        {
            tSiguienteResidual = intervaloResidual;

            GameObject go = new GameObject("SombraDash");
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.localScale;

            SpriteRenderer srObj = go.AddComponent<SpriteRenderer>();
            srObj.sprite = sprite.sprite;
            srObj.flipX = sprite.flipX;
            srObj.material = sprite.material;
            srObj.sortingLayerID = sprite.sortingLayerID;
            srObj.sortingOrder = sprite.sortingOrder - 1;

            ImagenResidual residual = go.AddComponent<ImagenResidual>();
            residual.Configurar(duracionResidual, colorResidual);
        }
    }

    void ActualizarAnim()
    {
        if (!anim || !sprite) return;

        if (ataque != null && ataque.EstaAtacando)
        {
            animActual = "";
            return;
        }

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
