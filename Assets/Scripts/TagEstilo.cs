using UnityEngine;

public class TagEstiloDeadlock3Rotaciones : MonoBehaviour
{
    [Header("Configuración de las 3 Rotaciones (Cambialo si quieres :p)")]
    public float angulo1 = -6f;
    public float angulo2 = 0f;
    public float angulo3 = 6f;

    [Header("Velocidad")]
    public float fps = 4f;

    RectTransform rt;
    float timer;
    float desincronizacion;
    int ultimoIndice = -1;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        desincronizacion = Random.Range(0f, 1f / fps);
        timer = desincronizacion;

        CambiarRotacionAleatoria();
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            CambiarRotacionAleatoria();
        }
    }

    void CambiarRotacionAleatoria()
    {
        int nuevoIndice;
        do
        {
            nuevoIndice = Random.Range(0, 3);
        } while (nuevoIndice == ultimoIndice);

        ultimoIndice = nuevoIndice;
        float rotacionDeseada = 0f;

        if (nuevoIndice == 0) rotacionDeseada = angulo1;
        else if (nuevoIndice == 1) rotacionDeseada = angulo2;
        else if (nuevoIndice == 2) rotacionDeseada = angulo3;

        Vector3 rotActual = rt.localEulerAngles;
        rotActual.z = rotacionDeseada;
        rt.localEulerAngles = rotActual;
    }
}