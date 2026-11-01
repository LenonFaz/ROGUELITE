using UnityEngine;

public class RastroSal : MonoBehaviour
{
    public float velocidadLenta = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out Jugador jugador))
        {
            jugador.CambiarVelocidad(velocidadLenta);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent(out Jugador jugador))
        {
            jugador.RestaurarVelocidad();
        }
    }
}
