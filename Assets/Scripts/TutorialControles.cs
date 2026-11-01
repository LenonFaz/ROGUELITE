using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialControles : MonoBehaviour
{
    public GameObject tutorialDash;

    bool usoEspacio = false;

    void Start()
    {
        if (tutorialDash != null) tutorialDash.SetActive(false);
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (tutorialDash != null && tutorialDash.activeSelf)
        {
            if (teclado.spaceKey.wasPressedThisFrame) usoEspacio = true;

            if (usoEspacio) tutorialDash.SetActive(false);
        }
    }
}