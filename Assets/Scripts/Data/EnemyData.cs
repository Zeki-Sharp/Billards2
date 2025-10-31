using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [BoxGroup("显示信息")]
    [LabelText("敌人信息")]
    [Tooltip("显示信息（名称、图标、描述等）")]
    [InlineProperty]
    [HideLabel]
    public EnemyInfo info = new EnemyInfo();
    
    [BoxGroup("敌人基本信息")]
    [LabelText("敌人预制体")]
    [Tooltip("敌人容器预制体（包含预告和敌人）")]
    [Required]
    public GameObject enemyContainerPrefab;
    
    [BoxGroup("等级配置")]
    [LabelText("敌人等级列表")]
    [Tooltip("敌人的所有等级配置。每个等级独立配置所有参数（类似技能系统）")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 3)]
    public List<EnemyLevelConfig> enemyLevels = new List<EnemyLevelConfig>();
    
    [Button("自动分配等级编号", ButtonSizes.Medium)]
    [BoxGroup("等级配置")]
    private void AutoAssignLevelNumbers()
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            Debug.LogWarning($"敌人 {enemyName} 没有等级配置");
            return;
        }
        
        for (int i = 0; i < enemyLevels.Count; i++)
        {
            enemyLevels[i].level = i + 1;
        }
        Debug.Log($"敌人 {enemyName} 已自动分配等级编号: [{string.Join(", ", enemyLevels.Select(l => l.level))}]");
    }
    
    #region 向后兼容属性（从 Info 读取）
    
    /// <summary>
    /// 敌人名称（向后兼容，从 Info 读取）
    /// </summary>
    public string enemyName => info?.name ?? "";
    
    /// <summary>
    /// 敌人图标（向后兼容，从 Info 读取）
    /// </summary>
    public Sprite enemyIcon => info?.icon;
    
    #endregion
    
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
    
    #region 多等级配置管理
    
    /// <summary>
    /// 获取指定等级的配置
    /// </summary>
    public EnemyLevelConfig GetLevelConfig(int level)
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return null;
        }
        
        return enemyLevels.FirstOrDefault(l => l.level == level && l.isActive);
    }
    
    /// <summary>
    /// 获取最高可用等级
    /// </summary>
    public int GetMaxLevel()
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return 1;
        }
        
        var maxLevel = enemyLevels.Where(l => l.isActive).Max(l => (int?)l.level);
        return maxLevel ?? 1;
    }
    
    /// <summary>
    /// 获取所有可用等级
    /// </summary>
    public List<int> GetAvailableLevels()
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return new List<int> { 1 };
        }
        
        return enemyLevels.Where(l => l.isActive).Select(l => l.level).OrderBy(l => l).ToList();
    }
    
    #endregion
}
