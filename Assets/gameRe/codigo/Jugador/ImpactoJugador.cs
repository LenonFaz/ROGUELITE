using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ImpactoJugador : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private Coroutine corrutina;

    void Awake()
    {
        TryGetComponent(out spriteRenderer);
        colorOriginal = spriteRenderer.color;
    }

    public void EjecutarImpacto()
    {
        EjecutarImpacto(0.15f);
    }

    public void EjecutarImpacto(float duracion)
    {
        if (!gameObject.activeInHierarchy) return;
        if (corrutina != null) StopCoroutine(corrutina);
        corrutina = StartCoroutine(Secuencia(duracion));
    }

    private IEnumerator Secuencia(float duracion)
    {
        float tiempo = 0f;
        bool alternar = false;

        while (tiempo < duracion)
        {
            spriteRenderer.color = alternar ? new Color(1f, 1f, 1f, 0.35f) : Color.black;
            alternar = !alternar;
            yield return new WaitForSeconds(0.06f);
            tiempo += 0.06f;
        }

        spriteRenderer.color = colorOriginal;
        corrutina = null;
    }

    void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.color = colorOriginal;
        corrutina = null;
    }
}
