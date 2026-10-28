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
    public float PorcentajeLuz => LuzActual / luzMaxima;
    public bool PlegariaActiva { get; private set; } = false;

    private bool juegoTerminado = false;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (juegoTerminado || !PlegariaActiva) return;

        LuzActual += aumentoPorSegundo * Time.deltaTime;
        LuzActual = Mathf.Clamp(LuzActual, 0f, luzMaxima);

        if (LuzActual >= luzMaxima)
        {
            EjecutarVaporizacion();
        }
    }

    public void EstablecerPlegariaActiva(bool activa) => PlegariaActiva = activa;
    public void ReiniciarLuz() => LuzActual = 0f;

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
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        if (jugador != null && jugador.TryGetComponent(out Vida vidaJugador))
        {
            vidaJugador.RecibirDanio(9999);
        }
    }
}
