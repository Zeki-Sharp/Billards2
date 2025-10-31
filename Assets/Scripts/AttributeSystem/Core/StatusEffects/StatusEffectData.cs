using UnityEngine;

/// <summary>
/// 状态效果数据 - 定义单个状态效果的配置
/// 
/// 【设计理念】：
/// - ScriptableObject 配置
/// - 定义临时状态效果（中毒、加速、护盾等）
/// - 支持持续时间和堆叠
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 StatusEffect
/// - 支持 OnStart/OnEnd/WhileActive 回调
/// 
/// 【典型应用】：
/// - 中毒效果（持续扣血）
/// - 加速效果（临时提升速度）
/// - 护盾效果（临时增加防御）
/// - 无敌状态
/// </summary>
[CreateAssetMenu(fileName = "StatusEffect", menuName = "Game/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("状态效果ID（唯一标识符）")]
    public string effectID = "Poison";
    
    [Tooltip("效果显示名称")]
    public string displayName = "中毒";
    
    [Tooltip("效果图标")]
    public Sprite icon;
    
    [Header("持续时间")]
    [Tooltip("持续时间（秒，0表示永久）")]
    [Min(0f)]
    public float duration = 5f;
    
    [Header("堆叠设置")]
    [Tooltip("是否可以堆叠")]
    public bool canStack = false;
    
    [Tooltip("最大堆叠层数（仅当可堆叠时有效）")]
    [Min(1)]
    public int maxStacks = 3;
    
    [Header("显示设置")]
    [Tooltip("是否在UI中隐藏")]
    public bool isHidden = false;
    
    [Tooltip("效果颜色标识")]
    public Color effectColor = Color.green;
    
    [Header("效果数值")]
    [Tooltip("效果强度（具体含义由效果类型决定）")]
    public float intensity = 1f;
    
    [Tooltip("效果描述")]
    [TextArea(2, 4)]
    public string description = "持续造成伤害";
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(effectID);
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"[{effectID}] {displayName}";
        if (duration > 0)
        {
            info += $" (持续 {duration}s)";
        }
        if (canStack)
        {
            info += $" [可堆叠 x{maxStacks}]";
        }
        return info;
    }
}

