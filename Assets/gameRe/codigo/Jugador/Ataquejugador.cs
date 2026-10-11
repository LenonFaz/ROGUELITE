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

    [Header("Empuje")]
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 8f;

    [Header("Debug")]
    [SerializeField] private bool mostrarGizmo = true;
    [SerializeField] private Color colorHitbox = Color.red;

    private float timerCooldown = 0f;
    private Vector2 ultimaDireccion = Vector2.down;
    private Vector2 ultimoCentro;

    // Variable para guardar el input de ataque
    private Vector2 inputAtaque;

    // --- INPUT SYSTEM ---
    void OnLook(InputValue value)
    {
        inputAtaque = value.Get<Vector2>();
    }

    void Update()
    {
        timerCooldown -= Time.deltaTime;

        if (inputAtaque != Vector2.zero && timerCooldown <= 0f)
        {
            Atacar(inputAtaque.normalized);
            timerCooldown = cooldown;
        }
    }

    void Atacar(Vector2 dir)
    {
        ultimaDireccion = dir;
        Vector2 centro = (Vector2)transform.position + dir * rango;
        ultimoCentro = centro;

        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Collider2D[] golpeados = Physics2D.OverlapBoxAll(centro, tamHitbox, angulo, capaEnemigos);

        foreach (var c in golpeados)
        {
            // Uso de TryGetComponent como recomendó tu profesor
            if (c.TryGetComponent(out Vida vida))
                vida.RecibirDanio(danio);

            if (c.TryGetComponent(out Empuje empuje))
                empuje.AplicarEmpuje(dir, fuerzaEmpuje);

            if (c.TryGetComponent(out Aturdido aturdido))
                aturdido.Aturdir();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo) return;
        Gizmos.color = colorHitbox;
        Vector2 centro = Application.isPlaying ? ultimoCentro : (Vector2)transform.position + ultimaDireccion * rango;
        Gizmos.matrix = Matrix4x4.TRS(centro, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamHitbox);
    }
}
