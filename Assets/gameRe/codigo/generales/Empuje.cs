using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Empuje : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duracionEmpuje = 0.15f;
    [SerializeField, Min(0f)] private float desaceleracion = 35f;
    [SerializeField] private bool inmuneAEmpuje;

    private Rigidbody2D rb;
    private Vida vida;
    private float timerEmpuje;

    public bool EstaEmpujado => timerEmpuje > 0f;
    public bool InmuneAEmpuje { get => inmuneAEmpuje; set => inmuneAEmpuje = value; }

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out vida);
    }

    public void AplicarEmpuje(Vector2 direccion, float fuerza)
    {
        if (inmuneAEmpuje || fuerza <= 0f) return;
        if (vida != null && vida.EsInvulnerable) return;

        timerEmpuje = duracionEmpuje;
        rb.linearVelocity = direccion.normalized * fuerza;
    }

    public void CancelarEmpuje()
    {
        timerEmpuje = 0f;
    }

    void FixedUpdate()
    {
        if (timerEmpuje <= 0f) return;

        timerEmpuje -= Time.fixedDeltaTime;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, desaceleracion * Time.fixedDeltaTime);
    }
}
