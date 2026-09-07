// Spawner.cs
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject[] prefabs;
    public Transform[] puntos;
    public float rate = 3f;

    float t;

    void Update()
    {
        t -= Time.deltaTime;
        if (t <= 0f)
        {
            Spawn();
            t = rate;
        }
    }

    void Spawn()
    {
        if (prefabs.Length == 0 || puntos.Length == 0) return;

        var p = prefabs[Random.Range(0, prefabs.Length)];
        var pos = puntos[Random.Range(0, puntos.Length)].position;

        Instantiate(p, pos, Quaternion.identity);
    }
}
