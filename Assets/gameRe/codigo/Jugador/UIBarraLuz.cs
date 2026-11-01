using UnityEngine;
using UnityEngine.UI;

public class UIBarraLuz : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image imagenRelleno;

    [Header("Configuración visual")]
    [SerializeField, Min(0.1f)] private float velocidadSuavizado = 5f;

    void Update()
    {
        if (GestorLuz.Instancia == null || imagenRelleno == null) return;

        float porcentajeObjetivo = GestorLuz.Instancia.PorcentajeLuz;
        imagenRelleno.fillAmount = Mathf.Lerp(imagenRelleno.fillAmount, porcentajeObjetivo, velocidadSuavizado * Time.deltaTime);
    }
}
