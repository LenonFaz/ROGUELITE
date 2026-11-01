using System.Collections;
using UnityEngine;

public class AtaqueEspiral : MonoBehaviour
{
    [Header("Ataque circular")]
    [SerializeField] private Bullet prefab;
    [SerializeField] private int shootAmount = 10;
    [SerializeField] private float shootInterval = 0.1f;
    [SerializeField] private float velocidadBala = 6f;
    [SerializeField] private int danioBala = 1;
    [SerializeField] private float vidaBala = 5f;

    private Coroutine ataqueActual;

    public bool Atacando { get; private set; }

    public void Atacar()
    {
        if (Atacando) return;
        ataqueActual = StartCoroutine(DispararCirculo());
    }

    private IEnumerator DispararCirculo()
    {
        Atacando = true;
        Collider2D miCol = GetComponent<Collider2D>();

        for (int i = 0; i < shootAmount; i++)
        {
            float angulo = Mathf.Deg2Rad * (360f / shootAmount) * i;
            Vector2 direccion = new Vector2(Mathf.Sin(angulo), Mathf.Cos(angulo));

            if (prefab != null)
            {
                Bullet bala = Instantiate(prefab, transform.position, Quaternion.identity);
                if (miCol != null && bala.TryGetComponent(out Collider2D colBala))
                {
                    Physics2D.IgnoreCollision(miCol, colBala, true);
                }
                bala.Init(direccion, velocidadBala, danioBala, vidaBala);
            }

            if (i < shootAmount - 1)
            {
                yield return new WaitForSeconds(shootInterval);
            }
        }

        Atacando = false;
        ataqueActual = null;
    }

    private void OnDisable()
    {
        if (ataqueActual != null) StopCoroutine(ataqueActual);
        ataqueActual = null;
        Atacando = false;
    }
}
