using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour, ISelectable
{
    public SpriteRenderer spriteRenderer;
    public Renderer cellHighlightRend;
    public Color redHighlightColor;
    public Color highlightColor;
    public Color hiddenColor;
    public bool placeableArea = true;
    internal bool hasStructure;


    //internal Structure currentStructure;

    public void ToggleRedHighlight()
    {
        cellHighlightRend.material.color = redHighlightColor;
    }

    public void ToggleHighlight(bool toggle)
    {
        cellHighlightRend.material.color = toggle ? highlightColor : hiddenColor;
    }

    public void SetStructure(int structureID)
    {
        hasStructure = true;
        spriteRenderer.sprite = GameManager.singleton.structureProperties[structureID].structureSprite[0];
    }

    public void Select()
    {
        throw new System.NotImplementedException();
    }

    public void Deselect()
    {
        throw new System.NotImplementedException();
    }
}
