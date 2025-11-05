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
    
    /// <summary>
    /// 统一碰撞事件（新伤害系统）
    /// </summary>
    public static event System.Action<CollisionEvent> OnCollision;
    
    /// <summary>
    /// 停止事件（新伤害系统 - 球停止范围攻击）
    /// </summary>
    public static event System.Action<StoppedEvent> OnStopped;
    
    #endregion
    
    #region 新伤害系统事件
    
    /// <summary>
    /// 伤害事件（新伤害系统）
    /// </summary>
    public static event System.Action<DamageEvent> OnDamage;
    
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
    /// 玩家 Playing 阶段开始事件（PlayerStateMachine 已准备好）
    /// </summary>
    public static event System.Action OnPlayerPlayingPhaseStarted;
    
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
    
    /// <summary>
    /// 游戏重启事件（返回角色选择，重置所有状态）
    /// </summary>
    public static event System.Action OnGameRestart;
    
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
    
    /// <summary>
    /// 技能升级事件
    /// </summary>
    public static event System.Action<string, int> OnSkillUpgraded;
    
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
    
    #region 多角色系统事件（带角色ID）
    
    // ===== 原始输入事件（来自 GlobalInputManager）=====
    
    /// <summary>
    /// 球体被点击事件（原始输入）
    /// </summary>
    public static event System.Action<GameObject> OnBallClicked;
    
    /// <summary>
    /// 滚轮输入事件（原始输入）
    /// </summary>
    public static event System.Action<float> OnScrollInput;
    
    /// <summary>
    /// 发射输入事件（原始输入 - 左键点击但未击中球体）
    /// </summary>
    public static event System.Action OnLaunchInput;
    
    /// <summary>
    /// 取消输入事件（原始输入 - 右键点击）
    /// </summary>
    public static event System.Action OnCancelInput;
    
    // ===== 输入与控制事件 =====
    
    /// <summary>
    /// 角色被选中事件
    /// </summary>
    public static event System.Action<string> OnCharacterSelected;
    
    /// <summary>
    /// 角色被取消选中事件
    /// </summary>
    public static event System.Action<string> OnCharacterDeselected;
    
    /// <summary>
    /// 特定角色开始蓄力事件
    /// </summary>
    public static event System.Action<string> OnCharacterChargingStarted;
    
    /// <summary>
    /// 特定角色停止蓄力事件
    /// </summary>
    public static event System.Action<string, float> OnCharacterChargingStopped;
    
    /// <summary>
    /// 特定角色发射事件
    /// </summary>
    public static event System.Action<string, Vector2, float> OnCharacterLaunched;
    
    /// <summary>
    /// 特定角色完成发射事件（进入 Completed 状态）
    /// </summary>
    public static event System.Action<string> OnCharacterCompleted;
    
    // ===== 战斗事件 =====
    
    /// <summary>
    /// 特定角色受伤事件
    /// </summary>
    public static event System.Action<string, float, string> OnCharacterDamaged;
    
    /// <summary>
    /// 特定角色治疗事件
    /// </summary>
    public static event System.Action<string, float> OnCharacterHealed;
    
    /// <summary>
    /// 特定角色死亡事件
    /// </summary>
    public static event System.Action<string> OnCharacterDied;
    
    // ===== 技能事件 =====
    
    /// <summary>
    /// 给特定角色添加技能事件
    /// </summary>
    public static event System.Action<string, string> OnCharacterSkillAdded;
    
    /// <summary>
    /// 特定角色的技能激活事件
    /// </summary>
    public static event System.Action<string, string> OnCharacterSkillActivated;
    
    /// <summary>
    /// 移除特定角色的技能事件
    /// </summary>
    public static event System.Action<string, string> OnCharacterSkillRemoved;
    
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
    /// 发布技能升级事件
    /// </summary>
    public static void PublishSkillUpgraded(string skillName, int newLevel) => OnSkillUpgraded?.Invoke(skillName, newLevel);
    
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
    /// 发布统一碰撞事件（新伤害系统）
    /// </summary>
    public static void PublishCollision(CollisionEvent collisionEvent) => OnCollision?.Invoke(collisionEvent);
    
    /// <summary>
    /// 发布停止事件（新伤害系统）
    /// </summary>
    public static void PublishStopped(StoppedEvent stoppedEvent) => OnStopped?.Invoke(stoppedEvent);
    
    /// <summary>
    /// 发布伤害事件（新伤害系统）
    /// </summary>
    public static void PublishDamage(DamageEvent damageEvent) => OnDamage?.Invoke(damageEvent);
    
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
    /// 发布玩家 Playing 阶段开始事件
    /// </summary>
    public static void PublishPlayerPlayingPhaseStarted() => OnPlayerPlayingPhaseStarted?.Invoke();
    
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
    /// 发布游戏重启事件
    /// </summary>
    public static void PublishGameRestart() => OnGameRestart?.Invoke();
    
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
    
    // ===== 多角色系统事件发布方法 =====
    
    // --- 原始输入事件 ---
    
    /// <summary>
    /// 发布球体被点击事件
    /// </summary>
    /// <param name="ballObject">被点击的球体GameObject</param>
    public static void PublishBallClicked(GameObject ballObject) => OnBallClicked?.Invoke(ballObject);
    
    /// <summary>
    /// 发布滚轮输入事件
    /// </summary>
    /// <param name="scrollDelta">滚轮滚动量</param>
    public static void PublishScrollInput(float scrollDelta) => OnScrollInput?.Invoke(scrollDelta);
    
    /// <summary>
    /// 发布发射输入事件
    /// </summary>
    public static void PublishLaunchInput() => OnLaunchInput?.Invoke();
    
    /// <summary>
    /// 发布取消输入事件
    /// </summary>
    public static void PublishCancelInput() => OnCancelInput?.Invoke();
    
    /// <summary>
    /// 发布角色被选中事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public static void PublishCharacterSelected(string characterID) => OnCharacterSelected?.Invoke(characterID);
    
    /// <summary>
    /// 发布角色被取消选中事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public static void PublishCharacterDeselected(string characterID) => OnCharacterDeselected?.Invoke(characterID);
    
    /// <summary>
    /// 发布特定角色开始蓄力事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public static void PublishCharacterChargingStarted(string characterID) => OnCharacterChargingStarted?.Invoke(characterID);
    
    /// <summary>
    /// 发布特定角色停止蓄力事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="force">蓄力力度</param>
    public static void PublishCharacterChargingStopped(string characterID, float force) => OnCharacterChargingStopped?.Invoke(characterID, force);
    
    /// <summary>
    /// 发布特定角色发射事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="direction">发射方向</param>
    /// <param name="force">发射力度</param>
    public static void PublishCharacterLaunched(string characterID, Vector2 direction, float force) => OnCharacterLaunched?.Invoke(characterID, direction, force);
    
    /// <summary>
    /// 发布特定角色完成发射事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public static void PublishCharacterCompleted(string characterID) => OnCharacterCompleted?.Invoke(characterID);
    
    /// <summary>
    /// 发布特定角色受伤事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="damage">伤害值</param>
    /// <param name="sourceID">伤害来源ID</param>
    public static void PublishCharacterDamaged(string characterID, float damage, string sourceID) => OnCharacterDamaged?.Invoke(characterID, damage, sourceID);
    
    /// <summary>
    /// 发布特定角色治疗事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="amount">治疗量</param>
    public static void PublishCharacterHealed(string characterID, float amount) => OnCharacterHealed?.Invoke(characterID, amount);
    
    /// <summary>
    /// 发布特定角色死亡事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public static void PublishCharacterDied(string characterID) => OnCharacterDied?.Invoke(characterID);
    
    /// <summary>
    /// 发布给特定角色添加技能事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="skillID">技能ID</param>
    public static void PublishCharacterSkillAdded(string characterID, string skillID) => OnCharacterSkillAdded?.Invoke(characterID, skillID);
    
    /// <summary>
    /// 发布特定角色的技能激活事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="skillID">技能ID</param>
    public static void PublishCharacterSkillActivated(string characterID, string skillID) => OnCharacterSkillActivated?.Invoke(characterID, skillID);
    
    /// <summary>
    /// 发布移除特定角色的技能事件
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="skillID">技能ID</param>
    public static void PublishCharacterSkillRemoved(string characterID, string skillID) => OnCharacterSkillRemoved?.Invoke(characterID, skillID);
    
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
    /// 发布简单死亡事件（向后兼容，无击杀者信息）
    /// </summary>
    /// <param name="deathType">死亡类型</param>
    /// <param name="position">死亡位置</param>
    /// <param name="deadObject">死亡对象</param>
    public static void PublishSimpleDeath(string deathType, Vector3 position, GameObject deadObject)
    {
        PublishSimpleDeath(deathType, position, deadObject, null);
    }
    
    /// <summary>
    /// 发布简单死亡事件（带击杀者信息）
    /// </summary>
    /// <param name="deathType">死亡类型</param>
    /// <param name="position">死亡位置</param>
    /// <param name="deadObject">死亡对象</param>
    /// <param name="attacker">击杀者对象</param>
    public static void PublishSimpleDeath(string deathType, Vector3 position, GameObject deadObject, GameObject attacker)
    {
        // 获取敌人类型
        EnemyType enemyType = EnemyType.Normal; // 默认为普通敌人
        if (deadObject != null)
        {
            var enemyBehavior = deadObject.GetComponent<EnemyBehavior>();
            if (enemyBehavior != null)
            {
                // 目前默认使用Normal类型
                enemyType = EnemyType.Normal;
            }
        }
        
        // ✅ 获取击杀者角色ID
        string attackerCharacterID = null;
        if (attacker != null)
        {
            attackerCharacterID = TriggerHelper.GetCharacterID(attacker);
        }
        
        var deathData = new DeathData
        {
            DeathType = deathType,
            Position = position,
            Direction = Vector3.zero,
            DeadObject = deadObject,
            DeadObjectTag = deadObject?.tag ?? "",
            DeathTime = Time.time,
            Attacker = attacker,
            AttackerCharacterID = attackerCharacterID,
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