using UnityEngine;

[System.Serializable]
public class SubCell
{
    public int x;
    public int y;
    public bool isOccupied;
    public Block currentBlock;
    public BlockHolder parentHolder;

    public SubCell(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isOccupied = false;
        this.currentBlock = null;
        this.parentHolder = null;
    }
}
