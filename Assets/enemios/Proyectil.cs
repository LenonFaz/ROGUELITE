using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Proyectil : MonoBehaviour
{
    public float velocidad = 14f;
    public float tiempoVida = 3f;
    public int danio = 1;

    private Rigidbody2D rb;
    private Vector2 direccion = Vector2.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Destroy(gameObject, tiempoVida);
    }

    public void Apuntar(Vector2 dir)
    {
        direccion = dir.normalized;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direccion * velocidad;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Destroy(gameObject);
    }
}