using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人阶段控制器
/// 只负责阶段切换通知，不实现具体功能
/// </summary>
public class EnemyPhaseController : MonoBehaviour
{
    public static EnemyPhaseController Instance { get; private set; }
    
    [Header("阶段设置")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前阶段
    private EnemyPhase currentPhase = EnemyPhase.Attack;
    
    // 阶段切换事件
    public static System.Action<EnemyPhase> OnPhaseStart;
    public static System.Action<EnemyPhase> OnPhaseComplete;
    
    // 阶段完成计数
    private int totalEnemies;
    private int completedEnemies;
    
    // 敌人生成器引用
    private EnemySpawner enemySpawner;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 获取敌人生成器引用
        enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("EnemyPhaseController: 未找到EnemySpawner！");
        }
        
        // 开始第一个阶段
        StartPhase(EnemyPhase.Attack);
    }
    
    /// <summary>
    /// 开始指定阶段
    /// </summary>
    public void StartPhase(EnemyPhase phase)
    {
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 开始阶段 {phase}");
        }
        
        currentPhase = phase;
        completedEnemies = 0;
        
        // 获取当前活跃敌人数量
        UpdateEnemyCount();
        
        // 触发阶段开始事件
        OnPhaseStart?.Invoke(phase);
        
        // 处理特殊阶段（Spawn和Telegraph）
        HandleSpecialPhases(phase);
        
        // 如果当前阶段没有敌人，直接完成
        if (totalEnemies == 0)
        {
            CompleteCurrentPhase();
        }
    }
    
    /// <summary>
    /// 敌人完成当前阶段行为
    /// </summary>
    public void OnEnemyPhaseActionComplete()
    {
        completedEnemies++;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 敌人完成 {currentPhase} 阶段 ({completedEnemies}/{totalEnemies})");
        }
        
        // 检查是否所有敌人都完成了当前阶段
        if (completedEnemies >= totalEnemies)
        {
            CompleteCurrentPhase();
        }
    }
    
    /// <summary>
    /// 完成当前阶段
    /// </summary>
    void CompleteCurrentPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 完成阶段 {currentPhase}");
        }
        
        // 触发阶段完成事件
        OnPhaseComplete?.Invoke(currentPhase);
        
        // 如果是预告阶段，直接结束敌人回合
        if (currentPhase == EnemyPhase.Telegraph)
        {
            EndEnemyPhase();
        }
        else
        {
            // 切换到下一个阶段
            SwitchToNextPhase();
        }
    }
    
    /// <summary>
    /// 切换到下一个阶段
    /// </summary>
    void SwitchToNextPhase()
    {
        EnemyPhase nextPhase = GetNextPhase(currentPhase);
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 切换到下一阶段 {nextPhase}");
        }
        
        // 开始下一个阶段
        StartPhase(nextPhase);
    }
    
    /// <summary>
    /// 获取下一个阶段
    /// </summary>
    EnemyPhase GetNextPhase(EnemyPhase current)
    {
        switch (current)
        {
            case EnemyPhase.Attack:
                return EnemyPhase.Spawn;
            case EnemyPhase.Spawn:
                return EnemyPhase.Move;
            case EnemyPhase.Move:
                return EnemyPhase.Telegraph;
            default:
                return EnemyPhase.Attack;
        }
    }
    
    /// <summary>
    /// 结束敌人阶段，回到玩家阶段
    /// </summary>
    void EndEnemyPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyPhaseController: 敌人回合结束，回到玩家阶段");
        }
        
        // 通知GameFlowController回到玩家阶段
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.RequestNormalState();
        }
    }
    
    /// <summary>
    /// 更新敌人数量
    /// </summary>
    void UpdateEnemyCount()
    {
        // 根据当前阶段获取相应的敌人数量
        switch (currentPhase)
        {
            case EnemyPhase.Attack:
            case EnemyPhase.Move:
                // 攻击和移动阶段：统计当前活跃的敌人
                totalEnemies = GetActiveEnemyCount();
                break;
            case EnemyPhase.Spawn:
            case EnemyPhase.Telegraph:
                // 生成和预告阶段：由EnemySpawner处理，不需要等待敌人完成
                totalEnemies = 0;
                break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyPhaseController: 当前阶段 {currentPhase} 需要处理 {totalEnemies} 个敌人");
        }
    }
    
    /// <summary>
    /// 获取活跃敌人数量
    /// </summary>
    int GetActiveEnemyCount()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        int count = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                count++;
            }
        }
        return count;
    }
    
    
    /// <summary>
    /// 获取当前阶段
    /// </summary>
    public EnemyPhase GetCurrentPhase()
    {
        return currentPhase;
    }
    
    /// <summary>
    /// 检查是否在指定阶段
    /// </summary>
    public bool IsInPhase(EnemyPhase phase)
    {
        return currentPhase == phase;
    }
    
    /// <summary>
    /// 处理特殊阶段（Spawn和Telegraph）
    /// </summary>
    void HandleSpecialPhases(EnemyPhase phase)
    {
        if (enemySpawner == null) return;
        
        switch (phase)
        {
            case EnemyPhase.Spawn:
                enemySpawner.SpawnEnemies();
                // 不需要调用OnEnemyPhaseActionComplete，SpawnEnemies内部会调用
                break;
            case EnemyPhase.Telegraph:
                enemySpawner.StartPreview();
                // 不需要调用OnEnemyPhaseActionComplete，StartPreview内部会调用
                break;
        }
    }
}
