using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Info")]
    public GridCell[,] gridCells;

    [Header("Match Settings")]
    public int minGroupSize = 2;

    private bool[,] visitedSub;

    void Awake()
    {
        LoadExistingGrid();
    }

    void Update()
    {
        CheckForMatch();
    }

    /// <summary>
    /// Tự động load toàn bộ GridCell có trong scene.
    /// </summary>
    public void LoadExistingGrid()
    {
        GridCell[] allCells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        if (allCells.Length == 0)
        {
            Debug.LogWarning("⚠️ There are no GridCells in the scene!");
            return;
        }

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

        Debug.Log($" Grid loaded: {width} x {height} ({allCells.Length} cells)");
    }

    public GridCell GetCell(int x, int y)
    {
        if (gridCells == null) return null;
        if (x < 0 || y < 0 || x >= gridCells.GetLength(0) || y >= gridCells.GetLength(1))
            return null;

        return gridCells[x, y];
    }

    /// <summary>
    /// Kiểm tra và xử lý các nhóm block trùng màu liền kề.
    /// </summary>
    public void CheckForMatch()
    {
        if (gridCells == null) return;

        int width = gridCells.GetLength(0);
        int height = gridCells.GetLength(1);

        int subW = width * 2;
        int subH = height * 2;
            visitedSub = new bool[subW, subH];

        for (int cx = 0; cx < width; cx++)
        {
            for (int cy = 0; cy < height; cy++)
            {
                GridCell cell = gridCells[cx, cy];
                if (cell == null || !cell.isOccupied || cell.currentHolder == null) continue;

                // Scan 2x2 subcells inside this cell
                for (int sx = 0; sx < 2; sx++)
                {
                    for (int sy = 0; sy < 2; sy++)
                    {
                        int gx = cx * 2 + sx;
                        int gy = cy * 2 + sy;
                        if (visitedSub[gx, gy]) continue;

                        Block start = GetBlockAtGlobalSubcell(gx, gy);
                        if (start == null || start.blockMaterialData == null || start.isPopping) continue;

                        List<Block> connected = FloodFillSubcells(gx, gy, start.blockMaterialData.colorID);

                        if (connected.Count >= minGroupSize)
                        {
                            foreach (var b in connected) b.isPopping = true;
                            StartCoroutine(MergeAndPop(connected));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// FloodFill tìm các block liền kề cùng màu.
    /// </summary>
    private List<Block> FloodFill(int startX, int startY, int colorId, bool[,] visited)
    {
        List<Block> result = new List<Block>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        int width = gridCells.GetLength(0);
        int height = gridCells.GetLength(1);

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            int x = pos.x;
            int y = pos.y;

            if (x < 0 || y < 0 || x >= width || y >= height) continue;
            if (visited[x, y]) continue;

            GridCell cell = gridCells[x, y];
            if (cell == null || !cell.isOccupied) continue;

            BlockHolder holder = cell.currentHolder;
            if (holder == null || !holder.HasColor(colorId)) continue;

            Block block = holder.GetBlockByColor(colorId);
            if (block == null) continue;

            visited[x, y] = true;
            result.Add(block);

            queue.Enqueue(new Vector2Int(x + 1, y));
            queue.Enqueue(new Vector2Int(x - 1, y));
            queue.Enqueue(new Vector2Int(x, y + 1));
            queue.Enqueue(new Vector2Int(x, y - 1));
        }

        return result;
    }

    /// <summary>
    /// Hiệu ứng gom nhóm rồi pop toàn bộ block.
    /// </summary>
    private IEnumerator MergeAndPop(List<Block> blocks)
    {
        if (blocks == null || blocks.Count == 0)
            yield break;

        // Tính trung tâm nhóm
        Vector3 center = Vector3.zero;
        foreach (var block in blocks)
        {
            if (block != null)
                center += block.transform.position;
        }
        center /= blocks.Count;

        float duration = 0.25f;
        float t = 0f;

        List<Vector3> startPos = new List<Vector3>();
        List<Vector3> startScale = new List<Vector3>();

        foreach (var block in blocks)
        {
            if (block != null)
            {
                startPos.Add(block.transform.position);
                startScale.Add(block.transform.localScale);
            }
            else
            {
                startPos.Add(Vector3.zero);
                startScale.Add(Vector3.one);
            }
        }

        // Gom dần về trung tâm (hiệu ứng giống Jelly Field)
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                {
                    blocks[i].transform.position = Vector3.Lerp(startPos[i], center, p);
                    blocks[i].transform.localScale = Vector3.Lerp(startScale[i], startScale[i] * 0.8f, p);
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Pop toàn bộ block trong nhóm
        foreach (var block in blocks)
        {
            if (block != null)
                block.PlayPopEffect();
        }

        Debug.Log($"Popped {blocks.Count} blocks!");
    }
    private List<Block> FloodFillSubcells(int startGX, int startGY, int colorId)
    {
        int subW = gridCells.GetLength(0) * 2;
        int subH = gridCells.GetLength(1) * 2;

        Block startBlock = GetBlockAtGlobalSubcell(startGX, startGY);
        if (startBlock == null || startBlock.blockMaterialData == null || startBlock.blockMaterialData.colorID != colorId)
            return new List<Block>();

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        HashSet<Block> resultBlocks = new HashSet<Block>();

        visitedSub[startGX, startGY] = true;
        q.Enqueue(new Vector2Int(startGX, startGY));
        resultBlocks.Add(startBlock);

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int ny = p.y + dy[i];
                if (nx < 0 || ny < 0 || nx >= subW || ny >= subH) continue;
                if (visitedSub[nx, ny]) continue;

                Block nb = GetBlockAtGlobalSubcell(nx, ny);
                if (nb == null || nb.blockMaterialData == null || nb.isPopping) continue;
                if (nb.blockMaterialData.colorID != colorId) continue;

                visitedSub[nx, ny] = true;
                q.Enqueue(new Vector2Int(nx, ny));
                resultBlocks.Add(nb);
            }
        }

        return new List<Block>(resultBlocks);
    }
    private Block GetBlockAtGlobalSubcell(int gx, int gy)
    {
        int cx = gx / 2;
        int cy = gy / 2;
        int sx = gx % 2;
        int sy = gy % 2;

        GridCell cell = GetCell(cx, cy);
        if (cell == null || !cell.isOccupied || cell.currentHolder == null) return null;

        return cell.currentHolder.GetBlockInSubCell(new Vector2Int(sx, sy));
    }
}
