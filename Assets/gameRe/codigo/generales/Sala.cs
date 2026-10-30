using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Sala : MonoBehaviour
{
    [Header("Elementos de la Sala")]
    [SerializeField] private List<Vida> enemigos = new List<Vida>();
    [SerializeField] private List<Puerta> puertas = new List<Puerta>();
    [SerializeField] private bool reiniciarLuzAlEntrar = true;

    private Transform jugador;
    private bool jugadorEnSala = false;
    private bool combateIniciado = false;
    private bool salaLimpia = false;

    void Awake()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var c in colliders)
        {
            c.isTrigger = true;
        }

        if (enemigos.Count == 0)
        {
            GetComponentsInChildren(true, enemigos);
        }

        foreach (var e in enemigos)
        {
            if (e != null)
            {
                e.AlMorir.AddListener(VerificarEnemigosRestantes);
                ConfigurarIAEnemigo(e.gameObject, false);
            }
        }
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
    }

    void Update()
    {
        if (!jugadorEnSala || salaLimpia || combateIniciado || jugador == null) return;

        foreach (var e in enemigos)
        {
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
        if (!other.CompareTag("Player") || salaLimpia || jugadorEnSala) return;

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

        foreach (var p in puertas)
        {
            if (p != null) p.Cerrar();
        }

        foreach (var e in enemigos)
        {
            if (e != null) ConfigurarIAEnemigo(e.gameObject, true);
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

        if (GestorLuz.Instancia != null)
        {
            GestorLuz.Instancia.EstablecerPlegariaActiva(false);
        }

        foreach (var p in puertas)
        {
            if (p != null) p.Abrir();
        }
    }

    private void LimpiarListaEnemigos()
    {
        enemigos.RemoveAll(e => e == null);
    }

    private void ConfigurarIAEnemigo(GameObject enemigoObj, bool activo)
    {
        MonoBehaviour[] scripts = enemigoObj.GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s is Vida || s is Empuje || s is Aturdido) continue;
            s.enabled = activo;
        }

        if (!activo && enemigoObj.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private float ObtenerRangoDeteccion(GameObject enemigoObj)
    {
        MonoBehaviour[] scripts = enemigoObj.GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s is Vida || s is Empuje || s is Aturdido) continue;
            var tipo = s.GetType();
            var campo = tipo.GetField("rangoDeteccion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? tipo.GetField("rangoDet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (campo != null && campo.FieldType == typeof(float))
            {
                return (float)campo.GetValue(s);
            }
        }
        return 8f;
    }
}
