using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 敌人控制器 - 执行具体的敌人阶段逻辑
/// 
/// 【核心职责】：
/// - 管理所有敌人实例
/// - 执行 EnemyPhaseController 下达的阶段命令
/// - 协调 EnemySpawner 生成敌人
/// - 向所有敌人发送阶段执行指令
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("敌人管理")]
    [SerializeField] private List<Enemy> allEnemies = new List<Enemy>();
    
    [Header("生成器引用")]
    private EnemySpawner enemySpawner;
    
    [Header("阶段执行")]
    private EnemyPhase currentExecutingPhase = EnemyPhase.None;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        InitializeController();
        ScanAndRegisterExistingEnemies();
    }
    
    void OnDestroy()
    {
        // 清理工作
    }
    
    /// <summary>
    /// 初始化控制器
    /// </summary>
    void InitializeController()
    {
        // 查找 EnemySpawner
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Debug.LogWarning("EnemyController: 未找到 EnemySpawner");
        }
        
        if (showDebugInfo)
        {
            Debug.Log("EnemyController: 初始化完成");
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
            RegisterEnemy(enemy);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyController: 扫描到 {existingEnemies.Length} 个现有敌人");
        }
    }
    
    /// <summary>
    /// 注册敌人
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy != null && !allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
            if (showDebugInfo)
            {
                Debug.Log($"EnemyController: 注册敌人 {enemy.name}");
            }
        }
    }
    
    /// <summary>
    /// 注销敌人
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy != null && allEnemies.Contains(enemy))
        {
            allEnemies.Remove(enemy);
            if (showDebugInfo)
            {
                Debug.Log($"EnemyController: 注销敌人 {enemy.name}");
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
            Debug.Log($"EnemyController: 执行阶段 {phase}");
        }
        
        // 根据阶段执行不同逻辑
        switch (phase)
        {
            case EnemyPhase.Telegraph:
                ExecuteTelegraphPhase();
                break;
            case EnemyPhase.Spawn:
                ExecuteSpawnPhase();
                break;
            case EnemyPhase.Attack:
                ExecuteAttackPhase();
                break;
            case EnemyPhase.Move:
                ExecuteMovePhase();
                break;
            default:
                Debug.LogWarning($"EnemyController: 未知阶段 {phase}");
                break;
        }
    }
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    void ExecuteTelegraphPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyController: 执行预告阶段");
        }
        
        // 生成新敌人（如果需要）
        if (enemySpawner != null)
        {
            enemySpawner.GenerateEnemies();
        }
        
        // 对所有敌人执行预告
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Telegraph);
            }
        }
    }
    
    /// <summary>
    /// 执行生成阶段
    /// </summary>
    void ExecuteSpawnPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyController: 执行生成阶段");
        }
        
        // 对所有敌人执行生成
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Spawn);
            }
        }
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    void ExecuteAttackPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyController: 执行攻击阶段");
        }
        
        // 对所有敌人执行攻击
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Attack);
            }
        }
    }
    
    /// <summary>
    /// 执行移动阶段
    /// </summary>
    void ExecuteMovePhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("EnemyController: 执行移动阶段");
        }
        
        // 对所有敌人执行移动
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.StartPhase(EnemyPhase.Move);
            }
        }
    }
    
    /// <summary>
    /// 获取所有敌人
    /// </summary>
    public List<Enemy> GetAllEnemies()
    {
        return allEnemies.Where(e => e != null).ToList();
    }
    
    /// <summary>
    /// 获取敌人数量
    /// </summary>
    public int GetEnemyCount()
    {
        return allEnemies.Count(e => e != null);
    }
}