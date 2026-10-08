// Vida.cs (Actualizado)
using UnityEngine;

public class Vida : MonoBehaviour
{
    public int max = 2;
    public bool esEnemigo = true;

    int actual;

    void Awake() => actual = max;

    public void RecibirDanio(int danio)
    {
        actual -= danio;
        if (actual <= 0) Morir();
    }

    void Morir()
    {
        if (esEnemigo && GestorLuz.I != null) GestorLuz.I.OnKill();
        Destroy(gameObject);
    }
}
