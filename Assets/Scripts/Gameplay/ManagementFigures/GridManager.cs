using UnityEngine;

/// <summary>
/// DEPRECATED: оставлен только для совместимости со сценой. Используйте Field3D.
/// Вся логика хранения состояния клеток теперь в Field3D.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("DEPRECATED — используйте Field3D")]
    public Vector3Int size;

    private bool[,,] occupied;

    private void Awake()
    {
        if (size.x <= 0) size.x = 8;
        if (size.y <= 0) size.y = 8;
        if (size.z <= 0) size.z = 8;
        occupied = new bool[size.x, size.y, size.z];
        Debug.LogWarning("[GridManager] Этот компонент устарел и не используется в геймплее. Используйте Field3D.");
    }
}
