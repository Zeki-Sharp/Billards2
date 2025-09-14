using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 特效管理器 - 纯事件监听器
/// 监听MMEventManager的特效事件和攻击事件，分发全局特效和对象特效
/// </summary>
public class EffectManager : MonoBehaviour, MMEventListener<EffectEvent>, MMEventListener<AttackEvent>, MMEventListener<DeathEvent>
{
    public static EffectManager Instance { get; private set; }
    
    [Header("特效系统说明")]
    [TextArea(4, 6)]
    public string systemInfo = "此系统使用 MMEventManager + EffectPlayer + 全局特效 架构\n" +
                              "全局特效：镜头摇晃、全局音效等（在EffectManager上）\n" +
                              "对象特效：粒子特效、对象动画等（在目标对象上）\n" +
                              "新增特效：只需在EffectMapping中添加映射即可";
    
    [Header("特效设置")]
    public bool enableDebugLog = true;          // 是否启用调试日志
    
    [Header("全局特效")]
    [Tooltip("全局特效播放器，自动查找 EffectManager 上的 EffectPlayer 组件")]
    private EffectPlayer globalEffectPlayer;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 自动查找全局特效播放器
        globalEffectPlayer = GetComponent<EffectPlayer>();
        if (globalEffectPlayer == null)
        {
            // 在子对象中查找 EffectPlayer
            globalEffectPlayer = GetComponentInChildren<EffectPlayer>();
            if (globalEffectPlayer == null)
            {
                Debug.LogWarning("EffectManager 及其子对象上没有找到 EffectPlayer 组件，全局特效将无法播放");
            }
            else
            {
                Debug.Log($"EffectManager 在子对象中找到全局特效播放器: {globalEffectPlayer.name}");
            }
        }
        else
        {
            Debug.Log($"EffectManager 找到全局特效播放器: {globalEffectPlayer.name}");
        }
    }
    
    void OnEnable()
    {
        // 订阅MMEventManager的特效事件、攻击事件和死亡事件
        this.MMEventStartListening<EffectEvent>();
        this.MMEventStartListening<AttackEvent>();
        this.MMEventStartListening<DeathEvent>();
        if (enableDebugLog)
        {
            Debug.Log("EffectManager 已订阅 MMEventManager 特效事件、攻击事件和死亡事件");
            Debug.Log($"EffectManager 实例: {Instance?.name}, 当前对象: {gameObject.name}");
        }
    }
    
    void Start()
    {
        // 在Start中测试MMEventManager是否工作
        if (enableDebugLog)
        {
            Debug.Log("测试 MMEventManager 是否工作...");
            EffectEvent.Trigger("Test", Vector3.zero);
        }
    }
    
    void OnDisable()
    {
        // 取消订阅MMEventManager的特效事件、攻击事件和死亡事件
        this.MMEventStopListening<EffectEvent>();
        this.MMEventStopListening<AttackEvent>();
        this.MMEventStopListening<DeathEvent>();
    }
    
    /// <summary>
    /// 处理特效事件（MMEventListener接口实现）
    /// 用于非攻击相关的特效
    /// </summary>
    public void OnMMEvent(EffectEvent effectEvent)
    {
        if (enableDebugLog)
            Debug.Log($"EffectManager接收到特效事件: {effectEvent.EffectType} at {effectEvent.Position}");
        
        // 播放全局特效（简化版本，不包含撞墙参数）
        if (globalEffectPlayer != null)
        {
            globalEffectPlayer.PlayEffect(effectEvent.EffectType, effectEvent.Position, effectEvent.Direction, Vector3.zero, 0f, 0f, Vector3.zero);
            if (enableDebugLog)
                Debug.Log($"播放全局{effectEvent.EffectType}特效 at {effectEvent.Position}");
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"全局特效播放器为null，无法播放全局{effectEvent.EffectType}特效");
        }
        
        // 播放目标对象特效（简化版本，不包含撞墙参数）
        if (effectEvent.TargetObject != null)
        {
            var objectEffectPlayer = FindEffectPlayerInTarget(effectEvent.TargetObject);
            if (objectEffectPlayer != null)
            {
                objectEffectPlayer.PlayEffect(effectEvent.EffectType, effectEvent.Position, effectEvent.Direction, Vector3.zero, 0f, 0f, Vector3.zero);
                if (enableDebugLog)
                    Debug.Log($"播放对象{effectEvent.EffectType}特效 - {effectEvent.TargetObject.name} at {effectEvent.Position}");
            }
            else
            {
                Debug.LogWarning($"目标对象 {effectEvent.TargetObject.name} 及其子对象上没有EffectPlayer组件");
            }
        }
    }
    
    /// <summary>
    /// 处理攻击事件（MMEventListener接口实现）
    /// 直接使用 AttackEvent 参数播放特效，避免重复传递
    /// </summary>
    public void OnMMEvent(AttackEvent attackEvent)
    {
        if (enableDebugLog)
        {
            Debug.Log($"EffectManager 收到攻击事件: {attackEvent.AttackType} -> {attackEvent.Attacker?.name} 攻击 {attackEvent.Target?.name}");
        }
        
        // 播放攻击者特效
        string attackEffectType = $"{attackEvent.AttackType} Attack Effect";
        PlayEffectDirectly(attackEffectType, attackEvent.Position, attackEvent.Direction, attackEvent.Attacker, attackEvent.AttackerTag, attackEvent);
        
        // 播放受击者特效
        string beHitEffectType = "Be Hit Effect";
        PlayEffectDirectly(beHitEffectType, attackEvent.Position, attackEvent.Direction, attackEvent.Target, attackEvent.TargetTag, attackEvent);
        
        if (enableDebugLog)
        {
            Debug.Log($"EffectManager 已播放特效: {attackEffectType} 和 {beHitEffectType}");
        }
    }
    
    /// <summary>
    /// 直接播放特效，使用 AttackEvent 的所有参数
    /// </summary>
    private void PlayEffectDirectly(string effectType, Vector3 position, Vector3 direction, GameObject targetObject, string targetTag, AttackEvent attackEvent)
    {
        // 播放全局特效
        if (globalEffectPlayer != null)
        {
            globalEffectPlayer.PlayEffect(effectType, position, direction, attackEvent.HitNormal, attackEvent.HitSpeed, attackEvent.WallHitRotationAngle, attackEvent.WallHitPositionOffset);
            if (enableDebugLog)
                Debug.Log($"播放全局{effectType}特效 at {position}, 速度: {attackEvent.HitSpeed:F2}");
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"全局特效播放器为null，无法播放全局{effectType}特效");
        }
        
        // 播放目标对象特效
        if (targetObject != null)
        {
            var objectEffectPlayer = FindEffectPlayerInTarget(targetObject);
            if (objectEffectPlayer != null)
            {
                objectEffectPlayer.PlayEffect(effectType, position, direction, attackEvent.HitNormal, attackEvent.HitSpeed, attackEvent.WallHitRotationAngle, attackEvent.WallHitPositionOffset);
                if (enableDebugLog)
                    Debug.Log($"播放对象{effectType}特效 - {targetObject.name} at {position}, 速度: {attackEvent.HitSpeed:F2}");
            }
            else
            {
                Debug.LogWarning($"目标对象 {targetObject.name} 及其子对象上没有EffectPlayer组件");
            }
        }
    }
    
    /// <summary>
    /// 在目标对象及其子对象和父对象中查找 EffectPlayer 组件
    /// </summary>
    private EffectPlayer FindEffectPlayerInTarget(GameObject targetObject)
    {
        // 首先在目标对象本身查找
        var effectPlayer = targetObject.GetComponent<EffectPlayer>();
        if (effectPlayer != null)
        {
            return effectPlayer;
        }
        
        // 在子对象中查找名为 "Effect Player" 的对象
        Transform effectPlayerTransform = targetObject.transform.Find("Effect Player");
        if (effectPlayerTransform != null)
        {
            effectPlayer = effectPlayerTransform.GetComponent<EffectPlayer>();
            if (effectPlayer != null)
            {
                return effectPlayer;
            }
        }
        
        // 如果没找到，尝试在所有子对象中查找
        effectPlayer = targetObject.GetComponentInChildren<EffectPlayer>();
        if (effectPlayer != null)
        {
            return effectPlayer;
        }
        
        // 如果还没找到，尝试在父对象中查找
        Transform parent = targetObject.transform.parent;
        while (parent != null)
        {
            effectPlayer = parent.GetComponentInChildren<EffectPlayer>();
            if (effectPlayer != null)
            {
                Debug.Log($"在父对象 {parent.name} 中找到 EffectPlayer");
                return effectPlayer;
            }
            parent = parent.parent;
        }
        
        Debug.LogWarning($"在 {targetObject.name} 及其父对象中未找到 EffectPlayer");
        return null;
    }
    
    /// <summary>
    /// 处理死亡事件（MMEventListener接口实现）
    /// 负责播放死亡相关的特效，对象销毁由 MMF 的 Destroy 组件处理
    /// </summary>
    public void OnMMEvent(DeathEvent deathEvent)
    {
        if (enableDebugLog)
        {
            Debug.Log($"EffectManager 收到死亡事件: {deathEvent.DeathType}, 位置: {deathEvent.Position}, 对象: {deathEvent.DeadObject?.name}");
        }
        
        // 播放死亡特效 - 使用死亡对象身上的 Dead Effect
        if (deathEvent.DeadObject != null)
        {
            // 查找死亡对象下的 Effect Player (有 EffectPlayer 组件)
            Transform effectPlayerTransform = deathEvent.DeadObject.transform.Find("Effect Player");
            if (effectPlayerTransform != null)
            {
                EffectPlayer effectPlayer = effectPlayerTransform.GetComponent<EffectPlayer>();
                if (effectPlayer != null)
                {
                    // 查找 Dead Effect 子对象 (有 MMF Player 组件)
                    Transform deadEffectTransform = effectPlayerTransform.Find("Dead Effect");
                    if (deadEffectTransform != null)
                    {
                        if (enableDebugLog)
                        {
                            Debug.Log($"EffectManager: 播放敌人 {deathEvent.DeadObject.name} 身上的死亡特效");
                        }
                        // 调用 Effect Player 的 PlayEffect 方法
                        effectPlayer.PlayEffect("Dead Effect", deathEvent.Position, deathEvent.Direction);
                    }
                    else
                    {
                        Debug.LogWarning($"EffectManager: 在 {deathEvent.DeadObject.name}/Effect Player 下未找到 Dead Effect 子对象");
                    }
                }
                else
                {
                    Debug.LogWarning($"EffectManager: 在 {deathEvent.DeadObject.name}/Effect Player 上未找到 EffectPlayer 组件");
                }
            }
            else
            {
                Debug.LogWarning($"EffectManager: 在 {deathEvent.DeadObject.name} 下未找到 Effect Player 子对象");
            }
        }
        else
        {
            Debug.LogWarning("EffectManager: 死亡事件中没有死亡对象");
        }
    }
    
}
