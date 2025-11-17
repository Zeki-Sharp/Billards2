using UnityEngine;

/// <summary>
/// 墙壁碰撞检测器 - 挂载在子墙壁上
/// 负责检测碰撞并通知父级WallManager
/// </summary>
public class WallCollisionDetector : MonoBehaviour
{
    private WallManager parentWallManager;
    
    /// <summary>
    /// 初始化碰撞检测器
    /// </summary>
    public void Initialize(WallManager wallManager)
    {
        parentWallManager = wallManager;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 通知父级墙壁管理器（2D版本）
        if (parentWallManager != null)
        {
            parentWallManager.OnWallHit(collision, transform);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 通知父级墙壁管理器（3D版本）
        if (parentWallManager != null)
        {
            parentWallManager.OnWallHit(collision, transform);
        }
    }
}
