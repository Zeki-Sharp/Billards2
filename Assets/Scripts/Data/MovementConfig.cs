using UnityEngine;

/// <summary>
/// 跟随移动配置
/// </summary>
[System.Serializable]
public class FollowMovementConfig
{
    [Header("跟随移动配置")]
    public float moveSpeed = 2f;        // 跟随移动速度
    public float moveDistance = 3f;     // 跟随移动距离
    public float minDistance = 1f;      // 保持的最小距离
}

/// <summary>
/// 逃跑移动配置
/// </summary>
[System.Serializable]
public class FleeMovementConfig
{
    [Header("逃跑移动配置")]
    public float moveSpeed = 2f;          // 逃跑移动速度
    public float moveDistance = 4f;       // 逃跑移动距离
    public float triggerDistance = 3f;    // 触发逃跑的距离（玩家接近到这个距离内时逃跑）
    
    [Header("接近玩家设置")]
    [Tooltip("如果离玩家太远，是否向玩家移动")]
    public bool approachWhenFar = false;
    
    [Tooltip("触发接近的距离（超过这个距离时向玩家移动）")]
    public float approachDistance = 8f;
    
    [Tooltip("接近玩家时的移动速度")]
    public float approachSpeed = 3f;
    
    [Tooltip("接近玩家时的移动距离")]
    public float approachMoveDistance = 3f;
}
