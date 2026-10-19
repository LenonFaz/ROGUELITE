using UnityEngine;
using UnityEngine.Rendering.Universal; // Necesario para controlar Light2D por código

[RequireComponent(typeof(Light2D))]
public class LuzJugador : MonoBehaviour
{
    [Header("Efecto de Respiración")]
    [SerializeField, Min(0f)] private float radioBase = 2.5f;
    [Tooltip("Cuánto crece y se achica la luz (ej: 0.5 sumará y restará medio metro)")]
    [SerializeField, Min(0f)] private float variacion = 0.4f;
    [Tooltip("Qué tan rápido respira la luz")]
    [SerializeField, Min(0f)] private float velocidadRespiracion = 3f;

    private Light2D luz;

    void Awake()
    {
        TryGetComponent(out luz);
    }

    void Update()
    {
        // Mathf.Sin crea una onda perfecta que va de -1 a 1 con el tiempo
        float oscilacion = Mathf.Sin(Time.time * velocidadRespiracion) * variacion;
        luz.pointLightOuterRadius = radioBase + oscilacion;
    }
}
