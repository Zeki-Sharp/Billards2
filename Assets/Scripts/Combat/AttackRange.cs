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
    
    // 朝向缓存
    private Vector2 telegraphedDirection = Vector2.right;  // 预告阶段保存的朝向
    private bool isDirectionSet = false;  // 是否已设置朝向
    
    void Start()
    {
        // 自动查找EnemyBehavior
        enemyBehavior = GetComponentInParent<EnemyBehavior>();
        if (enemyBehavior == null)
        {
            Debug.LogError($"AttackRange {name}: 未找到EnemyBehavior组件！");
            return;
        }
        
        // 检查子物体Image是否有碰撞体组件
        Transform imageTransform = transform.Find("Image");
        if (imageTransform == null)
        {
            Debug.LogError($"【攻击范围检测】{name}: 未找到子物体Image！");
        }
        else
        {
            Collider2D collider = imageTransform.GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"【攻击范围检测】{name}: Image子物体上未找到碰撞体组件！");
            }
            else
            {
                Debug.Log($"【攻击范围检测】{name}: 找到Image子物体上的碰撞体组件: {collider.GetType().Name}, IsTrigger: {collider.isTrigger}");
            }
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
        
        // 预告阶段：获取当前玩家位置并保存朝向
        UpdateTelegraphDirection();
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 显示攻击预告，保存朝向: {telegraphedDirection}");
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
    /// 预告阶段：更新并保存攻击方向
    /// </summary>
    void UpdateTelegraphDirection()
    {
        // 获取当前玩家位置
        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            telegraphedDirection = (player.transform.position - transform.position).normalized;
            isDirectionSet = true;
            
            // 立即应用朝向
            SetAttackDirection(telegraphedDirection);
        }
        else
        {
            // 默认方向
            telegraphedDirection = Vector2.right;
            isDirectionSet = true;
            SetAttackDirection(telegraphedDirection);
        }
    }
    
    /// <summary>
    /// 使用缓存的攻击方向（攻击阶段使用）
    /// </summary>
    public void ApplyTelegraphedDirection()
    {
        if (isDirectionSet)
        {
            SetAttackDirection(telegraphedDirection);
        }
        else
        {
            Debug.LogWarning($"【攻击范围检测】{name}: 朝向未设置，请先执行预告阶段");
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
                child.name.ToLower().Contains("point"))
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
        
        // 查找子物体Image上的碰撞体
        Transform imageTransform = transform.Find("Image");
        if (imageTransform == null)
        {
            Debug.LogError($"【攻击范围检测】{name}: 未找到子物体Image！");
            return targets;
        }
        
        Collider2D attackCollider = imageTransform.GetComponent<Collider2D>();
        if (attackCollider == null)
        {
            Debug.LogError($"【攻击范围检测】{name}: Image子物体上未找到碰撞体组件！");
            return targets;
        }
        
        // 使用OverlapCollider检测与当前碰撞体重叠的目标
        List<Collider2D> overlappingColliders = new List<Collider2D>();
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = true; // 只检测触发器
        contactFilter.useLayerMask = true;
        contactFilter.layerMask = Physics2D.AllLayers; // 检测所有层
        
        int overlapCount = attackCollider.Overlap(contactFilter, overlappingColliders);
        
        foreach (var collider in overlappingColliders)
        {
            if (collider.CompareTag("Player"))
            {
                targets.Add(collider.gameObject);
            }
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
