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

    private readonly List<GameObject> childBlocks = new List<GameObject>();

    void Start()
    {
        GenerateBlocks();
    }

    public void GenerateBlocks()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("Block prefab is missing!");
            return;
        }

        // --- Clear old children ---
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        childBlocks.Clear();

        // --- Default layout ---
        if (childOffsets == null || childOffsets.Length == 0)
            childOffsets = new Vector2Int[] { Vector2Int.zero };

        // Base parameters
        float spacing = cellSize * 0.25f;
        float baseScale = cellSize * 0.45f;
        float maxScale = cellSize * 0.95f;

        // --- Compute bounds ---
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

        // --- Step 1: place blocks ---
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
            childBlocks.Add(newBlock);
        }

        // --- Step 2: dynamic fitting ---
        float totalWidth = widthCount * baseScale + (widthCount - 1) * spacing * 2;
        float totalHeight = heightCount * baseScale + (heightCount - 1) * spacing * 2;

        // X first, then Y
        float fitX = totalWidth < (cellSize * 0.9f) ? (cellSize * 0.9f) / totalWidth : 1f;
        float fitY = totalHeight < (cellSize * 0.9f) ? (cellSize * 0.9f) / totalHeight : 1f;

        fitX = Mathf.Min(fitX, maxScale / baseScale);
        fitY = Mathf.Min(fitY, maxScale / baseScale);

        // --- Apply scaling ---
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
            Debug.LogError("Marials lib is not null or count = 0");
            return;
        }

        List<BlockMaterialData> aviable = new List<BlockMaterialData>(library.materials);
        for (int i = 0; i < childOffsets.Length; i++)
        {
            if (aviable.Count == 0) break;
            int index = Random.Range(0, aviable.Count); 
            BlockMaterialData choose = aviable[index];
            aviable.RemoveAt(index);
            Block block = childBlocks[i].GetComponent<Block>();
            if (block != null)
            {
                block.SetMaterialBlock(choose);
            }
        }
    }//SpawnUniqueBlocks
}
