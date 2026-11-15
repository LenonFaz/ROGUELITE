using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PortalSalida : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nombreSiguienteEscena;
    [SerializeField] private int indiceSiguienteEscena = -1;

    [Header("Estado y Visuales")]
    [SerializeField] private bool activoAlIniciar = false;
    [SerializeField] private SpriteRenderer spritePortal;
    [SerializeField] private Sprite spriteApagado;
    [SerializeField] private Sprite spriteEncendido;
    [SerializeField] private Animator animPortal;
    [SerializeField] private AnimationClip clipPortalApagado;
    [SerializeField] private AnimationClip clipPortalEncendido;
    [SerializeField] private GameObject indicadorInteraccionUI;
    [SerializeField] private Light2D luzPortal;
    [SerializeField, Min(0f)] private float intensidadLuzEncendido = 1.8f;

    private bool portalEncendido;
    private bool jugadorDentro;
    private bool cargandoEscena;

    void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }

        if (!spritePortal) TryGetComponent(out spritePortal);
        if (!animPortal) TryGetComponent(out animPortal);
        if (!luzPortal) luzPortal = GetComponentInChildren<Light2D>();

        if (indicadorInteraccionUI != null) indicadorInteraccionUI.SetActive(false);

        AplicarEstadoVisual(activoAlIniciar);
    }

    public void EncenderPortal()
    {
        if (portalEncendido) return;
        portalEncendido = true;
        gameObject.SetActive(true);
        StartCoroutine(RutinaEncendidoSuave());
    }

    private IEnumerator RutinaEncendidoSuave()
    {
        AplicarEstadoVisual(true);

        if (spritePortal != null)
        {
            Vector3 escalaBase = transform.localScale;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin((t / 0.35f) * Mathf.PI) * 0.25f;
                transform.localScale = escalaBase * s;
                yield return null;
            }
            transform.localScale = escalaBase;
        }

        if (jugadorDentro && indicadorInteraccionUI != null)
        {
            indicadorInteraccionUI.SetActive(true);
        }
    }

    private void AplicarEstadoVisual(bool encendido)
    {
        portalEncendido = encendido;

        if (animPortal != null)
        {
            AnimationClip clip = encendido ? clipPortalEncendido : clipPortalApagado;
            if (clip != null)
            {
                animPortal.enabled = true;
                animPortal.Play(clip.name);
            }
        }
        else if (spritePortal != null)
        {
            if (encendido && spriteEncendido != null) spritePortal.sprite = spriteEncendido;
            else if (!encendido && spriteApagado != null) spritePortal.sprite = spriteApagado;
        }

        if (luzPortal != null && luzPortal.lightType != Light2D.LightType.Global)
        {
            luzPortal.enabled = encendido;
            if (encendido) luzPortal.intensity = intensidadLuzEncendido;
        }
    }

    void Update()
    {
        if (!portalEncendido || !jugadorDentro || cargandoEscena) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool presionoEnter = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
                             (gp != null && gp.buttonSouth.wasPressedThisFrame);

        if (presionoEnter)
        {
            cargandoEscena = true;
            CargarSiguienteEscena();
        }
    }

    private void CargarSiguienteEscena()
    {
        if (!string.IsNullOrEmpty(nombreSiguienteEscena))
        {
            SceneManager.LoadScene(nombreSiguienteEscena);
        }
        else if (indiceSiguienteEscena >= 0)
        {
            SceneManager.LoadScene(indiceSiguienteEscena);
        }
        else
        {
            int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
            if (siguiente < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(siguiente);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = true;

        if (portalEncendido && indicadorInteraccionUI != null)
        {
            indicadorInteraccionUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        jugadorDentro = false;

        if (indicadorInteraccionUI != null)
        {
            indicadorInteraccionUI.SetActive(false);
        }
    }
}
