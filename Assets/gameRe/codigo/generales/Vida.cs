using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class Vida : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField, Min(1)] private int vidaMaxima = 5;
    [SerializeField] private bool esJugador;
    [SerializeField, Min(0f)] private float tiempoInvulnerabilidadDanio = 0.6f;
    [SerializeField, Min(0f)] private float retrasoDestruccionAlMorir;

    [Header("Eventos")]
    public UnityEvent AlRecibirDanio;
    public UnityEvent AlMorir;

    private int vidaActual;
    private ImpactoJugador impactoVisual;
    private Coroutine rutinaInvulnerable;

    public bool EsInvulnerable { get; private set; }
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;
    public float PorcentajeVida => vidaMaxima > 0 ? Mathf.Clamp01((float)vidaActual / vidaMaxima) : 0f;

    void Awake()
    {
        vidaActual = vidaMaxima;
        EsInvulnerable = false;
        TryGetComponent(out impactoVisual);

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

    public void EstablecerRetrasoDestruccion(float segundos)
    {
        retrasoDestruccionAlMorir = Mathf.Max(0f, segundos);
    }

    public void RecibirDanio(int cantidad)
    {
        if (vidaActual <= 0 || EsInvulnerable) return;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        AlRecibirDanio?.Invoke();

        if (GestorLuz.Instancia != null)
        {
            if (esJugador) GestorLuz.Instancia.RegistrarDanioAlJugador();
            else GestorLuz.Instancia.RegistrarGolpeAEnemigo();
        }

        if (vidaActual <= 0)
        {
            Morir();
            return;
        }

        if (esJugador && tiempoInvulnerabilidadDanio > 0f)
        {
            ActivarInvulnerabilidad(tiempoInvulnerabilidadDanio);
            if (impactoVisual != null) impactoVisual.EjecutarImpacto(tiempoInvulnerabilidadDanio);
        }
        else if (impactoVisual != null)
        {
            impactoVisual.EjecutarImpacto();
        }
    }

    public void ForzarMuerte()
    {
        if (vidaActual <= 0) return;
        EsInvulnerable = false;
        vidaActual = 0;
        AlRecibirDanio?.Invoke();
        Morir();
    }

    public void Curar(int cantidad)
    {
        if (vidaActual <= 0) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0, vidaMaxima);
    }

    public void ActivarInvulnerabilidad(float duracion)
    {
        if (!gameObject.activeInHierarchy || duracion <= 0f) return;
        if (rutinaInvulnerable != null) StopCoroutine(rutinaInvulnerable);
        rutinaInvulnerable = StartCoroutine(RutinaInvulnerabilidad(duracion));
    }

    private IEnumerator RutinaInvulnerabilidad(float duracion)
    {
        EsInvulnerable = true;
        yield return new WaitForSeconds(duracion);
        EsInvulnerable = false;
        rutinaInvulnerable = null;
    }

    private void Morir()
    {
        AlMorir?.Invoke();

        if (!esJugador && GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.RegistrarMuerteDeEnemigo();
        }

        if (retrasoDestruccionAlMorir > 0f)
        {
            if (TryGetComponent(out Collider2D col)) col.enabled = false;
            if (TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
            Destroy(gameObject, retrasoDestruccionAlMorir);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDisable()
    {
        EsInvulnerable = false;
        rutinaInvulnerable = null;
    }
}
