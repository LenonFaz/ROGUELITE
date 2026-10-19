using UnityEngine;
using UnityEngine.Events;

public class Vida : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField, Min(1)] private int vidaMaxima = 5;
    [Tooltip("Marca esta casilla SOLO si este script está en el Jugador")]
    [SerializeField] private bool esJugador = false;

    [Header("Eventos (Opcional)")]
    public UnityEvent AlRecibirDanio;
    public UnityEvent AlMorir;

    private int vidaActual;

    void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanio(int cantidad)
    {
        // Si el objeto ya está muerto, ignoramos más daño
        if (vidaActual <= 0) return;

        vidaActual -= cantidad;

        AlRecibirDanio?.Invoke();

        // --- INTEGRACIÓN CON EL GESTOR DE LUZ ---
        // (Aquí estaba el error. Ahora llama correctamente a .Instancia)
        if (GestorLuz.Instancia != null)
        {
            if (esJugador)
            {
                GestorLuz.Instancia.RegistrarDanioAlJugador();
            }
            else
            {
                GestorLuz.Instancia.RegistrarGolpeAEnemigo();
            }
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public void Curar(int cantidad)
    {
        if (vidaActual <= 0) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0, vidaMaxima);
    }

    private void Morir()
    {
        AlMorir?.Invoke();

        // Si es un enemigo el que muere, bajamos gran parte de la barra de luz
        if (!esJugador && GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.RegistrarMuerteDeEnemigo();
        }

        // Finalmente, destruimos el GameObject
        Destroy(gameObject);
    }
}
