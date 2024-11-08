using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    public CanvasScaler uiCanvasScaler;
    public RectTransform selectionBox;

    private void Awake()
    {
        UnpackReferences();
    }

    void UnpackReferences()
    {
        if (gameManager == null)
        {
            gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        }
        if (selectionBox == null)
        {
            selectionBox = GameObject.FindGameObjectWithTag("Selection Box").GetComponent<RectTransform>();
        }
        if (uiCanvasScaler == null)
        {
            uiCanvasScaler = GetComponent<CanvasScaler>();
        }
    }
}
