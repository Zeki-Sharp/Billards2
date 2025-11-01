using UnityEngine;

/// <summary>
/// CollisionEvent 扩展方法
/// 简化碰撞事件发布
/// </summary>
public static class CollisionEventExtensions
{
    /// <summary>
    /// 发布碰撞事件（扩展方法）
    /// </summary>
    public static void PublishCollisionEvent(this MonoBehaviour source, Collision2D collision)
    {
        CollisionEvent evt = CollisionEvent.Create(source.gameObject, collision);
        GameEventBus.PublishCollision(evt);
    }
    
    /// <summary>
    /// 发布碰撞事件（GameObject 扩展）
    /// </summary>
    public static void PublishCollisionEvent(this GameObject source, Collision2D collision)
    {
        CollisionEvent evt = CollisionEvent.Create(source, collision);
        GameEventBus.PublishCollision(evt);
    }
}

