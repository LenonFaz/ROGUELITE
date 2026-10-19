using System.Collections;
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

    // Propiedad pública para saber si actualmente es intocable (por el Dash)
    public bool EsInvulnerable { get; private set; }

    void Awake()
    {
        vidaActual = vidaMaxima;
        EsInvulnerable = false;
    }

    public void RecibirDanio(int cantidad)
    {
        // Si el objeto ya está muerto o es temporalmente invulnerable por el Dash, ignoramos el golpe
        if (vidaActual <= 0 || EsInvulnerable) return;

        vidaActual -= cantidad;

        AlRecibirDanio?.Invoke();

        // --- INTEGRACIÓN CON EL GESTOR DE LUZ ---
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

    // --- NUEVO: FUNCIONES PARA EL DASH (INVULNERABILIDAD) ---
    public void ActivarInvulnerabilidad(float duracion)
    {
        // Iniciamos el contador de tiempo en segundo plano
        StartCoroutine(RutinaInvulnerabilidad(duracion));
    }

    private IEnumerator RutinaInvulnerabilidad(float duracion)
    {
        EsInvulnerable = true; // Se vuelve intocable

        // Espera los segundos exactos que dura tu Dash
        yield return new WaitForSeconds(duracion);

        EsInvulnerable = false; // Vuelve a ser vulnerable
    }

    private void Morir()
    {
        AlMorir?.Invoke();

        // Si es un enemigo el que muere, le avisa al Gestor para bajar la barra de luz
        if (!esJugador && GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.RegistrarMuerteDeEnemigo();
        }

        // Finalmente, destruimos el GameObject
        Destroy(gameObject);
    }
}
