using UnityEngine;

public class Aturdido : MonoBehaviour
{
    public float duracion = 1.2f;
    private float timer = 0f;

    public bool EstaAturdido => timer > 0f;

    public void Aturdir() => Aturdir(duracion);

    public void Aturdir(float t)
    {
        timer = Mathf.Max(timer, t);
    }

    void Update()
    {
        if (timer > 0f) timer -= Time.deltaTime;
    }
}