using UnityEngine;
using UnityEngine.UI; // Necesario para la interfaz

public class UIBarraLuz : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí la imagen 'RellenoBarra' que tiene el Image Type en Filled")]
    [SerializeField] private Image imagenRelleno;

    [Header("Configuración visual")]
    [Tooltip("Velocidad con la que la barra visual alcanza el valor real (suavizado)")]
    [SerializeField, Min(0.1f)] private float velocidadSuavizado = 5f;

    void Update()
    {
        // Si el GestorLuz se destruye o falta la imagen, no hacemos nada
        if (GestorLuz.Instancia == null || imagenRelleno == null) return;

        // Obtenemos el porcentaje (de 0.0 a 1.0) desde el GestorLuz
        float porcentajeObjetivo = GestorLuz.Instancia.PorcentajeLuz;

        // Lerp mueve la barra visualmente hacia el objetivo de forma muy fluida
        imagenRelleno.fillAmount = Mathf.Lerp(imagenRelleno.fillAmount, porcentajeObjetivo, velocidadSuavizado * Time.deltaTime);
    }
}
