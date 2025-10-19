using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GridCell[,] gridCells;
    public int width;
    public int height;

    void Awake()
    {
        LoadExistingGrid();
    }

    public void LoadExistingGrid()
    {
        GridCell[] allCells = FindObjectsOfType<GridCell>();
        if (allCells.Length == 0)
        {
            Debug.LogWarning("Không có GridCell nào trong scene!");
            return;
        }

        // Tính width / height nếu bạn gán thủ công toạ độ x, y
        int maxX = 0, maxY = 0;
        foreach (var c in allCells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        width = maxX + 1;
        height = maxY + 1;

        gridCells = new GridCell[width, height];
        foreach (var c in allCells)
        {
            gridCells[c.x, c.y] = c;
        }
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return null;
        return gridCells[x, y];
    }
}
