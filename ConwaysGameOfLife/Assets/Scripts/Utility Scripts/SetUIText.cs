using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class SetUIText
{
    public static void SetText(TextMeshProUGUI textComponent, string text)
    {
        textComponent.SetText(text);
    }
}
