using System.Collections.Generic;
using UnityEngine;

public class BlockHolder : MonoBehaviour
{
    [Header("Prefab & Grid Settings")]
    public GameObject blockPrefab; // Prefab for the child block
    public float cellSize = 1f;    // Size of the full grid cell (this holder)

    [Header("Child Block Layout")]
    public Vector2Int[] childOffsets; // Positions of child blocks relative to the center

    private readonly List<GameObject> childBlocks = new List<GameObject>();

    void Start()
    {
        GenerateBlocks();
    }

    public void GenerateBlocks()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("❌ Block prefab is missing!");
            return;
        }

        // Clear old blocks
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        childBlocks.Clear();

        // Default: one centered block
        if (childOffsets == null || childOffsets.Length == 0)
        {
            childOffsets = new Vector2Int[] { Vector2Int.zero };
        }

        int count = childOffsets.Length;

        // --- Determine layout based on block count ---
        float spacing;
        float subScale;

        if (count == 1)
        {
            // Full-size single block
            spacing = 0f;
            subScale = cellSize * 0.95f; // occupy almost full cell
        }
        else if (count == 2)
        {
            spacing = cellSize * 0.25f;
            subScale = cellSize * 0.45f;
        }
        else if (count == 3)
        {
            spacing = cellSize * 0.25f;
            subScale = cellSize * 0.4f;
        }
        else // 4 or more
        {
            spacing = cellSize * 0.25f;
            subScale = cellSize * 0.45f;
        }

        // --- Compute pattern center ---
        float avgX = 0f, avgY = 0f;
        foreach (var offset in childOffsets)
        {
            avgX += offset.x;
            avgY += offset.y;
        }
        avgX /= count;
        avgY /= count;

        // --- Create children ---
        foreach (var offset in childOffsets)
        {
            Vector3 localPos = new Vector3(
                (offset.x - avgX) * spacing * 2,
                (offset.y - avgY) * spacing * 2,
                0);

            GameObject newBlock = Instantiate(blockPrefab, transform);
            newBlock.transform.localPosition = localPos;
            newBlock.transform.localRotation = Quaternion.identity;
            newBlock.transform.localScale = new Vector3(subScale, subScale,1);
            childBlocks.Add(newBlock);
        }
    }
}
