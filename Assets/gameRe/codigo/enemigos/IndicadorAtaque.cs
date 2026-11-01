using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
public class IndicadorAtaque : MonoBehaviour
{
    [Header("Configuración del Brillo")]
    [SerializeField] private Color colorBrillo = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float intensidadLuz = 1.3f;
    [SerializeField, Min(0f)] private float radioLuz = 1.6f;

    private SpriteRenderer sr;
    private Light2D luzAviso;
    private Color colorBase;
    private Coroutine rutina;

    void Awake()
    {
        TryGetComponent(out sr);
        colorBase = sr.color;

        GameObject objLuz = new GameObject("LuzAvisoAtaque");
        objLuz.transform.SetParent(transform, false);
        luzAviso = objLuz.AddComponent<Light2D>();
        luzAviso.lightType = Light2D.LightType.Point;
        luzAviso.color = colorBrillo;
        luzAviso.pointLightOuterRadius = radioLuz;
        luzAviso.intensity = 0f;
        luzAviso.shadowsEnabled = false;
        luzAviso.enabled = false;
    }

    public void MostrarAviso(float duracion)
    {
        if (!gameObject.activeInHierarchy) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaBrillo(duracion));
    }

    public void CancelarAviso()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }
        RestaurarVisuales();
    }

    private IEnumerator RutinaBrillo(float duracion)
    {
        colorBase = sr.color;
        luzAviso.color = colorBrillo;
        luzAviso.enabled = true;

        float t = 0f;
        float d = Mathf.Max(0.05f, duracion);

        while (t < d)
        {
            t += Time.deltaTime;
            float pulso = Mathf.PingPong(t * (4f / d), 1f);
            Color c = Color.Lerp(colorBase, colorBrillo, 0.65f + pulso * 0.35f);
            c.a = sr.color.a;
            sr.color = c;
            luzAviso.intensity = Mathf.Lerp(0.4f, intensidadLuz, pulso);
            yield return null;
        }

        RestaurarVisuales();
        rutina = null;
    }

    private void RestaurarVisuales()
    {
        if (sr != null)
        {
            Color c = colorBase;
            c.a = sr.color.a;
            sr.color = c;
        }
        if (luzAviso != null)
        {
            luzAviso.intensity = 0f;
            luzAviso.enabled = false;
        }
    }

    void OnDisable()
    {
        CancelarAviso();
    }
}
