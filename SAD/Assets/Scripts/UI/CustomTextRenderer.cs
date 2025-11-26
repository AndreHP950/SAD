using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
[ExecuteInEditMode]
public class CustomTextRenderer : MonoBehaviour
{
    [Serializable]
    public class Char
    {
        public char character;
        public Sprite sprite;
    }

    public Char[] characters;
    public Image[] images;

    public float alignment; //left center right
    public float spaceWidth = 0.1f;
    public float emptyCharacterWidth = 1;
    public float basePixels = 100f;

    private TMP_Text tmpText;
    private string lastText;

    public string Text
    {
        get => tmpText != null ? tmpText.text : string.Empty;
        set
        {
            if (tmpText != null)
            {
                tmpText.text = value;
                RefreshText();
            }
        }
    }

    public void OnEnable()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            lastText = tmpText.text;
            RefreshText();
        }
    }

    private void Update()
    {
        if (tmpText != null && tmpText.text != lastText)
        {
            lastText = tmpText.text;
            RefreshText();
        }
    }

    private void OnValidate()
    {
        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            lastText = tmpText.text;
            RefreshText();
        }
    }

    private void RefreshText()
    {
        if (tmpText == null) return;
        var chars = tmpText.text.ToCharArray();
        var size = 0f;
        foreach (var t in chars)
        {
            var c = Array.Find(characters, x => x.character == t);
            if (c != null)
                size += c.sprite.rect.width / c.sprite.rect.height;
            else
                size += emptyCharacterWidth;
        }

        size += (chars.Length - 1) * spaceWidth;
        var pos = Mathf.Lerp(0, -size, (alignment + 1) / 2f);
        for (var i = 0; i < images.Length; i++)
        {
            if (i >= chars.Length)
            {
                images[i].enabled = false;
                continue;
            }

            var c = Array.Find(characters, x => x.character == chars[i]);
            if (c != null)
            {
                var width = c.sprite.rect.width / c.sprite.rect.height;
                pos += width / 2;
                images[i].sprite = c.sprite;
                images[i].rectTransform.sizeDelta = new Vector2(basePixels, basePixels);
                images[i].rectTransform.anchoredPosition = new Vector2(pos * basePixels, 0);
                images[i].enabled = true;
                pos += width / 2;
            }
            else
            {
                images[i].enabled = false;
                pos += emptyCharacterWidth;
            }

            pos += spaceWidth;
        }
    }
}