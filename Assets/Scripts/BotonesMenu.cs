using UnityEngine;
using UnityEngine.SceneManagement;

public class BotonesMenu : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("PISO 1");
    }

    public void Salir()
    {
        Application.Quit();
    }
}