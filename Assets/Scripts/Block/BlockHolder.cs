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
            AssignToCell(parentCell);
    }

    public void GenerateBlocks()
    {
        foreach (Transform c in transform)
        {
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
        childBlocks.Clear();

        if (childOffsets == null || childOffsets.Length == 0)
            childOffsets = new Vector2Int[] { Vector2Int.zero };

        float spacing = cellSize * 0.25f;
        float baseScale = cellSize * 0.45f;

        float avgX = 0, avgY = 0;
        foreach (var o in childOffsets) { avgX += o.x; avgY += o.y; }
        avgX /= childOffsets.Length;
        avgY /= childOffsets.Length;

        foreach (var offset in childOffsets)
        {
            Vector3 pos = new Vector3(
                (offset.x - avgX) * spacing * 2,
                (offset.y - avgY) * spacing * 2,
                0);
            GameObject go = Instantiate(blockPrefab, transform);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * baseScale;
            childBlocks.Add(go.GetComponent<Block>());
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
    }
}
