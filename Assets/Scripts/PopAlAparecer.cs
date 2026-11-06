using UnityEngine;

public class PopAlAparecer : MonoBehaviour
{
    public float[] escalas = { 0f, 1.25f, 0.95f, 1f };
    public float fps = 12f;

    float timer;
    int paso;

    void OnEnable()
    {
        paso = 0;
        timer = 0f;
        transform.localScale = Vector3.one * escalas[0];
    }

    void Update()
    {
        if (paso >= escalas.Length) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            paso++;
            if (paso < escalas.Length)
                transform.localScale = Vector3.one * escalas[paso];
        }
    }
}