using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ImpactoJugador : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Color colorOriginal;
    Coroutine corrutina;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        colorOriginal = spriteRenderer.color;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EjecutarImpacto();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EjecutarImpacto();
        }
    }

    public void EjecutarImpacto()
    {
        if (corrutina != null) StopCoroutine(corrutina);
        corrutina = StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        spriteRenderer.color = Color.black;
        yield return new WaitForSeconds(0.05f);

        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.05f);

        spriteRenderer.color = Color.black;
        yield return new WaitForSeconds(0.05f);

        spriteRenderer.color = colorOriginal;
        corrutina = null;
    }
}