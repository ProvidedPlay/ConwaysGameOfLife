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
        Debug.Log(identifier + " local position is " + rectTransform.localPosition);
        Vector2 newPosition = hideObject ? new Vector2 (rectTransform.localPosition.x + hiddenPosition.x, rectTransform.localPosition.y + hiddenPosition.y) : new Vector2 (rectTransform.localPosition.x + shownPosition.x, rectTransform.localPosition.y + shownPosition.y);
        rectTransform.DOLocalMove(newPosition, defaultSlideDuration);
    }
    public void SlideToHide(bool hideObject, SlideToHideMenu linkedUIElement)
    {
        //rectTransform.anchoredPosition = hideObject ? hiddenPosition : shownPosition;
        Vector2 newPosition = hideObject ? new Vector2 (rectTransform.localPosition.x + hiddenPosition.x, rectTransform.localPosition.y + hiddenPosition.y) : new Vector2 (rectTransform.localPosition.x + shownPosition.x, rectTransform.localPosition.y + shownPosition.y);
        rectTransform.DOLocalMove(newPosition, defaultSlideDuration).OnComplete(() => linkedUIElement.SlideToHide(!hideObject));
    }
}
