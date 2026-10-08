using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float vel = 14f;
    public float vida = 3f;
    public int danio = 1;

    Rigidbody2D rb;
    Vector2 dir = Vector2.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Destroy(gameObject, vida);
    }

    public void Init(Vector2 d)
    {
        dir = d.normalized;
        float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rot - 90f);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = dir * vel;
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (c.CompareTag("Player"))
        {
            // c.GetComponent<Vida>()?.RecibirDanio(danio);
            Destroy(gameObject);
        }
    }
}
