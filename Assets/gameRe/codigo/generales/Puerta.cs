using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Puerta : MonoBehaviour
{
    public enum OrientacionPuerta
    {
        Frente,
        Lateral
    }

    [Header("Orientación y Estado Inicial")]
    [SerializeField] private OrientacionPuerta orientacion = OrientacionPuerta.Frente;
    [SerializeField] private bool voltearSpriteLateral = false;
    [SerializeField] private bool iniciarAbierta = true;

    [Header("Sprites (Frente y Lateral)")]
    [SerializeField] private SpriteRenderer spritePuerta;
    [SerializeField] private Sprite spriteFrente;
    [SerializeField] private Sprite spriteLateral;

    [Header("Clips de Animación (Opcional)")]
    [SerializeField] private Animator animPuerta;
    [SerializeField] private AnimationClip clipAbrirFrente;
    [SerializeField] private AnimationClip clipCerrarFrente;
    [SerializeField] private AnimationClip clipAbrirLateral;
    [SerializeField] private AnimationClip clipCerrarLateral;

    [Header("Movimiento Visual (Subir al Abrir / Bajar al Cerrar)")]
    [SerializeField] private Transform contenedorVisual;
    [SerializeField, Min(0f)] private float alturaSubidaAlAbrir = 1.15f;
    [SerializeField, Min(0.05f)] private float duracionSubida = 0.28f;
    [SerializeField, Min(0.05f)] private float duracionBajada = 0.16f;
    [SerializeField] private bool desvanecerAlSubir = true;
    [SerializeField, Range(0f, 1f)] private float alphaCuandoAbierta = 0f;

    private Collider2D colPuerta;
    private Vector3 posLocalCerrada;
    private Vector3 posLocalAbierta;
    private bool estaAbierta;
    private Coroutine rutinaMovimiento;

    public bool EstaAbierta => estaAbierta;

    void Awake()
    {
        TryGetComponent(out colPuerta);
        colPuerta.isTrigger = false;

        if (!spritePuerta) spritePuerta = GetComponentInChildren<SpriteRenderer>();
        if (!animPuerta) animPuerta = GetComponentInChildren<Animator>();

        AsegurarContenedorVisual();
        AplicarSpritePorOrientacion();

        posLocalCerrada = contenedorVisual.localPosition;
        posLocalAbierta = posLocalCerrada + Vector3.up * alturaSubidaAlAbrir;

        AplicarEstadoInmediato(iniciarAbierta);
    }

    private void AsegurarContenedorVisual()
    {
        if (contenedorVisual != null && contenedorVisual != transform) return;

        if (spritePuerta != null && spritePuerta.transform != transform)
        {
            contenedorVisual = spritePuerta.transform;
            return;
        }

        GameObject hijoVisual = new GameObject("VisualPuerta");
        hijoVisual.transform.SetParent(transform, false);
        hijoVisual.transform.localPosition = Vector3.zero;
        hijoVisual.transform.localRotation = Quaternion.identity;
        hijoVisual.transform.localScale = Vector3.one;

        if (spritePuerta != null && spritePuerta.transform == transform)
        {
            SpriteRenderer nuevoSr = hijoVisual.AddComponent<SpriteRenderer>();
            nuevoSr.sprite = spritePuerta.sprite;
            nuevoSr.color = spritePuerta.color;
            nuevoSr.sortingLayerID = spritePuerta.sortingLayerID;
            nuevoSr.sortingOrder = spritePuerta.sortingOrder;
            nuevoSr.sharedMaterial = spritePuerta.sharedMaterial;
            nuevoSr.flipX = spritePuerta.flipX;
            nuevoSr.flipY = spritePuerta.flipY;

            spritePuerta.enabled = false;
            spritePuerta = nuevoSr;
        }

        contenedorVisual = hijoVisual.transform;
    }

    private void AplicarSpritePorOrientacion()
    {
        if (spritePuerta == null) return;

        if (orientacion == OrientacionPuerta.Frente)
        {
            if (spriteFrente != null) spritePuerta.sprite = spriteFrente;
            spritePuerta.flipX = false;
        }
        else
        {
            if (spriteLateral != null) spritePuerta.sprite = spriteLateral;
            spritePuerta.flipX = voltearSpriteLateral;
        }
    }

    public void Abrir()
    {
        if (estaAbierta) return;
        estaAbierta = true;

        if (colPuerta != null) colPuerta.enabled = false;

        ReproducirClipOrientacion(true);

        if (!gameObject.activeInHierarchy)
        {
            AplicarEstadoInmediato(true);
            return;
        }

        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);
        rutinaMovimiento = StartCoroutine(RutinaSubirPuerta());
    }

    public void Cerrar()
    {
        if (!estaAbierta) return;
        estaAbierta = false;

        DespejarJugadorSiEstaEnMarco();
        if (colPuerta != null) colPuerta.enabled = true;

        AplicarSpritePorOrientacion();
        ReproducirClipOrientacion(false);

        if (!gameObject.activeInHierarchy)
        {
            AplicarEstadoInmediato(false);
            return;
        }

        if (rutinaMovimiento != null) StopCoroutine(rutinaMovimiento);
        rutinaMovimiento = StartCoroutine(RutinaBajarPuerta());
    }

    private void DespejarJugadorSiEstaEnMarco()
    {
        if (colPuerta == null || Jugador.Instancia == null) return;

        Transform tJug = Jugador.Instancia.transform;
        Bounds b = colPuerta.bounds;
        b.Expand(0.15f);

        if (b.Contains(tJug.position))
        {
            Vector2 dirEmpuje = Jugador.Instancia.UltimaDireccion;
            if (dirEmpuje.sqrMagnitude < 0.01f)
            {
                dirEmpuje = ((Vector2)tJug.position - (Vector2)transform.position).normalized;
            }
            if (dirEmpuje.sqrMagnitude < 0.01f) dirEmpuje = Vector2.up;

            Vector3 nuevaPos = tJug.position + (Vector3)(dirEmpuje.normalized * 0.55f);
            tJug.position = nuevaPos;
            if (tJug.TryGetComponent(out Rigidbody2D rbJug))
            {
                rbJug.position = nuevaPos;
            }
        }
    }

    private IEnumerator RutinaSubirPuerta()
    {
        if (spritePuerta != null) spritePuerta.enabled = true;

        Vector3 inicio = contenedorVisual.localPosition;
        float alphaInicio = spritePuerta != null ? spritePuerta.color.a : 1f;
        float alphaFin = desvanecerAlSubir ? alphaCuandoAbierta : 1f;

        float t = 0f;
        float d = Mathf.Max(0.04f, duracionSubida);

        while (t < d)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / d);
            float curva = 1f - Mathf.Pow(1f - p, 3f);

            contenedorVisual.localPosition = Vector3.Lerp(inicio, posLocalAbierta, curva);

            if (spritePuerta != null)
            {
                Color c = spritePuerta.color;
                c.a = Mathf.Lerp(alphaInicio, alphaFin, p);
                spritePuerta.color = c;
            }

            yield return null;
        }

        contenedorVisual.localPosition = posLocalAbierta;

        if (spritePuerta != null)
        {
            Color c = spritePuerta.color;
            c.a = alphaFin;
            spritePuerta.color = c;
            if (desvanecerAlSubir && alphaFin <= 0.01f)
            {
                spritePuerta.enabled = false;
            }
        }

        rutinaMovimiento = null;
    }

    private IEnumerator RutinaBajarPuerta()
    {
        if (spritePuerta != null)
        {
            spritePuerta.enabled = true;
        }

        Vector3 inicio = contenedorVisual.localPosition;
        float alphaInicio = spritePuerta != null ? spritePuerta.color.a : 0f;

        float t = 0f;
        float d = Mathf.Max(0.04f, duracionBajada);

        while (t < d)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / d);
            float curva = p * p;

            contenedorVisual.localPosition = Vector3.Lerp(inicio, posLocalCerrada, curva);

            if (spritePuerta != null)
            {
                Color c = spritePuerta.color;
                c.a = Mathf.Lerp(alphaInicio, 1f, Mathf.Clamp01(p * 2f));
                spritePuerta.color = c;
            }

            yield return null;
        }

        if (spritePuerta != null)
        {
            Color c = spritePuerta.color;
            c.a = 1f;
            spritePuerta.color = c;
        }

        float tRebote = 0f;
        float durRebote = 0.08f;
        while (tRebote < durRebote)
        {
            tRebote += Time.deltaTime;
            float s = Mathf.Sin((tRebote / durRebote) * Mathf.PI) * 0.07f;
            contenedorVisual.localPosition = posLocalCerrada + Vector3.up * s;
            yield return null;
        }

        contenedorVisual.localPosition = posLocalCerrada;
        rutinaMovimiento = null;
    }

    private void AplicarEstadoInmediato(bool abierta)
    {
        estaAbierta = abierta;

        if (colPuerta != null)
        {
            colPuerta.enabled = !abierta;
        }

        if (contenedorVisual != null)
        {
            contenedorVisual.localPosition = abierta ? posLocalAbierta : posLocalCerrada;
        }

        if (spritePuerta != null)
        {
            float alpha = abierta && desvanecerAlSubir ? alphaCuandoAbierta : 1f;
            Color c = spritePuerta.color;
            c.a = alpha;
            spritePuerta.color = c;
            spritePuerta.enabled = !(abierta && desvanecerAlSubir && alpha <= 0.01f);
        }
    }

    private void ReproducirClipOrientacion(bool abriendo)
    {
        if (animPuerta == null) return;

        AnimationClip clip = null;
        if (orientacion == OrientacionPuerta.Frente)
        {
            clip = abriendo ? clipAbrirFrente : clipCerrarFrente;
        }
        else
        {
            clip = abriendo ? clipAbrirLateral : clipCerrarLateral;
        }

        if (clip != null)
        {
            animPuerta.enabled = true;
            animPuerta.Play(clip.name, -1, 0f);
        }
    }

    void OnValidate()
    {
        if (!spritePuerta) spritePuerta = GetComponentInChildren<SpriteRenderer>();
        AplicarSpritePorOrientacion();
    }
}
