using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LiberarCadenas : MonoBehaviour
{
    [Header("Bloqueo")]
    public Behaviour[] scriptsABloquear;

    [Header("Cadenas")]
    public GameObject cadenas;
    public Animator animCadenas;
    public string triggerRomper = "Romper";

    [Header("Fase 1: WASD")]
    public GameObject panelMovimiento;
    public Image[] iconosWASD;

    [Header("Fase 2: Flechas")]
    public GameObject panelAtaque;
    public Image[] iconosFlechas;

    [Header("Config")]
    public int rondasPorGrupo = 2;
    public float pausaEntreRondas = 0.4f;
    public Color colorPresionada = new Color(0.4f, 1f, 0.4f, 1f);

    Key[] teclasWASD = { Key.W, Key.S, Key.D, Key.A };
    Key[] teclasFlechas = { Key.UpArrow,Key.RightArrow , Key.LeftArrow, Key.DownArrow };

    bool[] presionadasWASD, presionadasFlechas;
    Color[] coloresOriginalesWASD, coloresOriginalesFlechas;
    int rondaWASD = 0, rondaFlechas = 0;
    int fase = 0;
    bool esperando = false;

    void Start()
    {
        presionadasWASD = new bool[teclasWASD.Length];
        presionadasFlechas = new bool[teclasFlechas.Length];

        coloresOriginalesWASD = GuardarColores(iconosWASD);
        coloresOriginalesFlechas = GuardarColores(iconosFlechas);

        SetBloqueo(true);
        ResetIconos(iconosWASD, coloresOriginalesWASD);
        ResetIconos(iconosFlechas, coloresOriginalesFlechas);
        MostrarFase();
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || fase >= 2 || esperando) return;

        if (fase == 0)
        {
            if (RevisarTeclas(teclado, teclasWASD, presionadasWASD, iconosWASD))
                StartCoroutine(TerminarRonda());
        }
        else if (fase == 1)
        {
            if (RevisarTeclas(teclado, teclasFlechas, presionadasFlechas, iconosFlechas))
                StartCoroutine(TerminarRonda());
        }
    }

    bool RevisarTeclas(Keyboard teclado, Key[] teclas, bool[] presionadas, Image[] iconos)
    {
        bool todas = true;

        for (int i = 0; i < teclas.Length; i++)
        {
            if (teclado[teclas[i]].wasPressedThisFrame && !presionadas[i])
            {
                presionadas[i] = true;
                if (iconos != null && i < iconos.Length && iconos[i] != null)
                    iconos[i].color = colorPresionada;
            }

            if (!presionadas[i]) todas = false;
        }
        return todas;
    }

    IEnumerator TerminarRonda()
    {
        esperando = true;
        yield return new WaitForSeconds(pausaEntreRondas);

        if (fase == 0)
        {
            rondaWASD++;
            ResetPresionadas(presionadasWASD);
            ResetIconos(iconosWASD, coloresOriginalesWASD);

            if (rondaWASD >= rondasPorGrupo)
            {
                fase = 1;
                MostrarFase();
            }
        }
        else if (fase == 1)
        {
            rondaFlechas++;
            ResetPresionadas(presionadasFlechas);
            ResetIconos(iconosFlechas, coloresOriginalesFlechas);

            if (rondaFlechas >= rondasPorGrupo)
            {
                fase = 2;
                Liberar();
            }
        }

        esperando = false;
    }

    Color[] GuardarColores(Image[] iconos)
    {
        if (iconos == null) return new Color[0];

        Color[] colores = new Color[iconos.Length];
        for (int i = 0; i < iconos.Length; i++)
            colores[i] = iconos[i] != null ? iconos[i].color : Color.white;
        return colores;
    }

    void ResetPresionadas(bool[] arr)
    {
        for (int i = 0; i < arr.Length; i++) arr[i] = false;
    }

    void ResetIconos(Image[] iconos, Color[] originales)
    {
        if (iconos == null) return;

        for (int i = 0; i < iconos.Length; i++)
        {
            if (iconos[i] == null) continue;
            Color c = originales[i];
            c.a = 1f;
            iconos[i].color = c;
        }
    }
    void MostrarFase()
    {
        if (panelMovimiento != null) panelMovimiento.SetActive(fase == 0);
        if (panelAtaque != null) panelAtaque.SetActive(fase == 1);
    }

    void Liberar()
    {
        MostrarFase();
        if (animCadenas != null) animCadenas.SetTrigger(triggerRomper);
        if (cadenas != null) cadenas.SetActive(false);
        SetBloqueo(false);
    }

    void SetBloqueo(bool bloquear)
    {
        foreach (var s in scriptsABloquear)
            if (s != null) s.enabled = !bloquear;
    }
}