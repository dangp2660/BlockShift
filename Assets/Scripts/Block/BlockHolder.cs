using System.Collections.Generic;
using UnityEngine;

public class BlockHolder : MonoBehaviour
{
    [Header("Prefab & Grid Settings")]
    public GameObject blockPrefab;
    public float cellSize = 1f;

    [Header("Child Block Layout")]
    public BlockMaterialLibrary library;
    public Vector2Int[] childOffsets;

    private readonly List<Block> childBlocks = new List<Block>();
    public GridCell parentCell;

    [Header("Spawn Count")]
    public SpawnCountOption spawnCount = SpawnCountOption.One;
    public enum SpawnCountOption { One = 1, Two = 2, Four = 4 }

    public enum TwoBlockMode { Auto, Horizontal, Vertical }
    public TwoBlockMode twoBlockMode = TwoBlockMode.Auto;

    private List<Vector2Int> allSubCells = new List<Vector2Int>()
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0),
            new Vector2Int(0,1),
            new Vector2Int(1,1)
        };
    public bool useSpawnCount = true;
    public bool tiltEnabled = true;

    [Header("Spawn/Binding")]
    public bool autoBindToNearestCell = true;

    void Start()
    {
        if (useSpawnCount)
        {
            SpawnCountOption[] options = new SpawnCountOption[] {
                SpawnCountOption.One,
                SpawnCountOption.Two,
                SpawnCountOption.Four
            };
            spawnCount = options[Random.Range(0, options.Length)];
            ConfigureChildOffsetsBySpawn();
        }

        GenerateBlocks();

        // Ensure parentCell is set and sub-cells get assigned
        if (parentCell == null)
            parentCell = GetComponentInParent<GridCell>();

        if (parentCell != null)
        {
            AssignToCell(parentCell);
        }
        else
        {
            AssignSubCells();
            if (autoBindToNearestCell)
                BindToNearestCellIfMissing(); // snap to grid and register
        }
        // Ensure occupancy/scale applied immediately at spawn
        UpdateCellOccupancy();
        RecalculateChildScale(false);
        RecalculateTilt(false);
    
        ScheduleMatchCheck(0f); // run one pass after spawning
    }

    public void GenerateBlocks()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Block prefab is missing!");
            return;
        }

        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        childBlocks.Clear();

        if (childOffsets == null || childOffsets.Length == 0)
            childOffsets = new Vector2Int[] { Vector2Int.zero };

        float spacing = cellSize * 0.25f;
        float baseScale = cellSize * 0.45f;
        float maxScale = cellSize * 0.95f;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var o in childOffsets)
        {
            if (o.x < minX) minX = o.x;
            if (o.x > maxX) maxX = o.x;
            if (o.y < minY) minY = o.y;
            if (o.y > maxY) maxY = o.y;
        }

        int widthCount = maxX - minX + 1;
        int heightCount = maxY - minY + 1;

        float avgX = 0, avgY = 0;
        foreach (var o in childOffsets) { avgX += o.x; avgY += o.y; }
        avgX /= childOffsets.Length;
        avgY /= childOffsets.Length;

        foreach (var offset in childOffsets)
        {
            Vector3 localPos = new Vector3(
                (offset.x - avgX) * spacing * 2,
                (offset.y - avgY) * spacing * 2,
                0);

            GameObject newBlock = Instantiate(blockPrefab, transform);
            newBlock.transform.localPosition = localPos;
            newBlock.transform.localRotation = Quaternion.identity;
            newBlock.transform.localScale = new Vector3(baseScale, baseScale, 1);

            Block blockComp = newBlock.GetComponent<Block>();
            if (blockComp == null) blockComp = newBlock.AddComponent<Block>();

            childBlocks.Add(blockComp);
        }

        float totalWidth = widthCount * baseScale + (widthCount - 1) * spacing * 2;
        float totalHeight = heightCount * baseScale + (heightCount - 1) * spacing * 2;

        float fitX = totalWidth < (cellSize * 0.9f) ? (cellSize * 0.9f) / totalWidth : 1f;
        float fitY = totalHeight < (cellSize * 0.9f) ? (cellSize * 0.9f) / totalHeight : 1f;

        fitX = Mathf.Min(fitX, maxScale / baseScale);
        fitY = Mathf.Min(fitY, maxScale / baseScale);

        foreach (var block in childBlocks)
        {
            Vector3 s = block.transform.localScale;
            s.x *= fitX;
            s.y *= fitY;
            block.transform.localScale = s;
        }
        SpawnUniqueBlocks();
    }

    public void SpawnUniqueBlocks()
    {
        if (library == null || library.materials.Count == 0)
        {
            Debug.LogError("Material library empty!");
            return;
        }

        List<BlockMaterialData> available = new List<BlockMaterialData>(library.materials);
        foreach (var block in childBlocks)
        {
            if (available.Count == 0) break;
            int index = Random.Range(0, available.Count);
            BlockMaterialData data = available[index];
            available.RemoveAt(index);
            block.SetMaterialBlock(data);
        }
    }

    public void AssignToCell(GridCell cell)
    {
        parentCell = cell;
        foreach (var block in childBlocks)
        {
            block.findCurrentCell(cell);
        }

        AssignSubCells();
        ScheduleMatchCheck(0f); // grid updates once placed
    }


    private void AssignSubCells()
    {
        // Allow assigning sub-cells even if parentCell is not set
        if (childBlocks == null || childBlocks.Count == 0)
            return;

        int blockCount = childBlocks.Count;
        int baseCount = allSubCells.Count / blockCount;
        int remainder = allSubCells.Count % blockCount;
        int index = 0;

        for (int i = 0; i < blockCount; i++)
        {
            int take = baseCount + (i < remainder ? 1 : 0);
            List<Vector2Int> assign = new List<Vector2Int>();

            for (int j = 0; j < take && index < allSubCells.Count; j++, index++)
                assign.Add(allSubCells[index]);

            childBlocks[i].subCellIn = assign;
        }

        // Apply layout rules after assignment
        EnsureOneBlockOccupiesAll();
        ExpandSoloRowWhenThree();
        ArrangeTwoBlockForm();

        // Keep grid state and visuals in sync immediately
        UpdateCellOccupancy();
        RepositionChildrenBySubcells();
        RecalculateChildScale(false);
        RecalculateTilt(false);
    }

    private void ConfigureChildOffsetsBySpawn()
    {
        switch (spawnCount)
        {
            case SpawnCountOption.One:
                childOffsets = new Vector2Int[] { Vector2Int.zero };
                break;

            case SpawnCountOption.Two:
                bool horizontal = twoBlockMode == TwoBlockMode.Auto
                    ? Random.value > 0.5f
                    : twoBlockMode == TwoBlockMode.Horizontal;

                childOffsets = horizontal
                    ? new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0) } // horizontal
                    : new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }; // vertical
                break;

            case SpawnCountOption.Four:
                childOffsets = new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };
                break;
        }
    }

    public void SetSpawnCount(SpawnCountOption count, TwoBlockMode mode = TwoBlockMode.Auto)
    {
        spawnCount = count;
        twoBlockMode = mode;

        ConfigureChildOffsetsBySpawn();
        GenerateBlocks();

        if (parentCell != null)
            AssignToCell(parentCell);
        ScheduleMatchCheck(0f); // update after respawn
    }

    public Block GetAnyBlock()
    {
        return childBlocks.Count > 0 ? childBlocks[0] : null;
    }

    public bool HasColor(int colorId)
    {
        return GetBlockByColor(colorId) != null;
    }

    public Block GetBlockByColor(int colorId)
    {
        foreach (var b in childBlocks)
        {
            if (b != null && b.blockMaterialData != null && b.blockMaterialData.colorID == colorId)
                return b;
        }
        return null;
    }

    public IReadOnlyList<Block> GetBlocks()
    {
        return childBlocks;
    }

    public Block GetBlockInSubCell(Vector2Int sub)
    {
        // sub is one of (0,0), (1,0), (0,1), (1,1)
        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null) continue;
            for (int i = 0; i < b.subCellIn.Count; i++)
            {
                if (b.subCellIn[i] == sub)
                    return b;
            }
        }
        return null;
    }

    public void NotifyChildDestroyed(Block b)
    {
        if (b == null) return;

        // Remove from local list
        for (int i = 0; i < childBlocks.Count; i++)
        {
            if (childBlocks[i] == b)
            {
                childBlocks.RemoveAt(i);
                break;
            }
        }

        // Reapply rules based on current count
        EnsureOneBlockOccupiesAll();
        ExpandSoloRowWhenThree();
        ArrangeTwoBlockForm();

        // Sync occupancy and visuals before next match pass
        UpdateCellOccupancy();
        RepositionChildrenBySubcells();
        RecalculateChildScale(true);
        RecalculateTilt(true);

        if (parentCell != null && childBlocks.Count == 0)
        {
            parentCell.isOccupied = false;
            parentCell.currentHolder = null;
        }

        ScheduleMatchCheck(0.05f);
    }
    public float tiltScaleDegrees = 20f;
    private Coroutine tiltCoroutine;

    public void RecalculateTilt(bool animate = true)
    {
        if (!tiltEnabled) return;

        float perSubScale = 0.9f / 2f; // 0.45 per subcell

        bool has00 = GetBlockInSubCell(new Vector2Int(0, 0)) != null;
        bool has01 = GetBlockInSubCell(new Vector2Int(0, 1)) != null;
        bool has10 = GetBlockInSubCell(new Vector2Int(1, 0)) != null;
        bool has11 = GetBlockInSubCell(new Vector2Int(1, 1)) != null;

        float leftColumn  = (has00 ? perSubScale : 0f) + (has01 ? perSubScale : 0f);
        float rightColumn = (has10 ? perSubScale : 0f) + (has11 ? perSubScale : 0f);
        float bottomRow   = (has00 ? perSubScale : 0f) + (has10 ? perSubScale : 0f);
        float topRow      = (has01 ? perSubScale : 0f) + (has11 ? perSubScale : 0f);

        // “or” -> take the larger of the two sums for each axis, capped at 0.9
        float targetX = Mathf.Min(0.9f, Mathf.Max(leftColumn, rightColumn));
        float targetY = Mathf.Min(0.9f, Mathf.Max(bottomRow, topRow));

        Vector3 targetScale = new Vector3(targetX, targetY, transform.localScale.z);

        if (!animate)
        {
            transform.localScale = targetScale;
            return;
        }

        if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
        tiltCoroutine = StartCoroutine(ScaleToSize(targetScale, 0.25f));
    }

    private System.Collections.IEnumerator ScaleToSize(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            transform.localScale = Vector3.Lerp(start, target, p);
            yield return null;
        }
        transform.localScale = target;
        tiltCoroutine = null;
    }

    public void ResetTilt()
    {
        if (!tiltEnabled) return;

        if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
        transform.localScale = new Vector3(1f, 1f, transform.localScale.z);
    }

    private void ScheduleMatchCheck(float delay = 0f)
    {
        var gm = Object.FindFirstObjectByType<GridManager>();
        if (gm == null) gm = Object.FindAnyObjectByType<GridManager>();
        if (gm != null) gm.RequestMatchCheck(delay);
    }

    private GridCell FindNearestCell()
    {
        GridCell nearest = null;
        float best = float.MaxValue;
        var cells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        Vector3 p = transform.position;
        foreach (var c in cells)
        {
            float d = (c.transform.position - p).sqrMagnitude;
            if (d < best) { best = d; nearest = c; }
        }
        return nearest;
    }

    private void BindToNearestCellIfMissing()
    {
        if (parentCell != null) return;
        var nearest = FindNearestCell();
        if (nearest != null)
        {
            // become a child of the GridCell and snap to center
            transform.SetParent(nearest.transform, true);
            transform.localPosition = Vector3.zero;
            AssignToCell(nearest); // sets holder on the cell and registers blocks
        }
    }

    private Coroutine scaleCoroutine;

    public void RecalculateChildScale(bool animate = true)
    {
        float oneSub = cellSize * 0.45f; // each subcell = 0.45
        float twoSubs = cellSize * 0.9f; // sum of 2 subcells = 0.9

        if (!animate)
        {
            foreach (var b in childBlocks)
            {
                if (b == null) continue;
                Transform childTransform = b.transform;
                Vector3 s = childTransform.localScale;

                int count = (b.subCellIn != null) ? b.subCellIn.Count : 0;
                if (count >= 4)
                {
                    s.x = twoSubs; s.y = twoSubs;
                }
                else if (count == 2)
                {
                    var a = b.subCellIn[0];
                    var c = b.subCellIn[1];
                    bool horizontal = (a.y == c.y);
                    bool vertical = (a.x == c.x);

                    s.x = horizontal ? twoSubs : oneSub;
                    s.y = vertical   ? twoSubs : oneSub;
                }
                else
                {
                    s.x = oneSub; s.y = oneSub;
                }

                childTransform.localScale = s;
            }
            return;
        }

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleChildrenAnimated(oneSub, twoSubs, 0.25f));
    }

    private System.Collections.IEnumerator ScaleChildrenAnimated(float oneSub, float twoSubs, float duration)
    {
        List<Transform> targets = new List<Transform>();
        List<Vector3> start = new List<Vector3>();
        List<Vector3> end = new List<Vector3>();

        foreach (var b in childBlocks)
        {
            if (b == null) continue;
            Transform childTransform = b.transform;
            targets.Add(childTransform);
            start.Add(childTransform.localScale);

            Vector3 targetS = childTransform.localScale;
            int count = (b.subCellIn != null) ? b.subCellIn.Count : 0;

            if (count >= 4)
            {
                targetS.x = twoSubs; targetS.y = twoSubs;
            }
            else if (count == 2)
            {
                var a = b.subCellIn[0];
                var c = b.subCellIn[1];
                bool horizontal = (a.y == c.y);
                bool vertical = (a.x == c.x);

                targetS.x = horizontal ? twoSubs : oneSub;
                targetS.y = vertical   ? twoSubs : oneSub;
            }
            else
            {
                targetS.x = oneSub; targetS.y = oneSub;
            }

            end.Add(targetS);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].localScale = Vector3.Lerp(start[i], end[i], progress);
            }
            yield return null;
        }

        for (int i = 0; i < targets.Count; i++)
            targets[i].localScale = end[i];

        scaleCoroutine = null;
    }

    public int[,] GetOccupancy2x2()
    {
        int[,] occ = new int[2,2];
        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null) continue;
            for (int i = 0; i < b.subCellIn.Count; i++)
            {
                var s = b.subCellIn[i];
                if (s.x >= 0 && s.x < 2 && s.y >= 0 && s.y < 2)
                    occ[s.x, s.y] = 1;
            }
        }
        return occ;
    }

    public int GetScaleCount()
    {
        int count = 0;
        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null) continue;
            count += b.subCellIn.Count;
        }
        return Mathf.Clamp(count, 0, 4);
    }

    public bool[,] GetAvailabilityMask()
    {
        int[,] occ = (parentCell != null && parentCell.occupancy != null) ? parentCell.occupancy : GetOccupancy2x2();
        bool[,] avail = new bool[2,2];
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                if (occ[x, y] == 1) continue;
                bool adj = false;
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (nx >= 0 && ny >= 0 && nx < 2 && ny < 2)
                    {
                        if (occ[nx, ny] == 1) { adj = true; break; }
                    }
                }
                avail[x, y] = adj;
            }
        }
        return avail;
    }

    private void UpdateCellOccupancy()
    {
        if (parentCell == null) return;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                parentCell.occupancy[x, y] = 0;
                parentCell.subCellBlocks[x, y] = null;
            }
        }

        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null) continue;
            for (int i = 0; i < b.subCellIn.Count; i++)
            {
                var s = b.subCellIn[i];
                if (s.x >= 0 && s.x < 2 && s.y >= 0 && s.y < 2)
                {
                    parentCell.occupancy[s.x, s.y] = 1;
                    parentCell.subCellBlocks[s.x, s.y] = b;
                }
            }
        }
    }

    private void HandleSoloRow(int rowY)
    {
        int occupants = 0;
        Block solo = null;

        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null) continue;

            bool occupiesRow = false;
            for (int i = 0; i < b.subCellIn.Count; i++)
            {
                if (b.subCellIn[i].y == rowY) { occupiesRow = true; break; }
            }

            if (occupiesRow)
            {
                occupants++;
                solo = b;
                if (occupants > 1) return; // more than one block on this row, do nothing
            }
        }

        if (occupants != 1 || solo == null) return;

        // Count how many of solo’s subcells are on this row and whether it’s left/right
        int rowEntries = 0;
        bool hasLeft = false;
        bool hasRight = false;
        foreach (var sub in solo.subCellIn)
        {
            if (sub.y == rowY)
            {
                rowEntries++;
                if (sub.x == 0) hasLeft = true;
                if (sub.x == 1) hasRight = true;
            }
        }

        // Build a new subcell list: keep non-row entries, and enforce only left for this row
        List<Vector2Int> newSubs = new List<Vector2Int>();
        foreach (var sub in solo.subCellIn)
        {
            if (sub.y != rowY)
                newSubs.Add(sub);
        }
        // Always occupy the left subcell of this row
        newSubs.Add(new Vector2Int(0, rowY));
        solo.subCellIn = newSubs;

        // Physically anchor to left half of the parent cell; keep Y unchanged
        float half = cellSize * 0.25f; // left half offset from center
        var t = solo.transform;
        var lp = t.localPosition;
        lp.x = -half;
        t.localPosition = lp;
    }

    // When exactly 3 blocks remain, if a row has exactly one block, expand it horizontally and center on that row.
    private void ExpandSoloRowWhenThree()
    {
        if (childBlocks == null || childBlocks.Count != 3) return;

        int row0Count = 0; Block soloRow0 = null;
        int row1Count = 0; Block soloRow1 = null;

        foreach (var cb in childBlocks)
        {
            if (cb == null || cb.subCellIn == null) continue;

            bool hasRow0 = false, hasRow1 = false;
            for (int i = 0; i < cb.subCellIn.Count; i++)
            {
                if (cb.subCellIn[i].y == 0) hasRow0 = true;
                if (cb.subCellIn[i].y == 1) hasRow1 = true;
            }
            if (hasRow0) { row0Count++; soloRow0 = cb; }
            if (hasRow1) { row1Count++; soloRow1 = cb; }
        }

        if (row0Count == 1 && soloRow0 != null)
            ExpandBlockHorizontallyAtRow(soloRow0, 0);

        if (row1Count == 1 && soloRow1 != null)
            ExpandBlockHorizontallyAtRow(soloRow1, 1);
    }

    private void ExpandBlockHorizontallyAtRow(Block block, int rowY)
    {
        // Occupy both subcells of this row: (0,row) and (1,row)
        block.subCellIn = new List<Vector2Int>
    {
        new Vector2Int(0, rowY),
        new Vector2Int(1, rowY)
    };

        // Center horizontally (x=0) and align to the row center on Y
        float half = cellSize * 0.25f;
        var t = block.transform;
        var lp = t.localPosition;
        lp.x = 0f;
        lp.y = (rowY == 0 ? -half : half);
        t.localPosition = lp;
    }

    // Enforce a clean 2-block layout when exactly two blocks remain (respects TwoBlockMode)
    private void ArrangeTwoBlockForm()
    {
        // Enforce: with 2 blocks, each block has 2 subcells
        if (childBlocks == null || childBlocks.Count != 2) return;

        var a = childBlocks[0];
        var b = childBlocks[1];

        bool horizontal;
        if (twoBlockMode == TwoBlockMode.Horizontal) horizontal = true;
        else if (twoBlockMode == TwoBlockMode.Vertical) horizontal = false;
        else horizontal = InferTwoBlockOrientation(a, b); // Auto inference

        if (horizontal)
        {
            // Each block takes a full row (2 subcells each)
            int rowA = ChooseBestRowForTwo(a, b);
            int rowB = 1 - rowA;

            a.subCellIn = new List<Vector2Int> { new Vector2Int(0, rowA), new Vector2Int(1, rowA) };
            b.subCellIn = new List<Vector2Int> { new Vector2Int(0, rowB), new Vector2Int(1, rowB) };
        }
        else
        {
            // Each block takes a full column (2 subcells each)
            int colA = ChooseBestColForTwo(a, b);
            int colB = 1 - colA;

            a.subCellIn = new List<Vector2Int> { new Vector2Int(colA, 0), new Vector2Int(colA, 1) };
            b.subCellIn = new List<Vector2Int> { new Vector2Int(colB, 0), new Vector2Int(colB, 1) };
        }
    }
    private void RepositionChildrenBySubcells()
    {
        float half = cellSize * 0.25f; // subcell center offset

        foreach (var b in childBlocks)
        {
            if (b == null || b.subCellIn == null || b.subCellIn.Count == 0) continue;

            var t = b.transform;
            Vector3 lp = t.localPosition;

            if (b.subCellIn.Count == 1)
            {
                var s = b.subCellIn[0];
                lp.x = (s.x == 0 ? -half : half);
                lp.y = (s.y == 0 ? -half : half);
            }
            else if (b.subCellIn.Count == 2)
            {
                var a = b.subCellIn[0];
                var c = b.subCellIn[1];
                bool horizontal = (a.y == c.y);
                bool vertical   = (a.x == c.x);

                if (horizontal)
                {
                    int rowY = a.y; // same Y
                    lp.y = (rowY == 0 ? -half : half);
                    lp.x = 0f; // center between left/right
                }
                else if (vertical)
                {
                    int colX = a.x; // same X
                    lp.x = (colX == 0 ? -half : half);
                    lp.y = 0f; // center between bottom/top
                }
                else
                {
                    // Diagonal/unexpected: average centers
                    float ax = 0f, ay = 0f;
                    foreach (var s in b.subCellIn)
                    {
                        ax += (s.x == 0 ? -half : half);
                        ay += (s.y == 0 ? -half : half);
                    }
                    lp.x = ax / b.subCellIn.Count;
                    lp.y = ay / b.subCellIn.Count;
                }
            }
            else
            {
                lp.x = 0f;
                lp.y = 0f;
            }

            t.localPosition = lp;
        }
    }

    // Orientation helpers
    private bool IsSingle(Block bl) => bl != null && bl.subCellIn != null && bl.subCellIn.Count == 1;

    private bool IsHorizontalTwo(Block bl)
    {
        if (bl == null || bl.subCellIn == null || bl.subCellIn.Count != 2) return false;
        return bl.subCellIn[0].y == bl.subCellIn[1].y;
    }

    private bool IsVerticalTwo(Block bl)
    {
        if (bl == null || bl.subCellIn == null || bl.subCellIn.Count != 2) return false;
        return bl.subCellIn[0].x == bl.subCellIn[1].x;
    }

    private HashSet<int> GetRowSet(Block bl)
    {
        var s = new HashSet<int>();
        if (bl?.subCellIn != null) foreach (var sc in bl.subCellIn) s.Add(sc.y);
        return s;
    }

    private HashSet<int> GetColSet(Block bl)
    {
        var s = new HashSet<int>();
        if (bl?.subCellIn != null) foreach (var sc in bl.subCellIn) s.Add(sc.x);
        return s;
    }

    private int ChooseBestRowForTwo(Block a, Block b)
    {
        if (IsHorizontalTwo(a)) return a.subCellIn[0].y;
        if (IsHorizontalTwo(b)) return b.subCellIn[0].y;
        if (IsSingle(a) && IsSingle(b) && a.subCellIn[0].y == b.subCellIn[0].y) return a.subCellIn[0].y;

        var rowsA = GetRowSet(a);
        var rowsB = GetRowSet(b);
        if (rowsA.Count > rowsB.Count && rowsA.Count > 0) foreach (var r in rowsA) return r;
        if (rowsB.Count > 0) foreach (var r in rowsB) return r;
        return 0;
    }

    private int ChooseBestColForTwo(Block a, Block b)
    {
        if (IsVerticalTwo(a)) return a.subCellIn[0].x;
        if (IsVerticalTwo(b)) return b.subCellIn[0].x;
        if (IsSingle(a) && IsSingle(b) && a.subCellIn[0].x == b.subCellIn[0].x) return a.subCellIn[0].x;

        var colsA = GetColSet(a);
        var colsB = GetColSet(b);
        if (colsA.Count > colsB.Count && colsA.Count > 0) foreach (var c in colsA) return c;
        if (colsB.Count > 0) foreach (var c in colsB) return c;
        return 0;
    }

    private bool InferTwoBlockOrientation(Block a, Block b)
    {
        HashSet<int> rowsA = new HashSet<int>();
        HashSet<int> rowsB = new HashSet<int>();
        HashSet<int> colsA = new HashSet<int>();
        HashSet<int> colsB = new HashSet<int>();

        if (a != null && a.subCellIn != null)
        {
            for (int i = 0; i < a.subCellIn.Count; i++)
            {
                rowsA.Add(a.subCellIn[i].y);
                colsA.Add(a.subCellIn[i].x);
            }
        }
        if (b != null && b.subCellIn != null)
        {
            for (int i = 0; i < b.subCellIn.Count; i++)
            {
                rowsB.Add(b.subCellIn[i].y);
                colsB.Add(b.subCellIn[i].x);
            }
        }

        bool shareRow = false;
        foreach (var r in rowsA) { if (rowsB.Contains(r)) { shareRow = true; break; } }

        bool shareCol = false;
        foreach (var c in colsA) { if (colsB.Contains(c)) { shareCol = true; break; } }

        if (shareRow && !shareCol) return true;   // horizontal
        if (shareCol && !shareRow) return false;  // vertical
        // Ambiguous: prefer explicit mode; otherwise default to horizontal
        if (twoBlockMode == TwoBlockMode.Vertical) return false;
        return true;
    }

    // Inside BlockHolder
    private void EnsureOneBlockOccupiesAll()
    {
        if (childBlocks == null || childBlocks.Count != 1) return;

        var only = childBlocks[0];
        if (only == null) return;

        // Assign all subcells
        only.subCellIn = new List<Vector2Int>
        {
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        };

        // Center; RepositionChildrenBySubcells also keeps it centered
        var t = only.transform;
        var lp = t.localPosition;
        lp.x = 0f;
        lp.y = 0f;
        t.localPosition = lp;
    }
}
