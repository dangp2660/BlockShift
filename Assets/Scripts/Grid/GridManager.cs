using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Info")]
    public GridCell[,] gridCells;

    [Header("Match Settings")]
    public int minGroupSize = 2;
    public bool debugMatchLogs = true;

    private bool[,] visitedSub;
    private bool pendingMatchCheck = false;
    private float pendingDelayTimer = 0f;
    private bool isResolvingMatches = false;
    private int activePopCoroutines = 0;

    void Awake()
    {
        LoadExistingGrid();
        RequestMatchCheck(0f); // schedule initial check once
    }

    void Update()
    {
        // Only check when scheduled and not currently resolving animations/pops
        if (pendingMatchCheck && !isResolvingMatches)
        {
            if (pendingDelayTimer > 0f)
            {
                pendingDelayTimer -= Time.deltaTime;
            }
            else
            {
                if (debugMatchLogs) Debug.Log("[GridManager] Running CheckForMatch");
                pendingMatchCheck = false;
                CheckForMatch();
            }
        }
    }

    public void RequestMatchCheck(float delay = 0f)
    {
        pendingMatchCheck = true;
        pendingDelayTimer = delay;
        if (debugMatchLogs) Debug.Log($"[GridManager] Scheduled match check in {delay:0.00}s");
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

        bool foundAnyGroup = false;
        int groupsFound = 0;
        List<int> groupSizes = new List<int>();

        for (int cx = 0; cx < width; cx++)
        {
            for (int cy = 0; cy < height; cy++)
            {
                GridCell cell = gridCells[cx, cy];
                if (cell == null || cell.currentHolder == null) continue;

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
                            foundAnyGroup = true;
                            groupsFound++;
                            groupSizes.Add(connected.Count);
                            if (debugMatchLogs)
                                Debug.Log($"[GridManager] Match group color {start.blockMaterialData.colorID} size {connected.Count}");

                            activePopCoroutines++;
                            isResolvingMatches = true;
                            StartCoroutine(MergeAndPop(connected));
                        }
                    }
                }
            }
        }

        if (!foundAnyGroup)
        {
            isResolvingMatches = false;
            if (debugMatchLogs) Debug.Log("[GridManager] No groups found");
        }
        else
        {

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
        {
            activePopCoroutines = Mathf.Max(0, activePopCoroutines - 1);
            if (activePopCoroutines == 0) { isResolvingMatches = false; }
            yield break;
        }

        HashSet<BlockHolder> affectedHolders = new HashSet<BlockHolder>();
        foreach (var block in blocks)
        {
            if (block == null) continue;
            var holder = block.GetComponentInParent<BlockHolder>();
            if (holder != null) affectedHolders.Add(holder);
        }

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

        // Allow destroyed blocks to unregister from holders
        yield return new WaitForSeconds(0.35f);

        // Rescale all holders across the whole grid (not just affected ones)
        RescaleAllHolders(true);

        // Decrement and unblock further checks
        activePopCoroutines = Mathf.Max(0, activePopCoroutines - 1);
        isResolvingMatches = activePopCoroutines > 0 ? true : false;

        // Unconditionally schedule a re-check for follow-up matches
        RequestMatchCheck(0.1f);
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
        if (cell == null) return null;

        // Prefer holder lookup, fallback to subCellBlocks when holder is missing
        if (cell.currentHolder != null)
            return cell.currentHolder.GetBlockInSubCell(new Vector2Int(sx, sy));

        return cell.subCellBlocks[sx, sy];
    }
    private void RescaleAllHolders(bool animate)
    {
        if (gridCells == null) return;
    
        int width = gridCells.GetLength(0);
        int height = gridCells.GetLength(1);
    
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = gridCells[x, y];
                var holder = cell?.currentHolder;
                if (holder != null)
                {
                    holder.RecalculateChildScale(animate);
                    holder.RecalculateTilt(animate);
                }
            }
        }
    }

    public GridCell FindLowestFreeCellInColumn(int colX)
    {
        if (gridCells == null) return null;
        int width = gridCells.GetLength(0);
        int height = gridCells.GetLength(1);
        if (colX < 0 || colX >= width) return null;

        // y=0 is bottom; scan upwards for the first free cell
        for (int y = 0; y < height; y++)
        {
            var cell = gridCells[colX, y];
            if (cell != null && !cell.isOccupied)
                return cell;
        }
        return null;
    }

    public bool TryPlaceAndSettle(BlockHolder holder, GridCell targetCell, float duration = 0.2f)
    {
        if (holder == null || targetCell == null || gridCells == null) return false;

        // Place exactly at the selected cell if it's free (no column gravity)
        if (targetCell.isOccupied) return false;

        StartCoroutine(PlaceAndSettleRoutine(holder, targetCell, duration));
        return true;
    }

    private IEnumerator PlaceAndSettleRoutine(BlockHolder holder, GridCell dest, float duration)
    {
        Vector3 start = holder.transform.position;
        Vector3 end = dest.transform.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            holder.transform.position = Vector3.Lerp(start, end, p);
            yield return null;
        }

        holder.transform.SetParent(dest.transform, true);
        holder.transform.localPosition = Vector3.zero;
    
        // Register to cell and update occupancy; BlockHolder will schedule match check.
        holder.AssignToCell(dest);

        // Slight delay to allow visuals to settle before next checks
        RequestMatchCheck(0.05f);
    }
}
