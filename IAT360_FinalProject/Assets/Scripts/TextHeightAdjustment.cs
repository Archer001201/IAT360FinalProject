using System;
using TMPro;
using UnityEngine;

public class TextHeightAdjustment : MonoBehaviour
{
    private TextMeshProUGUI _tmpText;
    private RectTransform _contentRect;

    private void Awake()
    {
        _tmpText = GetComponent<TextMeshProUGUI>();
        _contentRect = GetComponent<RectTransform>();
    }

    public void UpdateText(string text)
    {
        _tmpText.text = text;
        AdjustHeight();
    }

    private void AdjustHeight()
    {
        var preferredHeight = _tmpText.GetPreferredValues().y;
        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, preferredHeight);
    }
}
