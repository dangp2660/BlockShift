using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour
{
    public GridCell currentCell;
    public int blockID;
    public bool isPopping = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Update()
    {
        findCurrentCell();
    }

    public void findCurrentCell()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.7f, Vector3.down, out hit))
        {
            GridCell cell = hit.collider.gameObject.GetComponent<GridCell>();
            if (cell != null)
            {
                if (!cell.isOccupied)
                {
                    currentCell = cell;
                    cell.isOccupied = true;
                    cell.currentCube = gameObject;
                    Debug.Log($"{name} in cell {cell.x}-{cell.y}");
                }
            }
        }
    }//findCurrentCell

    private void OnDestroy()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
        }
    }

    private void OnDisable()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
        }
    }

    public void PlayPopEffect()
    {
        if (this == null || isPopping) return;
        isPopping = true;
        StartCoroutine(PopAnimation());
    }//PlayPopEffect

    private IEnumerator PopAnimation()
    {
        Vector3 originScale =  transform.localScale;
        float time = 0f;
        while (time < 0.25f)
        {
            time += Time.deltaTime;
            float s = 1 + Mathf.Clamp01(time * Mathf.PI * 4) *0.2f;
            transform.localScale = originScale * s;
            yield return null;
        }
        Debug.Log($"Block at {currentCell.x}-{currentCell.y} is destroyed");
        Destroy(gameObject);
    }//PopAnimation
}
