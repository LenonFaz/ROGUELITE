using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProyectilEspiral : MonoBehaviour
{
    public float vel = 6f;
    public float vida = 5f;
    public int dano = 1;
    public string tagJugador = "Player";
    public LayerMask capaObstaculos;

    Vector2 dir = Vector2.right;

    public void Iniciar(Vector2 direccion)
    {
        dir = direccion.normalized;
    }

    void Start()
    {
        Destroy(gameObject, vida);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * vel * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagJugador))
        {
            other.SendMessage("RecibirDano", dano, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & capaObstaculos) != 0)
            Destroy(gameObject);
    }
}