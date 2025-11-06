using UnityEngine;

/// <summary>
/// 回合制状态组件基类 - 所有回合制状态的通用逻辑
/// 
/// 【核心职责】：
/// - 监听回合事件（GameFlowState变化）
/// - 管理剩余回合数
/// - 处理堆叠逻辑（回合数累加，伤害保持第一次的值）
/// - 自动清理（回合数为0时）
/// - 提供抽象方法供子类实现具体效果
/// 
/// 【设计原则】：
/// - MonoBehaviour，挂载到目标实体身上
/// - 每个实体独立管理自己的状态
/// - 完全事件驱动，不需要Update
/// - 利用Unity生命周期自动清理
/// 
/// 【子类职责】：
/// - 重写 OnTurnTrigger() 实现具体效果（造成伤害、减速等）
/// - 可选重写 OnStatusApplied()、OnStatusRemoved()
/// </summary>
public abstract class TurnBasedStatusComponent : MonoBehaviour
{
    #region 事件通知（通过 GameEventBus）
    
    /// <summary>
    /// 触发状态变化事件（通过 GameEventBus）
    /// </summary>
    protected void NotifyStatusChanged()
    {
        if (statusData != null)
        {
            // ✅ 发送真正的目标根物体，而不是 gameObject（可能是子对象）
            GameObject eventTarget = targetRoot != null ? targetRoot : gameObject;
            GameEventBus.PublishTurnBasedStatusChanged(eventTarget, statusData, remainingTurns);
        }
    }
    
    #endregion
    
    #region 受保护字段（子类可访问）
    
    /// <summary>
    /// 状态配置数据
    /// </summary>
    protected TurnBasedStatusData statusData;
    
    /// <summary>
    /// 剩余回合数
    /// </summary>
    protected int remainingTurns;
    
    /// <summary>
    /// 每回合伤害（保持第一次施加的值）
    /// </summary>
    protected float damagePerTurn;
    
    /// <summary>
    /// 状态来源（施加者）
    /// </summary>
    protected GameObject source;
    
    /// <summary>
    /// 状态目标（有 IDamageable 的根物体）
    /// </summary>
    protected GameObject targetRoot;
    
    /// <summary>
    /// 特效实例
    /// </summary>
    protected GameObject vfxInstance;
    
    /// <summary>
    /// 是否显示调试日志
    /// </summary>
    protected bool showDebugLog = true;
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 剩余回合数
    /// </summary>
    public int RemainingTurns => remainingTurns;
    
    /// <summary>
    /// 每回合伤害
    /// </summary>
    public float DamagePerTurn => damagePerTurn;
    
    /// <summary>
    /// 状态ID
    /// </summary>
    public string StatusID => statusData != null ? statusData.statusID : "";
    
    /// <summary>
    /// 状态显示名称
    /// </summary>
    public string DisplayName => statusData != null ? statusData.displayName : "";
    
    /// <summary>
    /// 状态数据（供UI访问）
    /// </summary>
    public TurnBasedStatusData StatusData => statusData;
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化状态（第一次施加）
    /// </summary>
    public virtual void Initialize(TurnBasedStatusData data, GameObject src, bool enableDebugLog = true)
    {
        if (data == null || !data.IsValid())
        {
            Debug.LogError($"[TurnBasedStatusComponent] 无效的状态数据！");
            Destroy(this);
            return;
        }
        
        statusData = data;
        source = src;
        remainingTurns = data.baseDurationInTurns;
        damagePerTurn = data.baseDamagePerTurn;
        showDebugLog = enableDebugLog;
        
        // ✅ 记录真正的目标根物体（有 IDamageable 的对象）
        var damageable = GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = GetComponentInParent<IDamageable>();
        }
        targetRoot = (damageable as MonoBehaviour)?.gameObject ?? gameObject;
        
        // 生成特效
        if (data.vfxPrefab != null)
        {
            vfxInstance = Instantiate(data.vfxPrefab, transform);
            vfxInstance.transform.localPosition = Vector3.zero;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[{data.displayName}] {gameObject.name} 被施加状态：{remainingTurns}回合，每回合{damagePerTurn}伤害");
        }
        
        // 子类初始化逻辑
        OnStatusApplied();
        
        // 通知UI更新
        NotifyStatusChanged();
    }
    
    /// <summary>
    /// 叠加层数（增加回合数，伤害保持第一次的值）
    /// </summary>
    public virtual void AddStack(int additionalTurns)
    {
        // ✅ 简化版：回合数累加，伤害不变
        remainingTurns += additionalTurns;
        
        // 检查最大堆叠限制
        if (statusData.maxStacks > 0 && remainingTurns > statusData.maxStacks)
        {
            remainingTurns = statusData.maxStacks;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[{DisplayName}] {gameObject.name} 状态叠加：+{additionalTurns}回合，总计{remainingTurns}回合");
        }
        
        // 通知UI更新
        NotifyStatusChanged();
    }
    
    #endregion
    
    #region Unity生命周期
    
    protected virtual void OnEnable()
    {
        // 监听回合事件
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
    }
    
    protected virtual void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnGameFlowStateChanged -= OnGameFlowStateChanged;
    }
    
    protected virtual void OnDestroy()
    {
        // 清理特效
        if (vfxInstance != null)
        {
            Destroy(vfxInstance);
        }
        
        // 子类清理逻辑
        OnStatusRemoved();
        
        // 通知UI更新（状态移除，回合数为0）
        if (statusData != null)
        {
            GameObject eventTarget = targetRoot != null ? targetRoot : gameObject;
            GameEventBus.PublishTurnBasedStatusChanged(eventTarget, statusData, 0);
        }
    }
    
    #endregion
    
    #region 回合事件处理
    
    /// <summary>
    /// 响应游戏流程状态变化
    /// </summary>
    void OnGameFlowStateChanged(GameFlowState newState)
    {
        if (statusData == null)
        {
            return;
        }
        
        // 检查是否是配置的触发阶段
        if (newState == statusData.triggerPhase)
        {
            ProcessTurn();
        }
    }
    
    /// <summary>
    /// 处理回合触发
    /// </summary>
    void ProcessTurn()
    {
        // 检查回合数
        if (remainingTurns <= 0)
        {
            Destroy(this);
            return;
        }
        
        // 调用子类的具体效果逻辑
        OnTurnTrigger();
        
        // 回合数递减
        remainingTurns--;
        
        // 通知UI更新（回合数变化）
        NotifyStatusChanged();
        
        // 如果回合数耗尽，销毁组件
        if (remainingTurns <= 0)
        {
            Destroy(this);
        }
    }
    
    #endregion
    
    #region 抽象方法（子类必须实现）
    
    /// <summary>
    /// 每回合触发时调用（子类必须实现）
    /// </summary>
    protected abstract void OnTurnTrigger();
    
    #endregion
    
    #region 虚方法（子类可选重写）
    
    /// <summary>
    /// 状态首次施加时调用（子类可选重写）
    /// </summary>
    protected virtual void OnStatusApplied()
    {
        // 子类可重写
    }
    
    /// <summary>
    /// 状态移除时调用（子类可选重写）
    /// </summary>
    protected virtual void OnStatusRemoved()
    {
        // 子类可重写
    }
    
    #endregion
    
    #region 公共查询接口
    
    /// <summary>
    /// 获取状态信息（用于UI显示或调试）
    /// </summary>
    public virtual string GetStatusInfo()
    {
        return $"{DisplayName}：{remainingTurns}回合，每回合{damagePerTurn}伤害";
    }
    
    #endregion
}

