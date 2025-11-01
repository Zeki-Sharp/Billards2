using UnityEngine;

/// <summary>
/// 可受伤接口 - 新伤害系统
/// 
/// 【核心职责】：
/// - 接收伤害事件（OnDamageReceived）
/// - 判断是否可受伤（CanTakeDamage）
/// - 提供血量查询（GetCurrentHealth）
/// 
/// 【设计原则】：
/// - 统一接口：玩家和敌人都实现此接口
/// - 事件驱动：订阅 GameEventBus.OnDamage
/// - 可扩展：支持护盾、无敌帧等
/// 
/// 【实现者】：
/// - PlayerBehavior
/// - EnemyBehavior
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 接收伤害
    /// </summary>
    /// <param name="damageEvent">伤害事件数据</param>
    void OnDamageReceived(DamageEvent damageEvent);
    
    /// <summary>
    /// 是否可以受伤（无敌帧、护盾等）
    /// </summary>
    /// <returns>是否可受伤</returns>
    bool CanTakeDamage();
    
    /// <summary>
    /// 获取当前血量
    /// </summary>
    /// <returns>当前血量</returns>
    float GetCurrentHealth();
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    /// <returns>最大血量</returns>
    float GetMaxHealth();
}

