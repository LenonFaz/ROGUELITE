using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BalaZigZag : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 dirBase;
    private Vector2 dirPerpendicular;
    private float velAvance;
    private float frecuencia;
    private float amplitud;
    private float tiempo;
    private bool activo;

    public void Configurar(Vector2 direccion, float velocidad, float frec, float amp)
    {
        TryGetComponent(out rb);
        dirBase = direccion.normalized;
        dirPerpendicular = new Vector2(-dirBase.y, dirBase.x);
        velAvance = velocidad;
        frecuencia = frec;
        amplitud = amp;
        activo = true;
    }

    void FixedUpdate()
    {
        if (!activo || rb == null) return;

        tiempo += Time.fixedDeltaTime;
        float velLateral = Mathf.Cos(tiempo * frecuencia) * amplitud;
        Vector2 velFinal = (dirBase * velAvance) + (dirPerpendicular * velLateral);
        rb.linearVelocity = velFinal;

        if (velFinal.sqrMagnitude > 0.01f)
        {
            float angulo = Mathf.Atan2(velFinal.y, velFinal.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angulo);
        }
    }
}
