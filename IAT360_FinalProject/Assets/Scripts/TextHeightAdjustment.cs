using System;
using TMPro;
using UnityEngine;

public class TextHeightAdjustment : MonoBehaviour
{
    private TextMeshProUGUI _tmpText; // 绑定 TextMeshPro 对象
    private RectTransform _contentRect; // 绑定 Content 的 RectTransform

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
