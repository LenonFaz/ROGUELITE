using UnityEngine;

public class TagEstilo : MonoBehaviour
{
    public float rotacionMax = 4f;
    public float desfaseYMax = 6f;
    public bool temblor = true;
    public float fps = 6f;
    public float temblorRot = 1f;
    public float temblorPos = 1.5f;

    RectTransform rt;
    Vector2 posBase;
    float rotBase;
    float timer;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        rotBase = Random.Range(-rotacionMax, rotacionMax);
        posBase = rt.anchoredPosition + new Vector2(0, Random.Range(-desfaseYMax, desfaseYMax));
        Aplicar(0, Vector2.zero);
    }

    void Update()
    {
        if (!temblor) return;
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            Aplicar(Random.Range(-temblorRot, temblorRot),
                    new Vector2(Random.Range(-temblorPos, temblorPos),
                                Random.Range(-temblorPos, temblorPos)));
        }
    }

    void Aplicar(float extraRot, Vector2 extraPos)
    {
        rt.anchoredPosition = posBase + extraPos;
        rt.localRotation = Quaternion.Euler(0, 0, rotBase + extraRot);
    }
}