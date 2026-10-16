
using UnityEngine;
using System.Collections;

public class AtaqueEspiral : MonoBehaviour
{
    [Header("Ataque circular")]
    [SerializeField] private ProyectilEspiral prefab;
    [SerializeField] private int shootAmount = 10;
    [SerializeField] private float shootInterval = 0.1f;

    private Coroutine ataqueActual;

    public bool Atacando { get; private set; }

    public void Atacar()
    {
        if (Atacando)
            return;

        ataqueActual = StartCoroutine(DispararCirculo());
    }

    private IEnumerator DispararCirculo()
    {
        Atacando = true;

        for (int i = 0; i < shootAmount; i++)
        {
            float angulo = Mathf.Deg2Rad * (360f / shootAmount) * i;

            Vector2 direccion = new Vector2(
                Mathf.Sin(angulo),
                Mathf.Cos(angulo)
            );

            if (prefab != null)
            {
                ProyectilEspiral bala = Instantiate(
                    prefab,
                    transform.position,
                    Quaternion.identity
                );

                bala.Iniciar(direccion);
            }

            if (i < shootAmount - 1)
                yield return new WaitForSeconds(shootInterval);
        }

        Atacando = false;
        ataqueActual = null;
    }

    private void OnDisable()
    {
        if (ataqueActual != null)
            StopCoroutine(ataqueActual);

        ataqueActual = null;
        Atacando = false;
    }
}
