using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Empuje : MonoBehaviour
{
    [Header("Empuje")]
    public float duracion = 0.2f;
    [Range(0.05f, 1f)] public float suavizado = 0.15f;
    [Range(0.1f, 3f)] public float resistencia = 1f;

    private Rigidbody2D rb;
    private float timer = 0f;
    private Vector2 velRef;

    public bool EstaEmpujado => timer > 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void AplicarEmpuje(Vector2 direccion, float fuerza)
    {
        timer = duracion;
        velRef = Vector2.zero;
        rb.linearVelocity = direccion.normalized * fuerza * resistencia;
    }

    void FixedUpdate()
    {
        if (timer <= 0f) return;

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, Vector2.zero, ref velRef, suavizado);
        timer -= Time.fixedDeltaTime;

        if (timer <= 0f || rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            timer = 0f;
        }
    }
}