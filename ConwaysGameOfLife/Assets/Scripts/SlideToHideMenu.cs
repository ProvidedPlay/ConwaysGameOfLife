using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideToHideMenu : MonoBehaviour
{
    public RectTransform rectTransform;

    public Vector2 hiddenPosition;
    public Vector2 shownPosition;

    public float defaultSlideDuration;

    public string identifier;
    public void SlideToHide(bool hideObject)
    {
        //rectTransform.anchoredPosition = hideObject ? hiddenPosition : shownPosition;
        Vector2 newPosition = hideObject ? new Vector2 (rectTransform.position.x + hiddenPosition.x, rectTransform.position.y + hiddenPosition.y) : new Vector2 (rectTransform.position.x + shownPosition.x, rectTransform.position.y + shownPosition.y);
        rectTransform.DOMove(newPosition, defaultSlideDuration);
    }
    public void SlideToHide(bool hideObject, SlideToHideMenu linkedUIElement)
    {
        //rectTransform.anchoredPosition = hideObject ? hiddenPosition : shownPosition;
        Vector2 newPosition = hideObject ? new Vector2 (rectTransform.position.x + hiddenPosition.x, rectTransform.position.y + hiddenPosition.y) : new Vector2 (rectTransform.position.x + shownPosition.x, rectTransform.position.y + shownPosition.y);
        rectTransform.DOMove(newPosition, defaultSlideDuration).OnComplete(() => linkedUIElement.SlideToHide(!hideObject));
    }
}
