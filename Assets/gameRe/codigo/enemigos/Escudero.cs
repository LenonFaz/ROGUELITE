using UnityEngine;

public class Escudero : MonoBehaviour
{
    [Header("Configuración de Ataque")]
    [SerializeField] private int danioAtaque = 1;

    // Se ejecuta si le marcaste "Is Trigger" al Collider de su ataque (Recomendado)
    private void OnTriggerEnter2D(Collider2D col)
    {
        ProcesarImpacto(col.gameObject);
    }

    // Se ejecuta si dejaste el Collider sólido (sin marcar "Is Trigger")
    private void OnCollisionEnter2D(Collision2D col)
    {
        ProcesarImpacto(col.gameObject);
    }

    private void ProcesarImpacto(GameObject objetoGolpeado)
    {
        if (objetoGolpeado.CompareTag("Player"))
        {
            if (objetoGolpeado.TryGetComponent(out Vida vidaJugador))
            {
                // Si el jugador está haciendo dash, ignoramos el impacto por completo
                if (vidaJugador.EsInvulnerable) return;

                // Si no está dasheando, aplicamos daño (esto sube la luz automáticamente desde Vida.cs)
                vidaJugador.RecibirDanio(danioAtaque);
            }
        }
    }

    // Nota: Si ya tenías lógica de movimiento (NavMesh, rb.velocity, etc.),
    // puedes pegarla aquí abajo dentro de un Update() o FixedUpdate() sin problemas.
}
