using UnityEngine;
using System.Linq;

/// <summary>
/// 收集打击效果 - 收集者角色主动技能
/// 
/// 【核心功能】：
/// - 在玩家回合结束时触发
/// - 根据本回合拾取的掉落物数量造成伤害
/// - 伤害公式：伤害系数 × 拾取数量
/// - 目标：距离收集者角色最近的敌人
/// 
/// 【使用场景】：
/// - 收集者角色的主动技能
/// - Trigger: PhaseStateTrigger [PlayerPhaseEnd]
/// - Condition: 可选（检查拾取数量 > 0）
/// - Effect: CollectorStrikeEffect
/// 
/// 【配置参数】：
/// - damagePerItem: 每个掉落物的伤害系数
/// - collectorCharacterID: 收集者角色ID
/// </summary>
public class CollectorStrikeEffect : IEffect
{
    // 配置字段
    private float damagePerItem = 10f;
    private string collectorCharacterID;
    private bool showDebugLog = true;
    private bool canExecute = true;
    
    public string EffectName => "CollectorStrikeEffect";
    public bool CanExecute => canExecute;
    
    #region IEffect 实现
    
    public void Initialize()
    {
        // 初始化逻辑（如果需要）
    }
    
    public void SetCanExecute(bool value)
    {
        canExecute = value;
    }
    
    public void SetTarget(string characterID)
    {
        collectorCharacterID = characterID;
    }
    
    public bool ExecuteEffect(SkillArgs args)
    {
        // 验证配置
        if (string.IsNullOrEmpty(collectorCharacterID))
        {
            Debug.LogWarning($"[{EffectName}] 收集者角色ID未设置！");
            return false;
        }
        
        // 从 DropItemTracker 获取本回合拾取的掉落物数量
        if (DropItemTracker.Instance == null)
        {
            Debug.LogError($"[{EffectName}] DropItemTracker 未找到！");
            return false;
        }
        
        int pickedUpCount = DropItemTracker.Instance.GetCurrentTurnPickups(collectorCharacterID);
        
        // 如果没有拾取任何掉落物，不造成伤害
        if (pickedUpCount <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] 本回合未拾取掉落物，不触发收集打击");
            }
            return false;
        }
        
        // 计算总伤害
        float totalDamage = damagePerItem * pickedUpCount;
        
        // 找到收集者角色的球对象
        GameObject collectorBall = FindCollectorBall();
        if (collectorBall == null)
        {
            Debug.LogError($"[{EffectName}] 未找到收集者角色的球对象！");
            return false;
        }
        
        // 找到最近的敌人
        GameObject nearestEnemy = FindNearestEnemy(collectorBall.transform.position);
        if (nearestEnemy == null)
        {
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] 场上没有敌人，收集打击未生效");
            }
            return false;
        }
        
        // 造成伤害
        DealDamageToEnemy(nearestEnemy, totalDamage, collectorBall);
        
        if (showDebugLog)
        {
            Debug.Log($"[{EffectName}] ✅ 收集打击生效！拾取数量={pickedUpCount}，" +
                     $"伤害系数={damagePerItem}，总伤害={totalDamage}，" +
                     $"目标={nearestEnemy.name}");
        }
        
        return true;
    }
    
    public void RemoveEffect()
    {
        // 收集打击是瞬时效果，无需清理
    }
    
    #endregion
    
    #region 配置方法
    
    /// <summary>
    /// 配置效果参数
    /// </summary>
    public void Configure(float damagePerItemValue, string collectorID, bool debugLog = true)
    {
        damagePerItem = damagePerItemValue;
        collectorCharacterID = collectorID;
        showDebugLog = debugLog;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 找到收集者角色的球对象
    /// </summary>
    GameObject FindCollectorBall()
    {
        // 查找所有玩家角色
        var allPlayers = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        
        foreach (var player in allPlayers)
        {
            // 通过 Player 的 CharacterID 进行匹配
            if (player.CharacterID == collectorCharacterID)
            {
                return player.gameObject;
            }
        }
        
        Debug.LogWarning($"[{EffectName}] 未找到收集者角色 {collectorCharacterID} 的球对象");
        return null;
    }
    
    /// <summary>
    /// 找到距离指定位置最近的敌人
    /// </summary>
    GameObject FindNearestEnemy(Vector3 fromPosition)
    {
        // 查找所有敌人
        var allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        
        if (allEnemies.Length == 0)
        {
            return null;
        }
        
        // 找到最近的敌人
        Enemy nearestEnemy = null;
        float minDistance = float.MaxValue;
        
        foreach (var enemy in allEnemies)
        {
            float distance = Vector3.Distance(fromPosition, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy?.gameObject;
    }
    
    /// <summary>
    /// 对敌人造成伤害
    /// </summary>
    void DealDamageToEnemy(GameObject enemyObj, float damage, GameObject sourceObj)
    {
        // 获取敌人的 IDamageable 接口
        IDamageable damageable = enemyObj.GetComponent<IDamageable>();
        if (damageable == null)
        {
            Debug.LogError($"[{EffectName}] 目标敌人没有 IDamageable 组件！");
            return;
        }
        
        // 检查是否可以受伤
        if (!damageable.CanTakeDamage())
        {
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] 目标敌人 {enemyObj.name} 当前无法受伤");
            }
            return;
        }
        
        // 构造伤害事件
        // ✅ 3D适配：使用XZ平面投影和真实3D位置
        Vector3 enemyPos3D = enemyObj.transform.position;
        Vector3 sourcePos3D = sourceObj.transform.position;
        Vector3 direction3D = (enemyPos3D - sourcePos3D).normalized;
        
        DamageEvent damageEvent = new DamageEvent
        {
            Source = sourceObj,
            Target = enemyObj,
            FinalDamage = damage,
            Type = DamageType.Physical,
            TriggerType = DamageTriggerType.Skill,  // 技能触发的伤害
            HitPosition = new Vector2(enemyPos3D.x, enemyPos3D.z),  // XZ平面投影（向后兼容）
            HitPosition3D = enemyPos3D,  // ✅ 真实3D位置（用于特效定位）
            HitDirection = new Vector2(direction3D.x, direction3D.z),  // XZ平面方向
            VelocityAtHit = 0f,
            KnockbackForce = 0f,  // 收集打击不击退
            StunDuration = 0f,
            CanBeBlocked = false,
            RuleName = "CollectorStrike",
            EventTime = Time.time
        };
        
        // 造成伤害
        damageable.OnDamageReceived(damageEvent);
        
        if (showDebugLog)
        {
            Debug.Log($"[{EffectName}] 对 {enemyObj.name} 造成 {damage} 点伤害");
        }
    }
    
    #endregion
}

