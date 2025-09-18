using UnityEngine;
using System.Collections;

/// <summary>
/// 预告管理器 - 统一管理所有预告逻辑
/// 
/// 【核心职责】：
/// - 统一管理敌人攻击预告和敌人生成预告
/// - 同时启动两种预告，等待都完成后通知EnemyPhaseController
/// - 作为预告阶段的唯一入口点
/// 
/// 【设计原则】：
/// - 单一职责：只管理预告逻辑
/// - 解耦合：EnemyPhaseController不需要了解具体预告细节
/// - 可扩展：未来可以轻松添加新的预告类型
/// </summary>
public class TelegraphManager : MonoBehaviour
{
    public static TelegraphManager Instance { get; private set; }
    
    [Header("预告设置")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float telegraphDuration = 2f; // 预告持续时间
    
    // 预告完成计数
    private int completedTelegraphs = 0;
    private int totalTelegraphs = 2; // 攻击预告 + 生成预告
    
    // 组件引用
    private EnemySpawner enemySpawner;
    
    // 预告状态
    private bool isTelegraphActive = false;
    
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
            Debug.LogWarning("TelegraphManager: 未找到EnemySpawner！");
        }
    }
    
    /// <summary>
    /// 开始预告阶段
    /// </summary>
    public void StartTelegraphPhase()
    {
        if (isTelegraphActive)
        {
            Debug.LogWarning("TelegraphManager: 预告阶段已在进行中，忽略重复调用");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("TelegraphManager: 开始预告阶段");
        }
        
        isTelegraphActive = true;
        completedTelegraphs = 0;
        
        // 同时启动两种预告
        StartAttackTelegraph();
        StartSpawnTelegraph();
        
        // 启动超时保护
        StartCoroutine(TelegraphTimeout());
    }
    
    /// <summary>
    /// 开始敌人攻击预告
    /// </summary>
    private void StartAttackTelegraph()
    {
        if (showDebugInfo)
        {
            Debug.Log("TelegraphManager: 开始敌人攻击预告");
        }
        
        // 通知所有敌人显示攻击范围
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        int activeEnemies = 0;
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                enemy.ShowAttackTelegraph();
                activeEnemies++;
            }
        }
        
        if (activeEnemies == 0)
        {
            // 没有活跃敌人，直接完成攻击预告
            OnTelegraphComplete("AttackTelegraph");
        }
        else
        {
            // 启动攻击预告完成检查
            StartCoroutine(CheckAttackTelegraphComplete());
        }
    }
    
    /// <summary>
    /// 开始敌人生成预告
    /// </summary>
    private void StartSpawnTelegraph()
    {
        if (showDebugInfo)
        {
            Debug.Log("TelegraphManager: 开始敌人生成预告");
        }
        
        if (enemySpawner != null)
        {
            // 调用EnemySpawner的预告功能
            enemySpawner.StartPreview();
        }
        else
        {
            // 没有EnemySpawner，直接完成生成预告
            OnTelegraphComplete("SpawnTelegraph");
        }
    }
    
    /// <summary>
    /// 检查攻击预告是否完成
    /// </summary>
    private IEnumerator CheckAttackTelegraphComplete()
    {
        // 等待预告持续时间
        yield return new WaitForSeconds(telegraphDuration);
        
        // 通知攻击预告完成
        OnTelegraphComplete("AttackTelegraph");
    }
    
    /// <summary>
    /// 预告完成回调
    /// </summary>
    public void OnTelegraphComplete(string telegraphType)
    {
        if (!isTelegraphActive)
        {
            Debug.LogWarning($"TelegraphManager: 收到预告完成通知但预告阶段未激活: {telegraphType}");
            return;
        }
        
        completedTelegraphs++;
        
        if (showDebugInfo)
        {
            Debug.Log($"TelegraphManager: {telegraphType} 完成 ({completedTelegraphs}/{totalTelegraphs})");
        }
        
        // 检查是否所有预告都完成
        if (completedTelegraphs >= totalTelegraphs)
        {
            CompleteTelegraphPhase();
        }
    }
    
    /// <summary>
    /// 完成预告阶段
    /// </summary>
    private void CompleteTelegraphPhase()
    {
        if (showDebugInfo)
        {
            Debug.Log("TelegraphManager: 所有预告完成，通知EnemyPhaseController");
        }
        
        isTelegraphActive = false;
        
        // 通知EnemyPhaseController阶段完成
        if (EnemyPhaseController.Instance != null)
        {
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
        else
        {
            Debug.LogError("TelegraphManager: EnemyPhaseController.Instance 为 null！");
        }
    }
    
    /// <summary>
    /// 预告超时保护
    /// </summary>
    private IEnumerator TelegraphTimeout()
    {
        yield return new WaitForSeconds(telegraphDuration + 1f); // 比预告时间稍长
        
        if (isTelegraphActive)
        {
            Debug.LogWarning("TelegraphManager: 预告阶段超时，强制完成");
            CompleteTelegraphPhase();
        }
    }
    
    /// <summary>
    /// 检查是否正在预告阶段
    /// </summary>
    public bool IsTelegraphActive()
    {
        return isTelegraphActive;
    }
    
    /// <summary>
    /// 重置预告状态
    /// </summary>
    public void ResetTelegraphState()
    {
        isTelegraphActive = false;
        completedTelegraphs = 0;
    }
}
