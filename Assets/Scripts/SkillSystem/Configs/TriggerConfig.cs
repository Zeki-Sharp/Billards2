using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 触发器配置 - 用于配置技能触发器的参数
/// 简化设计：选择类型后直接显示对应参数
/// </summary>
[System.Serializable]
public class TriggerConfig
{
    [LabelText("触发器类型")]
    [Tooltip("选择触发技能的事件类型")]
    public TriggerType triggerType = TriggerType.Collision;
    
    [ShowIf("triggerType", TriggerType.Collision)]
    [LabelText("碰撞目标标签")]
    [Tooltip("检测与哪个标签的物体碰撞")]
    public string targetTag = "Enemy";
    
    [ShowIf("triggerType", TriggerType.Collision)]
    [LabelText("使用攻击类型过滤")]
    [Tooltip("是否只检测特定类型的碰撞")]
    public bool useAttackTypeFilter = true;
    
    [ShowIf("@triggerType == TriggerType.Collision && useAttackTypeFilter")]
    [LabelText("攻击类型")]
    [Tooltip("攻击类型过滤（如果启用）")]
    public string attackType = "Hit";
    
    [ShowIf("triggerType", TriggerType.Kill)]
    [LabelText("击杀目标标签")]
    [Tooltip("检测击杀哪个标签的物体")]
    public string killTargetTag = "Enemy";
    
    [ShowIf("triggerType", TriggerType.DataSource)]
    [LabelText("数据提取器类型")]
    [Tooltip("从事件数据中提取什么类型的数据")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;
    
    /// <summary>
    /// 创建触发器实例
    /// </summary>
    public ITrigger CreateTrigger()
    {
        switch (triggerType)
        {
            case TriggerType.Collision:
                var collisionTrigger = new CollisionTrigger();
                // 传递配置参数给触发器实例
                collisionTrigger.SetTargetTag(targetTag);
                return collisionTrigger;
                
            case TriggerType.Kill:
                var killTrigger = new KillTrigger();
                // 传递配置参数给触发器实例
                killTrigger.SetTargetTag(killTargetTag);
                return killTrigger;
                
            case TriggerType.DataSource:
                var dataSourceTrigger = new DataSourceTrigger();
                // 根据数据提取器类型设置提取函数
                dataSourceTrigger.SetDataExtractorType(dataExtractorType);
                return dataSourceTrigger;
                
            case TriggerType.AlwaysTrue:
                var alwaysTrueTrigger = new AlwaysTrueTrigger();
                // 始终为真的触发器不需要额外配置
                return alwaysTrueTrigger;
                
            default:
                Debug.LogError($"不支持的触发器类型: {triggerType}");
                return null;
        }
    }
    
    /// <summary>
    /// 根据类型获取数据提取器
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    /// <returns>数据提取函数</returns>
    private System.Func<object, float> GetDataExtractor(DataExtractorType type)
    {
        switch (type)
        {
            case DataExtractorType.Health:
                return DataExtractors.HealthExtractor;
            case DataExtractorType.Attack:
                return DataExtractors.AttackExtractor;
            case DataExtractorType.Defense:
                return DataExtractors.DefenseExtractor;
            case DataExtractorType.Speed:
                return DataExtractors.SpeedExtractor;
            case DataExtractorType.Mana:
                return DataExtractors.ManaExtractor;
            default:
                Debug.LogError($"不支持的数据提取器类型: {type}");
                return (eventData) => 0f;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (triggerType)
        {
            case TriggerType.Collision:
                if (useAttackTypeFilter)
                {
                    return $"碰撞触发器: 标签={targetTag}, 类型={attackType}";
                }
                return $"碰撞触发器: 标签={targetTag}";
                
            case TriggerType.Kill:
                return $"击杀触发器: 标签={killTargetTag}";
                
            case TriggerType.AlwaysTrue:
                return $"始终为真触发器: 用于标记型技能";
                
            default:
                return $"触发器: {triggerType}";
        }
    }
}

/// <summary>
/// 触发器类型枚举
/// </summary>
public enum TriggerType
{
    Collision,  // 碰撞触发器
    Kill,       // 击杀触发器
    DataSource, // 数据源触发器
    AlwaysTrue  // 始终为真触发器
}

/// <summary>
/// 数据提取器类型枚举
/// </summary>
public enum DataExtractorType
{
    Health,     // 血量提取器
    Attack,     // 攻击力提取器
    Defense,    // 防御力提取器
    Speed,      // 移动速度提取器
    Mana        // 魔法值提取器
}
