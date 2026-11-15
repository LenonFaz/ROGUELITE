using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBarraJefe : MonoBehaviour
{
    public static UIBarraJefe Instancia { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject panelBarra;
    [SerializeField] private Image rellenoBarra;
    [SerializeField] private Image rellenoRetrasado;
    [SerializeField] private TMP_Text textoNombreJefe;
    [SerializeField] private string nombrePorDefecto = "La Inquisidora";
    [SerializeField, Min(0.5f)] private float velocidadSuavizado = 4f;

    private Vida jefeActual;
    private float fillObjetivo = 1f;

    void Awake()
    {
        Instancia = this;
        if (panelBarra != null) panelBarra.SetActive(false);
    }

    public void VincularJefe(Vida vidaJefe, string nombre = "")
    {
        if (jefeActual != null)
        {
            jefeActual.AlRecibirDanio.RemoveListener(ActualizarBarra);
            jefeActual.AlMorir.RemoveListener(OcultarBarra);
        }

        jefeActual = vidaJefe;
        if (jefeActual == null) return;

        if (textoNombreJefe != null)
        {
            textoNombreJefe.text = string.IsNullOrEmpty(nombre) ? nombrePorDefecto : nombre;
        }

        fillObjetivo = jefeActual.PorcentajeVida;
        if (rellenoBarra != null) rellenoBarra.fillAmount = fillObjetivo;
        if (rellenoRetrasado != null) rellenoRetrasado.fillAmount = fillObjetivo;

        jefeActual.AlRecibirDanio.AddListener(ActualizarBarra);
        jefeActual.AlMorir.AddListener(OcultarBarra);

        if (panelBarra != null) panelBarra.SetActive(true);
    }

    private void ActualizarBarra()
    {
        if (jefeActual == null) return;
        fillObjetivo = jefeActual.PorcentajeVida;
        if (rellenoBarra != null) rellenoBarra.fillAmount = fillObjetivo;
    }

    public void OcultarBarra()
    {
        if (panelBarra != null) panelBarra.SetActive(false);
    }

    void Update()
    {
        if (jefeActual == null || panelBarra == null || !panelBarra.activeSelf) return;

        if (rellenoRetrasado != null)
        {
            rellenoRetrasado.fillAmount = Mathf.Lerp(rellenoRetrasado.fillAmount, fillObjetivo, velocidadSuavizado * Time.deltaTime);
        }
    }
}
