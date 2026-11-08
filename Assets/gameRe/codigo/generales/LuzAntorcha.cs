using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LuzAntorcha : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SpriteRenderer spriteAntorcha;

    [Header("Suavizado")]
    [SerializeField, Min(0.1f)] private float velocidadSuavizado = 4f;

    [Header("Estado Inicial (0% de Plegaria)")]
    [SerializeField, Min(0f)] private float radioMinimo = 2f;
    [SerializeField, Min(0f)] private float intensidadMinima = 0.5f;
    [SerializeField, Min(0f)] private float escalaSpriteMinima = 0.4f;

    [Header("Estado Peligroso (100% de Plegaria)")]
    [SerializeField, Min(0f)] private float radioMaximo = 8f;
    [SerializeField, Min(0f)] private float intensidadMaxima = 2.5f;
    [SerializeField, Min(0f)] private float escalaSpriteMaxima = 1.6f;

    private Light2D luz;
    private Transform transformSprite;
    private Vector3 escalaOriginal = Vector3.one;
    private float nivelSuavizado;

    void Awake()
    {
        TryGetComponent(out luz);
        if (!spriteAntorcha) spriteAntorcha = GetComponentInChildren<SpriteRenderer>();

        if (spriteAntorcha != null)
        {
            transformSprite = spriteAntorcha.transform;
            escalaOriginal = transformSprite.localScale;
        }
    }

    void Start()
    {
        nivelSuavizado = GestorLuz.Instancia != null ? GestorLuz.Instancia.PorcentajeLuz : 0f;
        AplicarValores(nivelSuavizado);
    }

    void Update()
    {
        if (GestorLuz.Instancia == null) return;

        float nivelObjetivo = GestorLuz.Instancia.PorcentajeLuz;
        nivelSuavizado = Mathf.Lerp(nivelSuavizado, nivelObjetivo, velocidadSuavizado * Time.deltaTime);

        AplicarValores(nivelSuavizado);
    }

    private void AplicarValores(float t)
    {
        luz.pointLightOuterRadius = Mathf.Lerp(radioMinimo, radioMaximo, t);
        luz.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, t);

        if (transformSprite != null)
        {
            float factorEscala = Mathf.Lerp(escalaSpriteMinima, escalaSpriteMaxima, t);
            transformSprite.localScale = escalaOriginal * factorEscala;
        }
    }
}
