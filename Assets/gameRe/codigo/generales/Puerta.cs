using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Puerta : MonoBehaviour
{
    [SerializeField] private Sprite spriteAbierta;
    [SerializeField] private Sprite spriteCerrada;
    [SerializeField] private bool iniciarAbierta = true;

    private Collider2D col;
    private SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (iniciarAbierta) Abrir();
        else Cerrar();
    }

    public void Abrir()
    {
        col.enabled = false;
        if (spriteAbierta != null) sr.sprite = spriteAbierta;
        else sr.enabled = false;
    }

    public void Cerrar()
    {
        col.enabled = true;
        sr.enabled = true;
        if (spriteCerrada != null) sr.sprite = spriteCerrada;
    }
}
