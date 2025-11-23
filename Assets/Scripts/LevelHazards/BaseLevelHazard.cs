using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 关卡障碍物基类
/// 
/// 【设计原则】：
/// - 提供通用的碰撞检测和组件获取
/// - 子类重写 OnHazardTriggered() 实现具体逻辑
/// - 支持 Trigger 和 Collision 两种模式
/// - 内置冷却机制，避免连续触发
/// - 统一特效播放接口，子类配置各自的 MMF Player
/// 
/// 【使用方式】：
/// 1. 继承此类创建具体障碍物（如 BouncePad, SpikeTrap）
/// 2. 重写 OnHazardTriggered(GameObject ball) 方法
/// 3. 调用提供的工具方法获取组件
/// 4. 在 Inspector 中配置 hazardEffect（MMF Player）用于触发特效
/// 
/// 【配置】：
/// - cooldownDuration: 触发冷却时间
/// - affectPlayer: 是否影响玩家
/// - affectEnemy: 是否影响敌人
/// - hazardEffect: 触发特效（MMF Player，可选）
/// 
/// 【Unity 配置】：
/// - Layer: Obstacle（统一使用此 Layer）
/// - Collider2D: 根据需要选择 BoxCollider2D / CircleCollider2D
/// - Collider.Is Trigger: 根据障碍物类型配置（脚本会自动适配）
///   - Trigger = true: 不阻挡移动，只触发效果（如尖刺）
///   - Trigger = false: 有物理碰撞（如弹簧垫、墙体）
/// </summary>
public abstract class BaseLevelHazard : MonoBehaviour
{
    #region 配置字段
    
    [Header("冷却设置")]
    [Tooltip("触发冷却时间（秒），0表示无冷却")]
    [SerializeField] protected float cooldownDuration = 0.5f;
    
    [Header("作用目标")]
    [Tooltip("是否影响玩家球体")]
    [SerializeField] protected bool affectPlayer = true;
    
    [Tooltip("是否影响敌人球体")]
    [SerializeField] protected bool affectEnemy = false;
    
    [Header("特效设置")]
    [Tooltip("障碍物触发特效（MMF Player），可选配置。每个子类可以配置不同的特效")]
    [SerializeField] protected MMF_Player hazardEffect;
    
    [Header("调试")]
    [SerializeField] protected bool showDebugInfo = false;
    
    #endregion
    
    #region 内部状态
    
    // 冷却计时
    private float lastTriggerTime = -999f;
    
    #endregion
    
    #region 生命周期
    
    protected virtual void Start()
    {
        // 验证 Collider 配置
        ValidateCollider();
    }
    
    #endregion
    
    
    #region 虚方法（子类重写）
    
    /// <summary>
    /// 障碍物被触发时调用（子类必须实现）
    /// </summary>
    /// <param name="ball">触发的球体对象</param>
    protected abstract void OnHazardTriggered(GameObject ball);
    
    /// <summary>
    /// 修改墙体碰撞的反弹系数（可选，子类重写）
    /// </summary>
    /// <param name="ball">碰撞的球体 GameObject</param>
    /// <param name="currentSpeed">当前速度</param>
    /// <param name="defaultBounceFactor">默认反弹系数</param>
    /// <returns>修改后的反弹系数（如果返回 null，使用默认值）</returns>
    public virtual float? ModifyBounceFactor(GameObject ball, float currentSpeed, float defaultBounceFactor)
    {
        return null; // 默认不修改
    }
    
    #endregion
    
    #region 特效播放
    
    /// <summary>
    /// 播放障碍物触发特效（如果配置了 MMF Player）
    /// </summary>
    protected void PlayHazardEffect()
    {
        if (hazardEffect != null)
        {
            hazardEffect.PlayFeedbacks();
            
            if (showDebugInfo)
            {
                Debug.Log($"[{GetType().Name}] 播放触发特效");
            }
        }
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 检查是否是有效的触发目标（供子类和接口使用）
    /// </summary>
    protected bool IsValidTarget(GameObject obj)
    {
        if (obj == null) return false;
        
        // 检查 Layer
        int layer = obj.layer;
        string layerName = LayerMask.LayerToName(layer);
        
        bool isPlayer = layerName == "Player";
        bool isEnemy = layerName == "Enemy";
        
        // 根据配置判断是否影响该目标
        if (isPlayer && !affectPlayer) return false;
        if (isEnemy && !affectEnemy) return false;
        
        // 必须是玩家或敌人
        return isPlayer || isEnemy;
    }
    
    /// <summary>
    /// 检查是否可以触发（冷却时间）
    /// </summary>
    protected bool CanTrigger()
    {
        if (cooldownDuration <= 0f) return true;
        return Time.time - lastTriggerTime >= cooldownDuration;
    }
    
    /// <summary>
    /// 更新冷却时间（供子类或接口使用）
    /// </summary>
    protected void UpdateCooldown()
    {
        lastTriggerTime = Time.time;
    }
    
    /// <summary>
    /// 处理碰撞修改完成后的通用逻辑（冷却、特效等）
    /// 供 BallPhysics 在调用 IWallCollisionModifier 后使用
    /// </summary>
    /// <param name="ball">碰撞的球体</param>
    public void HandleCollisionModification(GameObject ball)
    {
        if (!IsValidTarget(ball)) return;
        if (!CanTrigger()) return;
        
        // 更新冷却时间
        UpdateCooldown();
        
        // 触发子类的逻辑和特效
        OnHazardTriggered(ball);
        PlayHazardEffect();
    }
    
    /// <summary>
    /// 获取球体物理组件
    /// </summary>
    protected BallPhysics GetBallPhysics(GameObject ball)
    {
        if (ball == null) return null;
        return ball.GetComponent<BallPhysics>();
    }
    
    /// <summary>
    /// 获取玩家行为组件
    /// </summary>
    protected PlayerBehavior GetPlayerBehavior(GameObject ball)
    {
        if (ball == null) return null;
        return ball.GetComponent<PlayerBehavior>();
    }
    
    /// <summary>
    /// 获取角色ID（通过 GameSession.TeamData）
    /// </summary>
    protected string GetCharacterID(GameObject ball)
    {
        if (ball == null) return null;
        
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return null;
        
        foreach (var character in teamData.characters)
        {
            if (character.ballInstance == ball)
            {
                return character.characterID;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 验证 Collider 配置（支持 2D 和 3D）
    /// </summary>
    void ValidateCollider()
    {
        Collider collider3D = GetComponent<Collider>();
        Collider2D collider2D = GetComponent<Collider2D>();
        
        if (collider3D == null && collider2D == null)
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name} 缺少 Collider 组件！");
            return;
        }
        
        if (showDebugInfo)
        {
            string mode = collider3D != null 
                ? (collider3D.isTrigger ? "Trigger" : "Collision") 
                : (collider2D.isTrigger ? "Trigger" : "Collision");
            Debug.Log($"[{GetType().Name}] {gameObject.name} Collider 模式: {mode}");
        }
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("测试 - 强制触发")]
    void DebugForceTrigger()
    {
        // 查找场景中的第一个玩家球体
        var player = FindFirstObjectByType<PlayerBehavior>();
        if (player != null)
        {
            Debug.Log($"[{GetType().Name}] 强制触发，目标: {player.gameObject.name}");
            OnHazardTriggered(player.gameObject);
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] 场景中没有找到玩家球体");
        }
    }
    
    [ContextMenu("测试 - 重置冷却")]
    void DebugResetCooldown()
    {
        lastTriggerTime = -999f;
        Debug.Log($"[{GetType().Name}] 冷却已重置");
    }
    
    [ContextMenu("显示配置信息")]
    void ShowConfigInfo()
    {
        Collider2D collider = GetComponent<Collider2D>();
        string colliderMode = collider != null ? (collider.isTrigger ? "Trigger" : "Collision") : "无Collider";
        
        string info = $"=== {GetType().Name} 配置 ===\n";
        info += $"触发模式: {colliderMode}\n";
        info += $"冷却时间: {cooldownDuration}秒\n";
        info += $"影响玩家: {affectPlayer}\n";
        info += $"影响敌人: {affectEnemy}\n";
        info += $"当前冷却剩余: {Mathf.Max(0, cooldownDuration - (Time.time - lastTriggerTime)):F2}秒";
        
        Debug.Log(info);
    }
    
    #endregion
}

