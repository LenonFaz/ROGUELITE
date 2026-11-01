using UnityEngine;

public class EfectoTemporal : MonoBehaviour
{
    [SerializeField, Min(0f)] private float tiempoVida = 0.2f;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }
}
