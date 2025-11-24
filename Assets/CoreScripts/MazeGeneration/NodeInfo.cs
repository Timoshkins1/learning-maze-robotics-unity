using UnityEngine;

public class NodeInfo : MonoBehaviour
{
    public int chunkX;
    public int chunkZ;
    public int cellX;
    public int cellZ;

    public void SetCoordinates(int chunkX, int chunkZ, int cellX, int cellZ)
    {
        this.chunkX = chunkX;
        this.chunkZ = chunkZ;
        this.cellX = cellX;
        this.cellZ = cellZ;
    }

    public Vector2Int ChunkCoordinates => new Vector2Int(chunkX, chunkZ);
    public Vector2Int CellCoordinates => new Vector2Int(cellX, cellZ);
}