using UnityEngine;
using System.Collections;

/// <summary>
/// 敌人脚本 - 管理整个敌人生命周期
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("子对象引用")]
    public Transform spawnPreview;        // 攻击预告预制体（首次出现用）
    public Transform enemyItem;           // 敌人物体
    public Transform attackArea;          // 攻击范围预制体
    
    [Header("事件")]
    public System.Action<Enemy> OnTelegraphComplete;
    public System.Action<Enemy> OnSpawnComplete;
    public System.Action<Enemy> OnAttackComplete;
    public System.Action<Enemy> OnMoveComplete;
    public System.Action<Enemy, EnemyPhase> OnEnemyPhaseComplete;
    
    private EnemyState currentState = EnemyState.None;
    private EnemyBehavior enemyBehavior;
    
    [Header("状态管理")]
    private bool isFirstAppearance = true;  // 是否首次出现
    
    [Header("测试模式")]
    public bool testMode = false;
    private int currentTestStep = 0;
    
    
    /// <summary>
    /// 重置测试模式
    /// </summary>
    public void ResetTestMode()
    {
        currentTestStep = 0;
        Debug.Log($"Enemy {name}: 重置测试模式");
    }
    
    
    void Start()
    {
        // 获取行为组件
        enemyBehavior = GetComponentInChildren<EnemyBehavior>();
        if (enemyBehavior == null)
        {
            Debug.LogError($"Enemy {name}: 未找到EnemyBehavior组件！");
        }
        else
        {
            // 设置攻击范围引用
            if (attackArea != null)
            {
                enemyBehavior.SetAttackArea(attackArea);
            }
        }
    }
    
    void Update()
    {
        if (testMode)
        {
            // 按空格键执行下一步
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteTestStep();
            }
            
            // 按R键重置测试模式
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetTestMode();
            }
        }
    }
    
    /// <summary>
    /// 执行测试步骤
    /// </summary>
    private void ExecuteTestStep()
    {
        switch (currentTestStep)
        {
            case 0:
                Debug.Log("=== 测试步骤 1: 预告 ===");
                StartTelegraph();
                currentTestStep++;
                break;
            case 1:
                Debug.Log("=== 测试步骤 2: 生成 ===");
                StartSpawn();
                currentTestStep++;
                break;
            case 2:
                Debug.Log("=== 测试步骤 3: 攻击 ===");
                StartPhase(EnemyPhase.Attack);
                currentTestStep++;
                break;
            case 3:
                Debug.Log("=== 测试步骤 4: 移动 ===");
                StartPhase(EnemyPhase.Move);
                currentTestStep++;
                break;
            case 4:
                Debug.Log("=== 测试完成，等待玩家阶段 ===");
                // 不重置，等待玩家阶段
                break;
        }
    }
    
    /// <summary>
    /// 开始预告
    /// </summary>
    public void StartTelegraph()
    {
        Debug.Log($"Enemy {name}: 开始预告");
        SetState(EnemyState.Telegraphing);
        
        if (isFirstAppearance)
        {
            // 首次出现：显示攻击预告预制体
            if (enemyItem != null)
            {
                enemyItem.gameObject.SetActive(false);
            }
            
            if (spawnPreview != null)
            {
                spawnPreview.gameObject.SetActive(true);
            }
            
            Debug.Log($"Enemy {name}: 首次出现，显示攻击预告");
        }
        else
        {
            // 后续循环：更新攻击范围
            enemyBehavior?.ExecuteTelegraphPhase();
            Debug.Log($"Enemy {name}: 后续循环，更新攻击范围");
        }
    }
    
    /// <summary>
    /// 开始生成
    /// </summary>
    public void StartSpawn()
    {
        Debug.Log($"Enemy {name}: 开始生成");
        SetState(EnemyState.Spawning);
        
        if (isFirstAppearance)
        {
            // 首次出现：关闭攻击预告，显示敌人物体
            if (spawnPreview != null)
            {
                spawnPreview.gameObject.SetActive(false);
            }
            
            if (enemyItem != null)
            {
                enemyItem.gameObject.SetActive(true);
            }
            
            // 标记为已出现，后续不再参与生成阶段
            isFirstAppearance = false;
            Debug.Log($"Enemy {name}: 首次生成完成，后续不再参与生成阶段");
        }
        else
        {
            // 后续循环：跳过生成阶段
            Debug.Log($"Enemy {name}: 后续循环，跳过生成阶段");
        }
        
        // 生成完成后设置为活跃状态
        SetState(EnemyState.Active);
        OnSpawnComplete?.Invoke(this);
    }
    
    /// <summary>
    /// 开始阶段
    /// </summary>
    public void StartPhase(EnemyPhase phase)
    {
        Debug.Log($"Enemy {name}: 开始阶段 {phase}");
        
        switch (phase)
        {
            case EnemyPhase.Attack:
                StartAttackPhase();
                break;
            case EnemyPhase.Move:
                StartCoroutine(ExecuteMovePhaseCoroutine());
                break;
            case EnemyPhase.Telegraph:
                StartTelegraph();
                break;
            case EnemyPhase.Spawn:
                StartSpawn();
                break;
        }
    }
    
    /// <summary>
    /// 更新攻击范围（不改变状态，仅用于已激活的敌人）
    /// </summary>
    public void UpdateAttackRange()
    {
        if (enemyBehavior != null)
        {
            enemyBehavior.ExecuteTelegraphPhase();
        }
    }
    
    /// <summary>
    /// 执行移动阶段的协程
    /// </summary>
    IEnumerator ExecuteMovePhaseCoroutine()
    {
        if (enemyBehavior != null)
        {
            // 开始移动
            enemyBehavior.ExecuteMovePhase();
            
            // 检查是否真正开始移动
            if (enemyBehavior.IsMoving())
            {
                // 等待移动完成
                while (enemyBehavior.IsMoving())
                {
                    yield return null;
                }
            }
            else
            {
                // 如果没有开始移动（比如找不到玩家），直接跳过
                Debug.Log($"Enemy {name}: 无法移动，跳过移动阶段");
            }
        }
        
        // 移动完成后触发事件
        OnMoveComplete?.Invoke(this);
    }
    
    /// <summary>
    /// 开始攻击阶段
    /// </summary>
    private void StartAttackPhase()
    {
        enemyBehavior?.ExecuteAttackPhase();
        OnEnemyPhaseComplete?.Invoke(this, EnemyPhase.Attack);
    }
    
    /// <summary>
    /// 开始移动阶段
    /// </summary>
    private void StartMovePhase()
    {
        enemyBehavior?.ExecuteMovePhase();
        OnEnemyPhaseComplete?.Invoke(this, EnemyPhase.Move);
    }
    
    /// <summary>
    /// 获取状态
    /// </summary>
    public EnemyState GetState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 设置状态
    /// </summary>
    public void SetState(EnemyState newState)
    {
        currentState = newState;
    }
    
    /// <summary>
    /// 获取敌人行为组件
    /// </summary>
    public EnemyBehavior GetEnemyBehavior()
    {
        return enemyBehavior;
    }
    
    /// <summary>
    /// 设置敌人数据
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        if (enemyBehavior != null)
        {
            enemyBehavior.enemyData = data;
        }
    }
    
    /// <summary>
    /// 显示攻击范围并更新位置
    /// </summary>
    public void ShowAttackRange()
    {
        if (enemyBehavior != null)
        {
            enemyBehavior.ShowAttackRange();
        }
    }
    
    /// <summary>
    /// 隐藏攻击范围
    /// </summary>
    public void HideAttackRange()
    {
        if (enemyBehavior != null)
        {
            enemyBehavior.HideAttackRange();
        }
    }
}
