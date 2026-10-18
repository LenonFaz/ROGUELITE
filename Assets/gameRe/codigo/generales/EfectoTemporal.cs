using UnityEngine;

public class EfectoTemporal : MonoBehaviour
{
    [Tooltip("Tiempo en segundos antes de destruirse (ajústalo a lo que dure tu animación)")]
    [SerializeField, Min(0f)] private float tiempoVida = 0.2f;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }
}
