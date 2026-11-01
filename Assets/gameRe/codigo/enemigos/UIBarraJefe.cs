using UnityEngine;
using UnityEngine.UI;

public class UIBarraJefe : MonoBehaviour
{
    public static UIBarraJefe Instancia { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject contenedorVisual;
    [SerializeField] private Image barraRelleno;
    [SerializeField] private Image barraRetraso;

    [Header("Ajustes")]
    [SerializeField, Min(0.1f)] private float velocidadSuavizado = 5f;

    private Vida vidaJefe;

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);

        if (contenedorVisual != null)
        {
            contenedorVisual.SetActive(false);
        }
    }

    public void VincularJefe(Vida jefe)
    {
        vidaJefe = jefe;

        if (barraRelleno != null) barraRelleno.fillAmount = 1f;
        if (barraRetraso != null) barraRetraso.fillAmount = 1f;
        if (contenedorVisual != null) contenedorVisual.SetActive(true);

        vidaJefe.AlMorir.AddListener(OcultarBarra);
    }

    public void OcultarBarra()
    {
        vidaJefe = null;
        if (contenedorVisual != null)
        {
            contenedorVisual.SetActive(false);
        }
    }

    void Update()
    {
        if (vidaJefe == null) return;

        float porcentaje = vidaJefe.PorcentajeVida;

        if (barraRelleno != null)
        {
            barraRelleno.fillAmount = porcentaje;
        }

        if (barraRetraso != null)
        {
            barraRetraso.fillAmount = Mathf.Lerp(barraRetraso.fillAmount, porcentaje, velocidadSuavizado * Time.deltaTime);
        }
    }
}
