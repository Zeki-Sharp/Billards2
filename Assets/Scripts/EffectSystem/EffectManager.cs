using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace DeepSpaceLabs.SAM
{
/// <summary>
    /// 特效管理器 - 统一的特效管理系统
    /// 基于注册机制，监听游戏事件，管理所有特效的注册和播放
/// </summary>
public class EffectManager : MonoBehaviour
{
        #region 单例模式
        
    public static EffectManager Instance { get; private set; }
    
    [Header("特效系统说明")]
    [TextArea(4, 6)]
        public string systemInfo = "此系统使用注册机制 + GameEventBus + 直接引用 架构\n" +
                              "全局特效：镜头摇晃、全局音效等（在EffectManager上）\n" +
                              "对象特效：粒子特效、对象动画等（在目标对象上）\n" +
                                  "配置方式：在Inspector中直接拖拽MMF_Player组件";
    
    [Header("特效设置")]
        public bool enableDebugLog = true;
    
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
                effectObjMMPlayerMap = new Dictionary<GameObject, Dictionary<string, MMF_Player>>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
    }
    
    void OnEnable()
    {
        // 订阅统一事件系统的特效事件
        GameEventBus.OnEffect += OnEffectEvent;
        
        // 订阅游戏逻辑事件
        GameEventBus.OnAttack += OnAttackEvent;
        GameEventBus.OnDeath += OnDeathEvent;
        if (enableDebugLog)
        {
            Debug.Log("EffectManager 已订阅 GameEventBus 统一事件系统");
            Debug.Log($"EffectManager 实例: {Instance?.name}, 当前对象: {gameObject.name}");
        }
    }
    
    void OnDisable()
    {
        // 取消订阅统一事件系统的特效事件
        GameEventBus.OnEffect -= OnEffectEvent;
        
        // 取消订阅游戏逻辑事件
        GameEventBus.OnAttack -= OnAttackEvent;
        GameEventBus.OnDeath -= OnDeathEvent;
    }
        
        #endregion
        
        #region 核心数据结构
        
        /// <summary>
        /// 核心注册字典：GameObject -> Dictionary<effectKey, MMF_Player>
        /// 每个特效都是一个完整的MMF Player组件，包含多个Feedbacks
        /// </summary>
        private Dictionary<GameObject, Dictionary<string, MMF_Player>> effectObjMMPlayerMap;
        
        #endregion
        
        #region 注册管理方法
        
        
        /// <summary>
        /// 注册特效到中央管理器
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="mmfPlayer">MMF Player组件引用</param>
        public void RegisterEffect(GameObject effectObj, string effectKey, MMF_Player mmfPlayer)
        {
            if (effectObj == null)
            {
                Debug.LogError("EffectManager: 注册特效时 effectObj 不能为 null");
                return;
            }
            
            if (string.IsNullOrEmpty(effectKey))
            {
                Debug.LogError("EffectManager: 注册特效时 effectKey 不能为空");
                return;
            }
            
            if (mmfPlayer == null)
            {
                Debug.LogError($"EffectManager: 注册特效 {effectKey} 时 mmfPlayer 不能为 null");
                return;
            }
            
            // 获取或创建对象特效字典
            if (!effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap))
            {
                playerMap = new Dictionary<string, MMF_Player>();
                effectObjMMPlayerMap.Add(effectObj, playerMap);
            }
            
            // 检查是否重复注册
            if (playerMap.ContainsKey(effectKey))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"EffectManager: 特效 {effectKey} 重复注册，将覆盖原有注册");
                }
            }
            
            // 注册特效
            playerMap[effectKey] = mmfPlayer;
            
            if (enableDebugLog)
            {
                Debug.Log($"EffectManager: 成功注册特效 {effectObj.name}.{effectKey} -> {mmfPlayer.name}");
            }
        }
        
        /// <summary>
        /// 注销对象的所有特效
        /// </summary>
        /// <param name="effectObj">要注销特效的游戏对象</param>
        public void UnregisterEffect(GameObject effectObj)
        {
            if (effectObj == null) return;
            
            if (effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap))
            {
                int effectCount = playerMap.Count;
                effectObjMMPlayerMap.Remove(effectObj);
                
                if (enableDebugLog)
                {
                    Debug.Log($"EffectManager: 成功注销对象 {effectObj.name} 的 {effectCount} 个特效");
                }
            }
        }
        
        /// <summary>
        /// 注销指定特效
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        public void UnregisterEffect(GameObject effectObj, string effectKey)
        {
            if (effectObj == null || string.IsNullOrEmpty(effectKey)) return;
            
            if (effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap))
            {
                if (playerMap.ContainsKey(effectKey))
                {
                    playerMap.Remove(effectKey);
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"EffectManager: 成功注销特效 {effectObj.name}.{effectKey}");
                    }
                    
                    // 如果对象没有特效了，移除对象条目
                    if (playerMap.Count == 0)
                    {
                        effectObjMMPlayerMap.Remove(effectObj);
                    }
                }
            }
        }
        
        #endregion
        
        #region 特效播放方法
    
    /// <summary>
        /// 播放特效 - 使用 AttackData 复杂参数
    /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="attackData">攻击数据</param>
        public void PlayEffect(GameObject effectObj, string effectKey, AttackData attackData)
        {
            if (!TryGetEffect(effectObj, effectKey, out var mmfPlayer))
                return;
            
            // 设置特效位置和方向
            if (attackData.Position != Vector3.zero)
            {
                mmfPlayer.transform.position = attackData.Position;
            }
            
            if (attackData.Direction != Vector3.zero)
            {
                mmfPlayer.transform.rotation = Quaternion.LookRotation(attackData.Direction);
            }
            
            // 传递复杂参数到 MMF Player
            SetMMFPlayerParameters(mmfPlayer, attackData);
            
            // 播放特效
            mmfPlayer.PlayFeedbacks();
            
            if (enableDebugLog)
            {
                Debug.Log($"EffectManager: 播放特效 {effectObj.name}.{effectKey} at {attackData.Position}");
            }
        }
        
        /// <summary>
        /// 播放特效 - 使用基础参数
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="position">特效位置</param>
        /// <param name="direction">特效方向</param>
        public void PlayEffect(GameObject effectObj, string effectKey, Vector3 position, Vector3 direction = default)
        {
            if (!TryGetEffect(effectObj, effectKey, out var mmfPlayer))
                return;
            
            // 设置特效位置和方向
            mmfPlayer.transform.position = position;
            
            if (direction != Vector3.zero)
            {
                mmfPlayer.transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // 播放特效
            mmfPlayer.PlayFeedbacks();
            
            if (enableDebugLog)
            {
                Debug.Log($"EffectManager: 播放特效 {effectObj.name}.{effectKey} at {position}");
            }
        }
        
        /// <summary>
        /// 播放特效 - 使用 DeathData 参数
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="deathData">死亡数据</param>
        public void PlayEffect(GameObject effectObj, string effectKey, DeathData deathData)
        {
            if (!TryGetEffect(effectObj, effectKey, out var mmfPlayer))
                return;
            
            // 设置特效位置和方向
            if (deathData.Position != Vector3.zero)
            {
                mmfPlayer.transform.position = deathData.Position;
            }
            
            if (deathData.Direction != Vector3.zero)
            {
                mmfPlayer.transform.rotation = Quaternion.LookRotation(deathData.Direction);
            }
            
            // 播放特效
            mmfPlayer.PlayFeedbacks();
            
            if (enableDebugLog)
            {
                Debug.Log($"EffectManager: 播放特效 {effectObj.name}.{effectKey} at {deathData.Position}");
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取特效引用
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="mmfPlayer">输出的 MMF Player 组件</param>
        /// <returns>是否成功获取</returns>
        private bool TryGetEffect(GameObject effectObj, string effectKey, out MMF_Player mmfPlayer)
        {
            mmfPlayer = null;
            
            if (effectObj == null)
            {
                if (enableDebugLog)
                    Debug.LogWarning("EffectManager: 播放特效时 effectObj 为 null");
                return false;
            }
            
            if (string.IsNullOrEmpty(effectKey))
            {
                if (enableDebugLog)
                    Debug.LogWarning("EffectManager: 播放特效时 effectKey 为空");
                return false;
            }
            
            if (!effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"EffectManager: 对象 {effectObj.name} 未注册任何特效");
                return false;
            }
            
            if (!playerMap.TryGetValue(effectKey, out mmfPlayer))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"EffectManager: 对象 {effectObj.name} 未注册特效 {effectKey}");
                return false;
            }
            
            if (mmfPlayer == null)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"EffectManager: 特效 {effectObj.name}.{effectKey} 的 MMF Player 引用为 null");
                return false;
            }
            
            // 检查对象是否仍然活跃
            if (!effectObj.activeInHierarchy)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"EffectManager: 对象 {effectObj.name} 不在活跃层级中，跳过特效播放");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 设置 MMF Player 的复杂参数
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="attackData">攻击数据</param>
        private void SetMMFPlayerParameters(MMF_Player mmfPlayer, AttackData attackData)
        {
            if (mmfPlayer == null) return;
            
            // 设置撞击相关的参数
            if (attackData.HitNormal != Vector3.zero)
            {
                // 查找所有 MMF_Position 反馈，设置位置偏移
                var positionFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Position>();
                if (positionFeedbacks != null && positionFeedbacks.Count > 0)
                {
                    foreach (var positionFeedback in positionFeedbacks)
                    {
                        // 应用墙壁撞击的位置偏移（由 WallHitPositionController 计算）
                        Vector3 finalPosition = attackData.Position + attackData.WallHitPositionOffset;
                        positionFeedback.DestinationPosition = finalPosition;
                        positionFeedback.InitialPosition = finalPosition;
                    }
                }
                
                // 查找所有 MMF_PositionSpring 反馈，设置位置弹簧效果
                var positionSpringFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_PositionSpring>();
                if (positionSpringFeedbacks != null && positionSpringFeedbacks.Count > 0)
                {
                    foreach (var springFeedback in positionSpringFeedbacks)
                    {
                        // 使用旧系统的正确方法：直接设置属性
                        springFeedback.BumpPositionMin = Vector3.zero;
                        springFeedback.BumpPositionMax = attackData.WallHitPositionOffset;
                    }
                }
                
                // 查找所有 MMF_Rotation 反馈，设置旋转角度
                var rotationFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Rotation>();
                if (rotationFeedbacks != null && rotationFeedbacks.Count > 0)
                {
                    foreach (var rotationFeedback in rotationFeedbacks)
                    {
                        // 应用墙壁撞击的旋转角度（由 WallHitRotationController 计算）
                        // 使用旧系统的正确方法：直接设置 RemapCurveOne
                        rotationFeedback.RemapCurveOne = attackData.WallHitRotationAngle;
                    }
                }
                
                // 查找所有 MMF_Scale 反馈，根据撞击速度设置缩放
                var scaleFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Scale>();
                if (scaleFeedbacks != null && scaleFeedbacks.Count > 0)
                {
                    // 根据撞击速度计算缩放强度（0.5-2.0范围）
                    float speedRatio = Mathf.Clamp01(attackData.HitSpeed / 50f);
                    float scaleMultiplier = Mathf.Lerp(0.5f, 2.0f, speedRatio);
                    Vector3 scale = Vector3.one * scaleMultiplier;
                    
                    foreach (var scaleFeedback in scaleFeedbacks)
                    {
                        try
                        {
                            // 尝试设置缩放参数
                            var scaleField = scaleFeedback.GetType().GetField("DestinationScale");
                            if (scaleField != null)
                            {
                                scaleField.SetValue(scaleFeedback, scale);
                            }
                        }
                        catch (System.Exception e)
                        {
                            if (enableDebugLog)
                            {
                                Debug.LogWarning($"EffectManager: 设置缩放参数失败 - {e.Message}");
                            }
                        }
                    }
                }
                
                if (enableDebugLog)
                {
                    Debug.Log($"EffectManager: 设置墙壁撞击参数 - Normal: {attackData.HitNormal}, Speed: {attackData.HitSpeed:F2}, " +
                             $"Rotation: {attackData.WallHitRotationAngle:F2}°, PositionOffset: {attackData.WallHitPositionOffset}");
                }
            }
        }
        
        #endregion
        
        #region 调试和监控方法
        
        /// <summary>
        /// 获取注册状态信息
        /// </summary>
        /// <returns>注册状态字符串</returns>
        public string GetRegistrationStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("EffectManager 注册状态:");
            status.AppendLine($"总对象数: {effectObjMMPlayerMap.Count}");
            
            foreach (var kvp in effectObjMMPlayerMap)
            {
                var obj = kvp.Key;
                var playerMap = kvp.Value;
                status.AppendLine($"  {obj.name}: {playerMap.Count} 个特效");
                
                foreach (var effectKvp in playerMap)
                {
                    var effectKey = effectKvp.Key;
                    var mmfPlayer = effectKvp.Value;
                    status.AppendLine($"    - {effectKey}: {mmfPlayer?.name ?? "null"}");
                }
            }
            
            return status.ToString();
        }
        
        /// <summary>
        /// 检查特效是否已注册
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <returns>是否已注册</returns>
        public bool IsEffectRegistered(GameObject effectObj, string effectKey)
        {
            if (effectObj == null || string.IsNullOrEmpty(effectKey))
                return false;
                
            return effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap) && 
                   playerMap.ContainsKey(effectKey);
        }
        
        /// <summary>
        /// 获取对象的特效数量
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <returns>特效数量</returns>
        public int GetEffectCount(GameObject effectObj)
        {
            if (effectObj == null)
                return 0;
                
            return effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap) ? playerMap.Count : 0;
        }
        
        #endregion
        
        #region 事件处理方法
        
        /// <summary>
        /// 处理特效事件（统一事件系统）
        /// 用于非攻击相关的特效
        /// </summary>
        public void OnEffectEvent(EffectEvent effectEvent)
        {
            // 播放目标对象特效 - 使用新架构
            if (effectEvent.TargetObject != null)
            {
                Instance.PlayEffect(effectEvent.TargetObject, effectEvent.EffectType, effectEvent.Position, effectEvent.Direction);
            }
        }
    
    /// <summary>
    /// 处理攻击事件（GameEventBus订阅）
    /// 直接使用 AttackData 参数播放特效，避免重复传递
    /// </summary>
    public void OnAttackEvent(AttackData attackData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"EffectManager 收到攻击事件: {attackData.AttackType} -> {attackData.Attacker?.name} 攻击 {attackData.Target?.name}");
        }
        
            // 播放攻击者特效 - 直接使用配置的键名
            PlayEffectDirectly(attackData.AttackType, attackData.Position, attackData.Direction, attackData.Attacker, attackData.AttackerTag, attackData);
        
            // 播放受击者特效 - 直接使用配置的键名
        if (ShouldPlayBeHitEffect(attackData.Target))
        {
                PlayEffectDirectly("Be Hit", attackData.Position, attackData.Direction, attackData.Target, attackData.TargetTag, attackData);
            
            if (enableDebugLog)
            {
                    Debug.Log($"EffectManager 已播放特效: {attackData.AttackType} 和 Be Hit");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"EffectManager 跳过受击特效播放 - 目标状态不允许");
            }
        }
    }
    
    /// <summary>
    /// 直接播放特效，使用 AttackData 的所有参数
    /// </summary>
        private void PlayEffectDirectly(string effectType, Vector3 position, Vector3 direction, GameObject targetObject, string targetTag, AttackData attackData)
        {
            // 播放目标对象特效 - 使用新架构
            if (targetObject != null)
            {
                Instance.PlayEffect(targetObject, effectType, attackData);
                if (enableDebugLog)
                    Debug.Log($"播放对象{effectType}特效 - {targetObject.name} at {position}, 速度: {attackData.HitSpeed:F2}");
            }
        }
    
    /// <summary>
    /// 检查是否应该播放受击特效
    /// 与PlayerCore的TakeDamage方法保持一致的逻辑
    /// </summary>
    private bool ShouldPlayBeHitEffect(GameObject target)
    {
        if (target == null) return false;
        
        // 检查玩家状态，只有在Idle状态才能播放受击特效
        if (target.CompareTag("Player"))
        {
            PlayerStateMachine stateMachine = target.GetComponent<PlayerStateMachine>();
            if (stateMachine != null && !stateMachine.IsIdle)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"EffectManager: 玩家不在Idle状态，跳过受击特效 - 当前状态: {stateMachine.CurrentState}");
                }
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 处理死亡事件（GameEventBus订阅）
    /// 负责播放死亡相关的特效，对象销毁由 MMF 的 Destroy 组件处理
    /// </summary>
    public void OnDeathEvent(DeathData deathData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"EffectManager 收到死亡事件: {deathData.DeathType}, 位置: {deathData.Position}, 对象: {deathData.DeadObject?.name}");
        }
        
            // 播放死亡特效 - 使用新架构，使用配置的键名
        if (deathData.DeadObject != null)
            {
                Instance.PlayEffect(deathData.DeadObject, "Dead", deathData);
                        if (enableDebugLog)
                {
                    Debug.Log($"EffectManager: 播放敌人 {deathData.DeadObject.name} 的死亡特效");
                }
            }
            else
            {
                Debug.LogWarning("EffectManager: 死亡事件中没有死亡对象");
            }
        }
        
        #endregion
        
    }
}
