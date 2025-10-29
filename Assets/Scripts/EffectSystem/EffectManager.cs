using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace DeepSpaceLabs.SAM
{
    /// <summary>
    /// 特效类型枚举 - 提供类型安全的特效键名
    /// </summary>
    public enum EffectType
    {
        // 全局特效
        GlobalHitAttack,
        
        // 攻击特效
        Hit,
        BeHit,
        
        // 死亡特效
        Dead,
        
        // 玩家特效
        Launch,     // 发射特效
        Charge      // 蓄力特效
    }

    /// <summary>
    /// 攻击类型常量
    /// </summary>
    public static class AttackTypes
    {
        public const string Hit = "Hit";
    }

    /// <summary>
    /// 特效管理器 - 统一的特效管理系统
    /// 基于注册机制，监听游戏事件，管理所有特效的注册和播放
    /// 支持枚举类型安全的特效键名，提供统一的特效播放接口
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
        [Tooltip("启用调试日志")]
        public bool enableDebugLog = true;
    
    [Header("全局特效配置")]
    [Tooltip("全局特效配置列表，在 Inspector 中直接拖拽 MMF_Player 组件")]
    public List<EffectConfig> globalEffects = new List<EffectConfig>();
    
    
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
    
    void Start()
    {
        // 注册全局特效到EffectManager自己
        RegisterGlobalEffects();
    }
    
    /// <summary>
    /// 注册全局特效
    /// </summary>
    void RegisterGlobalEffects()
    {
        foreach (var effect in globalEffects)
        {
            if (effect.IsValid())
            {
                RegisterEffect(gameObject, effect.effectType, effect.mmfPlayer);
            }
            else
            {
                Debug.LogWarning($"EffectManager: 无效的全局特效配置: {effect.GetDebugInfo()}");
            }
        }
    }
    
    void OnEnable()
    {
        // 订阅统一事件系统的特效事件
        GameEventBus.OnEffect += OnEffectEvent;
        
        // 订阅游戏逻辑事件
        GameEventBus.OnAttack += OnAttackEvent;
        GameEventBus.OnDeath += OnDeathEvent;
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
        /// 每个特效都引用一个MMF Player组件，包含多个Feedbacks
        /// </summary>
        private Dictionary<GameObject, Dictionary<string, MMF_Player>> effectObjMMPlayerMap;
        
        /// <summary>
        /// 枚举解析缓存：字符串 -> EffectType
        /// 避免重复的字符串解析操作，提升性能
        /// </summary>
        private static readonly Dictionary<string, EffectType> EffectTypeCache = new Dictionary<string, EffectType>();
        
        #endregion
        
        #region 注册管理方法
        
        /// <summary>
        /// 注册特效到中央管理器（枚举版本 - 推荐使用）
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectType">特效类型枚举</param>
        /// <param name="mmfPlayer">MMF Player组件引用</param>
        public void RegisterEffect(GameObject effectObj, EffectType effectType, MMF_Player mmfPlayer)
        {
            RegisterEffect(effectObj, effectType.ToString(), mmfPlayer);
        }
        
        /// <summary>
        /// 注册特效到中央管理器（内部实现）
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectKey">特效键名</param>
        /// <param name="mmfPlayer">MMF Player组件引用</param>
        private void RegisterEffect(GameObject effectObj, string effectKey, MMF_Player mmfPlayer)
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
                Debug.LogWarning($"EffectManager: 特效 {effectKey} 重复注册，将覆盖原有注册");
            }
            
            // 注册特效
            playerMap[effectKey] = mmfPlayer;
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
                effectObjMMPlayerMap.Remove(effectObj);
            }
        }
        
        
        #endregion
        
        #region 特效播放方法
    
        /// <summary>
        /// 统一的特效播放方法（枚举版本 - 推荐使用）
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectType">特效类型枚举</param>
        /// <param name="position">特效位置（可选）</param>
        /// <param name="direction">特效方向（可选）</param>
        /// <param name="attackData">攻击数据（可选）</param>
        /// <param name="deathData">死亡数据（可选）</param>
        public void PlayEffect(GameObject effectObj, EffectType effectType, 
            Vector3? position = null, 
            Vector3? direction = null, 
            AttackData? attackData = null, 
            DeathData? deathData = null)
        {
            string effectKey = effectType.ToString();
            
            if (!TryGetEffect(effectObj, effectKey, out var mmfPlayer))
                return;
            
            // 确定位置和方向
            Vector3 finalPosition = position ?? attackData?.Position ?? deathData?.Position ?? Vector3.zero;
            Vector3 finalDirection = direction ?? attackData?.Direction ?? deathData?.Direction ?? Vector3.zero;
            
            // 设置位置和方向
            if (finalPosition != Vector3.zero)
                mmfPlayer.transform.position = finalPosition;
            
            if (finalDirection != Vector3.zero)
                mmfPlayer.transform.rotation = Quaternion.LookRotation(finalDirection);
            
            // 设置复杂参数
            if (attackData.HasValue)
            {
                SetMMFPlayerParameters(mmfPlayer, attackData.Value, effectKey);
            }
            
            // 播放特效
            mmfPlayer.PlayFeedbacks();
        }
        
        
        /// <summary>
        /// 便利方法：使用 AttackData 播放特效（枚举版本 - 推荐）
        /// </summary>
        public void PlayAttackEffect(GameObject effectObj, EffectType effectType, AttackData attackData)
        {
            PlayEffect(effectObj, effectType, attackData: attackData);
        }
        
        /// <summary>
        /// 便利方法：使用基础参数播放特效（枚举版本 - 推荐）
        /// </summary>
        public void PlayBasicEffect(GameObject effectObj, EffectType effectType, Vector3 position, Vector3 direction = default)
        {
            PlayEffect(effectObj, effectType, position, direction);
        }
        
        /// <summary>
        /// 便利方法：使用 DeathData 播放特效（枚举版本 - 推荐）
        /// </summary>
        public void PlayDeathEffect(GameObject effectObj, EffectType effectType, DeathData deathData)
        {
            PlayEffect(effectObj, effectType, deathData: deathData);
        }
        
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 缓存的枚举解析方法
        /// </summary>
        /// <param name="effectTypeString">特效类型字符串</param>
        /// <param name="effectType">输出的枚举值</param>
        /// <returns>是否解析成功</returns>
        private static bool TryParseEffectType(string effectTypeString, out EffectType effectType)
        {
            if (string.IsNullOrEmpty(effectTypeString))
            {
                effectType = default;
                return false;
            }
            
            if (EffectTypeCache.TryGetValue(effectTypeString, out effectType))
            {
                return true;
            }
            
            if (System.Enum.TryParse<EffectType>(effectTypeString, out effectType))
            {
                EffectTypeCache[effectTypeString] = effectType;
                return true;
            }
            
            return false;
        }
        
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
            
            if (effectObj == null || string.IsNullOrEmpty(effectKey))
                return false;
            
            if (!effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap) ||
                !playerMap.TryGetValue(effectKey, out mmfPlayer) ||
                mmfPlayer == null ||
                !effectObj.activeInHierarchy)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 设置 MMF Player 参数
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="attackData">攻击数据</param>
        /// <param name="effectKey">特效键名</param>
        private void SetMMFPlayerParameters(MMF_Player mmfPlayer, AttackData attackData, string effectKey)
        {
            if (mmfPlayer == null) return;
            
            // 全局特效始终使用真实碰撞位置，不使用墙壁偏移
            bool isGlobalEffect = effectKey == EffectType.GlobalHitAttack.ToString();
            bool isWallHit = !isGlobalEffect && MMFPlayerParameterSetter.IsWallHitEffect(effectKey);
            
            MMFPlayerParameterSetter.SetEffectPosition(
                mmfPlayer, 
                attackData.Position, 
                isWallHit, 
                attackData.WallHitPositionOffset
            );
            
            // 如果是墙壁特效，设置墙壁专用参数
            if (isWallHit && attackData.HitNormal != Vector3.zero)
            {
                MMFPlayerParameterSetter.SetWallHitParameters(mmfPlayer, attackData);
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
        /// 检查特效是否已注册（枚举版本 - 推荐使用）
        /// </summary>
        /// <param name="effectObj">特效所属的游戏对象</param>
        /// <param name="effectType">特效类型枚举</param>
        /// <returns>是否已注册</returns>
        public bool IsEffectRegistered(GameObject effectObj, EffectType effectType)
        {
            if (effectObj == null)
                return false;
                
            return effectObjMMPlayerMap.TryGetValue(effectObj, out var playerMap) && 
                   playerMap.ContainsKey(effectType.ToString());
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
                // 使用缓存的枚举解析
                if (TryParseEffectType(effectEvent.EffectType, out var effectType))
                {
                    PlayEffect(effectEvent.TargetObject, effectType, effectEvent.Position, effectEvent.Direction);
                }
                else
                {
                    Debug.LogWarning($"EffectManager: 无法识别的特效类型: {effectEvent.EffectType}");
                }
            }
        }
    
    /// <summary>
    /// 处理攻击事件（GameEventBus订阅）
    /// 直接使用 AttackData 参数播放特效，避免重复传递
    /// </summary>
    public void OnAttackEvent(AttackData attackData)
    {
        PlayAttackerEffect(attackData);
        PlayGlobalEffect(attackData);
        PlayTargetEffect(attackData);
    }
    
    /// <summary>
    /// 播放攻击者特效
    /// </summary>
    private void PlayAttackerEffect(AttackData attackData)
    {
        PlayEffectDirectly(EffectType.Hit, attackData.Position, attackData.Direction, 
                          attackData.Attacker, attackData.AttackerTag, attackData);
    }
    
    /// <summary>
    /// 播放全局特效（只对Player发起的Hit攻击）
    /// </summary>
    private void PlayGlobalEffect(AttackData attackData)
    {
        if (attackData.Attacker != null && attackData.Attacker.CompareTag("Player") && 
            attackData.AttackType == AttackTypes.Hit)
        {
            PlayEffectDirectly(EffectType.GlobalHitAttack, attackData.Position, attackData.Direction, 
                             gameObject, "EffectManager", attackData);
        }
    }
    
    /// <summary>
    /// 播放受击者特效
    /// </summary>
    private void PlayTargetEffect(AttackData attackData)
    {
        if (ShouldPlayBeHitEffect(attackData.Target))
        {
            PlayEffectDirectly(EffectType.BeHit, attackData.Position, attackData.Direction, 
                              attackData.Target, attackData.TargetTag, attackData);
        }
    }
    
    /// <summary>
    /// 直接播放特效，使用传入的位置和方向参数（枚举版本 - 推荐）
    /// </summary>
        private void PlayEffectDirectly(EffectType effectType, Vector3 position, Vector3 direction, GameObject targetObject, string targetTag, AttackData attackData)
        {
            // 播放目标对象特效 - 使用传入的位置和方向参数
            if (targetObject != null)
            {
                PlayEffect(targetObject, effectType, position, direction, attackData);
            }
        }
        
    
    /// <summary>
    /// 检查是否应该播放受击特效
    /// 与PlayerCore的TakeDamage方法保持一致的逻辑
    /// </summary>
    private bool ShouldPlayBeHitEffect(GameObject target)
    {
        // if (target == null) return false;
        
        // // 检查玩家状态，只有在Idle状态才能播放受击特效
        // if (target.CompareTag("Player"))
        // {
        //     PlayerStateMachine stateMachine = target.GetComponent<PlayerStateMachine>();
        //     if (stateMachine != null && !stateMachine.IsIdle)
        //     {
        //         return false;
        //     }
        // }
        
        return true;
    }
    
    /// <summary>
    /// 处理死亡事件（GameEventBus订阅）
    /// 负责播放死亡相关的特效，对象销毁由 MMF 的 Destroy 组件处理
    /// </summary>
    public void OnDeathEvent(DeathData deathData)
    {
        // 播放死亡特效
        if (deathData.DeadObject != null)
        {
            Instance.PlayEffect(deathData.DeadObject, EffectType.Dead, deathData: deathData);
        }
        else
        {
            Debug.LogWarning("EffectManager: 死亡事件中没有死亡对象");
        }
    }
    
    #endregion
    
    }
}
