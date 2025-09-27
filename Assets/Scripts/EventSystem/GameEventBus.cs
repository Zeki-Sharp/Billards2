using UnityEngine;

/// <summary>
/// 统一事件总线 - 基于现有 MMEventManager 的封装
/// 提供统一的事件接口，同时保持与现有系统的兼容性
/// 
/// 【核心职责】：
/// - 提供统一的事件订阅接口 (C# Action)
/// - 提供统一的事件发布方法
/// - 封装游戏逻辑和表现的桥接
/// - 集成 MMEventManager 调用
/// 
/// 【设计原则】：
/// - 统一性：所有事件通过 GameEventBus 处理
/// - 简洁性：统一的事件接口，无冗余代码
/// - 扩展性：易于添加新事件类型
/// - 类型安全：编译时检查
/// </summary>
public static class GameEventBus
{
    #region 游戏逻辑事件 (C# Action)
    
    /// <summary>
    /// 攻击事件
    /// </summary>
    public static event System.Action<AttackData> OnAttack;
    
    /// <summary>
    /// 死亡事件
    /// </summary>
    public static event System.Action<DeathData> OnDeath;
    
    #endregion
    
    #region 瞄准线事件 (新增)
    
    /// <summary>
    /// 瞄准方向变化事件
    /// </summary>
    public static event System.Action<Vector2> OnAimDirectionChanged;
    
    /// <summary>
    /// 瞄准线可见性变化事件
    /// </summary>
    public static event System.Action<bool> OnAimVisibilityChanged;
    
    /// <summary>
    /// 发射事件
    /// </summary>
    public static event System.Action<Vector2, float> OnLaunch;
    
    #endregion
    
    #region 蓄力系统事件 (新增)
    
    /// <summary>
    /// 蓄力进度变化事件
    /// </summary>
    public static event System.Action<float> OnChargingProgressChanged;
    
    /// <summary>
    /// 力度变化事件
    /// </summary>
    public static event System.Action<float> OnForceChanged;
    
    /// <summary>
    /// 开始蓄力事件
    /// </summary>
    public static event System.Action OnChargingStarted;
    
    /// <summary>
    /// 停止蓄力事件
    /// </summary>
    public static event System.Action OnChargingStopped;
    
    /// <summary>
    /// 重置蓄力事件
    /// </summary>
    public static event System.Action OnChargingReset;
    
    #endregion
    
    #region 游戏状态事件 (新增)
    
    /// <summary>
    /// 玩家状态变化事件
    /// </summary>
    public static event System.Action<PlayerStateMachine.PlayerState> OnPlayerStateChanged;
    
    /// <summary>
    /// 游戏流程状态变化事件
    /// </summary>
    public static event System.Action<GameFlowController.GameFlowState> OnGameFlowStateChanged;
    
    #endregion
    
    #region 统一事件发布接口
    
    /// <summary>
    /// 发布攻击事件
    /// </summary>
    /// <param name="attackData">攻击数据</param>
    public static void PublishAttack(AttackData attackData)
    {
        // 触发游戏逻辑事件
        OnAttack?.Invoke(attackData);
        
        // 触发表现事件 (通过 MMEventManager)
        var attackEffectEvent = new AttackEffectEvent
        {
            AttackType = attackData.AttackType,
            Position = attackData.Position,
            Direction = attackData.Direction,
            Attacker = attackData.Attacker,
            Target = attackData.Target,
            Damage = attackData.Damage,
            AttackerTag = attackData.AttackerTag,
            TargetTag = attackData.TargetTag,
            HitNormal = attackData.HitNormal,
            HitSpeed = attackData.HitSpeed,
            WallHitRotationAngle = attackData.WallHitRotationAngle,
            WallHitPositionOffset = attackData.WallHitPositionOffset
        };
        
        MoreMountains.Tools.MMEventManager.TriggerEvent(attackEffectEvent);
    }
    
    /// <summary>
    /// 发布死亡事件
    /// </summary>
    /// <param name="deathData">死亡数据</param>
    public static void PublishDeath(DeathData deathData)
    {
        // 触发游戏逻辑事件
        OnDeath?.Invoke(deathData);
        
        // 触发表现事件 (通过 MMEventManager)
        var deathEffectEvent = new DeathEffectEvent
        {
            DeathType = deathData.DeathType,
            Position = deathData.Position,
            Direction = deathData.Direction,
            DeadObject = deathData.DeadObject,
            DeadObjectTag = deathData.DeadObjectTag
        };
        
        MoreMountains.Tools.MMEventManager.TriggerEvent(deathEffectEvent);
    }
    
    /// <summary>
    /// 发布瞄准方向变化事件
    /// </summary>
    /// <param name="direction">瞄准方向</param>
    public static void PublishAimDirectionChanged(Vector2 direction)
    {
        OnAimDirectionChanged?.Invoke(direction);
    }
    
    /// <summary>
    /// 发布瞄准线可见性变化事件
    /// </summary>
    /// <param name="isVisible">是否可见</param>
    public static void PublishAimVisibilityChanged(bool isVisible)
    {
        OnAimVisibilityChanged?.Invoke(isVisible);
    }
    
    /// <summary>
    /// 发布发射事件
    /// </summary>
    /// <param name="direction">发射方向</param>
    /// <param name="force">发射力度</param>
    public static void PublishLaunch(Vector2 direction, float force)
    {
        OnLaunch?.Invoke(direction, force);
    }
    
    /// <summary>
    /// 发布蓄力进度变化事件
    /// </summary>
    /// <param name="progress">蓄力进度 (0-1)</param>
    public static void PublishChargingProgressChanged(float progress)
    {
        OnChargingProgressChanged?.Invoke(progress);
    }
    
    /// <summary>
    /// 发布力度变化事件
    /// </summary>
    /// <param name="force">力度值</param>
    public static void PublishForceChanged(float force)
    {
        OnForceChanged?.Invoke(force);
    }
    
    /// <summary>
    /// 发布开始蓄力事件
    /// </summary>
    public static void PublishChargingStarted()
    {
        OnChargingStarted?.Invoke();
    }
    
    /// <summary>
    /// 发布停止蓄力事件
    /// </summary>
    public static void PublishChargingStopped()
    {
        OnChargingStopped?.Invoke();
    }
    
    /// <summary>
    /// 发布重置蓄力事件
    /// </summary>
    public static void PublishChargingReset()
    {
        OnChargingReset?.Invoke();
    }
    
    /// <summary>
    /// 发布玩家状态变化事件
    /// </summary>
    /// <param name="playerState">玩家状态</param>
    public static void PublishPlayerStateChanged(PlayerStateMachine.PlayerState playerState)
    {
        OnPlayerStateChanged?.Invoke(playerState);
    }
    
    /// <summary>
    /// 发布游戏流程状态变化事件
    /// </summary>
    /// <param name="gameFlowState">游戏流程状态</param>
    public static void PublishGameFlowStateChanged(GameFlowController.GameFlowState gameFlowState)
    {
        OnGameFlowStateChanged?.Invoke(gameFlowState);
    }
    
    #endregion
    
    #region 特效事件发布 (直接使用 MMEventManager)
    
    /// <summary>
    /// 发布特效事件 - 直接使用 MMEventManager
    /// </summary>
    /// <param name="effectType">特效类型</param>
    /// <param name="position">特效位置</param>
    /// <param name="direction">特效方向</param>
    /// <param name="targetObject">目标对象</param>
    /// <param name="targetTag">目标标签</param>
    public static void PublishEffectEvent(string effectType, Vector3 position, Vector3 direction = default, GameObject targetObject = null, string targetTag = "")
    {
        var effectEvent = new EffectEvent
        {
            EffectType = effectType,
            Position = position,
            Direction = direction,
            TargetObject = targetObject,
            TargetTag = targetTag,
            Intensity = 1f
        };
        
        MoreMountains.Tools.MMEventManager.TriggerEvent(effectEvent);
    }
    
    #endregion
    
    #region 简化工厂方法
    
    /// <summary>
    /// 发布简单攻击事件
    /// </summary>
    /// <param name="attackType">攻击类型</param>
    /// <param name="position">攻击位置</param>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">目标</param>
    /// <param name="damage">伤害值</param>
    public static void PublishSimpleAttack(string attackType, Vector3 position, GameObject attacker, GameObject target, float damage = 0f)
    {
        var attackData = new AttackData
        {
            AttackType = attackType,
            Position = position,
            Direction = Vector3.zero,
            Attacker = attacker,
            Target = target,
            Damage = damage,
            AttackTime = Time.time,
            AttackerTag = attacker?.tag ?? "",
            TargetTag = target?.tag ?? "",
            HitNormal = Vector3.zero,
            HitSpeed = 0f,
            WallHitRotationAngle = 0f,
            WallHitPositionOffset = Vector3.zero
        };
        PublishAttack(attackData);
    }
    
    /// <summary>
    /// 发布简单死亡事件
    /// </summary>
    /// <param name="deathType">死亡类型</param>
    /// <param name="position">死亡位置</param>
    /// <param name="deadObject">死亡对象</param>
    public static void PublishSimpleDeath(string deathType, Vector3 position, GameObject deadObject)
    {
        var deathData = new DeathData
        {
            DeathType = deathType,
            Position = position,
            Direction = Vector3.zero,
            DeadObject = deadObject,
            DeadObjectTag = deadObject?.tag ?? "",
            DeathTime = Time.time
        };
        PublishDeath(deathData);
    }
    
    #endregion
    
    #region 调试和统计
    
    /// <summary>
    /// 获取事件订阅统计信息
    /// </summary>
    /// <returns>事件订阅统计信息</returns>
    public static string GetEventStats()
    {
        return $"GameEventBus 事件订阅统计:\n" +
               $"- OnAttack: {OnAttack?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnDeath: {OnDeath?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnAimDirectionChanged: {OnAimDirectionChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnAimVisibilityChanged: {OnAimVisibilityChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnLaunch: {OnLaunch?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnChargingProgressChanged: {OnChargingProgressChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnForceChanged: {OnForceChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnChargingStarted: {OnChargingStarted?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnChargingStopped: {OnChargingStopped?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnChargingReset: {OnChargingReset?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnPlayerStateChanged: {OnPlayerStateChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnGameFlowStateChanged: {OnGameFlowStateChanged?.GetInvocationList()?.Length ?? 0} 订阅者";
    }
    
    #endregion
}
