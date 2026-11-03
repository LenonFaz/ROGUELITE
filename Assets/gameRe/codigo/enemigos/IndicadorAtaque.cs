using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class IndicadorAtaque : MonoBehaviour
{
    [Header("Configuración del Borde")]
    [SerializeField] private Color colorBorde = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(0.005f)] private float grosorBorde = 0.03125f;
    [SerializeField, Min(1f)] private float velocidadParpadeo = 14f;
    [SerializeField] private bool incluirDiagonales = false;

    private static Material materialSilueta;

    private SpriteRenderer srPrincipal;
    private GameObject contenedorBorde;
    private SpriteRenderer[] renderersBorde;
    private Coroutine rutina;
    private bool bordeActivo;

    private static readonly Vector2[] direcciones =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1f, 1f),
        new Vector2(-1f, 1f),
        new Vector2(1f, -1f),
        new Vector2(-1f, -1f)
    };

    void Awake()
    {
        TryGetComponent(out srPrincipal);
        InicializarMaterial();
        CrearRenderersBorde();
    }

    private void InicializarMaterial()
    {
        if (materialSilueta != null) return;

        Shader s = Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default");
        if (s != null)
        {
            materialSilueta = new Material(s);
        }
    }

    private void CrearRenderersBorde()
    {
        int cantidad = incluirDiagonales ? 8 : 4;
        renderersBorde = new SpriteRenderer[cantidad];

        contenedorBorde = new GameObject("ContornoAviso");
        contenedorBorde.transform.SetParent(srPrincipal.transform, false);
        contenedorBorde.transform.localPosition = Vector3.zero;
        contenedorBorde.transform.localRotation = Quaternion.identity;
        contenedorBorde.transform.localScale = Vector3.one;

        for (int i = 0; i < cantidad; i++)
        {
            GameObject hijo = new GameObject("Borde_" + i);
            hijo.transform.SetParent(contenedorBorde.transform, false);
            hijo.transform.localPosition = (Vector3)(direcciones[i] * grosorBorde);

            SpriteRenderer sr = hijo.AddComponent<SpriteRenderer>();
            if (materialSilueta != null) sr.sharedMaterial = materialSilueta;
            sr.color = colorBorde;
            sr.sortingLayerID = srPrincipal.sortingLayerID;
            sr.sortingOrder = srPrincipal.sortingOrder - 1;
            sr.enabled = false;

            renderersBorde[i] = sr;
        }

        contenedorBorde.SetActive(false);
    }

    void LateUpdate()
    {
        if (!bordeActivo || srPrincipal == null) return;

        Sprite spriteActual = srPrincipal.sprite;
        bool flipX = srPrincipal.flipX;
        bool flipY = srPrincipal.flipY;
        int layerID = srPrincipal.sortingLayerID;
        int order = srPrincipal.sortingOrder - 1;

        for (int i = 0; i < renderersBorde.Length; i++)
        {
            SpriteRenderer sr = renderersBorde[i];
            if (sr == null) continue;

            sr.transform.localPosition = (Vector3)(direcciones[i] * grosorBorde);
            sr.sprite = spriteActual;
            sr.flipX = flipX;
            sr.flipY = flipY;
            sr.sortingLayerID = layerID;
            sr.sortingOrder = order;
        }
    }

    public void MostrarAviso(float duracion)
    {
        if (!gameObject.activeInHierarchy) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaParpadeo(duracion));
    }

    public void CancelarAviso()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }
        ApagarBorde();
    }

    private IEnumerator RutinaParpadeo(float duracion)
    {
        bordeActivo = true;
        if (contenedorBorde != null) contenedorBorde.SetActive(true);

        for (int i = 0; i < renderersBorde.Length; i++)
        {
            if (renderersBorde[i] != null) renderersBorde[i].enabled = true;
        }

        float t = 0f;
        float d = Mathf.Max(0.05f, duracion);

        while (t < d)
        {
            t += Time.deltaTime;
            float onda = Mathf.Sin(t * velocidadParpadeo * Mathf.PI);
            Color c = colorBorde;
            c.a = onda >= 0f ? colorBorde.a : 0.15f;

            for (int i = 0; i < renderersBorde.Length; i++)
            {
                if (renderersBorde[i] != null) renderersBorde[i].color = c;
            }

            yield return null;
        }

        ApagarBorde();
        rutina = null;
    }

    private void ApagarBorde()
    {
        bordeActivo = false;
        if (renderersBorde != null)
        {
            for (int i = 0; i < renderersBorde.Length; i++)
            {
                if (renderersBorde[i] != null) renderersBorde[i].enabled = false;
            }
        }
        if (contenedorBorde != null)
        {
            contenedorBorde.SetActive(false);
        }
    }

    void OnDisable()
    {
        CancelarAviso();
    }
}
