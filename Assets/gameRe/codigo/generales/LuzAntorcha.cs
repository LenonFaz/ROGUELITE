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
        if (GestorLuz.Instancia == null) return;

        float nivelPlegaria = GestorLuz.Instancia.PorcentajeLuz;
        luz.pointLightOuterRadius = Mathf.Lerp(radioMinimo, radioMaximo, nivelPlegaria);
        luz.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, nivelPlegaria);
    }
}
