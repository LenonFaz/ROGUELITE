using UnityEngine;

public class GameOver : MonoBehaviour
{
    [SerializeField] private string escenaInicial = "PISO 1";
    [SerializeField] private string escenaMenu = "MenuPrincipal";

    public void Retry()
    {
        Time.timeScale = 1f;

        string destino = DatosCheckpoint.hayCheckpoint
            ? DatosCheckpoint.escenaActual
            : escenaInicial;

        FadeManager.Instance.CargarEscena(destino);
    }

    public void Salir()
    {
        Time.timeScale = 1f;
        DatosCheckpoint.Limpiar();
        FadeManager.Instance.CargarEscena(escenaMenu);
    }
}