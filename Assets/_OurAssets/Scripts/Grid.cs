using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public static Grid singleton;
    public GameObject cellObject;
    public float cellSize;
    public float buffer = 0;
    public int rows, cols;
    public Vector3 offset = Vector3.zero;
    
    public Transform gridParent;
    public Transform structuresParent;
    public CellArray[] cellArray;
    //public Cell[,] currentCells;

    private void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
    }

    private void Start()
    {
        for (int k = 0; k < cellArray.Length; k++)
            for (int l = 0; l < cellArray[k].cells.Length; l++)
                cellArray[k].cells[l].DisableHighlight();
    }

    private void Update()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                cellArray[row].cells[col].transform.position = new Vector3(offset.x + (col * (cellSize + buffer)), offset.y + (row * (cellSize + buffer)), 0);
                cellArray[row].cells[col].transform.localScale = Vector3.one * cellSize;
            }
        }
    }

    [ContextMenu("Build Grid")]
    public void BuildGrid()
    {
        if(gridParent != null)
        {
            DestroyImmediate(gridParent.gameObject);
        }

        gridParent = new GameObject("GridParent").transform;
        gridParent.parent = transform;
        gridParent.transform.localPosition = Vector3.zero;
        cellArray = new CellArray[rows];
        for (int row = 0; row < rows; row++)
        {
            cellArray[row].cells = new Cell[cols];
            for (int col = 0; col < cols; col++)
            {
                GameObject cell = Instantiate(cellObject, new Vector3(offset.x + (col * (cellSize + buffer)), offset.y + (row * (cellSize + buffer)), 0), Quaternion.identity);
                cell.transform.parent = gridParent;
                cell.transform.localScale = Vector3.one * cellSize;
                cell.name = "Cell " + col + ", " + row;

                Cell newCell = cell.GetComponent<Cell>();
                cellArray[row].cells[col] = newCell;
                newCell.x = col;
                newCell.y = row;
                //Cell newCell = cell.GetComponent<Cell>();
                //newCell.ToggleHighlight(false);
            }
        }
    }
}

[System.Serializable]
public struct CellArray
{
    public Cell[] cells;
}
