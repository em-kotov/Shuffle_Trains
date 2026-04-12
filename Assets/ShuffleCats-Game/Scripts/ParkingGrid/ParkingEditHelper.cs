using UnityEngine;

public class ParkingEditHelper : MonoBehaviour
{
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private int _startX = 0;
    [SerializeField] private int _startY = 0;
    [SerializeField] private float _axisYLevel = 0f;

    private Vector3 GridToWorld(Vector2Int gridPosition)
    {
        Vector3 gridOrigin = new Vector3(_startX, _axisYLevel, _startY);
        float worldX = (gridPosition.x * _cellSize) + gridOrigin.x;
        float worldZ = (gridPosition.y * _cellSize) + gridOrigin.z;
        return new Vector3(worldX, _axisYLevel, worldZ);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float ySize = 0.1f;
        float sideMultiplier = 0.7f;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                Gizmos.DrawWireCube(cellCenter, new Vector3(
                                  _cellSize * sideMultiplier, ySize,
                                  _cellSize * sideMultiplier));
            }
        }
    }
}
