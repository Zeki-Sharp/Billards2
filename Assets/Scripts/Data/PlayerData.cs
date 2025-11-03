using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    public enum AttackMode
    {
        Collision,    // 碰撞攻击
        Area,         // 范围攻击
        Triangle      // 三角形攻击（新增）
    }
    
    [BoxGroup("显示信息")]
    [LabelText("玩家信息")]
    [Tooltip("显示信息（名称、图标、描述等）")]
    [InlineProperty]
    [HideLabel]
    public PlayerInfo info = new PlayerInfo();
    
    [BoxGroup("玩家基本信息")]
    [LabelText("玩家预制体")]
    [Required]
    public GameObject playerPrefab;
    
    
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
    [LabelText("攻击方式")]
    [Tooltip("选择玩家的攻击方式")]
    public AttackMode attackMode = AttackMode.Collision;
    
    [BoxGroup("战斗配置")]
    [ShowIf("attackMode", AttackMode.Collision)]
    [LabelText("碰撞伤害")]
    [Tooltip("碰撞攻击的伤害值")]
    [MinValue(0.1f)]
    public float collisionDamage = 10f;
    
    [BoxGroup("战斗配置")]
    [ShowIf("attackMode", AttackMode.Area)]
    [LabelText("范围伤害")]
    [Tooltip("范围攻击的伤害值")]
    [MinValue(0.1f)]
    public float areaDamage = 15f;
    
    [BoxGroup("战斗配置")]
    [ShowIf("attackMode", AttackMode.Area)]
    [LabelText("攻击范围")]
    [Tooltip("范围攻击的半径")]
    [MinValue(0.1f)]
    public float areaRadius = 2f;
    
    [BoxGroup("新伤害系统配置")]
    [LabelText("伤害配置列表")]
    [Tooltip("玩家的伤害规则配置列表（新伤害系统），支持组合多个 Profile 实现规则复用")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<DamageProfile> damageProfiles = new List<DamageProfile>();
    
    /// <summary>
    /// 向后兼容：返回第一个 Profile
    /// </summary>
    public DamageProfile damageProfile => damageProfiles != null && damageProfiles.Count > 0 ? damageProfiles[0] : null;
    
    [BoxGroup("战斗配置")]
    [ShowIf("attackMode", AttackMode.Area)]
    [LabelText("敌人层遮罩")]
    [Tooltip("范围攻击检测的敌人图层")]
    public LayerMask enemyLayerMask = -1;
    
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