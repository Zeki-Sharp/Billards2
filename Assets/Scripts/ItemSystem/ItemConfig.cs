using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 道具配置ScriptableObject - 定义道具的基本信息和效果
/// 完全基于技能系统，道具效果由引用的SkillConfig处理
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Game/Item Config")]
public class ItemConfig : ScriptableObject
{
    [BoxGroup("基本信息")]
    [LabelText("道具名称")]
    [Tooltip("道具的显示名称")]
    public string itemName = "未命名道具";
    
    [BoxGroup("基本信息")]
    [LabelText("道具描述")]
    [Tooltip("道具的详细描述")]
    [TextArea(3, 5)]
    public string description = "这是一个神秘的道具...";
    
    [BoxGroup("基本信息")]
    [LabelText("道具图标")]
    [Tooltip("道具在UI和场景中显示的图标")]
    public Sprite icon;
    
    [BoxGroup("效果配置")]
    [LabelText("关联技能")]
    [Tooltip("拾取后触发的技能（治疗、增益等效果由技能的Effect处理）")]
    [InfoBox("道具效果完全由引用的技能配置决定，支持所有技能系统功能")]
    public SkillConfig itemSkill;
    
    [BoxGroup("效果配置")]
    [LabelText("是否为一次性效果")]
    [Tooltip("true=立即执行后移除技能，false=技能会持续存在")]
    public bool isInstantEffect = true;
    
    [BoxGroup("掉落配置")]
    [LabelText("道具预制体")]
    [Tooltip("场景中显示的道具预制体")]
    public GameObject itemPrefab;
    
    [BoxGroup("视觉配置")]
    [LabelText("拾取特效")]
    [Tooltip("拾取时播放的特效预制体")]
    public GameObject pickupEffect;
    
    [BoxGroup("视觉配置")]
    [LabelText("拾取音效")]
    [Tooltip("拾取时播放的音效")]
    public AudioClip pickupSound;
    
    [BoxGroup("调试信息")]
    [LabelText("道具类型")]
    [ReadOnly]
    [ShowInInspector]
    public ItemType itemType
    {
        get
        {
            if (itemSkill == null) return ItemType.Unknown;
            
            // 根据技能效果类型推断道具类型（从等级1获取）
            var level1Config = itemSkill.GetLevelConfig(1);
            if (level1Config?.effectConfig == null) return ItemType.Unknown;
            
            switch (level1Config.effectConfig.effectType)
            {
                case SkillEffectType.Heal:
                    return ItemType.Consumable;
                case SkillEffectType.StatModifier:
                    return ItemType.Buff;
                case SkillEffectType.DropItem:
                    return ItemType.Special;
                default:
                    return ItemType.Unknown;
            }
        }
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogError($"[ItemConfig] {name} 道具名称为空");
            return false;
        }
        
        if (itemSkill == null)
        {
            Debug.LogError($"[ItemConfig] {name} 未设置关联技能");
            return false;
        }
        
        if (itemPrefab == null)
        {
            Debug.LogError($"[ItemConfig] {name} 未设置道具预制体");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string skillInfo = itemSkill != null ? itemSkill.skillName : "无技能";
        return $"道具: {itemName} | 类型: {itemType} | 技能: {skillInfo}";
    }
}

/// <summary>
/// 道具类型枚举
/// </summary>
public enum ItemType
{
    Unknown,        // 未知类型
    Consumable,     // 消耗品（治疗药水等）
    Buff,          // 增益道具（属性提升等）
    Special,       // 特殊道具（生成其他道具等）
    Equipment      // 装备（未来扩展）
}
