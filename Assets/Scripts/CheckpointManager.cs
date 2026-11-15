using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Checkpoint currentCheckpoint;

    private void Start()
    {
        if (DatosCheckpoint.hayCheckpoint &&
            SceneManager.GetActiveScene().name == DatosCheckpoint.escenaActual)
        {
            player.position = DatosCheckpoint.posicion;
        }
    }

    public void ChangeCheckpoint(Checkpoint newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;

        DatosCheckpoint.Guardar(
            SceneManager.GetActiveScene().name,
            newCheckpoint.transform.position
        );
    }

    [ContextMenu("Respawn")]
    public void RespawnCharacter()
    {
        player.position = currentCheckpoint.transform.position;
    }
}