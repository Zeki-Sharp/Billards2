using UnityEngine;

/// <summary>
/// 敌人阶段控制器 - 管理游戏阶段转换
/// 
/// 【核心职责】：
/// - 管理敌人阶段的循环（攻击 -> 移动 -> 生成 -> 预告）
/// - 响应 EnemyController 的阶段完成事件
/// - 协调 EnemyController 执行具体阶段逻辑
/// 
/// 【阶段逻辑】：
/// - 攻击：攻击上一回合预告的位置
/// - 移动：敌人移动
/// - 生成：生成上一回合预告的位置
/// - 预告：预告下一个位置
/// 
/// 【执行顺序】：CONTROLLER 层 (0)
/// 【依赖】：SYSTEM 层, EnemyManager (CONTROLLER 层)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class EnemyPhaseController : SingletonManager<EnemyPhaseController>
{
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前敌人阶段
    private EnemyPhase currentEnemyPhase = EnemyPhase.None;
    
    // 敌人阶段顺序
    private readonly EnemyPhase[] enemyPhaseSequence = {
        EnemyPhase.Attack,     // 攻击（攻击上一回合预告的位置）
        EnemyPhase.Move,       // 移动
        EnemyPhase.Spawn,      // 生成（生成上一回合预告的位置）
        EnemyPhase.Telegraph   // 预告（更新下一个位置）
    };
    
    private int currentEnemyPhaseIndex = 0;
    
    // 组件引用
    private EnemyManager enemyManager;
    
    // 阶段切换事件
    public static System.Action<EnemyPhase> OnPhaseStart;
    public static System.Action<EnemyPhase> OnPhaseComplete;
    public static System.Action OnEnemyPhaseComplete; // 整个敌人阶段完成事件
    
    // 公共属性
    public EnemyPhase CurrentEnemyPhase => currentEnemyPhase;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    
    protected override void OnManagerCreated()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyPhaseController: 单例创建成功（CONTROLLER 层）");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        CancelInvoke(); // 取消所有定时器
        
        // 取消事件订阅
        if (enemyManager != null)
        {
            enemyManager.OnPhaseCanSwitch -= OnEnemyPhaseCanSwitch;
        }
    }
    
    #endregion
    
    void Start()
    {
        enemyManager = EnemyManager.Instance;
        if (enemyManager == null)
        {
            Debug.LogError("EnemyPhaseController: 未找到 EnemyManager！");
        }
        else
        {
            enemyManager.OnPhaseCanSwitch += OnEnemyPhaseCanSwitch;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("EnemyPhaseController: 初始化完成");
        }
    }
    
    /// <summary>
    /// 敌人阶段可以切换的回调
    /// </summary>
    void OnEnemyPhaseCanSwitch(EnemyPhase completedPhase)
    {
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 收到阶段完成通知 - {completedPhase}");
        }
        
        // 进入下一个阶段
        currentEnemyPhaseIndex++;
        ExecuteNextEnemyPhase();
    }
    
    /// <summary>
    /// 开始敌人阶段（由 GameFlowController 调用）
    /// </summary>
    public void StartEnemyPhase()
    {
        CancelInvoke(); // 确保取消所有之前的定时器
        
        if (showDebugInfo)
        {
            Debug.Log("=== 敌人阶段开始 (由 GameFlowController 触发) ===");
        }
        
        // 重置阶段索引
        currentEnemyPhaseIndex = 0;
        
        // 开始执行第一个敌人阶段
        ExecuteNextEnemyPhase();
    }
    
    /// <summary>
    /// 执行下一个敌人阶段
    /// </summary>
    void ExecuteNextEnemyPhase()
    {
        if (currentEnemyPhaseIndex >= enemyPhaseSequence.Length)
        {
            // 所有敌人阶段完成
            if (showDebugInfo)
            {
                Debug.Log("=== 所有敌人阶段完成，通知 GameFlowController ===");
            }
            
            // 通知 GameFlowController 敌人阶段完成
            OnEnemyPhaseComplete?.Invoke();
            return;
        }
        
        // 获取当前阶段
        EnemyPhase phase = enemyPhaseSequence[currentEnemyPhaseIndex];
        currentEnemyPhase = phase;
        
        if (showDebugInfo)
        {
            string phaseDescription = GetPhaseDescription(phase);
            Debug.Log($"--- 开始执行敌人子阶段: {phase} ({phaseDescription}) (索引: {currentEnemyPhaseIndex}) ---");
        }
        
        // 通知阶段开始
        OnPhaseStart?.Invoke(phase);
        
        // 执行具体阶段逻辑
        if (enemyManager != null)
        {
            enemyManager.ExecutePhase(phase);
        }
        else
        {
            Debug.LogWarning("EnemyPhaseController: enemyManager 为空！");
        }
        
        // 不再使用定时器，由 EnemyController 通知阶段完成
    }
    
    
    
    /// <summary>
    /// 手动开始指定阶段（用于测试）
    /// </summary>
    public void StartPhase(EnemyPhase phase)
    {
        currentEnemyPhase = phase;
        OnPhaseStart?.Invoke(phase);
        
        if (enemyManager != null)
        {
            enemyManager.ExecutePhase(phase);
        }
    }
    
    /// <summary>
    /// 获取当前阶段
    /// </summary>
    public EnemyPhase GetCurrentPhase()
    {
        return currentEnemyPhase;
    }
    
    /// <summary>
    /// 强制开始敌人阶段（用于测试）
    /// </summary>
    public void ForceEnemyPhase()
    {
        CancelInvoke(); // 取消所有定时器
        StartEnemyPhase();
    }
    
    /// <summary>
    /// 获取阶段描述
    /// </summary>
    string GetPhaseDescription(EnemyPhase phase)
    {
        switch (phase)
        {
            case EnemyPhase.Attack:
                return "攻击上一回合预告的位置";
            case EnemyPhase.Move:
                return "移动";
            case EnemyPhase.Spawn:
                return "生成上一回合预告的位置";
            case EnemyPhase.Telegraph:
                return "预告下一个位置";
            default:
                return "未知阶段";
        }
    }
}
