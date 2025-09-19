using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 攻击范围脚本 - 基于事件驱动的攻击范围展示
/// 
/// 【核心功能】：
/// - 只负责展示攻击范围，不执行攻击逻辑
/// - 基于EnemyPhaseController事件驱动
/// - 预告阶段显示，攻击阶段隐藏
/// 
/// 【设计原则】：
/// - 单一职责：只处理攻击范围展示
/// - 事件驱动：响应EnemyPhaseController事件
/// - 不执行逻辑：不包含攻击逻辑
/// </summary>
public class AttackRange : MonoBehaviour
{
    [Header("攻击范围设置")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float attackRange = 3f;  // 攻击范围距离
    
    // 组件引用
    private EnemyBehavior enemyBehavior;
    private EnemySpawner enemySpawner;
    
    void Start()
    {
        // 自动查找EnemyBehavior
        enemyBehavior = GetComponentInParent<EnemyBehavior>();
        if (enemyBehavior == null)
        {
            Debug.LogError($"AttackRange {name}: 未找到EnemyBehavior组件！");
            return;
        }
        
        // 初始状态为隐藏
        gameObject.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 初始化完成");
        }
    }
    
    void OnDestroy()
    {
        // AttackRange不再订阅阶段事件
    }
    
    /// <summary>
    /// 显示预告（由Enemy调用）
    /// </summary>
    public void ShowTelegraph()
    {
        gameObject.SetActive(true);
        UpdateDirection();
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 显示攻击预告");
        }
    }
    
    /// <summary>
    /// 隐藏预告（由Enemy调用）
    /// </summary>
    public void HideTelegraph()
    {
        gameObject.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 隐藏攻击预告");
        }
    }
    
    /// <summary>
    /// 更新攻击方向
    /// </summary>
    void UpdateDirection()
    {
        // 直接朝向玩家当前位置
        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            Vector2 playerDirection = (player.transform.position - transform.position).normalized;
            SetAttackDirection(playerDirection);
            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 朝向玩家当前位置 {playerDirection} (玩家位置: {player.transform.position})");
            }
        }
        else
        {
            // 默认方向
            SetAttackDirection(Vector2.right);
            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 未找到玩家，使用默认方向");
            }
        }
    }
    
    /// <summary>
    /// 设置攻击方向
    /// </summary>
    void SetAttackDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            // 获取攻击范围的两个点
            Vector2 startPoint = transform.position;
            Vector2 endPoint = GetEndPointWorldPosition();
            
            // 计算攻击范围当前的方向向量
            Vector2 currentRangeDirection = (endPoint - startPoint).normalized;
            
            // 计算需要旋转的角度，让当前方向与目标方向一致
            float angle = Vector2.SignedAngle(currentRangeDirection, direction);
            
            // 旋转攻击范围
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward) * transform.rotation;
            
            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 当前方向 {currentRangeDirection}, 目标方向 {direction}");
                Debug.Log($"AttackRange {name}: 旋转角度 {angle:F2} 度");
            }
        }
    }
    
    /// <summary>
    /// 获取攻击范围终点的世界坐标
    /// </summary>
    private Vector2 GetEndPointWorldPosition()
    {
        // 尝试找到endpoint子对象
        Transform endPointTransform = transform.Find("EndPoint");
        if (endPointTransform != null)
        {
            return endPointTransform.position;
        }
        
        // 如果没有EndPoint子对象，尝试其他可能的子对象名称
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.ToLower().Contains("end") || 
                child.name.ToLower().Contains("point") ||
                child.name.ToLower().Contains("tip"))
            {
                return child.position;
            }
        }
        
        // 如果都没找到，假设endpoint在正右方
        return (Vector2)transform.position + Vector2.right * attackRange;
    }
    
    /// <summary>
    /// 获取攻击范围内的目标（由Enemy调用）
    /// </summary>
    public List<GameObject> GetTargetsInRange()
    {
        List<GameObject> targets = new List<GameObject>();
        
        // 查找玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 计算到玩家的距离
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // 检查是否在攻击范围内
            if (distanceToPlayer <= attackRange)
            {
                targets.Add(player);
                
                if (showDebugInfo)
                {
                    Debug.Log($"AttackRange {name}: 玩家在攻击范围内，距离: {distanceToPlayer:F2}");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"AttackRange {name}: 玩家不在攻击范围内，距离: {distanceToPlayer:F2}, 范围: {attackRange}");
                }
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"AttackRange {name}: 未找到玩家！");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 检测到 {targets.Count} 个目标");
        }
        
        return targets;
    }
    
    // 调试绘制攻击范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // 绘制攻击方向
        Vector2 direction = transform.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
