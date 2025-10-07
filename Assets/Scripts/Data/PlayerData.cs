using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [BoxGroup("玩家基本信息")]
    [LabelText("玩家名称")]
    public string playerName;
    
    [BoxGroup("玩家基本信息")]
    [LabelText("玩家预制体")]
    [Required]
    public GameObject playerPrefab;
    
    [BoxGroup("玩家基本信息")]
    [LabelText("玩家图标")]
    public Sprite playerIcon;
    
    [BoxGroup("物理数据")]
    [LabelText("球体数据")]
    [Required]
    public BallData ballData;
    
    [BoxGroup("战斗配置")]
    [LabelText("基础最大血量")]
    [Tooltip("基础最大血量")]
    [MinValue(1f)]
    public float baseMaxHealth = 100f;
    
    [BoxGroup("战斗配置")]
    [LabelText("基础攻击力")]
    [Tooltip("基础攻击力")]
    [MinValue(0.1f)]
    public float baseDamage = 10f;
    
    [BoxGroup("移动配置")]
    [LabelText("基础微调移动速度")]
    [Tooltip("基础微调移动速度")]
    [MinValue(0.1f)]
    public float baseMicroMoveSpeed = 5f;
    
    [BoxGroup("向后兼容属性")]
    [LabelText("最大血量")]
    [Tooltip("最大血量 - 通过PlayerStatsManager获取最终值")]
    [ReadOnly]
    public float maxHealth => baseMaxHealth;
    
    [BoxGroup("向后兼容属性")]
    [LabelText("攻击力")]
    [Tooltip("攻击力 - 通过PlayerStatsManager获取最终值")]
    [ReadOnly]
    public float damage => baseDamage;
    
    [BoxGroup("向后兼容属性")]
    [LabelText("微调移动速度")]
    [Tooltip("微调移动速度 - 通过PlayerStatsManager获取最终值")]
    [ReadOnly]
    public float microMoveSpeed => baseMicroMoveSpeed;
    
    [BoxGroup("玩家特有配置")]
    [LabelText("可以升级")]
    public bool canLevelUp = true;
    
    [BoxGroup("玩家特有配置")]
    [LabelText("初始等级")]
    [MinValue(1)]
    public int startingLevel = 1;
    
    [BoxGroup("玩家特有配置")]
    [LabelText("最大等级")]
    [MinValue(1)]
    public int maxLevel = 100;
    
    [BoxGroup("玩家特有配置")]
    [LabelText("经验倍数")]
    [MinValue(0.1f)]
    public float experienceMultiplier = 1f;
}