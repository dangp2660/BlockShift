using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour
{
    public GridCell currentCell;
    public BlockMaterialData blockMaterialData;
    public bool isPopping = false;
    public LayerMask gridLayer;

    public void findCurrentCell(GridCell cell)
    {
        if (cell == null) return;

        // Nếu block đang có cell cũ → giải phóng
        if (currentCell != null && currentCell != cell)
        {
            currentCell.isOccupied = false;
            currentCell.currentCube = null;
        }

        // Gán cell mới
        if (!cell.isOccupied)
        {
            currentCell = cell;
            cell.isOccupied = true;
            cell.currentCube = gameObject;
        }
    }

    private void OnDestroy()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
            currentCell.currentCube = null;
        }
    }

    private void OnDisable()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
            currentCell.currentCube = null;
        }
    }

    public void PlayPopEffect()
    {
        if (this == null || isPopping) return;
        isPopping = true;
        StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        Vector3 originScale = transform.localScale;
        float time = 0f;
        while (time < 0.25f)
        {
            time += Time.deltaTime;
            float s = 1 + Mathf.Sin(time * Mathf.PI * 2f) * 0.2f;
            transform.localScale = originScale * s;
            yield return null;
        }
        Debug.Log($"Block at {currentCell?.x}-{currentCell?.y} destroyed");
        Destroy(gameObject);
    }

    public void SetMaterialBlock(BlockMaterialData data)
    {
        blockMaterialData = data;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = data.material;
    }
}
