// GestorLuz.cs
using UnityEngine;
using UnityEngine.UI;

public class GestorLuz : MonoBehaviour
{
    public static GestorLuz I;

    public float max = 100f;
    public float tasa = 5f;
    public float killDrop = 15f;

    public Slider ui;

    public float actual { get; private set; }
    bool fin;

    void Awake() => I = this;

    void Start()
    {
        if (ui) ui.maxValue = max;
    }

    void Update()
    {
        if (fin) return;

        actual += tasa * Time.deltaTime;
        if (ui) ui.value = actual;

        if (actual >= max)
        {
            fin = true;
            Debug.Log("GameOver");
        }
    }

    public void OnKill()
    {
        if (fin) return;
        actual = Mathf.Max(0, actual - killDrop);
    }
}
