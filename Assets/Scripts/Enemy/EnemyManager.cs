using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 敌人管理器 - 管理所有敌人实例和阶段执行
/// 
/// 【核心职责】：
/// - 管理所有敌人实例列表
/// - 控制阶段转换条件（1秒定时器）
/// - 执行 EnemyPhaseController 下达的阶段命令
/// - 协调 EnemySpawner 生成敌人
/// - 向所有敌人发送阶段执行指令
/// - 向 EnemyPhaseController 报告阶段完成
/// 
/// 【执行顺序】：CONTROLLER 层 (0)
/// 【依赖】：SYSTEM 层
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class EnemyManager : SingletonManager<EnemyManager>
{
    [Header("敌人管理")]
    [SerializeField] private List<Enemy> telegraphingEnemies = new List<Enemy>(); // 预告阶段的敌人
    [SerializeField] private List<Enemy> activeEnemies = new List<Enemy>(); // 已激活的敌人
    
    [Header("阶段转换控制")]
    [SerializeField] private float phaseInterval = 1f; // 每个阶段间隔1秒
    
    [Header("移动阶段控制")]
    private int expectedMoveCompletions = 0; // 期望的移动完成数量
    private int actualMoveCompletions = 0;   // 实际的移动完成数量
    
    [Header("生成器引用")]
    private EnemySpawner enemySpawner;
    private Game.SpawnSystem.Triggers.WaveSpawnTrigger waveSpawnTrigger;
    
    [Header("阶段执行")]
    private EnemyPhase currentExecutingPhase = EnemyPhase.None;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 阶段转换事件
    public System.Action<EnemyPhase> OnPhaseCanSwitch;
    
    // 公共属性
    public List<Enemy> TelegraphingEnemies => telegraphingEnemies.Where(e => e != null).ToList();
    public List<Enemy> ActiveEnemies => activeEnemies.Where(e => e != null).ToList();
    public List<Enemy> AllEnemies => TelegraphingEnemies.Concat(ActiveEnemies).ToList();
    public int TelegraphingEnemyCount => telegraphingEnemies.Count(e => e != null);
    public int ActiveEnemyCount => activeEnemies.Count(e => e != null);
    public int TotalEnemyCount => TelegraphingEnemyCount + ActiveEnemyCount;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 单例创建成功（CONTROLLER 层）");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        CancelInvoke();
    }
    
    #endregion
    
    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        waveSpawnTrigger = FindFirstObjectByType<Game.SpawnSystem.Triggers.WaveSpawnTrigger>();
        
        if (waveSpawnTrigger == null)
        {
            Debug.LogError("EnemyManager: 未找到 WaveSpawnTrigger！");
        }
        
        ScanAndRegisterExistingEnemies();
        
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 初始化完成");
        }
    }
    
    /// <summary>
    /// 扫描并注册现有的敌人
    /// </summary>
    void ScanAndRegisterExistingEnemies()
    {
        Enemy[] existingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in existingEnemies)
        {
            // 现有敌人直接加入激活列表（假设它们已经是实体状态）
            RegisterActiveEnemy(enemy);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: 扫描到 {existingEnemies.Length} 个现有敌人，已加入激活列表");
        }
    }
    
    /// <summary>
    /// 注册预告阶段敌人
    /// </summary>
    public void RegisterTelegraphingEnemy(Enemy enemy)
    {
        if (enemy != null && !telegraphingEnemies.Contains(enemy) && !activeEnemies.Contains(enemy))
        {
            telegraphingEnemies.Add(enemy);
            if (showDebugInfo)
            {
                Debug.Log($"EnemyManager: 注册预告阶段敌人 {enemy.name}");
            }
        }
    }
    
    /// <summary>
    /// 注册激活敌人
    /// </summary>
    public void RegisterActiveEnemy(Enemy enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            // 如果之前在预告列表中，先移除
            if (telegraphingEnemies.Contains(enemy))
            {
                telegraphingEnemies.Remove(enemy);
            }
            
            activeEnemies.Add(enemy);
            
            // 设置移动完成事件监听（先移除再添加，避免重复）
            enemy.OnMoveComplete -= OnEnemyMoveComplete;
            enemy.OnMoveComplete += OnEnemyMoveComplete;
            
            if (showDebugInfo)
            {
                Debug.Log($"EnemyManager: 注册激活敌人 {enemy.name}");
            }
        }
    }
    
    /// <summary>
    /// 注销敌人（从所有列表中移除）
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy != null)
        {
            bool removed = false;
            
            if (telegraphingEnemies.Contains(enemy))
            {
                telegraphingEnemies.Remove(enemy);
                removed = true;
            }
            
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
                // 移除移动完成事件监听
                enemy.OnMoveComplete -= OnEnemyMoveComplete;
                removed = true;
            }
            
            if (removed && showDebugInfo)
            {
                Debug.Log($"EnemyManager: 注销敌人 {enemy.name}");
            }
        }
    }
    
    /// <summary>
    /// 将敌人从预告列表转移到激活列表
    /// </summary>
    public void TransferToActive(Enemy enemy)
    {
        if (enemy != null && telegraphingEnemies.Contains(enemy))
        {
            telegraphingEnemies.Remove(enemy);
            activeEnemies.Add(enemy);
            
            // 设置移动完成事件监听（先移除再添加，避免重复）
            enemy.OnMoveComplete -= OnEnemyMoveComplete;
            enemy.OnMoveComplete += OnEnemyMoveComplete;
            
            if (showDebugInfo)
            {
                Debug.Log($"EnemyManager: 敌人 {enemy.name} 从预告阶段转移到激活阶段");
            }
        }
    }
    
    /// <summary>
    /// 执行阶段
    /// </summary>
    public void ExecutePhase(EnemyPhase phase)
    {
        currentExecutingPhase = phase;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: 执行阶段 {phase}");
        }
        
        // 根据阶段执行不同逻辑
        switch (phase)
        {
            case EnemyPhase.Telegraph:
                ExecuteTelegraphPhase();
                // 启动定时器，1秒后通知可以切换到下一个阶段
                Invoke(nameof(NotifyPhaseCanSwitch), phaseInterval);
                break;
            case EnemyPhase.Spawn:
                ExecuteSpawnPhase();
                // 启动定时器，1秒后通知可以切换到下一个阶段
                Invoke(nameof(NotifyPhaseCanSwitch), phaseInterval);
                break;
            case EnemyPhase.Attack:
                ExecuteAttackPhase();
                // 启动定时器，1秒后通知可以切换到下一个阶段
                Invoke(nameof(NotifyPhaseCanSwitch), phaseInterval);
                break;
            case EnemyPhase.Move:
                ExecuteMovePhase();
                // 移动阶段使用回调，不启动定时器
                break;
            default:
                Debug.LogWarning($"EnemyManager: 未知阶段 {phase}");
                break;
        }
    }
    
    /// <summary>
    /// 通知阶段可以切换
    /// </summary>
    void NotifyPhaseCanSwitch()
    {
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: 阶段 {currentExecutingPhase} 完成，可以切换到下一个阶段");
        }
        
        OnPhaseCanSwitch?.Invoke(currentExecutingPhase);
    }
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    void ExecuteTelegraphPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 执行预告阶段");
        }
        
        // 1. 生成新敌人（如果需要）并加入预告列表
        if (waveSpawnTrigger != null)
        {
            // 使用新的WaveSpawnTrigger进行敌人生成
            // 只生成波次敌人，初始敌人已经在Start中生成
            waveSpawnTrigger.GenerateCurrentWave();
        }
        else if (enemySpawner != null)
        {
            // 警告：没有配置WaveSpawnTrigger，无法生成敌人
            Debug.LogError("EnemyManager: 未配置WaveSpawnTrigger，无法生成敌人！请配置WaveSpawnTrigger组件。");
        }
        
        // 2. 对预告阶段的敌人执行预告（新生成的敌人）
        foreach (Enemy enemy in telegraphingEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Telegraph);
            }
        }
        
        // 3. 对已激活的敌人开启并更新攻击范围（不改变状态）
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                // 开启攻击范围并更新位置
                enemy.ShowAttackRange();
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: 预告阶段完成 - 预告敌人: {telegraphingEnemies.Count}, 更新范围敌人: {activeEnemies.Count}");
        }
    }
    
    /// <summary>
    /// 执行生成阶段
    /// </summary>
    void ExecuteSpawnPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 执行生成阶段");
        }
        
        // 对预告阶段的敌人执行生成，生成后转移到激活列表
        var enemiesToTransfer = new List<Enemy>();
        foreach (Enemy enemy in telegraphingEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Spawn);
                enemiesToTransfer.Add(enemy);
            }
        }
        
        // 将生成完成的敌人转移到激活列表
        foreach (Enemy enemy in enemiesToTransfer)
        {
            TransferToActive(enemy);
        }
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    void ExecuteAttackPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 执行攻击阶段");
        }
        
        // 只对已激活的敌人执行攻击
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Attack);
            }
        }
        
        // 延迟关闭攻击范围，让特效有时间播放
        Invoke(nameof(CloseAllAttackRanges), 0.5f);
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyManager: 攻击阶段执行完成，{activeEnemies.Count} 个已激活敌人参与攻击");
        }
    }
    
    /// <summary>
    /// 执行移动阶段
    /// </summary>
    void ExecuteMovePhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyManager: 执行移动阶段");
        }
        
        // 重置移动完成计数
        expectedMoveCompletions = activeEnemies.Count(e => e != null);
        actualMoveCompletions = 0;
        
        if (expectedMoveCompletions == 0)
        {
            // 如果没有激活的敌人，直接切换到下一个阶段
            if (showDebugInfo)
            {
                Debug.Log("EnemyManager: 没有激活的敌人，直接切换阶段");
            }
            NotifyPhaseCanSwitch();
            return;
        }
        
        // 只对已激活的敌人执行移动
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Move);
            }
        }
        
        // 移动阶段开始，等待敌人移动完成
    }
    
    /// <summary>
    /// 敌人移动完成事件处理
    /// </summary>
    void OnEnemyMoveComplete(Enemy enemy)
    {
        // 检查当前是否在移动阶段
        if (currentExecutingPhase != EnemyPhase.Move)
        {
            // 非移动阶段的移动完成事件被忽略
            return;
        }
        
        actualMoveCompletions++;
        
        // 移除冗余的移动完成日志
        
        // 检查是否所有敌人都移动完成
        if (actualMoveCompletions >= expectedMoveCompletions)
        {
        // 所有敌人移动完成，切换到下一个阶段
            NotifyPhaseCanSwitch();
        }
    }
    
    /// <summary>
    /// 关闭所有敌人的攻击范围
    /// </summary>
    void CloseAllAttackRanges()
    {
        // 关闭已激活敌人的攻击范围
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.HideAttackRange();
            }
        }
        
        // 关闭预告阶段敌人的攻击范围
        foreach (Enemy enemy in telegraphingEnemies)
        {
            if (enemy != null)
            {
                enemy.HideAttackRange();
            }
        }
    }
}
