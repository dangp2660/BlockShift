using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GridCell[,] gridCells;

    void Awake()
    {
        LoadExistingGrid();
    }
    private void Update()
    {
        CheckForMatch();
    }

    public void LoadExistingGrid()
    {
        GridCell[] allCells = FindObjectsOfType<GridCell>();
        if (allCells.Length == 0)
        {
            Debug.LogWarning("Không có GridCell nào trong scene!");
            return;
        }

        // Tự động tính width và height
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (var c in allCells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }

        int width = maxX + 1;
        int height = maxY + 1;

        gridCells = new GridCell[width, height];
        foreach (var c in allCells)
        {
            gridCells[c.x, c.y] = c;
        }

        Debug.Log($"Grid đã load: {width} x {height} ({allCells.Length} ô)");
    }//LoadExistingGrid

    public GridCell GetCell(int x, int y)
    {
        if (gridCells == null) return null;
        if (x < 0 || y < 0 || x >= gridCells.GetLength(0) || y >= gridCells.GetLength(1))
            return null;

        return gridCells[x, y];
    }//GridCell

        public void CheckForMatch()
        {
            List<Block> blockesToRemvoe = new List<Block>();
            bool[,] visted = new bool[gridCells.GetLength(0), gridCells.GetLength(1)];
            for (int x = 0; x < gridCells.GetLength(0); x++)
            {
                for (int y = 0; y< gridCells.GetLength(1);y++)
                {
                    GridCell cell = gridCells[x, y];
                    if(cell == null || !cell.isOccupied) continue;
                    Block block = cell.currentCube.GetComponent<Block>();
                    if(block == null || visted[x,y]) continue;
                    List<Block> connected = FloodFill(x, y, block.blockID, visted);
                    if(connected.Count >= 2)
                    {
                        blockesToRemvoe.AddRange(connected);
                    }
                }
            }
            foreach (Block block in blockesToRemvoe)
            {
                if(block.currentCell != null)
                {
                    block.currentCell.isOccupied = false;
                }
                Debug.Log($"Block at {block.currentCell.x}-{block.currentCell.y} is destroyed");
                Destroy(block.gameObject);
            }

        }//checkForMatch

        private List<Block> FloodFill (int startX, int startY, int colorId, bool[,] visited)
        {
            List<Block> result = new List<Block>();
            Queue<Vector2Int> quene = new Queue<Vector2Int>();
            quene.Enqueue(new Vector2Int(startX, startY));

            int width = gridCells.GetLength(0);
            int height = gridCells.GetLength(1);

            while (quene.Count > 0)
            {
                var pos = quene.Dequeue();
                int x = pos.x;
                int y = pos.y;

                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                if(visited[x, y]) continue;

                GridCell cell = gridCells[x, y];
                if(cell == null || !cell.isOccupied) continue;

                Block block = cell.currentCube?.GetComponent<Block>();

                if(block == null || block.blockID != colorId) continue;

                visited[x, y] = true;
                result.Add(block);

                quene.Enqueue(new Vector2Int(x + 1, y));
                quene.Enqueue(new Vector2Int(x - 1, y));
                quene.Enqueue(new Vector2Int(x, y + 1));
                quene.Enqueue(new Vector2Int(x, y - 1));
            }//end while

            return result;
        }//FloodFill
}
