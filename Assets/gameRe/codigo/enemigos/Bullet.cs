using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private int danio;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Configuramos el Rigidbody por código para evitar errores en el Inspector
        rb.isKinematic = true;
        rb.gravityScale = 0f;
    }

    // Esta es la función modular. Cualquier enemigo puede llamarla con parámetros diferentes.
    public void Init(Vector2 direccion, float velocidad, int danioBala, float tiempoDeVida = 5f)
    {
        this.danio = danioBala;

        // Le damos el impulso inicial. Al ser isKinematic, mantendrá esta velocidad constante.
        rb.linearVelocity = direccion.normalized * velocidad;

        // Rotamos la bala para que apunte visualmente hacia donde viaja (útil si tu sprite no es redondo)
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        Destroy(gameObject, tiempoDeVida);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (col.TryGetComponent(out Vida vidaJugador))
            {
                // Si está en dash, la bala es ignorada y sigue de largo
                if (vidaJugador.EsInvulnerable) return;

                vidaJugador.RecibirDanio(danio);
            }
            Destroy(gameObject);
        }
        else if (col.CompareTag("Pared"))
        {
            Destroy(gameObject);
        }
    }
}
