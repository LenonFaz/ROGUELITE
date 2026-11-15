using System.Collections.Generic;
using UnityEngine;

public interface IRangoDeteccion
{
    float RangoDeteccion { get; }
}

[RequireComponent(typeof(Collider2D))]
public class Sala : MonoBehaviour
{
    [Header("Identificador y Checkpoint")]
    [SerializeField] private string idUnicoSala;
    [SerializeField] private Transform puntoReaparicionPersonalizado;

    [Header("Elementos de la Sala")]
    [SerializeField] private List<Vida> enemigos = new List<Vida>();
    [SerializeField] private List<Puerta> puertas = new List<Puerta>();
    [SerializeField] private bool reiniciarLuzAlEntrar = true;

    [Header("Configuración de Cámara")]
    [SerializeField] private bool limitarCamaraAEsteEspacio = true;
    [SerializeField] private bool ajustarZoomCamara = true;
    [SerializeField, Min(0f)] private float zoomPersonalizado;

    private Collider2D colSala;
    private Transform jugador;
    private bool jugadorEnSala;
    private bool combateIniciado;
    private bool salaLimpia;

    void Awake()
    {
        if (string.IsNullOrEmpty(idUnicoSala))
        {
            Vector3 p = transform.position;
            idUnicoSala = $"{gameObject.scene.name}_{gameObject.name}_{Mathf.RoundToInt(p.x)}_{Mathf.RoundToInt(p.y)}";
        }

        colSala = GetComponent<PolygonCollider2D>();
        if (colSala == null) TryGetComponent(out colSala);

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }

        AutodetectarEnemigosEnPoligono();

        if (GestorProgresoSalas.EstaSalaCompletada(idUnicoSala))
        {
            salaLimpia = true;
            for (int i = 0; i < enemigos.Count; i++)
            {
                if (enemigos[i] != null)
                {
                    Destroy(enemigos[i].gameObject);
                }
            }
            enemigos.Clear();

            for (int i = 0; i < puertas.Count; i++)
            {
                if (puertas[i] != null) puertas[i].Abrir();
            }
            return;
        }

        for (int i = 0; i < enemigos.Count; i++)
        {
            Vida e = enemigos[i];
            if (e != null)
            {
                e.AlMorir.AddListener(VerificarEnemigosRestantes);
                ConfigurarIAEnemigo(e.gameObject, false);
            }
        }
    }

    private void AutodetectarEnemigosEnPoligono()
    {
        if (enemigos.Count == 0)
        {
            GetComponentsInChildren(true, enemigos);
        }

        if (enemigos.Count == 0 && colSala != null)
        {
            Vida[] todasLasVidas = FindObjectsByType<Vida>(FindObjectsSortMode.None);
            for (int i = 0; i < todasLasVidas.Length; i++)
            {
                Vida v = todasLasVidas[i];
                if (v == null || v.CompareTag("Player")) continue;

                if (colSala.OverlapPoint(v.transform.position) && !enemigos.Contains(v))
                {
                    enemigos.Add(v);
                }
            }
        }
    }

    void Start()
    {
        if (Jugador.Instancia != null)
        {
            jugador = Jugador.Instancia.transform;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (jugador != null && colSala != null && colSala.OverlapPoint(jugador.position))
        {
            ActivarEntradaSala(jugador.position);
        }
    }

    void Update()
    {
        if (!jugadorEnSala || salaLimpia || combateIniciado || jugador == null) return;

        for (int i = 0; i < enemigos.Count; i++)
        {
            Vida e = enemigos[i];
            if (e == null) continue;

            float rango = ObtenerRangoDeteccion(e.gameObject);
            if (Vector2.Distance(e.transform.position, jugador.position) <= rango)
            {
                IniciarCombate();
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        ActivarEntradaSala(other.transform.position);
    }

    private void ActivarEntradaSala(Vector3 posEntradaJugador)
    {
        if (limitarCamaraAEsteEspacio && CamaraSeguimiento.Instancia != null && colSala != null)
        {
            CamaraSeguimiento.Instancia.EstablecerLimitesSala(colSala, ajustarZoomCamara, zoomPersonalizado);
        }

        Vector3 puntoCheckpoint = puntoReaparicionPersonalizado != null ? puntoReaparicionPersonalizado.position : posEntradaJugador;
        GestorProgresoSalas.GuardarCheckpointEntrada(puntoCheckpoint);

        if (salaLimpia || jugadorEnSala) return;

        jugadorEnSala = true;

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
            if (reiniciarLuzAlEntrar) GestorLuz.Instancia.ReiniciarLuz();
        }

        LimpiarListaEnemigos();

        if (enemigos.Count == 0)
        {
            CompletarSala();
            return;
        }

        for (int i = 0; i < puertas.Count; i++)
        {
            if (puertas[i] != null) puertas[i].Cerrar();
        }

        for (int i = 0; i < enemigos.Count; i++)
        {
            if (enemigos[i] != null) ConfigurarIAEnemigo(enemigos[i].gameObject, true);
        }
    }

    private void IniciarCombate()
    {
        combateIniciado = true;
        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(true);
        }
    }

    private void VerificarEnemigosRestantes()
    {
        Invoke(nameof(ChequearEstadoSala), 0.05f);
    }

    private void ChequearEstadoSala()
    {
        if (salaLimpia) return;
        LimpiarListaEnemigos();

        if (enemigos.Count == 0)
        {
            CompletarSala();
        }
    }

    private void CompletarSala()
    {
        salaLimpia = true;
        GestorProgresoSalas.MarcarSalaCompletada(idUnicoSala);

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
            GestorLuz.Instancia.ReiniciarLuz();
        }

        for (int i = 0; i < puertas.Count; i++)
        {
            if (puertas[i] != null) puertas[i].Abrir();
        }
    }

    private void LimpiarListaEnemigos()
    {
        enemigos.RemoveAll(e => e == null);
    }

    private void ConfigurarIAEnemigo(GameObject enemigoObj, bool activo)
    {
        MonoBehaviour[] scripts = enemigoObj.GetComponents<MonoBehaviour>();
        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour s = scripts[i];
            if (s is Vida || s is Empuje || s is Aturdido || s is IndicadorAtaque) continue;
            s.enabled = activo;
        }

        if (!activo && enemigoObj.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private float ObtenerRangoDeteccion(GameObject enemigoObj)
    {
        if (enemigoObj.TryGetComponent(out IRangoDeteccion detector))
        {
            return detector.RangoDeteccion;
        }
        return 8f;
    }
}
