using Unity.Cinemachine;
using UnityEngine;

public class MapTransation : MonoBehaviour
{
    [SerializeField] PolygonCollider2D salaA;   // sala de un lado
    [SerializeField] PolygonCollider2D salaB;   // sala del otro lado
    [SerializeField] bool horizontal = true;    // true: salas a izquierda/derecha, false: arriba/abajo

    static CinemachineConfiner2D confiner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (confiner == null)
            confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        if (confiner == null || salaA == null || salaB == null) return;

        Vector2 centro = transform.position;
        Vector2 pos = collision.transform.position;

        // salaA = lado izquierdo/abajo, salaB = lado derecho/arriba
        bool jugadorEnLadoA = horizontal ? pos.x < centro.x : pos.y < centro.y;

        // Entra por el lado A -> va hacia B, y viceversa
        confiner.BoundingShape2D = jugadorEnLadoA ? salaB : salaA;
        confiner.InvalidateBoundingShapeCache();
    }
    //AYUDA NO ME SALE
}