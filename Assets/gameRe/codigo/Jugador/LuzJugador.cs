using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LuzJugador : MonoBehaviour
{
    [Header("Efecto de Respiración")]
    [SerializeField, Min(0f)] private float radioBase = 2.5f;
    [SerializeField, Min(0f)] private float variacion = 0.4f;
    [SerializeField, Min(0f)] private float velocidadRespiracion = 3f;

    [Header("Sombras")]
    [SerializeField] private bool activarSombras = true;
    [SerializeField, Range(0f, 1f)] private float intensidadSombra = 0.85f;

    private Light2D luz;

    void Awake()
    {
        TryGetComponent(out luz);
        luz.shadowsEnabled = activarSombras;
        luz.shadowIntensity = intensidadSombra;
    }

    void Update()
    {
        float oscilacion = Mathf.Sin(Time.time * velocidadRespiracion) * variacion;
        luz.pointLightOuterRadius = radioBase + oscilacion;
    }
}
