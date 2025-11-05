using UnityEngine;

/// <summary>
/// GameObject 扩展方法
/// 提供便捷的事件发布接口
/// </summary>
public static class GameObjectExtensions
{
    /// <summary>
    /// 发布死亡事件
    /// </summary>
    /// <param name="deadObject">死亡对象</param>
    /// <param name="deathType">死亡类型</param>
    /// <param name="position">死亡位置</param>
    /// <param name="attacker">击杀者对象</param>
    public static void PublishDeath(this GameObject deadObject, string deathType, Vector3 position, GameObject attacker)
    {
        GameEventBus.PublishSimpleDeath(deathType, position, deadObject, attacker);
    }
    
    /// <summary>
    /// 发布特效事件
    /// </summary>
    /// <param name="sourceObject">源对象</param>
    /// <param name="effectType">特效类型</param>
    /// <param name="position">特效位置</param>
    /// <param name="direction">特效方向</param>
    public static void PublishEffect(this GameObject sourceObject, string effectType, Vector3 position, Vector3 direction = default)
    {
        GameEventBus.PublishEffectEvent(effectType, position, direction, sourceObject);
    }
}
