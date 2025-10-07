using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [BoxGroup("敌人基本信息")]
    [LabelText("敌人名称")]
    public string enemyName;
    
    [BoxGroup("敌人基本信息")]
    [LabelText("敌人预制体")]
    [Tooltip("敌人容器预制体（包含预告和敌人）")]
    [Required]
    public GameObject enemyContainerPrefab;
    
    [BoxGroup("敌人基本信息")]
    [LabelText("敌人图标")]
    public Sprite enemyIcon;
    
    [BoxGroup("物理数据")]
    [LabelText("球体数据")]
    [Tooltip("打包的物理数据")]
    [Required]
    public BallData ballData;
    
    [BoxGroup("战斗配置")]
    [LabelText("最大血量")]
    [MinValue(1f)]
    public float maxHealth = 100f;
    
    [BoxGroup("战斗配置")]
    [LabelText("攻击力")]
    [MinValue(0.1f)]
    public float damage = 10f;
    
    [BoxGroup("战斗配置")]
    [LabelText("攻击冷却")]
    [MinValue(0.1f)]
    public float attackCooldown = 1f;
    
    [BoxGroup("战斗配置")]
    [LabelText("移动速度")]
    [MinValue(0.1f)]
    public float moveSpeed = 2f;
    
    [BoxGroup("攻击配置")]
    [LabelText("攻击范围")]
    [Tooltip("保留用于其他用途，如检测范围")]
    [MinValue(0.1f)]
    public float attackRange = 3f;
    
    [BoxGroup("攻击配置")]
    [LabelText("攻击类型")]
    [Tooltip("攻击类型")]
    public AttackType attackType = AttackType.Melee;
    
    [BoxGroup("攻击配置")]
    [ShowIf("attackType", AttackType.Ranged)]
    public RangedAttackConfig rangedConfig = new RangedAttackConfig();
    
    [BoxGroup("攻击配置")]
    [ShowIf("attackType", AttackType.Thorn)]
    public ThornAttackConfig thornConfig = new ThornAttackConfig();
    
    [BoxGroup("AI配置")]
    [LabelText("启用AI")]
    public bool enableAI = true;
    
    [BoxGroup("AI配置")]
    [LabelText("移动类型")]
    public MovementType movementType = MovementType.FollowPlayer;
    
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.FollowPlayer)]
    public FollowMovementConfig followConfig = new FollowMovementConfig();
    
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.Flee)]
    public FleeMovementConfig fleeConfig = new FleeMovementConfig();
    
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.IntervalMovement)]
    public IntervalMovementConfig intervalConfig = new IntervalMovementConfig();
    
    [BoxGroup("生成配置")]
    [LabelText("生成权重")]
    [MinValue(1)]
    public int spawnWeight = 1;
    
    [BoxGroup("生成配置")]
    [LabelText("生成成本")]
    [MinValue(1)]
    public int spawnCost = 1;
    
    [BoxGroup("生成配置")]
    [LabelText("是否为Boss")]
    public bool isBoss = false;
    
    [BoxGroup("生成配置")]
    [LabelText("经验值")]
    [MinValue(0)]
    public int experienceValue = 10;
}
