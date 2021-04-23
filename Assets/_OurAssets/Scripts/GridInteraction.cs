using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridInteraction : MonoBehaviour
{
    public Camera mainCam;

    public enum SelectionMode { Build, Select}
    public SelectionMode currentSelectionMode;

    public Transform selectionTransform;

    Cell currentCell;
    Cell selectedCell;
    RaycastHit2D hit;

    void Start()
    {
        if(mainCam == null)
        {
            mainCam = Camera.main;
        }
    }

    void Update()
    {
        CheckCurrentCell();
        if(currentSelectionMode == SelectionMode.Build)
        {
            CheckInput();
        }
        else if(currentSelectionMode == SelectionMode.Select)
        {
            CheckSelection();
            if(selectedCell != null)
            {
                selectionTransform.position = selectedCell.transform.position;
            }
        }
    }

    #region Current Cell Raycast
    void CheckCurrentCell()
    {
        hit = Physics2D.Raycast(mainCam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        bool removeCurrentCell = false;
        if (hit.collider != null)
        {
            if (hit.transform.CompareTag("Selectable"))
            {
                Cell hitCell = hit.transform.GetComponent<Cell>();
                if (currentCell != null && hitCell != currentCell)
                {
                    currentCell.ToggleHighlight(false);

                    if (!currentCell.hasStructure)
                    {
                        SetCurrentCell(hitCell, currentSelectionMode == SelectionMode.Build);
                    }
                    else
                    {
                        removeCurrentCell = true;
                    }
                }
                else if (currentCell == null & !hitCell.hasStructure)
                {
                    SetCurrentCell(hitCell, currentSelectionMode == SelectionMode.Build);
                }
            }
            else if (currentCell != null)
            {
                removeCurrentCell = true;
            }
        }
        else
        {
            if (currentCell != null)
            {
                removeCurrentCell = true;
            }
        }

        if (removeCurrentCell)
        {
            currentCell.ToggleHighlight(false);
            currentCell = null;
        }
    }
    void SetCurrentCell(Cell cell, bool highlight)
    {
        currentCell = cell;
        if (highlight)
        {
            if (currentCell.placeableArea)
                currentCell.ToggleHighlight(true);
            else
                currentCell.ToggleRedHighlight();
        }
    }
    #endregion

    #region BuildMode
    //Called From UI Buttons
    public void ActivateBuildMode()
    {
        if(selectedCell != null)
        {
            RemoveSelected();
        }
    }

    void CheckInput()
    {
        if (Input.GetMouseButtonDown(0) && currentCell != null && currentCell.placeableArea &! currentCell.hasStructure)
        {
            currentCell.SetStructure(0);
            currentCell.ToggleHighlight(false);
            currentCell = null;
        }
    }
    #endregion

    #region SelectionMode
    //Called From UI Buttons
    public void ActivateSelectMode()
    {

    }

    void CheckSelection()
    {
        if (Input.GetMouseButtonDown(0) && currentCell != null && currentCell.hasStructure)
        {
            selectedCell = currentCell;
            selectedCell.Select();
        }
    }

    void RemoveSelected()
    {
        selectedCell.Deselect();
        selectedCell = null;
        selectionTransform.position = Vector3.down * 100;
    }
    #endregion
}
