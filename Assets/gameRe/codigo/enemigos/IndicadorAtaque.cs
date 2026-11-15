using System.Collections;
using UnityEngine;

public class IndicadorAtaque : MonoBehaviour
{
    [Header("Configuración del Aviso")]
    [SerializeField] private Color colorAviso = new Color(1f, 0.9f, 0.2f, 0.9f);
    [SerializeField, Min(1f)] private float velocidadParpadeo = 25f;
    [SerializeField, Min(1f)] private float escalaContorno = 1.18f;

    private SpriteRenderer srPrincipal;
    private SpriteRenderer srContorno;
    private Coroutine rutina;
    private static Material materialBlancoCompartido;

    void Awake()
    {
        srPrincipal = GetComponentInChildren<SpriteRenderer>();
        CrearContorno();
    }

    private void CrearContorno()
    {
        if (srPrincipal == null) return;

        if (materialBlancoCompartido == null)
        {
            Shader s = Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default");
            if (s != null) materialBlancoCompartido = new Material(s);
        }

        GameObject objContorno = new GameObject("ContornoAviso");
        objContorno.transform.SetParent(srPrincipal.transform, false);
        objContorno.transform.localPosition = Vector3.zero;
        objContorno.transform.localScale = Vector3.one * escalaContorno;

        srContorno = objContorno.AddComponent<SpriteRenderer>();
        if (materialBlancoCompartido != null) srContorno.sharedMaterial = materialBlancoCompartido;
        srContorno.sortingLayerID = srPrincipal.sortingLayerID;
        srContorno.sortingOrder = srPrincipal.sortingOrder - 1;
        srContorno.enabled = false;
    }

    public void ConfigurarColorAviso(Color nuevoColor)
    {
        colorAviso = nuevoColor;
    }

    public void MostrarAviso(float duracion)
    {
        MostrarAviso(duracion, colorAviso);
    }

    public void MostrarAviso(float duracion, Color colorPersonalizado)
    {
        if (!gameObject.activeInHierarchy || duracion <= 0f) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaParpadeo(duracion, colorPersonalizado));
    }

    public void CancelarAviso()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }
        if (srContorno != null) srContorno.enabled = false;
    }

    private IEnumerator RutinaParpadeo(float duracion, Color colorBaseAviso)
    {
        if (srContorno == null) CrearContorno();
        if (srContorno == null) yield break;

        srContorno.enabled = true;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            if (srPrincipal != null)
            {
                srContorno.sprite = srPrincipal.sprite;
                srContorno.flipX = srPrincipal.flipX;
                srContorno.flipY = srPrincipal.flipY;
                srContorno.transform.localScale = Vector3.one * escalaContorno;
            }

            float onda = (Mathf.Sin(t * velocidadParpadeo) + 1f) * 0.5f;
            Color c = colorBaseAviso;
            c.a = Mathf.Lerp(0.15f, colorBaseAviso.a, onda);
            srContorno.color = c;

            yield return null;
        }

        srContorno.enabled = false;
        rutina = null;
    }

    void OnDisable()
    {
        CancelarAviso();
    }
}
