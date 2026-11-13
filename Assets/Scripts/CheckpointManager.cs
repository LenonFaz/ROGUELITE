using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Checkpoint currentCheckpoint;

    public void ChangeCheckpoint(Checkpoint newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    [ContextMenu("Respawn")]
    public void RespawnCharacter()
    {
        player.position = currentCheckpoint.transform.position;
    }
}