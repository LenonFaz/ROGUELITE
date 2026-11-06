using UnityEngine;
using UnityEngine.InputSystem;
public class MenuPausa : MonoBehaviour
{
    public GameObject panelMenu;

    void Start()
    {
        panelMenu.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            bool abierto = !panelMenu.activeSelf;
            panelMenu.SetActive(abierto);
            Time.timeScale = abierto ? 0f : 1f;
        }
    }
}