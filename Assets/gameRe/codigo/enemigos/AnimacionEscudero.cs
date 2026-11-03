using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class AnimacionEscudero : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] frente = new Sprite[4];
    [SerializeField] private Sprite[] atras = new Sprite[4];

    [Header("Animacion")]
    [SerializeField, Min(0.1f)] private float fps = 8f;
    [Tooltip("Por debajo de esta velocidad se considera quieto (frame 0).")]
    [SerializeField, Min(0f)] private float velMinima = 0.05f;
    [Tooltip("Zona muerta vertical para no cambiar de frente/espalda con movimientos casi horizontales.")]
    [SerializeField, Min(0f)] private float zonaMuertaY = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool mirandoAtras;
    private float tiempo;

    void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out sr);
    }

    void Update()
    {
        Vector2 v = rb.linearVelocity;
        bool moviendo = v.sqrMagnitude > velMinima * velMinima;

        if (moviendo)
        {
            if (v.y > zonaMuertaY) mirandoAtras = true;
            else if (v.y < -zonaMuertaY) mirandoAtras = false;
        }

        Sprite[] frames = mirandoAtras ? atras : frente;
        if (frames == null || frames.Length == 0) return;

        int idx = 0;
        if (moviendo)
        {
            tiempo += Time.deltaTime * fps;
            idx = (int)tiempo % frames.Length;
        }
        else
        {
            tiempo = 0f;
        }

        sr.sprite = frames[idx];
    }
}