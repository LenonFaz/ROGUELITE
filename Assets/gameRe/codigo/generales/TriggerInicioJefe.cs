using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerInicioJefe : MonoBehaviour
{
    [SerializeField] private LaInquisidora jefe;

    private bool activado;

    void Awake()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activado || !other.CompareTag("Player")) return;

        activado = true;
        if (jefe != null)
        {
            jefe.IniciarSecuenciaIntro();
        }
    }
}
