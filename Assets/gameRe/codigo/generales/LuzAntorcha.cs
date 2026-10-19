using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LuzAntorcha : MonoBehaviour
{
    [Header("Estado Inicial (0% de Plegaria)")]
    [SerializeField, Min(0f)] private float radioMinimo = 2f;
    [SerializeField, Min(0f)] private float intensidadMinima = 0.5f;

    [Header("Estado Peligroso (100% de Plegaria)")]
    [SerializeField, Min(0f)] private float radioMaximo = 8f;
    [SerializeField, Min(0f)] private float intensidadMaxima = 2.5f;

    private Light2D luz;

    void Awake()
    {
        TryGetComponent(out luz);
    }

    void Update()
    {
        // Si no hay gestor en la escena, no hace nada
        if (GestorLuz.Instancia == null) return;

        // Lee el porcentaje de 0.0 a 1.0
        float nivelPlegaria = GestorLuz.Instancia.PorcentajeLuz;

        // Lerp suaviza matemáticamente la transición entre el valor mínimo y el máximo
        luz.pointLightOuterRadius = Mathf.Lerp(radioMinimo, radioMaximo, nivelPlegaria);
        luz.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, nivelPlegaria);
    }
}
