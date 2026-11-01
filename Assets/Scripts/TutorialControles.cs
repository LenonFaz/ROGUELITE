using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialControles : MonoBehaviour
{
    public GameObject tutorialMovimiento;
    public GameObject tutorialAtaque;
    public GameObject tutorialDash;

    bool usoW = false, usoA = false, usoS = false, usoD = false;
    bool usoArriba = false, usoAbajo = false, usoIzquierda = false, usoDerecha = false;
    bool usoEspacio = false;

    void Start()
    {
        if (tutorialMovimiento != null) tutorialMovimiento.SetActive(false);
        if (tutorialAtaque != null) tutorialAtaque.SetActive(false);
        if (tutorialDash != null) tutorialDash.SetActive(false);
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (tutorialMovimiento != null && tutorialMovimiento.activeSelf)
        {
            if (teclado.wKey.wasPressedThisFrame) usoW = true;
            if (teclado.aKey.wasPressedThisFrame) usoA = true;
            if (teclado.sKey.wasPressedThisFrame) usoS = true;
            if (teclado.dKey.wasPressedThisFrame) usoD = true;

            if (usoW && usoA && usoS && usoD)
            {
                tutorialMovimiento.SetActive(false);
            }
        }

        if (tutorialAtaque != null && tutorialAtaque.activeSelf)
        {
            if (teclado.upArrowKey.wasPressedThisFrame) usoArriba = true;
            if (teclado.downArrowKey.wasPressedThisFrame) usoAbajo = true;
            if (teclado.leftArrowKey.wasPressedThisFrame) usoIzquierda = true;
            if (teclado.rightArrowKey.wasPressedThisFrame) usoDerecha = true;

            if (usoArriba && usoAbajo && usoIzquierda && usoDerecha)
            {
                tutorialAtaque.SetActive(false);
            }
        }

        if (tutorialDash != null && tutorialDash.activeSelf)
        {
            if (teclado.spaceKey.wasPressedThisFrame)
            {
                usoEspacio = true;
            }

            if (usoEspacio)
            {
                tutorialDash.SetActive(false);
            }
        }
    }
}