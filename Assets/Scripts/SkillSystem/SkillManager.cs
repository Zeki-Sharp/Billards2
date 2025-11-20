using System.Collections.Generic;
using System.Linq;  // ✅ 多角色系统：LINQ 支持
using UnityEngine;

/// <summary>
/// 技能管理器 - 管理所有技能配置和运行时状态
/// 替代 TestSkillChain，提供可配置的技能系统
/// 使用单例模式，跨场景保留技能数据
/// 
/// 【执行顺序】：SYSTEM 层 (-50)，早于 Controller 层
/// 【依赖】：GameManager (CORE 层)
/// 【初始化】：OnManagerCreated 中订阅事件，Start 中初始化场景相关对象
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class SkillManager : SingletonManager<SkillManager>
{
    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool enableDebugLog = true;
    
    /// <summary>
    /// 技能状态管理器引用
    /// </summary>
    private SkillStateManager skillStateManager;
    
    // 技能实例管理
    private Dictionary<string, SkillInstance> skillInstances = new Dictionary<string, SkillInstance>();
    
    // ✅ 多角色系统改造：技能归属映射
    /// <summary>
    /// 角色技能映射：characterID → List<SkillConfig>
    /// </summary>
    private Dictionary<string, List<SkillConfig>> characterSkills = new Dictionary<string, List<SkillConfig>>();
    
    /// <summary>
    /// 技能归属映射：skillInstanceID → characterID
    /// 用于快速查询技能归属于哪个角色
    /// </summary>
    private Dictionary<string, string> skillOwnership = new Dictionary<string, string>();
    
    /// <summary>
    /// DropItem类型技能名称列表（用于掉落系统）
    /// </summary>
    private HashSet<string> dropItemSkillNames = new HashSet<string>();
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => enableDebugLog;
    
    protected override void OnManagerCreated()
    {
        // ✅ Manager 自身初始化（事件订阅）
        GameEventBus.OnGameRestart += ResetState;
        SubscribeToEvents();
        
        if (enableDebugLog)
        {
            Debug.Log("[SkillManager] 单例创建成功（SYSTEM 层），将跨场景保留");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅游戏重启事件
        GameEventBus.OnGameRestart -= ResetState;
        
        UnsubscribeFromEvents();
    }
    
    #endregion
    
    #region Unity生命周期
    
    void Start()
    {
        // ✅ 场景相关初始化（需要查找场景对象）
        skillStateManager = FindFirstObjectByType<SkillStateManager>();
        if (skillStateManager == null)
        {
            Debug.LogWarning("[SkillManager] 未找到SkillStateManager，技能状态跟踪功能将不可用");
        }
        
        // ✅ 重新初始化技能实例（保持技能配置，重建实例）
        ReinitializeSkillInstances();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 重新初始化技能实例（用于新场景）
    /// </summary>
    void ReinitializeSkillInstances()
    {
        // 清除旧的实例
        skillInstances.Clear();
        dropItemSkillNames.Clear();
        
        // ✅ 多角色系统：从 characterSkills 重新创建技能实例
        foreach (var kvp in characterSkills)
        {
            string characterID = kvp.Key;
            foreach (var skillConfig in kvp.Value)
            {
                if (skillConfig != null && skillConfig.IsValid())
                {
                    var skillInstance = skillConfig.CreateSkillInstance();
                    if (skillInstance != null)
                    {
                        // ✅ 修复：使用统一的 key 格式（characterID_skillName）
                        string skillInstanceID = $"{characterID}_{skillConfig.skillName}";
                        if (skillInstances.ContainsKey(skillInstanceID))
                        {
                            int duplicateIndex = 1;
                            string originalId = skillInstanceID;
                            while (skillInstances.ContainsKey(skillInstanceID))
                            {
                                skillInstanceID = $"{originalId}#{duplicateIndex}";
                                duplicateIndex++;
                            }
                            if (enableDebugLog)
                            {
                                Debug.LogWarning($"[SkillManager] 检测到技能名称冲突：{originalId}，自动重命名为 {skillInstanceID}" );
                            }
                        }
                        skillInstances[skillInstanceID] = skillInstance;
                        
                        // ✅ 设置归属角色
                        skillInstance.SetOwner(characterID);
                        
                        // ✅ 记录技能归属
                        skillOwnership[skillInstanceID] = characterID;
                        
                        // 检查是否为DropItem类型技能（从当前等级获取）
                        var currentLevelConfig = skillConfig.GetLevelConfig(skillInstance.currentLevel);
                        if (currentLevelConfig?.effectConfig is DropItemEffectConfig)
                        {
                            dropItemSkillNames.Add(skillConfig.skillName);
                            if (enableDebugLog)
                            {
                                Debug.Log($"SkillManager: 重新注册DropItem技能 - {skillConfig.skillName} Lv{skillInstance.currentLevel} (归属: {kvp.Key})");
                            }
                        }
                        
                        // 通知技能状态管理器技能已激活
                        skillStateManager?.AddActiveSkill(skillConfig.skillName);
                        
                        if (enableDebugLog)
                        {
                            Debug.Log($"SkillManager: 重新初始化技能实例 - {skillConfig.skillName} (归属: {kvp.Key})");
                        }

                        TryExecuteImmediateSkill(skillInstance);
                    }
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SkillManager: 技能实例初始化完成，共 {skillInstances.Count} 个技能");
        }
        
        if (enableDebugLog)
        {
            foreach (var skillName in skillInstances.Keys)
            {
                Debug.Log($"SkillManager: 已重新加载技能实例 - {skillName}");
            }
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnCollision += HandleCollisionEvent;
        GameEventBus.OnDeath += HandleDeathEvent;
        GameEventBus.OnHealthChanged += HandleHealthChangedEvent;
        GameEventBus.OnGameFlowStateChanged += HandleGameFlowStateChanged;
        GameEventBus.OnBallStopped += HandleBallStoppedEvent;
        GameEventBus.OnStopped += HandleStoppedEvent;  // ✅ 3D适配：优先使用带轨迹数据的StoppedEvent
        GameEventBus.OnDamage += HandleDamageEvent;  // ✅ 新增：监听伤害事件（用于点燃等DoT技能）
        
        if (enableDebugLog)
        {
            Debug.Log("SkillManager: 已订阅所有相关事件");
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnCollision -= HandleCollisionEvent;
        GameEventBus.OnDeath -= HandleDeathEvent;
        GameEventBus.OnHealthChanged -= HandleHealthChangedEvent;
        GameEventBus.OnGameFlowStateChanged -= HandleGameFlowStateChanged;
        GameEventBus.OnBallStopped -= HandleBallStoppedEvent;
        GameEventBus.OnStopped -= HandleStoppedEvent;  // ✅ 3D适配：取消订阅StoppedEvent
        GameEventBus.OnDamage -= HandleDamageEvent;  // ✅ 新增：取消订阅伤害事件
        
        if (enableDebugLog)
        {
            Debug.Log("SkillManager: 已取消事件订阅");
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理碰撞事件
    /// </summary>
    void HandleCollisionEvent(CollisionEvent collisionEvent)
    {
        // 只处理玩家发起的碰撞
        if (collisionEvent.Source == null || !collisionEvent.Source.CompareTag("Player"))
        {
            return;
        }
        
        // 遍历所有技能实例，检查是否需要处理此碰撞事件
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(collisionEvent, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(collisionEvent);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发（碰撞事件）");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理死亡事件
    /// </summary>
    void HandleDeathEvent(DeathData deathData)
    {
        // 只处理死亡相关的技能
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(deathData, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(deathData);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
                }
            }
        }
    }
    
    /// <summary>
    /// ✅ 新增：处理伤害事件（用于点燃等DoT技能）
    /// </summary>
    void HandleDamageEvent(DamageEvent damageEvent)
    {
        // 遍历所有技能实例，检查是否需要处理此伤害事件
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(damageEvent, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(damageEvent);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发（伤害事件）");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理生命值变化事件
    /// </summary>
    void HandleHealthChangedEvent(HealthStateData healthData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到生命值变化事件: {healthData.CurrentHealth}/{healthData.MaxHealth} ({healthData.HealthPercentage:P1})");
        }
        
        // 只处理血量相关的技能
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(healthData, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(healthData);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理游戏流程状态变化事件
    /// </summary>
    void HandleGameFlowStateChanged(GameFlowState newState)
    {
        // 遍历所有技能实例，检查是否需要处理此阶段变化
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(newState, skillInstance))
            {
                skillInstance.ProcessEvent(newState);
            }
        }
        
        // 通知所有技能实例处理回合结束事件（重置条件等）
        foreach (var skillInstance in skillInstances.Values)
        {
            skillInstance.HandlePhaseEndEvent(newState);
        }
    }
    
    /// <summary>
    /// 处理球停止事件 - 用于触发 MovingEnd 技能（后备方案，当没有StoppedEvent时使用）
    /// </summary>
    void HandleBallStoppedEvent(BallPhysics ballPhysics)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到球停止事件（BallPhysics）: {ballPhysics.gameObject.name}");
        }
        
        // 只处理玩家球的停止事件
        if (ballPhysics.gameObject.CompareTag("Player"))
        {
            // 处理所有 MovingEnd 类型的技能
            foreach (var skillInstance in skillInstances.Values)
            {
                if (IsEventRelevantForSkill(ballPhysics, skillInstance))
                {
                    bool processed = skillInstance.ProcessEvent(ballPhysics);
                    if (processed && enableDebugLog)
                    {
                        Debug.Log($"[SkillManager] MovingEnd 技能 {skillInstance.config.skillName} 被触发（BallPhysics）");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// ✅ 3D适配：处理停止事件 - 优先使用带轨迹数据的StoppedEvent
    /// </summary>
    void HandleStoppedEvent(StoppedEvent stoppedEvent)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到停止事件（StoppedEvent）: {stoppedEvent.Source?.name}, 位置3D: {stoppedEvent.StoppedPosition3D}");
        }
        
        // 只处理玩家球的停止事件
        if (stoppedEvent.Source != null && stoppedEvent.Source.CompareTag("Player"))
        {
            // 处理所有 MovingEnd 类型的技能
            foreach (var skillInstance in skillInstances.Values)
            {
                if (IsEventRelevantForSkill(stoppedEvent, skillInstance))
                {
                    bool processed = skillInstance.ProcessEvent(stoppedEvent);
                    if (processed && enableDebugLog)
                    {
                        Debug.Log($"[SkillManager] MovingEnd 技能 {skillInstance.config.skillName} 被触发（StoppedEvent）");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 检查事件是否与技能相关
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="skillInstance">技能实例</param>
    /// <returns>是否相关</returns>
    private bool IsEventRelevantForSkill(object eventData, SkillInstance skillInstance)
    {
        // 获取当前等级的触发器
        var trigger = skillInstance.currentLevelInstance?.trigger;
        if (trigger == null) return false;
        
        // 根据触发器的类型判断事件相关性
        if (trigger is DataSourceTrigger dataSourceTrigger)
        {
            // DataSourceTrigger 根据配置的数据提取器类型判断
            var currentLevelConfig = skillInstance.config.GetLevelConfig(skillInstance.currentLevel);
            var dataSourceConfig = currentLevelConfig?.triggerConfig as DataSourceTriggerConfig;
            
            if (dataSourceConfig != null)
            {
                switch (dataSourceConfig.dataExtractorType)
                {
                    case DataExtractorType.Health:
                        return eventData is HealthStateData;
                    case DataExtractorType.Attack:
                        return eventData is AttackData;
                    case DataExtractorType.Defense:
                        return eventData is AttackData; // 防御通常与攻击事件相关
                    case DataExtractorType.Speed:
                        return false; // 速度变化事件暂未实现
                    case DataExtractorType.Mana:
                        return false; // 法力变化事件暂未实现
                    default:
                        return false;
                }
            }
            
            return false;
        }
        else if (trigger is CollisionTrigger)
        {
            return eventData is CollisionEvent;
        }
        else if (trigger is KillTrigger)
        {
            // KillTrigger 只对死亡事件有效
            return eventData is DeathData;
        }
        else if (trigger is MovingEndTrigger)
        {
            // ✅ 3D适配：MovingEndTrigger 对球停止事件有效（优先StoppedEvent，后备BallPhysics）
            return eventData is StoppedEvent || eventData is BallPhysics;
        }
        else if (trigger is DamageTrigger)
        {
            // DamageTrigger 只对伤害事件有效
            return eventData is DamageEvent;
        }
        else if (trigger is AlwaysTrueTrigger)
        {
            // AlwaysTrueTrigger 对所有事件都有效（总是返回true）
            return true;
        }
        else if (trigger is PhaseStateTrigger)
        {
            // PhaseStateTrigger 只对 GameFlowState 事件有效
            return eventData is GameFlowState;
        }
        
        // 默认情况下，不处理任何事件
        return false;
    }
    
    #endregion
    
    #region 公共方法
    
    #region 多角色系统 - 技能归属管理
    
    /// <summary>
    /// ✅ 多角色系统：添加技能到指定角色
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="skillConfig">技能配置</param>
    public void AddSkillToCharacter(string characterID, SkillConfig skillConfig)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError("[SkillManager] 无法添加技能：角色ID为空");
            return;
        }
        
        if (skillConfig == null || !skillConfig.IsValid())
        {
            Debug.LogError("[SkillManager] 无法添加无效的技能配置");
            return;
        }
        
        // 初始化角色技能列表（如果不存在）
        if (!characterSkills.ContainsKey(characterID))
        {
            characterSkills[characterID] = new List<SkillConfig>();
        }
        
        // 添加技能到角色列表
        characterSkills[characterID].Add(skillConfig);
        
        // 创建技能实例并设置归属
        var skillInstance = skillConfig.CreateSkillInstance();
        if (skillInstance != null)
        {
            // 生成唯一的技能实例ID：characterID + skillName
            string skillInstanceID = $"{characterID}_{skillConfig.skillName}";
            
            // ✅ 设置归属角色（会自动传递给触发器和效果）
            skillInstance.SetOwner(characterID);
            
            // 存储技能实例
            skillInstances[skillInstanceID] = skillInstance;
            
            // 记录技能归属
            skillOwnership[skillInstanceID] = characterID;
            
            // 检查是否为DropItem类型技能
            var currentLevelConfig = skillConfig.GetLevelConfig(skillInstance.currentLevel);
            if (currentLevelConfig?.effectConfig is DropItemEffectConfig)
            {
                dropItemSkillNames.Add(skillInstanceID);
                if (enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 注册DropItem技能 - {skillConfig.skillName} Lv{skillInstance.currentLevel} → 角色 {characterID}");
                }
            }
            
            // 通知技能状态管理器
            skillStateManager?.AddActiveSkill(skillConfig.skillName);
            
            if (enableDebugLog)
            {
                Debug.Log($"[SkillManager] ✅ 添加技能 '{skillConfig.skillName}' 到角色 '{characterID}'");
            }

            TryExecuteImmediateSkill(skillInstance);
        }
        else
        {
            Debug.LogError($"[SkillManager] 技能实例创建失败：{skillConfig.skillName}");
        }
    }
    
    /// <summary>
    /// ✅ 多角色系统：获取指定角色的所有技能
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>技能配置列表</returns>
    public List<SkillConfig> GetCharacterSkills(string characterID)
    {
        if (characterSkills.TryGetValue(characterID, out var skills))
        {
            return new List<SkillConfig>(skills);  // 返回副本
        }
        return new List<SkillConfig>();
    }
    
    /// <summary>
    /// ✅ 多角色系统：获取技能的归属角色ID
    /// </summary>
    /// <param name="skillInstanceID">技能实例ID</param>
    /// <returns>角色ID，如果未找到返回 null</returns>
    public string GetSkillOwner(string skillInstanceID)
    {
        if (skillOwnership.TryGetValue(skillInstanceID, out var characterID))
        {
            return characterID;
        }
        return null;
    }
    
    /// <summary>
    /// ✅ 多角色系统：获取指定角色的所有技能实例
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>技能实例列表</returns>
    public List<SkillInstance> GetCharacterSkillInstances(string characterID)
    {
        List<SkillInstance> characterSkills = new List<SkillInstance>();
        
        foreach (var kvp in skillInstances)
        {
            if (skillOwnership.TryGetValue(kvp.Key, out string owner) && owner == characterID)
            {
                characterSkills.Add(kvp.Value);
            }
        }
        
        return characterSkills;
    }
    
    /// <summary>
    /// ✅ 多角色系统：移除指定角色的所有技能（角色死亡时调用）
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public void RemoveCharacterSkills(string characterID)
    {
        if (!characterSkills.ContainsKey(characterID))
        {
            return;
        }
        
        // 获取角色的所有技能实例ID
        var skillInstanceIDs = skillOwnership
            .Where(kvp => kvp.Value == characterID)
            .Select(kvp => kvp.Key)
            .ToList();
        
        // 移除技能实例
        foreach (var skillInstanceID in skillInstanceIDs)
        {
            if (skillInstances.ContainsKey(skillInstanceID))
            {
                var skillInstance = skillInstances[skillInstanceID];
                skillInstances.Remove(skillInstanceID);
                
                // 移除DropItem记录
                dropItemSkillNames.Remove(skillInstanceID);
                
                // 通知技能状态管理器
                skillStateManager?.RemoveActiveSkill(skillInstance.config.skillName);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 移除角色 '{characterID}' 的技能：{skillInstance.config.skillName}");
                }
            }
            
            // 移除归属记录
            skillOwnership.Remove(skillInstanceID);
        }
        
        // 移除角色技能列表
        characterSkills.Remove(characterID);
        
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] ✅ 已移除角色 '{characterID}' 的所有技能（共 {skillInstanceIDs.Count} 个）");
        }
    }
    
    /// <summary>
    /// ✅ 多角色系统：获取所有角色ID
    /// </summary>
    /// <returns>角色ID列表</returns>
    public List<string> GetAllCharacterIDs()
    {
        return new List<string>(characterSkills.Keys);
    }
    
    #endregion
    
    /// <summary>
    /// 移除技能配置
    /// </summary>
    public void RemoveSkill(string skillName)
    {
        if (skillInstances.ContainsKey(skillName))
        {
            skillInstances.Remove(skillName);
            
            // 通知技能状态管理器技能已失效
            skillStateManager?.RemoveActiveSkill(skillName);
            
            if (enableDebugLog)
            {
                Debug.Log($"SkillManager: 移除技能 - {skillName}");
            }
        }
    }
    
    /// <summary>
    /// 重新加载所有技能
    /// </summary>
    [ContextMenu("重新加载技能")]
    public void ReloadSkills()
    {
        ReinitializeSkillInstances();
    }
    
    /// <summary>
    /// 重置技能管理器状态（游戏重启时调用）
    /// </summary>
    public void ResetState()
    {
        // ✅ 多角色系统：清空角色技能映射
        characterSkills.Clear();
        skillOwnership.Clear();
        
        // 复用现有的重新初始化方法（清空实例和掉落记录）
        ReinitializeSkillInstances();
        
        // 清空技能状态管理器
        if (skillStateManager != null)
        {
            // 暂时不处理，SkillStateManager会随场景销毁
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[SkillManager] 重置完成 - 所有技能已清空（包括角色技能映射）");
        }
    }
    
    /// <summary>
    /// 获取技能实例
    /// </summary>
    public SkillInstance GetSkillInstance(string skillName)
    {
        skillInstances.TryGetValue(skillName, out SkillInstance skillInstance);
        return skillInstance;
    }

    /// <summary>
    /// 尝试立即执行无需事件驱动的技能（如被动属性增益）
    /// </summary>
    /// <param name="skillInstance">技能实例</param>
    private void TryExecuteImmediateSkill(SkillInstance skillInstance)
    {
        if (skillInstance?.currentLevelInstance?.trigger is AlwaysTrueTrigger)
        {
            // 允许重新执行一次
            skillInstance.currentLevelInstance?.effect?.SetCanExecute(true);

            bool executed = skillInstance.ProcessEvent(null);
            
            if (enableDebugLog)
            {
                Debug.Log(executed
                    ? $"[SkillManager] 被动技能 '{skillInstance.config.skillName}' 立即生效"
                    : $"[SkillManager] 被动技能 '{skillInstance.config.skillName}' 立即生效失败，等待后续事件");
            }
        }
    }
    
    /// <summary>
    /// 获取所有技能名称
    /// </summary>
    public List<string> GetAllSkillNames()
    {
        return new List<string>(skillInstances.Keys);
    }
    
    /// <summary>
    /// 获取所有技能实例
    /// </summary>
    public List<SkillInstance> GetAllActiveSkills()
    {
        return new List<SkillInstance>(skillInstances.Values);
    }
    
    /// <summary>
    /// 获取所有DropItem类型技能名称（用于掉落系统）
    /// </summary>
    public HashSet<string> GetDropItemSkillNames()
    {
        return new HashSet<string>(dropItemSkillNames);
    }
    
    /// <summary>
    /// 检查指定技能是否为DropItem类型
    /// </summary>
    public bool IsDropItemSkill(string skillName)
    {
        return dropItemSkillNames.Contains(skillName);
    }
    
    /// <summary>
    /// 检查是否有指定类型的激活技能
    /// </summary>
    /// <typeparam name="T">效果配置类型</typeparam>
    /// <returns>是否有该类型的技能</returns>
    public bool HasActiveSkillOfType<T>() where T : EffectBase
    {
        foreach (var skillInstance in skillInstances.Values)
        {
            // 从当前等级获取效果类型
            var currentLevelConfig = skillInstance.config.GetLevelConfig(skillInstance.currentLevel);
            if (currentLevelConfig?.effectConfig is T)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// 升级技能到指定等级
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <param name="newLevel">新等级</param>
    /// <returns>是否升级成功</returns>
    public bool UpgradeSkill(string skillName, int newLevel)
    {
        if (!skillInstances.ContainsKey(skillName))
        {
            Debug.LogError($"SkillManager: 技能 {skillName} 不存在，无法升级");
            return false;
        }
        
        var skillInstance = skillInstances[skillName];
        
        // 检查是否可以升级
        if (newLevel <= skillInstance.currentLevel)
        {
            Debug.LogWarning($"SkillManager: 技能 {skillName} 无法降级到等级 {newLevel}");
            return false;
        }
        
        // 执行升级
        bool upgradeSuccess = skillInstance.UpgradeToLevel(newLevel);
        if (upgradeSuccess)
        {
            // 更新DropItem技能列表
            UpdateDropItemSkillList(skillName, skillInstance);
            
            TryExecuteImmediateSkill(skillInstance);
            
            if (enableDebugLog)
            {
                Debug.Log($"SkillManager: 技能 {skillName} 升级到等级 {newLevel}");
            }
            
            // 发布技能升级事件（暂时注释，等待Unity重新编译）
            // GameEventBus.PublishSkillUpgraded(skillName, skillInstance.currentLevel);
        }
        
        return upgradeSuccess;
    }
    
    /// <summary>
    /// 检查技能是否可以升级
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <returns>是否可以升级</returns>
    public bool CanUpgradeSkill(string skillName)
    {
        if (!skillInstances.ContainsKey(skillName))
        {
            return false;
        }
        
        return skillInstances[skillName].CanUpgrade();
    }
    
    /// <summary>
    /// 获取技能的下一个等级
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <returns>下一个等级，如果没有返回-1</returns>
    public int GetSkillNextLevel(string skillName)
    {
        if (!skillInstances.ContainsKey(skillName))
        {
            return -1;
        }
        
        return skillInstances[skillName].GetNextLevel();
    }
    
    /// <summary>
    /// 角色生成后通知技能管理器，重新激活该角色的被动技能效果
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public void NotifyCharacterSpawned(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            return;
        }

        foreach (var kvp in skillInstances)
        {
            if (skillOwnership.TryGetValue(kvp.Key, out var owner) && owner == characterID)
            {
                TryExecuteImmediateSkill(kvp.Value);
            }
        }
    }
    
    /// <summary>
    /// 更新DropItem技能列表
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <param name="skillInstance">技能实例</param>
    private void UpdateDropItemSkillList(string skillName, SkillInstance skillInstance)
    {
        // 移除旧的DropItem注册
        dropItemSkillNames.Remove(skillName);
        
        // 检查新等级是否为DropItem类型
        var currentLevelConfig = skillInstance.config.GetLevelConfig(skillInstance.currentLevel);
        if (currentLevelConfig?.effectConfig is DropItemEffectConfig)
        {
            dropItemSkillNames.Add(skillName);
            if (enableDebugLog)
            {
                Debug.Log($"SkillManager: 更新DropItem技能 - {skillName} Lv{skillInstance.currentLevel}");
            }
        }
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 显示技能状态信息
    /// </summary>
    [ContextMenu("显示技能状态")]
    public void ShowSkillStatus()
    {
        Debug.Log("=== 技能状态信息 ===");
        
        // ✅ 显示角色技能映射
        Debug.Log($"角色数量: {characterSkills.Count}");
        foreach (var kvp in characterSkills)
        {
            Debug.Log($"  角色 '{kvp.Key}': {kvp.Value.Count} 个技能");
            foreach (var skillConfig in kvp.Value)
            {
                Debug.Log($"    - {skillConfig.skillName}");
            }
        }
        
        // 显示技能实例
        Debug.Log($"技能实例总数: {skillInstances.Count}");
        foreach (var kvp in skillInstances)
        {
            var skillInstance = kvp.Value;
            var owner = GetSkillOwner(kvp.Key);
            Debug.Log($"技能: {skillInstance.config.skillName} (实例ID: {kvp.Key})");
            Debug.Log($"  - 归属角色: {owner ?? "全局"}");
            Debug.Log($"  - 配置: {skillInstance.config.GetDebugInfo()}");
        }
        Debug.Log("===================");
    }
    
    #endregion
}
