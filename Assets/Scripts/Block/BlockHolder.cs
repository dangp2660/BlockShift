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

    void Start()
    {
        GenerateBlocks();

        if (parentCell != null)
        {
            AssignToCell(parentCell);
        }
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
    }//GenerateBlocks

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

        // 🔹 Gọi logic chia subcell
        AssignSubCells();
    }

    // ============================================================
    // 🔸 Thêm đoạn này để chia subcell linh hoạt 4 vị trí (2x2)
    // ============================================================
    private void AssignSubCells()
    {
        if (childBlocks == null || childBlocks.Count == 0 || parentCell == null)
            return;

        // Danh sách 4 vị trí subcell trong 1 ô
        List<Vector2Int> allSubCells = new List<Vector2Int>()
        {
            new Vector2Int(0,0),
            new Vector2Int(1,0),
            new Vector2Int(0,1),
            new Vector2Int(1,1)
        };

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
    }
}
