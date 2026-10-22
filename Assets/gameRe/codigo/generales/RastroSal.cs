using UnityEngine;

public class RastroSal : MonoBehaviour
{
    public float velocidadLenta = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Jugador jugador = other.GetComponent<Jugador>();

            if (jugador != null)
            {
                jugador.CambiarVelocidad(velocidadLenta);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Jugador jugador = other.GetComponent<Jugador>();

            if (jugador != null)
            {
                jugador.RestaurarVelocidad();
            }
        }
    }
}
