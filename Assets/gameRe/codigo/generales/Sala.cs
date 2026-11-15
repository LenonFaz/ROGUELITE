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
    [SerializeField, Min(0f)] private float distanciaAdentroParaCerrarPuertas = 0.65f;
    [SerializeField] private bool reiniciarLuzAlEntrar = true;

    [Header("Configuración de Cámara")]
    [SerializeField] private bool limitarCamaraAEsteEspacio = true;
    [SerializeField] private bool ajustarZoomCamara = true;
    [SerializeField, Min(0f)] private float zoomPersonalizado;

    private Collider2D colSala;
    private Transform jugador;
    private Vector3 posContactoInicial;
    private bool esperandoCruzarUmbral;
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
        AutodetectarPuertasCercanas();

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

    private void AutodetectarPuertasCercanas()
    {
        if (puertas.Count > 0) return;

        GetComponentsInChildren(true, puertas);
        if (puertas.Count > 0 || colSala == null) return;

        Bounds bExpandido = colSala.bounds;
        bExpandido.Expand(1.5f);

        Puerta[] todasLasPuertas = FindObjectsByType<Puerta>(FindObjectsSortMode.None);
        for (int i = 0; i < todasLasPuertas.Length; i++)
        {
            Puerta p = todasLasPuertas[i];
            if (p != null && bExpandido.Contains(p.transform.position) && !puertas.Contains(p))
            {
                puertas.Add(p);
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
            ActivarCamaraYCheckpoint(jugador.position);
            ConfirmarIngresoYEncerrar();
        }
    }

    void Update()
    {
        if (esperandoCruzarUmbral && !jugadorEnSala && jugador != null)
        {
            if (colSala != null && colSala.OverlapPoint(jugador.position))
            {
                float distRecorrida = Vector2.Distance(posContactoInicial, jugador.position);
                if (distRecorrida >= distanciaAdentroParaCerrarPuertas || !EstaCercaDeAlgunaPuerta(jugador.position))
                {
                    ConfirmarIngresoYEncerrar();
                }
            }
        }

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

    private bool EstaCercaDeAlgunaPuerta(Vector3 posJugador)
    {
        for (int i = 0; i < puertas.Count; i++)
        {
            if (puertas[i] != null && Vector2.Distance(posJugador, puertas[i].transform.position) < 0.85f)
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        jugador = other.transform;
        ActivarCamaraYCheckpoint(other.transform.position);

        if (salaLimpia || jugadorEnSala) return;

        posContactoInicial = other.transform.position;
        if (distanciaAdentroParaCerrarPuertas <= 0.01f)
        {
            ConfirmarIngresoYEncerrar();
        }
        else
        {
            esperandoCruzarUmbral = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!jugadorEnSala)
        {
            esperandoCruzarUmbral = false;
        }
    }

    private void ActivarCamaraYCheckpoint(Vector3 posEntradaJugador)
    {
        if (limitarCamaraAEsteEspacio && CamaraSeguimiento.Instancia != null && colSala != null)
        {
            CamaraSeguimiento.Instancia.EstablecerLimitesSala(colSala, ajustarZoomCamara, zoomPersonalizado);
        }

        Vector3 puntoCheckpoint = puntoReaparicionPersonalizado != null ? puntoReaparicionPersonalizado.position : posEntradaJugador;
        GestorProgresoSalas.GuardarCheckpointEntrada(puntoCheckpoint);
    }

    private void ConfirmarIngresoYEncerrar()
    {
        if (salaLimpia || jugadorEnSala) return;

        esperandoCruzarUmbral = false;
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
