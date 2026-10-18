using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ImagenResidual : MonoBehaviour
{
    private SpriteRenderer sr;
    private float tiempoVidaMax;
    private float tiempoVida;
    private Color colorInicial;

    public void Configurar(float duracion, Color color)
    {
        TryGetComponent(out sr);
        tiempoVidaMax = duracion;
        tiempoVida = duracion;
        colorInicial = color;
        sr.color = colorInicial;
    }

    void Update()
    {
        if (tiempoVida > 0f)
        {
            tiempoVida -= Time.deltaTime;

            float ratio = tiempoVida / tiempoVidaMax;
            Color c = colorInicial;
            c.a = Mathf.Lerp(0f, colorInicial.a, ratio);
            sr.color = c;

            if (tiempoVida <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
