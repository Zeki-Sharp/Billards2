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
    
    // 3D组件引用
    [Header("3D碰撞体引用")]
    [Tooltip("攻击范围的3D碰撞体（MeshCollider），如果为空则自动从当前GameObject获取")]
    [SerializeField] private Collider attackCollider3D;
    
    void Start()
    {
        // 自动查找EnemyBehavior
        enemyBehavior = GetComponentInParent<EnemyBehavior>();
        if (enemyBehavior == null)
        {
            Debug.LogError($"AttackRange {name}: 未找到EnemyBehavior组件！");
            return;
        }
        
        // 如果未手动指定碰撞体，从当前GameObject获取
        if (attackCollider3D == null)
        {
            attackCollider3D = GetComponent<Collider>();
            if (attackCollider3D == null)
            {
                Debug.LogError($"【攻击范围检测】{name}: 未找到3D碰撞体组件！请手动指定或确保当前GameObject上有Collider组件。");
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"【攻击范围检测】{name}: 自动找到3D碰撞体组件: {attackCollider3D.GetType().Name}, IsTrigger: {attackCollider3D.isTrigger}");
                }
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"【攻击范围检测】{name}: 使用手动指定的3D碰撞体: {attackCollider3D.GetType().Name}, IsTrigger: {attackCollider3D.isTrigger}");
            }
        }
        
        // 初始状态为隐藏
        gameObject.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 初始化完成 (3D模式)");
        }
    }
    
    void OnDestroy()
    {
        // AttackRange不再订阅阶段事件
    }
    
    // ⚠️ 不使用 OnTriggerEnter2D 被动检测
    // 改为在 Attack 阶段主动调用 GetTargetsInRange() 检测
    // 这样可以确保只在 Attack 阶段造成伤害，而不是 Telegraph 阶段
    
    /// <summary>
    /// 显示预告（由Enemy调用）
    /// </summary>
    public void ShowTelegraph()
    {
        Player player = FindAnyObjectByType<Player>();
        if (player != null)
        {
            ShowTelegraph(player.transform.position);
        }
        else
        {
            gameObject.SetActive(true);
            UpdateTelegraphDirection(null);

            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 显示攻击预告（未找到玩家，使用默认方向）");
            }
        }
    }

    /// <summary>
    /// 显示预告并指定目标位置
    /// </summary>
    /// <param name="targetPosition">需要朝向的目标世界坐标</param>
    public void ShowTelegraph(Vector3 targetPosition)
    {
        gameObject.SetActive(true);
        UpdateTelegraphDirection(targetPosition);

        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 显示攻击预告，目标位置: {targetPosition}, 方向: {telegraphedDirection}");
        }
    }
    
    /// <summary>
    /// 隐藏预告（由Enemy调用）
    /// </summary>
    public void HideTelegraph()
    {
        gameObject.SetActive(false);
        
        // ⚠️ 不在 HideTelegraph 清理 CanAttack
        // CanAttack 应该在 Move 阶段开始时清理
        
        if (showDebugInfo)
        {
            Debug.Log($"AttackRange {name}: 隐藏攻击预告");
        }
    }
    
    /// <summary>
    /// 预告阶段：更新并保存攻击方向（3D版本：XZ平面）
    /// </summary>
    void UpdateTelegraphDirection(Vector3? targetPosition)
    {
        Vector3 target;
        if (targetPosition.HasValue)
        {
            target = targetPosition.Value;
        }
        else
        {
            Player fallbackPlayer = FindAnyObjectByType<Player>();
            target = fallbackPlayer != null ? fallbackPlayer.transform.position : transform.position + Vector3.right;
        }

        // 计算XZ平面上的方向（忽略Y轴）
        Vector3 direction3D = target - transform.position;
        direction3D.y = 0f; // 只考虑XZ平面
        direction3D.Normalize();
        
        if (direction3D == Vector3.zero)
        {
            direction3D = Vector3.right;
        }

        // 转换为Vector2（x, z）用于缓存
        telegraphedDirection = new Vector2(direction3D.x, direction3D.z);
        isDirectionSet = true;

        SetAttackDirection(telegraphedDirection);
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
    /// 设置攻击方向（3D版本：XZ平面，绕Y轴旋转）
    /// </summary>
    void SetAttackDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            // 将2D方向转换为3D XZ平面方向
            Vector3 direction3D = new Vector3(direction.x, 0f, direction.y);
            
            // 计算目标旋转（让forward方向指向目标方向）
            Quaternion targetRotation = Quaternion.LookRotation(direction3D, Vector3.up);
            
            // 设置旋转（只绕Y轴旋转）
            transform.rotation = targetRotation;
            
            if (showDebugInfo)
            {
                Debug.Log($"AttackRange {name}: 设置攻击方向 - 2D方向:{direction}, 3D方向:{direction3D}, 旋转:{targetRotation.eulerAngles}");
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
    /// 获取攻击范围内的目标（由Enemy调用，3D版本）
    /// </summary>
    public List<GameObject> GetTargetsInRange()
    {
        List<GameObject> targets = new List<GameObject>();
        
        if (attackCollider3D == null)
        {
            Debug.LogError($"【攻击范围检测】{name}: 3D碰撞体未找到！请确保在Inspector中指定或当前GameObject上有Collider组件。");
            return targets;
        }
        
        try
        {
            // 使用3D Physics检测重叠目标
            Vector3 center = attackCollider3D.bounds.center;
            float radius = attackCollider3D.bounds.extents.magnitude; // 使用bounds的最大半径
            
            // 获取Player层的LayerMask
            int playerLayer = LayerMask.NameToLayer("Player");
            LayerMask playerLayerMask = playerLayer >= 0 ? (1 << playerLayer) : -1; // 如果找不到Player层，检测所有层
            
            // 先用球形检测获取候选目标（快速）
            Collider[] candidateColliders = Physics.OverlapSphere(center, radius, playerLayerMask, QueryTriggerInteraction.Ignore);

            if (showDebugInfo)
            {
                Debug.Log($"【攻击范围检测】{name}: OverlapSphere center={center}, radius={radius:F2}, 候选数={candidateColliders.Length}");
            }
            
            // 对每个候选目标，检查是否与攻击范围碰撞体有实际重叠
            foreach (var candidateCollider in candidateColliders)
            {
                if (candidateCollider != null && candidateCollider.CompareTag("Player"))
                {
                    // 统一使用挂有 PlayerBehavior 的根对象作为伤害目标，避免命中玩家子节点导致无法扣血
                    GameObject targetGO = null;
                    var playerBehavior = candidateCollider.GetComponentInParent<PlayerBehavior>();
                    if (playerBehavior != null)
                    {
                        targetGO = playerBehavior.gameObject;
                    }
                    else
                    {
                        // 回退：直接使用碰撞到的这个 GameObject
                        targetGO = candidateCollider.gameObject;
                    }

                    Vector3 targetPos = targetGO.transform.position;
                    
                    // 原来的逻辑：只要玩家“中心点”在 bounds 内才算命中，太严格
                    bool centerInsideBounds = attackCollider3D.bounds.Contains(targetPos);
                    
                    // 更合理的逻辑：只要两个碰撞体的 bounds 有重叠，就认为在范围内
                    bool boundsOverlap = attackCollider3D.bounds.Intersects(candidateCollider.bounds);

                    if (showDebugInfo)
                    {
                        Debug.Log(
                            $"【攻击范围检测】{name}: 检查 collider={candidateCollider.name}, 目标={targetGO.name}, 位置={targetPos}, " +
                            $"中心在bounds内={centerInsideBounds}, bounds重叠={boundsOverlap}");
                    }

                    if (boundsOverlap)
                    {
                        if (!targets.Contains(targetGO))
                        {
                            targets.Add(targetGO);
                        }
                    }
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"【攻击范围检测】{name}: 最终命中目标数={targets.Count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"【攻击范围检测】{name}: 检测过程中发生异常 - {e.Message}\n{e.StackTrace}");
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
