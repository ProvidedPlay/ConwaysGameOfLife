using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushManager : MonoBehaviour
{
    
    public SpriteRenderer activeBrushRenderer;
    public bool brushCursorActive;

    public BrushData activeBrushData;
    public BrushData[] allBrushDataObjects;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void InitializeBrushManager()
    {
        SelectBrushType(0);
    }
    public void ToggleBrushCursor(bool cursorActive)
    {
        brushCursorActive = cursorActive;
        activeBrushRenderer.sprite = cursorActive ? activeBrushData.brushImage : null;
        Debug.Log("Just applied activeBrushData's brush image to brsuh render sprite");
    }
    public void SelectBrushType(int brushIndex)
    {
        activeBrushData = allBrushDataObjects[brushIndex];
        Debug.Log("just set the active brush data");
    }
    public void ShowBrushAtMousePosition(Vector3Int mousePosition)
    {
        if (brushCursorActive && activeBrushData.brushImage != null && activeBrushRenderer != null)
        {
            activeBrushRenderer.transform.position = mousePosition;
        }
    }
}
