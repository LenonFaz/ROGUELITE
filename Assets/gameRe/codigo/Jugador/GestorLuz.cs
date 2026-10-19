using UnityEngine;

public class GestorLuz : MonoBehaviour
{
    public static GestorLuz Instancia { get; private set; }

    [Header("Configuración Principal")]
    [SerializeField, Min(0f)] private float luzMaxima = 100f;
    [SerializeField, Min(0f)] private float aumentoPorSegundo = 1.5f;

    [Header("Modificadores de Plegaria")]
    [SerializeField, Min(0f)] private float aumentoPorDanio = 20f;
    [SerializeField, Min(0f)] private float reduccionPorGolpe = 1f;
    [SerializeField, Min(0f)] private float reduccionPorMuerte = 8f;

    public float LuzActual { get; private set; }

    // Devuelve un valor de 0.0 a 1.0 para que la interfaz lo lea fácilmente
    public float PorcentajeLuz => LuzActual / luzMaxima;

    private bool juegoTerminado = false;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (juegoTerminado) return;

        // La luz de la sala sube constantemente con el tiempo
        LuzActual += aumentoPorSegundo * Time.deltaTime;
        LuzActual = Mathf.Clamp(LuzActual, 0f, luzMaxima);

        if (LuzActual >= luzMaxima)
        {
            EjecutarVaporizacion();
        }
    }

    public void RegistrarGolpeAEnemigo() => ModificarLuz(-reduccionPorGolpe);
    public void RegistrarMuerteDeEnemigo() => ModificarLuz(-reduccionPorMuerte);
    public void RegistrarDanioAlJugador() => ModificarLuz(aumentoPorDanio);

    private void ModificarLuz(float cantidad)
    {
        if (juegoTerminado) return;
        LuzActual = Mathf.Clamp(LuzActual + cantidad, 0f, luzMaxima);
    }

    private void EjecutarVaporizacion()
    {
        juegoTerminado = true;
        Debug.Log("La barra de plegaria se llenó. Vaporizando al jugador...");

        // Buscamos al jugador en la escena mediante su Tag
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null && jugador.TryGetComponent(out Vida vidaJugador))
        {
            // Le aplicamos daño masivo (9999) para saltar toda su armadura o vida extra
            // y obligar a que su propio script ejecute la función Morir()
            vidaJugador.RecibirDanio(9999);
        }
    }
}
