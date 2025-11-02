 using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 敌人等级配置 - 单个等级的完整配置
/// 参考 SkillLevelConfig 的设计：每个等级独立配置所有参数
/// 共享的只有 enemyContainerPrefab
/// </summary>
[System.Serializable]
public class EnemyLevelConfig
{
    [BoxGroup("等级基本信息")]
    [LabelText("等级")]
    [ReadOnly]
    [Tooltip("等级编号（根据列表位置自动确定，不可手动修改）")]
    public int level = 1;
    
    [BoxGroup("显示配置")]
    [LabelText("敌人贴图")]
    [Tooltip("此等级的敌人显示图片（SpriteRenderer 使用）")]
    [PreviewField(50)]
    public Sprite enemyImage;
    
    [BoxGroup("战斗配置")]
    [LabelText("最大血量")]
    [MinValue(1f)]
    public float maxHealth = 100f;
    
    [BoxGroup("战斗配置")]
    [LabelText("攻击力")]
    [MinValue(0.1f)]
    public float damage = 10f;
    
    [BoxGroup("战斗配置")]
    [LabelText("移动速度")]
    [MinValue(0.1f)]
    public float moveSpeed = 2f;
    
    [BoxGroup("战斗配置")]
    [LabelText("攻击冷却")]
    [MinValue(0.1f)]
    public float attackCooldown = 1f;
    
    [BoxGroup("攻击配置")]
    [LabelText("攻击范围")]
    [MinValue(0.1f)]
    public float attackRange = 3f;
    
    [BoxGroup("攻击配置")]
    [LabelText("攻击类型")]
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
    
    [BoxGroup("AI配置")]
    [LabelText("阶段序列配置（统一系统）⭐")]
    [Tooltip("统一的阶段序列系统，支持 Sequential（顺序）和 Conditional（条件）模式。配置此字段后，将覆盖上方的旧配置")]
    public PhaseSequenceConfig phaseSequenceConfig;
    
    // ===== 原子行为配置 =====
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.MoveTowards)]
    [LabelText("向目标靠近配置")]
    public MoveTowardsConfig moveTowardsConfig = new MoveTowardsConfig();
    
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.MoveAway)]
    [LabelText("远离目标配置")]
    public MoveAwayConfig moveAwayConfig = new MoveAwayConfig();
    
    [BoxGroup("AI配置")]
    [ShowIf("movementType", MovementType.Idle)]
    [LabelText("静止配置")]
    public IdleConfig idleConfig = new IdleConfig();
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        if (level < 1) return false;
        if (maxHealth <= 0) return false;
        if (damage < 0) return false;
        if (moveSpeed <= 0) return false;
        return true;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Lv{level}: HP={maxHealth}, Dmg={damage}, Spd={moveSpeed}, Type={attackType}/{movementType}";
    }
}

