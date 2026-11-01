using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(100)]
public class MargenTerreno : MonoBehaviour
{
    [Header("Configuración de Margen")]
    [SerializeField, Min(0.2f)] private float radioMargen = 1.3f;
    [SerializeField, Min(0f)] private float fuerzaRepulsion = 5f;
    [SerializeField, Range(8, 24)] private int cantidadRayos = 12;

    [Header("Tags a Evitar")]
    [SerializeField] private string[] tagsTerreno = { "pared", "hueco" };

    private Rigidbody2D rb;
    private Collider2D[] misColliders;
    private Vector2[] direccionesRayos;
    private readonly RaycastHit2D[] bufferHits = new RaycastHit2D[8];
    private ContactFilter2D filtro;

    void Awake()
    {
        TryGetComponent(out rb);
        misColliders = GetComponentsInChildren<Collider2D>();

        filtro = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = false
        };

        PrecalcularDirecciones();
    }

    void OnValidate()
    {
        PrecalcularDirecciones();
    }

    private void PrecalcularDirecciones()
    {
        direccionesRayos = new Vector2[cantidadRayos];
        for (int i = 0; i < cantidadRayos; i++)
        {
            float angulo = (i / (float)cantidadRayos) * Mathf.PI * 2f;
            direccionesRayos[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        }
    }

    void FixedUpdate()
    {
        Vector2 vectorRepulsion = Vector2.zero;
        int obstaculosDetectados = 0;

        for (int i = 0; i < cantidadRayos; i++)
        {
            Vector2 dir = direccionesRayos[i];
            int cantidadImpactos = Physics2D.Raycast(transform.position, dir, filtro, bufferHits, radioMargen);

            float distanciaMasCercana = radioMargen;
            bool impactoValido = false;

            for (int j = 0; j < cantidadImpactos; j++)
            {
                Collider2D col = bufferHits[j].collider;
                if (col == null || EsPropioCollider(col) || !TieneTagTerreno(col.gameObject)) continue;

                if (bufferHits[j].distance < distanciaMasCercana)
                {
                    distanciaMasCercana = bufferHits[j].distance;
                    impactoValido = true;
                }
            }

            if (impactoValido)
            {
                float peso = 1f - (distanciaMasCercana / radioMargen);
                vectorRepulsion -= dir * (peso * peso);
                obstaculosDetectados++;
            }
        }

        if (obstaculosDetectados == 0 || vectorRepulsion.sqrMagnitude < 0.0001f) return;

        Vector2 dirAlejamiento = vectorRepulsion.normalized;
        float intensidad = Mathf.Clamp01(vectorRepulsion.magnitude);

        Vector2 velocidadActual = rb.linearVelocity;

        float proyeccionHaciaMuro = Vector2.Dot(velocidadActual, -dirAlejamiento);
        if (proyeccionHaciaMuro > 0f)
        {
            velocidadActual += dirAlejamiento * (proyeccionHaciaMuro * intensidad);
        }

        velocidadActual += dirAlejamiento * (fuerzaRepulsion * intensidad);
        rb.linearVelocity = velocidadActual;
    }

    private bool EsPropioCollider(Collider2D col)
    {
        for (int i = 0; i < misColliders.Length; i++)
        {
            if (misColliders[i] == col) return true;
        }
        return false;
    }

    private bool TieneTagTerreno(GameObject obj)
    {
        for (int i = 0; i < tagsTerreno.Length; i++)
        {
            if (string.IsNullOrEmpty(tagsTerreno[i])) continue;
            if (string.Equals(obj.tag, tagsTerreno[i], System.StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radioMargen);
    }
}
