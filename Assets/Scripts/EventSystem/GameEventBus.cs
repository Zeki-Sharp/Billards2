using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 事件优先级枚举
/// 用于控制事件处理器的执行顺序
/// </summary>
public enum EventPriority
{
    /// <summary>关键优先级（系统级）</summary>
    Critical = 0,
    
    /// <summary>高优先级（弱点判定、状态检查）</summary>
    High = 1,
    
    /// <summary>普通优先级（默认）</summary>
    Normal = 2,
    
    /// <summary>低优先级（伤害应用、UI更新）</summary>
    Low = 3,
    
    /// <summary>后台优先级（日志、统计）</summary>
    Background = 4
}

/// <summary>
/// 统一事件总线 - 基于现有 MMEventManager 的封装
/// 提供统一的事件接口，同时保持与现有系统的兼容性
/// 
/// 【核心职责】：
/// - 提供统一的事件订阅接口 (C# Action)
/// - 提供统一的事件发布方法
/// - 封装游戏逻辑和表现的桥接
/// - 集成 MMEventManager 调用
/// - 支持伤害处理流程
/// 
/// 【设计原则】：
/// - 统一性：所有事件通过 GameEventBus 处理
/// - 简洁性：统一的事件接口，无冗余代码
/// - 扩展性：易于添加新事件类型
/// - 类型安全：编译时检查
/// - 伤害处理：通过 DamageProcessor 统一处理
/// </summary>
public static class GameEventBus
{
    #region 核心游戏事件
    
    /// <summary>
    /// 攻击事件
    /// </summary>
    public static event System.Action<AttackData> OnAttack;
    
    /// <summary>
    /// 伤害处理完成事件
    /// </summary>
    public static event System.Action<ProcessedDamageData> OnDamageProcessed;
    
    /// <summary>
    /// 死亡事件
    /// </summary>
    public static event System.Action<DeathData> OnDeath;
    
    /// <summary>
    /// 发射事件
    /// </summary>
    public static event System.Action<Vector2, float> OnLaunch;
    
    #endregion
    
    #region 物理事件
    
    /// <summary>
    /// 球停止运动事件
    /// </summary>
    public static event System.Action<BallPhysics> OnBallStopped;
    
    /// <summary>
    /// 球开始运动事件
    /// </summary>
    public static event System.Action<BallPhysics> OnBallStarted;
    
    /// <summary>
    /// 球碰撞事件
    /// </summary>
    public static event System.Action<BallPhysics, BallPhysics> OnBallCollision;
    
    #endregion
    
    #region 玩家状态事件
    
    /// <summary>
    /// 玩家状态变化事件
    /// </summary>
    public static event System.Action<PlayerStateMachine.PlayerState> OnPlayerStateChanged;
    
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
    
    #region 游戏流程事件
    
    /// <summary>
    /// 游戏流程状态变化事件
    /// </summary>
    public static event System.Action<GameFlowState> OnGameFlowStateChanged;
    
    /// <summary>
    /// 游戏状态变化事件
    /// </summary>
    public static event System.Action<bool> OnGameStateChanged;
    
    /// <summary>
    /// 游戏结束事件
    /// </summary>
    public static event System.Action OnGameOver;
    
    /// <summary>
    /// 游戏胜利事件
    /// </summary>
    public static event System.Action OnGameWin;
    
    /// <summary>
    /// 游戏完成事件（所有关卡完成）
    /// </summary>
    public static event System.Action OnGameCompleted;
    
    #endregion
    
    #region 关卡事件
    
    /// <summary>
    /// 关卡开始事件
    /// </summary>
    public static event System.Action<int, LevelConfig> OnLevelStarted;
    
    /// <summary>
    /// 关卡完成事件
    /// </summary>
    public static event System.Action<int, LevelConfig> OnLevelCompleted;
    
    #endregion
    
    #region 技能系统事件
    
    /// <summary>
    /// 技能激活事件
    /// </summary>
    public static event System.Action<string> OnSkillActivated;
    
    /// <summary>
    /// 技能失效事件
    /// </summary>
    public static event System.Action<string> OnSkillDeactivated;
    
    /// <summary>
    /// 技能选择开始事件
    /// </summary>
    public static event System.Action<List<SkillConfig>> OnSkillSelectionStarted;
    
    /// <summary>
    /// 技能选择事件
    /// </summary>
    public static event System.Action<SkillConfig, List<SkillConfig>> OnSkillSelected;
    
    /// <summary>
    /// 技能添加到玩家事件
    /// </summary>
    public static event System.Action<SkillConfig> OnSkillAddedToPlayer;
    
    /// <summary>
    /// 技能选择完成事件
    /// </summary>
    public static event System.Action OnSkillSelectionCompleted;
    
    #endregion
    
    #region 游戏数据事件
    
    /// <summary>
    /// 分数变化事件
    /// </summary>
    public static event System.Action<int> OnScoreChanged;
    
    /// <summary>
    /// 生命值变化事件
    /// </summary>
    public static event System.Action<HealthStateData> OnHealthChanged;
    
    /// <summary>
    /// 波次变化事件
    /// </summary>
    public static event System.Action<int> OnWaveChanged;
    
    /// <summary>
    /// 初始敌人生成完成事件
    /// </summary>
    public static event System.Action OnInitialWaveSpawnComplete;
    
    /// <summary>
    /// 波次敌人生成完成事件
    /// </summary>
    public static event System.Action OnWaveEnemiesSpawnComplete;
    
    #endregion
    
    #region UI/表现事件
    
    /// <summary>
    /// 瞄准方向变化事件
    /// </summary>
    public static event System.Action<Vector2> OnAimDirectionChanged;
    
    /// <summary>
    /// 瞄准线可见性变化事件
    /// </summary>
    public static event System.Action<bool> OnAimVisibilityChanged;
    
    /// <summary>
    /// 蓄力进度变化事件
    /// </summary>
    public static event System.Action<float> OnChargingProgressChanged;
    
    /// <summary>
    /// 力度变化事件
    /// </summary>
    public static event System.Action<float> OnForceChanged;
    
    #endregion
    
    #region 事件发布方法
    
    /// <summary>
    /// 发布攻击事件
    /// </summary>
    public static void PublishAttack(AttackData attackData) => OnAttack?.Invoke(attackData);
    
    /// <summary>
    /// 发布伤害处理完成事件
    /// </summary>
    public static void PublishDamageProcessed(ProcessedDamageData processedData) => OnDamageProcessed?.Invoke(processedData);
    
    /// <summary>
    /// 发布死亡事件
    /// </summary>
    public static void PublishDeath(DeathData deathData) => OnDeath?.Invoke(deathData);
    
    /// <summary>
    /// 发布发射事件
    /// </summary>
    public static void PublishLaunch(Vector2 direction, float force) => OnLaunch?.Invoke(direction, force);
    
    /// <summary>
    /// 发布技能激活事件
    /// </summary>
    public static void PublishSkillActivated(string skillName) => OnSkillActivated?.Invoke(skillName);
    
    /// <summary>
    /// 发布技能失效事件
    /// </summary>
    public static void PublishSkillDeactivated(string skillName) => OnSkillDeactivated?.Invoke(skillName);
    
    /// <summary>
    /// 发布技能选择开始事件
    /// </summary>
    public static void PublishSkillSelectionStarted(List<SkillConfig> availableSkills) => OnSkillSelectionStarted?.Invoke(availableSkills);
    
    /// <summary>
    /// 发布技能选择事件
    /// </summary>
    public static void PublishSkillSelected(SkillConfig selectedSkill, List<SkillConfig> availableSkills) => OnSkillSelected?.Invoke(selectedSkill, availableSkills);
    
    /// <summary>
    /// 发布技能添加到玩家事件
    /// </summary>
    public static void PublishSkillAddedToPlayer(SkillConfig skill) => OnSkillAddedToPlayer?.Invoke(skill);
    
    /// <summary>
    /// 发布技能选择完成事件
    /// </summary>
    public static void PublishSkillSelectionCompleted() => OnSkillSelectionCompleted?.Invoke();
    
    /// <summary>
    /// 发布球停止运动事件
    /// </summary>
    public static void PublishBallStopped(BallPhysics ballPhysics) => OnBallStopped?.Invoke(ballPhysics);
    
    /// <summary>
    /// 发布球开始运动事件
    /// </summary>
    public static void PublishBallStarted(BallPhysics ballPhysics) => OnBallStarted?.Invoke(ballPhysics);
    
    /// <summary>
    /// 发布球碰撞事件
    /// </summary>
    public static void PublishBallCollision(BallPhysics ball1, BallPhysics ball2) => OnBallCollision?.Invoke(ball1, ball2);
    
    /// <summary>
    /// 发布玩家状态变化事件
    /// </summary>
    public static void PublishPlayerStateChanged(PlayerStateMachine.PlayerState playerState) => OnPlayerStateChanged?.Invoke(playerState);
    
    /// <summary>
    /// 发布开始蓄力事件
    /// </summary>
    public static void PublishChargingStarted() => OnChargingStarted?.Invoke();
    
    /// <summary>
    /// 发布停止蓄力事件
    /// </summary>
    public static void PublishChargingStopped() => OnChargingStopped?.Invoke();
    
    /// <summary>
    /// 发布重置蓄力事件
    /// </summary>
    public static void PublishChargingReset() => OnChargingReset?.Invoke();
    
    /// <summary>
    /// 发布游戏流程状态变化事件
    /// </summary>
    public static void PublishGameFlowStateChanged(GameFlowState gameFlowState) => OnGameFlowStateChanged?.Invoke(gameFlowState);
    
    /// <summary>
    /// 发布游戏状态变化事件
    /// </summary>
    public static void PublishGameStateChanged(bool isGameActive) => OnGameStateChanged?.Invoke(isGameActive);
    
    /// <summary>
    /// 发布游戏结束事件
    /// </summary>
    public static void PublishGameOver() => OnGameOver?.Invoke();
    
    /// <summary>
    /// 发布游戏胜利事件
    /// </summary>
    public static void PublishGameWin() => OnGameWin?.Invoke();
    
    /// <summary>
    /// 发布游戏完成事件
    /// </summary>
    public static void PublishGameCompleted() => OnGameCompleted?.Invoke();
    
    /// <summary>
    /// 发布关卡开始事件
    /// </summary>
    public static void PublishLevelStarted(int levelIndex, LevelConfig levelConfig) => OnLevelStarted?.Invoke(levelIndex, levelConfig);
    
    /// <summary>
    /// 发布关卡完成事件
    /// </summary>
    public static void PublishLevelCompleted(int levelIndex, LevelConfig levelConfig) => OnLevelCompleted?.Invoke(levelIndex, levelConfig);
    
    
    /// <summary>
    /// 发布分数变化事件
    /// </summary>
    public static void PublishScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    
    /// <summary>
    /// 发布生命值变化事件
    /// </summary>
    public static void PublishHealthChanged(HealthStateData healthData) => OnHealthChanged?.Invoke(healthData);
    
    /// <summary>
    /// 发布波次变化事件
    /// </summary>
    public static void PublishWaveChanged(int wave) => OnWaveChanged?.Invoke(wave);
    
    /// <summary>
    /// 发布初始敌人生成完成事件
    /// </summary>
    public static void PublishInitialWaveSpawnComplete() => OnInitialWaveSpawnComplete?.Invoke();
    
    /// <summary>
    /// 发布波次敌人生成完成事件
    /// </summary>
    public static void PublishWaveEnemiesSpawnComplete() => OnWaveEnemiesSpawnComplete?.Invoke();
    
    /// <summary>
    /// 发布瞄准方向变化事件
    /// </summary>
    public static void PublishAimDirectionChanged(Vector2 direction) => OnAimDirectionChanged?.Invoke(direction);
    
    /// <summary>
    /// 发布瞄准线可见性变化事件
    /// </summary>
    public static void PublishAimVisibilityChanged(bool isVisible) => OnAimVisibilityChanged?.Invoke(isVisible);
    
    /// <summary>
    /// 发布蓄力进度变化事件
    /// </summary>
    public static void PublishChargingProgressChanged(float progress) => OnChargingProgressChanged?.Invoke(progress);
    
    /// <summary>
    /// 发布力度变化事件
    /// </summary>
    public static void PublishForceChanged(float force) => OnForceChanged?.Invoke(force);
    
    #endregion
    
    #region 特效事件
    
    /// <summary>
    /// 特效事件
    /// </summary>
    public static event System.Action<EffectEvent> OnEffect;
    
    /// <summary>
    /// 发布特效事件 - 统一事件系统
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
        
        OnEffect?.Invoke(effectEvent);
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
        // 获取敌人类型
        EnemyType enemyType = EnemyType.Normal; // 默认为普通敌人
        if (deadObject != null)
        {
            var enemyBehavior = deadObject.GetComponent<EnemyBehavior>();
            if (enemyBehavior != null)
            {
                // TODO: 从EnemyBehavior获取敌人类型
                // 目前默认使用Normal
                enemyType = EnemyType.Normal;
            }
        }
        
        var deathData = new DeathData
        {
            DeathType = deathType,
            Position = position,
            Direction = Vector3.zero,
            DeadObject = deadObject,
            DeadObjectTag = deadObject?.tag ?? "",
            DeathTime = Time.time,
            // 新增字段
            target = deadObject,
            enemyType = enemyType
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
               $"- OnDamageProcessed: {OnDamageProcessed?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
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
               $"- OnGameFlowStateChanged: {OnGameFlowStateChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnGameStateChanged: {OnGameStateChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnScoreChanged: {OnScoreChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnHealthChanged: {OnHealthChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnWaveChanged: {OnWaveChanged?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnGameOver: {OnGameOver?.GetInvocationList()?.Length ?? 0} 订阅者\n" +
               $"- OnGameWin: {OnGameWin?.GetInvocationList()?.Length ?? 0} 订阅者";
    }
    
    #endregion
}