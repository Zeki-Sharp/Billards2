using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 道具目标类型
/// </summary>
public enum ItemTargetType
{
    Picker,             // 拾取者自己
    AllCharacters,      // 所有存活角色
    SpecificCharacter   // 指定角色
}

/// <summary>
/// 道具拾取限制
/// </summary>
public enum ItemPickupRestriction
{
    None,               // 无限制，所有角色都能拾取
    SpecificCharacter,  // 只有特定角色能拾取
    HealthBelow50       // 血量低于50%才能拾取
}

/// <summary>
/// 道具配置ScriptableObject - 定义道具的基本信息和效果
/// 完全基于技能系统，道具效果由引用的SkillConfig处理
/// 
/// 【多角色系统支持】：
/// - targetType: 配置效果作用于谁（拾取者/全队/指定角色）
/// - pickupRestriction: 配置谁可以拾取（无限制/特定角色/条件限制）
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
    
    [BoxGroup("目标配置")]
    [LabelText("目标类型")]
    [Tooltip("道具效果作用于谁")]
    public ItemTargetType targetType = ItemTargetType.Picker;
    
    [BoxGroup("目标配置")]
    [LabelText("指定角色名")]
    [ShowIf("targetType", ItemTargetType.SpecificCharacter)]
    [Tooltip("当目标类型为'指定角色'时，填写角色名称（如'撞击角色'）")]
    public string targetCharacterName = "";
    
    [BoxGroup("拾取限制")]
    [LabelText("拾取限制")]
    [Tooltip("谁可以拾取这个道具")]
    public ItemPickupRestriction pickupRestriction = ItemPickupRestriction.None;
    
    [BoxGroup("拾取限制")]
    [LabelText("限制角色名")]
    [ShowIf("pickupRestriction", ItemPickupRestriction.SpecificCharacter)]
    [Tooltip("只有该角色能拾取（填写角色名称，如'撞击角色'）")]
    public string restrictedCharacterName = "";
    
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
            
            if (level1Config.effectConfig is HealEffectConfig)
            {
                return ItemType.Consumable;
            }
            else if (level1Config.effectConfig is StatModifierEffectConfig)
            {
                return ItemType.Buff;
            }
            else if (level1Config.effectConfig is DropItemEffectConfig)
            {
                return ItemType.Special;
            }
            else
            {
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
