using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        GridCell[] allCells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        if (allCells.Length == 0)
        {
            Debug.LogWarning("There are no GridCells in the scene!");
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
                if(block == null || visted[x,y] || block.isPopping) continue;
                List<Block> connected = FloodFill(x, y, block.blockMaterialData.colorID, visted);
                if(connected.Count >= 2)
                {
                    StartCoroutine(MarchAndPop(connected));
                }
            }
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

            if(block == null || block.blockMaterialData.colorID != colorId) continue;

            visited[x, y] = true;
            result.Add(block);

            quene.Enqueue(new Vector2Int(x + 1, y));
            quene.Enqueue(new Vector2Int(x - 1, y));
            quene.Enqueue(new Vector2Int(x, y + 1));
            quene.Enqueue(new Vector2Int(x, y - 1));
        }//end while

        return result;
    }//FloodFill

    private IEnumerator MarchAndPop(List<Block> blocks)
    {
        Vector3 center = Vector3.zero;
        foreach (var block in blocks)
        {
            center += block.transform.position;
        }
        center /= blocks.Count;

        float mearchDuartion = 0.3f;
        float time = 0;
        List<Vector3> startPos = new List<Vector3>();
        List<Vector3> startScaled = new List<Vector3>();
        foreach (var block in blocks)
        {
            startPos.Add(block.transform.position);
            startScaled.Add(block.transform.localScale);
        }
        while (time < mearchDuartion)
        {
            time += Time.deltaTime;
            float progress = time / mearchDuartion;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                {
                    blocks[i].transform.position = Vector3.Lerp(startPos[i],center, progress);
                    blocks[i].transform.localScale = Vector3.Lerp(startScaled[i], startScaled[i] * 0.8f, progress);
                }
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.1f);
        foreach (var block in blocks)
        {
            if (block != null && !block.isPopping)
            {
                block.PlayPopEffect();
            }
        }
    }//MarchAndPop


}
