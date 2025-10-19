using UnityEngine;

/// <summary>
/// 通用的数据提取器集合
/// 提供各种常见数据的提取函数
/// </summary>
public static class DataExtractors
{
    /// <summary>
    /// 血量提取器 - 返回血量百分比 (0-1)
    /// </summary>
    public static System.Func<object, float> HealthExtractor = (eventData) => 
    {
        if (eventData is HealthStateData healthState)
            return healthState.HealthPercentage; // 返回百分比而不是绝对血量
        return 0f;
    };
    
    /// <summary>
    /// 攻击力提取器
    /// </summary>
    public static System.Func<object, float> AttackExtractor = (eventData) => 
    {
        if (eventData is PlayerStateData playerState)
            return playerState.CurrentAttack;
        return 0f;
    };
    
    /// <summary>
    /// 移动速度提取器
    /// </summary>
    public static System.Func<object, float> SpeedExtractor = (eventData) => 
    {
        if (eventData is MovementData movement)
            return movement.CurrentSpeed;
        return 0f;
    };
    
    /// <summary>
    /// 防御力提取器
    /// </summary>
    public static System.Func<object, float> DefenseExtractor = (eventData) => 
    {
        if (eventData is PlayerStateData playerState)
            return playerState.CurrentDefense;
        return 0f;
    };
    
    /// <summary>
    /// 魔法值提取器
    /// </summary>
    public static System.Func<object, float> ManaExtractor = (eventData) => 
    {
        if (eventData is PlayerStateData playerState)
            return playerState.CurrentMana;
        return 0f;
    };
    
    /// <summary>
    /// 根据类型获取对应的数据提取器
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    /// <returns>数据提取函数</returns>
    public static System.Func<object, float> GetExtractor(DataExtractorType type)
    {
        switch (type)
        {
            case DataExtractorType.Health:
                return HealthExtractor;
            case DataExtractorType.Attack:
                return AttackExtractor;
            case DataExtractorType.Defense:
                return DefenseExtractor;
            case DataExtractorType.Speed:
                return SpeedExtractor;
            case DataExtractorType.Mana:
                return ManaExtractor;
            default:
                UnityEngine.Debug.LogError($"不支持的数据提取器类型: {type}");
                return (eventData) => 0f;
        }
    }
}

/// <summary>
/// 血量状态数据
/// </summary>
public struct HealthStateData
{
    public float CurrentHealth;
    public float MaxHealth;
    public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
}

/// <summary>
/// 玩家状态数据
/// </summary>
public struct PlayerStateData
{
    public float CurrentAttack;
    public float CurrentDefense;
    public float CurrentMana;
    public float CurrentSpeed;
}

/// <summary>
/// 移动数据
/// </summary>
public struct MovementData
{
    public float CurrentSpeed;
    public Vector3 Velocity;
    public bool IsMoving => CurrentSpeed > 0.1f;
}
