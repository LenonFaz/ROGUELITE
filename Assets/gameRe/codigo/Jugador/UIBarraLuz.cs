using UnityEngine;
using UnityEngine.UI;

public class UIBarraLuz : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private RectTransform contenedorSacudida;
    [SerializeField] private Image rellenoBarra;

    [Header("Suavizado de Relleno")]
    [SerializeField, Min(1f)] private float velocidadRelleno = 10f;

    [Header("Feedback de Peligro (Cerca al 100%)")]
    [SerializeField, Range(0.1f, 0.95f)] private float umbralInicioTemblor = 0.65f;
    [SerializeField, Min(0f)] private float intensidadTemblorMaxima = 6f;
    [SerializeField, Min(0f)] private float amplitudLatidoEscala = 0.08f;
    [SerializeField, Min(1f)] private float velocidadLatidoMaxima = 18f;
    [SerializeField] private Color colorNormal = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color colorPeligro = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color colorDestelloImpacto = Color.white;

    [Header("Animación al Recibir Daño")]
    [SerializeField, Min(0.05f)] private float duracionImpactoDanio = 0.28f;
    [SerializeField, Min(0f)] private float fuerzaTemblorDanio = 11f;
    [SerializeField, Min(0f)] private float escalaGolpeDanio = 0.22f;

    private Vector2 posAncladaOriginal;
    private Vector3 escalaOriginal;
    private float fillVisual;
    private float ultimoPorcentajeLeido;
    private float timerImpactoDanio;
    private bool jugadorVinculado;

    void Awake()
    {
        if (!contenedorSacudida) TryGetComponent(out contenedorSacudida);
        if (contenedorSacudida != null)
        {
            posAncladaOriginal = contenedorSacudida.anchoredPosition;
            escalaOriginal = contenedorSacudida.localScale;
        }

        if (rellenoBarra != null)
        {
            rellenoBarra.color = colorNormal;
        }
    }

    void Start()
    {
        IntentarVincularJugador();

        if (GestorLuz.Instancia != null)
        {
            fillVisual = GestorLuz.Instancia.PorcentajeLuz;
            ultimoPorcentajeLeido = fillVisual;
            if (rellenoBarra != null) rellenoBarra.fillAmount = fillVisual;
        }
    }

    private void IntentarVincularJugador()
    {
        if (jugadorVinculado) return;

        Jugador jug = Jugador.Instancia;
        if (jug != null && jug.TryGetComponent(out Vida vidaJugador))
        {
            vidaJugador.AlRecibirDanio.AddListener(EjecutarAnimacionImpacto);
            jugadorVinculado = true;
        }
    }

    public void EjecutarAnimacionImpacto()
    {
        timerImpactoDanio = duracionImpactoDanio;
    }

    void Update()
    {
        if (!jugadorVinculado) IntentarVincularJugador();
        if (GestorLuz.Instancia == null) return;

        float porcentajeActual = Mathf.Clamp01(GestorLuz.Instancia.PorcentajeLuz);

        if (porcentajeActual - ultimoPorcentajeLeido >= 0.08f)
        {
            EjecutarAnimacionImpacto();
        }
        ultimoPorcentajeLeido = porcentajeActual;

        fillVisual = Mathf.Lerp(fillVisual, porcentajeActual, velocidadRelleno * Time.deltaTime);
        if (rellenoBarra != null)
        {
            rellenoBarra.fillAmount = fillVisual;
        }

        float factorPeligro = 0f;
        if (porcentajeActual > umbralInicioTemblor)
        {
            factorPeligro = Mathf.InverseLerp(umbralInicioTemblor, 1f, porcentajeActual);
        }

        Vector2 offsetTemblor = Vector2.zero;
        float multiplicadorEscala = 1f;

        if (factorPeligro > 0f)
        {
            float curvaPeligro = factorPeligro * factorPeligro;
            offsetTemblor += Random.insideUnitCircle * (intensidadTemblorMaxima * curvaPeligro);

            float velLatido = Mathf.Lerp(6f, velocidadLatidoMaxima, factorPeligro);
            float ondaLatido = (Mathf.Sin(Time.time * velLatido) + 1f) * 0.5f;
            multiplicadorEscala += ondaLatido * amplitudLatidoEscala * factorPeligro;
        }

        float factorDestello = 0f;
        if (timerImpactoDanio > 0f)
        {
            timerImpactoDanio -= Time.deltaTime;
            float progresoImpacto = Mathf.Clamp01(timerImpactoDanio / duracionImpactoDanio);

            offsetTemblor += Random.insideUnitCircle * (fuerzaTemblorDanio * progresoImpacto);
            multiplicadorEscala += Mathf.Sin(progresoImpacto * Mathf.PI) * escalaGolpeDanio;
            factorDestello = progresoImpacto;
        }

        if (contenedorSacudida != null)
        {
            contenedorSacudida.anchoredPosition = posAncladaOriginal + offsetTemblor;
            contenedorSacudida.localScale = escalaOriginal * multiplicadorEscala;
        }

        if (rellenoBarra != null)
        {
            Color colorBase = Color.Lerp(colorNormal, colorPeligro, factorPeligro);

            if (porcentajeActual > 0.85f)
            {
                float parpadeoCritico = (Mathf.Sin(Time.time * 28f) + 1f) * 0.5f * 0.45f;
                factorDestello = Mathf.Max(factorDestello, parpadeoCritico);
            }

            rellenoBarra.color = Color.Lerp(colorBase, colorDestelloImpacto, factorDestello);
        }
    }

    void OnDisable()
    {
        if (contenedorSacudida != null)
        {
            contenedorSacudida.anchoredPosition = posAncladaOriginal;
            contenedorSacudida.localScale = escalaOriginal;
        }
    }
}
