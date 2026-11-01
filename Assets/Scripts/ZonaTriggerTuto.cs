using UnityEngine;

public class ZonaTriggerTuto : MonoBehaviour
{
    public GameObject panelTutorialAsociado;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (panelTutorialAsociado != null)
            {
                panelTutorialAsociado.SetActive(true);
            }

            gameObject.SetActive(false);
        }
    }
}