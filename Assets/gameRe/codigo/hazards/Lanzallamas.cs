using UnityEngine;

public class LanzallamasBar : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform rotar;
    [SerializeField] GameObject bolaPrefab;

    [Header("Forma")]
    [SerializeField] int cantidadBolas = 6;
    [SerializeField] float separacion = 0.8f;
    [SerializeField] float inicioOffset = 0.6f;

    [Header("Giro")]
    [SerializeField] float degreesPerSecond = 120f;
    [SerializeField] float startAngle = 0f;
    [SerializeField] bool clockwise = true;
    [SerializeField] float giroPropio = 360f;

    [Header("Daño")]
    [SerializeField] float hitRadius = 0.2f;
    [SerializeField] int damage = 1;

    Transform[] bolas;

    void Start()
    {
        if (rotar == null || bolaPrefab == null)
        {
            Debug.LogError("LanzallamasBar: faltan Rotar o Bola Prefab", this);
            enabled = false;
            return;
        }

        rotar.localRotation = Quaternion.Euler(0, 0, startAngle);

        bolas = new Transform[cantidadBolas];
        for (int i = 0; i < cantidadBolas; i++)
        {
            var b = Instantiate(bolaPrefab, rotar);
            b.transform.localPosition = new Vector3(inicioOffset + i * separacion, 0, 0);
            bolas[i] = b.transform;
        }
    }

    void Update()
    {
        float dir = clockwise ? -1f : 1f;
        rotar.Rotate(0, 0, dir * degreesPerSecond * Time.deltaTime);

        if (giroPropio != 0f)
            foreach (var b in bolas)
                b.Rotate(0, 0, giroPropio * Time.deltaTime);
    }

    void FixedUpdate()
    {
        foreach (var b in bolas)
        {
            var hits = Physics2D.OverlapCircleAll(b.position, hitRadius);
            foreach (var h in hits)
            {
                var vida = h.GetComponentInParent<Vida>();
                if (vida != null)
                {
                    vida.RecibirDanio(damage);
                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (rotar == null) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < cantidadBolas; i++)
        {
            Vector3 pos = rotar.position + rotar.right * (inicioOffset + i * separacion);
            Gizmos.DrawWireSphere(pos, hitRadius);
        }
    }
}