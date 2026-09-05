using UnityEngine;

public class Vida : MonoBehaviour
{
    public int vidaMaxima = 2;
    private int vidaActual;

    void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanio(int cantidad)
    {
        vidaActual -= cantidad;
        if (vidaActual <= 0) Morir();
    }

    void Morir()
    {
        Destroy(gameObject);
    }
}