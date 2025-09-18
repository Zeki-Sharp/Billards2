using UnityEngine;

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
    
    // 组件引用
    private Enemy enemy;
    
    void Start()
    {
        // 获取敌人组件
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"AttackRange {name}: 未找到Enemy组件！");
            return;
        }
        
        // 订阅敌人阶段事件
        EnemyPhaseController.OnPhaseStart += OnPhaseStart;
        
        // 初始状态为隐藏
        gameObject.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 初始化完成");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        EnemyPhaseController.OnPhaseStart -= OnPhaseStart;
    }
    
    /// <summary>
    /// 阶段开始事件处理
    /// </summary>
    void OnPhaseStart(EnemyPhase phase)
    {
        switch (phase)
        {
            case EnemyPhase.Telegraph:
                // 预告阶段：显示并更新方向
                ShowTelegraph();
                break;
            case EnemyPhase.Attack:
                // 攻击阶段：隐藏
                HideTelegraph();
                break;
            default:
                // 其他阶段：隐藏
                HideTelegraph();
                break;
        }
    }
    
    /// <summary>
    /// 显示预告
    /// </summary>
    void ShowTelegraph()
    {
        gameObject.SetActive(true);
        UpdateDirection();
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 显示攻击预告");
        }
    }
    
    /// <summary>
    /// 隐藏预告
    /// </summary>
    void HideTelegraph()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 更新攻击方向
    /// </summary>
    void UpdateDirection()
    {
        if (enemy == null) return;
        
        // 获取敌人移动方向
        Vector2 moveDirection = enemy.GetCurrentMovementDirection();
        
        if (moveDirection != Vector2.zero)
        {
            // 使用敌人移动方向
            SetAttackDirection(moveDirection);
            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 使用移动方向 {moveDirection}");
            }
        }
        else
        {
            // 没有移动方向，朝向玩家
            Player player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                Vector2 playerDirection = (player.transform.position - transform.position).normalized;
                SetAttackDirection(playerDirection);
                if (showDebugInfo)
                {
                    Debug.Log($"AttackRange {name}: 朝向玩家 {playerDirection}");
                }
            }
            else
            {
                // 默认方向
                SetAttackDirection(Vector2.right);
                if (showDebugInfo)
                {
                    Debug.Log($"AttackRange {name}: 使用默认方向");
                }
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
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
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
