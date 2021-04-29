using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteraction : MonoBehaviour
{
    public static GridInteraction singleton;
    public Camera mainCam;

    public enum SelectionMode { Build, Select}
    public SelectionMode currentSelectionMode;

    public Transform selectionTransform;
    public Transform hoverSelectionTransform;

    internal Cell currentCell;
    internal List<Cell> currentPlacementCells = new List<Cell>();
    Cell selectedCell;
    RaycastHit2D hit;

    List<Cell> currentStructures = new List<Cell>();
    List<Cell> currentPowerStructures = new List<Cell>();

    private void Awake()
    {
        if(singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    void Start()
    {
        if(mainCam == null)
        {
            mainCam = Camera.main;
        }
    }

    void Update()
    {
        CheckMouseCell();
        if(currentSelectionMode == SelectionMode.Build)
        {
            CheckInput();
        }
        else if(currentSelectionMode == SelectionMode.Select)
        {
            CheckSelection();
            if(selectedCell != null)
            {
                selectionTransform.position = selectedCell.GetMidPoint();
                selectionTransform.localScale = Vector3.one * .75f * (selectedCell.structureProperty.size + ((selectedCell.structureProperty.size - 1) * .25f));
            }
        }
    }

    #region Current Cell Raycast

    void CheckMouseCell()
    {
        hit = Physics2D.Raycast(mainCam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider != null && hit.transform.CompareTag("Selectable"))
        {
            Cell hitCell = hit.transform.GetComponent<Cell>();
            if(hitCell != currentCell)
            {
                SetCurrentCell(hitCell);
            }
        }
        else if (currentCell != null)
        {
            if(currentPlacementCells.Count > 0)
            {
                ToggleCellHighlights(false);
                currentPlacementCells.Clear();
            }
            currentCell = null;
        }
    }

    void SetCurrentCell(Cell cell)
    {

        ToggleCellHighlights(false);
        currentPlacementCells.Clear();

        currentCell = cell;

        if(currentSelectionMode == SelectionMode.Build)
        {
            currentPlacementCells.Add(currentCell);
            if(GameManager.currentStructure.size > 1)
            {
                if (!BuildableArea())
                {
                    currentCell.SetBuildable(false);
                }
            }
            else
            {
                ToggleCellHighlights(true);
            }
        }
        else if(currentCell.hasStructure || currentCell.isChild)
        {
            if (currentCell.isChild)
            {
                currentCell = currentCell.parentCell;
            }


            hoverSelectionTransform.position = currentCell.GetMidPoint();
            hoverSelectionTransform.localScale = Vector3.one * .75f * (currentCell.structureProperty.size + ((currentCell.structureProperty.size - 1) * .25f));
        }
        else
        {
            hoverSelectionTransform.position = Vector3.down * 1000;
        }
    }

    void ToggleCellHighlights(bool toggle)
    {
        for (int i = 0; i < currentPlacementCells.Count; i++)
        {
            if (!toggle)
            {
                currentPlacementCells[i].DisableHighlight();
            }
            else
            {
                currentPlacementCells[i].DetermineBuildable();
            }
        }
    }

    bool BuildableArea()
    {
        if(currentCell.y + GameManager.currentStructure.size - 1 >= Grid.singleton.cellArray.Length)
        {
            Debug.Log("Rows out of range");
            currentCell.SetBuildable(false);
            return false;
        }

        if (currentCell.x + GameManager.currentStructure.size - 1 >= Grid.singleton.cellArray[currentCell.y + GameManager.currentStructure.size - 1].cells.Length)
        {
            Debug.Log("Cols out of range");
            currentCell.SetBuildable(false);
            return false;
        }

        Cell rightCell = Grid.singleton.cellArray[currentCell.y].cells[currentCell.x + 1];
        Cell topCell = Grid.singleton.cellArray[currentCell.y + 1].cells[currentCell.x];
        Cell diagCell = Grid.singleton.cellArray[currentCell.y + 1].cells[currentCell.x + 1];

        if(GameManager.currentStructure.size == 3)
        {
            currentPlacementCells.Add(Grid.singleton.cellArray[currentCell.y].cells[currentCell.x + 2]);
            currentPlacementCells.Add(Grid.singleton.cellArray[currentCell.y + 1].cells[currentCell.x + 2]);

            currentPlacementCells.Add(Grid.singleton.cellArray[currentCell.y + 2].cells[currentCell.x]);
            currentPlacementCells.Add(Grid.singleton.cellArray[currentCell.y + 2].cells[currentCell.x + 1]);
            currentPlacementCells.Add(Grid.singleton.cellArray[currentCell.y + 2].cells[currentCell.x + 2]);
        }

        currentPlacementCells.Add(rightCell);
        currentPlacementCells.Add(topCell);
        currentPlacementCells.Add(diagCell);

        ToggleCellHighlights(true);

        return true;
    }
    #endregion

    #region Build Mode
    public void ActivateBuildMode()
    {
        if(selectedCell != null)
        {
            RemoveSelected();
        }
        selectionTransform.position = Vector3.down * 1000;

        for (int i = 0; i < currentStructures.Count; i++)
        {
            if (currentStructures[i].structureProperty.powerStructure)
            {
                currentStructures[i].ShowRange();
            }
        }

        currentSelectionMode = SelectionMode.Build;
    }

    void CheckInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ActivateSelectMode();

        }
        if (Input.GetMouseButtonUp(0) && currentCell != null && CanBuildStructure() && !EventSystem.current.IsPointerOverGameObject())
        {
            BuildCurrentCell();
        }
    }

    bool CanBuildStructure()
    {
        for (int i = 0; i < currentPlacementCells.Count; i++)
        {
            if (!currentPlacementCells[i].buildable)
            {
                return false;
            }
        }
        return true;
    }

    void BuildCurrentCell()
    {
        if (GameManager.currentStructure.size > 1)
        {
            currentPlacementCells[0].childrenCells = new Cell[GameManager.currentStructure.size == 2 ? 4 : 9];
            for (int i = 0; i < currentPlacementCells.Count; i++)
            {
                currentPlacementCells[i].SetBuildable(false);
                currentPlacementCells[0].childrenCells[i] = currentPlacementCells[i];

                if (i == 0)
                {
                    currentPlacementCells[i].isParent = true;
                }
                else
                {
                    currentPlacementCells[i].isChild = true;
                    currentPlacementCells[i].parentCell = currentCell;
                }
            }
        }

        if (GameManager.currentStructure.powerStructure)
        {
            currentPowerStructures.Add(currentCell);
        }

        GameManager.singleton.StructurePlaced();

        currentCell.SetStructure(GameManager.singleton.currentStructureID);
        currentStructures.Add(currentCell);


        for (int i = 0; i < currentStructures.Count; i++)
        {
            if (currentStructures[i].structureProperty.powerStructure)
            {
                currentStructures[i].ShowRange();
            }
        }

        ActivateSelectMode();
    }

    #endregion

    #region Selection Mode
    public void ActivateSelectMode()
    {
        GameManager.singleton.placeholder.RemovePlaceholder();
        if (currentCell != null)
        {
            currentCell.DisableHighlight();
            currentCell = null;
        }

        for (int i = 0; i < currentStructures.Count; i++)
        {
            if (currentStructures[i].structureProperty.powerStructure)
            {
                currentStructures[i].HideRange();
            }
        }

        currentSelectionMode = SelectionMode.Select;
    }

    void CheckSelection()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {

            if(selectedCell != null)
            {
                RemoveSelected();
            }

            if (currentCell != null && (currentCell.hasStructure || currentCell.isChild))
            {
                if (currentCell.isChild)
                {
                    selectedCell = currentCell.parentCell;
                }
                else
                {
                    selectedCell = currentCell;
                }
                selectedCell.Select();
            }
        }
    }

    void RemoveSelected()
    {
        selectedCell.Deselect();
        selectedCell = null;
        selectionTransform.position = Vector3.down * 1000;
    }

    internal void SellSelected()
    {
        if (selectedCell.isParent)
        {
            for (int i = 0; i < selectedCell.childrenCells.Length; i++)
            {
                currentStructures.Remove(selectedCell.childrenCells[i]);
                currentPowerStructures.Remove(selectedCell.childrenCells[i]);
            }
        }

        currentStructures.Remove(selectedCell);
        currentPowerStructures.Remove(selectedCell);
        RemoveSelected();
    }
    #endregion

    #region Public Helpers
    public Vector3 GetCurrentPlacementPosition()
    {
        Vector3 returnVector = Vector3.zero;
        for (int i = 0; i < currentPlacementCells.Count; i++)
        {
            returnVector += currentPlacementCells[i].transform.position;
        }

        return returnVector == Vector3.zero ? returnVector : returnVector / currentPlacementCells.Count;
    }
    #endregion
}
