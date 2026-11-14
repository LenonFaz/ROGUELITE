using UnityEngine;
using UnityEngine.UI;

public class Detect : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private float intensity = 8f;
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private Gradient gradient;

    private RectTransform _rect;
    private Vector2 _origen;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _origen = _rect.anchoredPosition;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        float v = curve.Evaluate(image.fillAmount);

        _rect.anchoredPosition = _origen + Random.insideUnitCircle * (intensity * v);

        if (gradient != null) image.color = gradient.Evaluate(v);
    }

    private void OnDisable()
    {
        if (_rect != null) _rect.anchoredPosition = _origen;
    }
}