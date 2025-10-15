using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    public enum AttackMode
    {
        Collision,    // 碰撞攻击
        Area          // 范围攻击
    }
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