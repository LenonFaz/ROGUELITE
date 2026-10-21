using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Vida : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField, Min(1)] private int vidaMaxima = 5;
    [SerializeField] private bool esJugador = false;

    [Header("Eventos")]
    public UnityEvent AlRecibirDanio;
    public UnityEvent AlMorir;

    private int vidaActual;

    public bool EsInvulnerable { get; private set; }

    void Awake()
    {
        vidaActual = vidaMaxima;
        EsInvulnerable = false;

        if (!esJugador)
        {
            if (!TryGetComponent(out ShadowCaster2D sombra))
            {
                sombra = gameObject.AddComponent<ShadowCaster2D>();
            }
            sombra.castsShadows = true;
            sombra.selfShadows = false;
        }
    }

    public void RecibirDanio(int cantidad)
    {
        if (vidaActual <= 0 || EsInvulnerable) return;

        vidaActual -= cantidad;

        AlRecibirDanio?.Invoke();

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

    public void ActivarInvulnerabilidad(float duracion)
    {
        StartCoroutine(RutinaInvulnerabilidad(duracion));
    }

    private IEnumerator RutinaInvulnerabilidad(float duracion)
    {
        EsInvulnerable = true;
        yield return new WaitForSeconds(duracion);
        EsInvulnerable = false;
    }

    private void Morir()
    {
        AlMorir?.Invoke();

        if (!esJugador && GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.RegistrarMuerteDeEnemigo();
        }

        Destroy(gameObject);
    }
}
