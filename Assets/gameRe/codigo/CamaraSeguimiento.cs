using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(200)]
public class CamaraSeguimiento : MonoBehaviour
{
    public static CamaraSeguimiento Instancia { get; private set; }

    [Header("Objetivo y Seguimiento")]
    [SerializeField] private Transform objetivo;
    [SerializeField, Min(0.01f)] private float tiempoSuavizadoPosicion = 0.15f;
    [SerializeField] private Vector2 offset;

    [Header("Límites de Cámara (Polygon / Collider de Sala)")]
    [SerializeField] private Collider2D limitesActuales;
    [SerializeField, Min(0f)] private float margenBorde;

    [Header("Zoom Dinámico por Sala")]
    [SerializeField] private bool ajustarZoomASala = true;
    [SerializeField, Min(1f)] private float zoomPorDefecto = 5f;
    [SerializeField, Min(1f)] private float zoomMinimo = 3.5f;
    [SerializeField, Min(1f)] private float zoomMaximo = 12f;
    [SerializeField, Min(0.01f)] private float tiempoSuavizadoZoom = 0.25f;

    private Camera cam;
    private Vector3 velocidadPosRef;
    private float velocidadZoomRef;
    private float zoomObjetivo;
    private float zInicial;
    private bool usarZoomEnSalaActual;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else
        {
            Destroy(this);
            return;
        }

        TryGetComponent(out cam);
        cam.orthographic = true;
        zInicial = transform.position.z;

        if (zoomPorDefecto <= 0f) zoomPorDefecto = cam.orthographicSize;
        zoomObjetivo = zoomPorDefecto;
        usarZoomEnSalaActual = ajustarZoomASala;
    }

    void Start()
    {
        if (!objetivo)
        {
            if (Jugador.Instancia != null) objetivo = Jugador.Instancia.transform;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p) objetivo = p.transform;
            }
        }

        if (objetivo)
        {
            Vector3 posInicial = CalcularPosicionConLimites((Vector2)objetivo.position + offset);
            transform.position = new Vector3(posInicial.x, posInicial.y, zInicial);
        }
    }

    void LateUpdate()
    {
        if (!objetivo)
        {
            if (Jugador.Instancia != null) objetivo = Jugador.Instancia.transform;
            else return;
        }

        ActualizarZoom();
        ActualizarPosicion();
    }

    private void ActualizarZoom()
    {
        if (limitesActuales != null && ajustarZoomASala && usarZoomEnSalaActual)
        {
            Bounds b = limitesActuales.bounds;
            float anchoSala = Mathf.Max(0.1f, b.size.x - margenBorde * 2f);
            float altoSala = Mathf.Max(0.1f, b.size.y - margenBorde * 2f);

            float zoomNecesarioAlto = altoSala * 0.5f;
            float zoomNecesarioAncho = (anchoSala * 0.5f) / Mathf.Max(0.1f, cam.aspect);

            float zoomParaEncuadrar = Mathf.Min(zoomNecesarioAlto, zoomNecesarioAncho);
            zoomObjetivo = Mathf.Clamp(zoomParaEncuadrar, zoomMinimo, zoomMaximo);
        }
        else
        {
            zoomObjetivo = zoomPorDefecto;
        }

        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoomObjetivo, ref velocidadZoomRef, tiempoSuavizadoZoom);
    }

    private void ActualizarPosicion()
    {
        Vector2 posDeseada = (Vector2)objetivo.position + offset;
        Vector3 posLimitada = CalcularPosicionConLimites(posDeseada);
        posLimitada.z = zInicial;

        transform.position = Vector3.SmoothDamp(transform.position, posLimitada, ref velocidadPosRef, tiempoSuavizadoPosicion);
    }

    private Vector3 CalcularPosicionConLimites(Vector2 posDeseada)
    {
        if (limitesActuales == null)
        {
            return new Vector3(posDeseada.x, posDeseada.y, zInicial);
        }

        Bounds b = limitesActuales.bounds;
        float altoCam = cam.orthographicSize + margenBorde;
        float anchoCam = cam.orthographicSize * cam.aspect + margenBorde;

        float minX = b.min.x + anchoCam;
        float maxX = b.max.x - anchoCam;
        float minY = b.min.y + altoCam;
        float maxY = b.max.y - altoCam;

        float x = minX <= maxX ? Mathf.Clamp(posDeseada.x, minX, maxX) : b.center.x;
        float y = minY <= maxY ? Mathf.Clamp(posDeseada.y, minY, maxY) : b.center.y;
        Vector2 centro = new Vector2(x, y);

        if (limitesActuales is PolygonCollider2D || limitesActuales is CompositeCollider2D)
        {
            Vector2 halfSize = new Vector2(
                Mathf.Min(anchoCam, b.extents.x * 0.9f),
                Mathf.Min(altoCam, b.extents.y * 0.9f)
            );

            Vector2[] offsetsEsquinas =
            {
                new Vector2(-halfSize.x, -halfSize.y),
                new Vector2(-halfSize.x,  halfSize.y),
                new Vector2( halfSize.x, -halfSize.y),
                new Vector2( halfSize.x,  halfSize.y)
            };

            for (int iter = 0; iter < 2; iter++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 esquina = centro + offsetsEsquinas[i];
                    if (!limitesActuales.OverlapPoint(esquina))
                    {
                        Vector2 puntoEnBorde = limitesActuales.ClosestPoint(esquina);
                        centro += (puntoEnBorde - esquina);
                    }
                }
            }

            if (!limitesActuales.OverlapPoint(centro))
            {
                centro = limitesActuales.ClosestPoint(centro);
            }
        }

        return new Vector3(centro.x, centro.y, zInicial);
    }

    public void EstablecerLimitesSala(Collider2D nuevosLimites, bool permitirZoomAutomatico = true, float zoomForzado = 0f)
    {
        limitesActuales = nuevosLimites;
        usarZoomEnSalaActual = permitirZoomAutomatico;

        if (zoomForzado > 0f)
        {
            usarZoomEnSalaActual = false;
            zoomPorDefecto = zoomForzado;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (limitesActuales == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
        Bounds b = limitesActuales.bounds;
        Gizmos.DrawWireCube(b.center, b.size - new Vector3(margenBorde * 2f, margenBorde * 2f, 0f));
    }
}
