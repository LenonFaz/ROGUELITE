using UnityEngine;
using UnityEngine.UI;

public class GestorLuz : MonoBehaviour
{
    public static GestorLuz Instancia { get; private set; }

    [Header("Configuración Principal")]
    [SerializeField, Min(0.01f)] private float luzMaxima = 100f;
    [SerializeField, Min(0f)] private float aumentoPorSegundo = 1.5f;

    [Header("Modificadores de Plegaria")]
    [SerializeField, Min(0f)] private float aumentoPorDanio = 20f;
    [SerializeField, Min(0f)] private float reduccionPorGolpe = 1f;
    [SerializeField, Min(0f)] private float reduccionPorMuerte = 8f;

    [Header("Interfaz Visual")]
    [SerializeField] private Image barraLuzUI;

    public float LuzActual { get; private set; }
    public float PorcentajeLuz => luzMaxima > 0f ? LuzActual / luzMaxima : 0f;
    public bool PlegariaActiva { get; private set; }

    private bool juegoTerminado;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ReiniciarLuz();
    }

    void Update()
    {
        if (juegoTerminado || !PlegariaActiva) return;

        LuzActual = Mathf.Clamp(LuzActual + aumentoPorSegundo * Time.deltaTime, 0f, luzMaxima);
        ActualizarUI();

        if (LuzActual >= luzMaxima)
        {
            EjecutarVaporizacion();
        }
    }

    public void EstablecerPlegariaActiva(bool activa) => PlegariaActiva = activa;

    public void ReiniciarLuz()
    {
        juegoTerminado = false;
        LuzActual = 0f;
        ActualizarUI();
    }

    public void RegistrarGolpeAEnemigo() => ModificarLuz(-reduccionPorGolpe);
    public void RegistrarMuerteDeEnemigo() => ModificarLuz(-reduccionPorMuerte);
    public void RegistrarDanioAlJugador() => ModificarLuz(aumentoPorDanio);

    private void ModificarLuz(float cantidad)
    {
        if (juegoTerminado) return;
        LuzActual = Mathf.Clamp(LuzActual + cantidad, 0f, luzMaxima);
        ActualizarUI();

        if (LuzActual >= luzMaxima)
        {
            EjecutarVaporizacion();
        }
    }

    private void ActualizarUI()
    {
        if (barraLuzUI != null)
        {
            barraLuzUI.fillAmount = PorcentajeLuz;
        }
    }

    private void EjecutarVaporizacion()
    {
        juegoTerminado = true;
        GameObject jugador = Jugador.Instancia != null ? Jugador.Instancia.gameObject : GameObject.FindGameObjectWithTag("Player");

        if (jugador != null && jugador.TryGetComponent(out Vida vidaJugador))
        {
            vidaJugador.ForzarMuerte();
        }
    }
}
