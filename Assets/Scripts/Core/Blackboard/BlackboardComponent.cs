using UnityEngine;

/// <summary>
/// Blackboard 组件 - 可选的 MonoBehaviour 包装
/// 
/// 【用途】：
/// - 提供 Inspector 可见的 Blackboard 调试信息
/// - 自动清理 Blackboard（OnDestroy 时）
/// - 可选组件：不是必须的，扩展方法已提供功能
/// 
/// 【使用场景】：
/// - 需要在 Inspector 中查看 Blackboard 数据
/// - 需要自动管理 Blackboard 生命周期
/// </summary>
public class BlackboardComponent : MonoBehaviour
{
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool logOnUpdate = false;
    
    private Blackboard blackboard;
    
    void Awake()
    {
        // 获取或创建 Blackboard
        blackboard = this.GetBlackboard();
        
        if (showDebugInfo)
        {
            Debug.Log($"[BlackboardComponent] {gameObject.name} Blackboard 初始化");
        }
    }
    
    void Update()
    {
        if (logOnUpdate && blackboard != null && blackboard.Count > 0)
        {
            Debug.Log(blackboard.GetDebugInfo());
        }
    }
    
    void OnDestroy()
    {
        // 清理 Blackboard，避免内存泄漏
        if (blackboard != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[BlackboardComponent] {gameObject.name} Blackboard 清理，数据项: {blackboard.Count}");
            }
            
            gameObject.RemoveBlackboard();
        }
    }
    
    /// <summary>
    /// 获取 Blackboard 实例
    /// </summary>
    public Blackboard GetBlackboard()
    {
        if (blackboard == null)
        {
            blackboard = this.GetBlackboard();
        }
        return blackboard;
    }
    
    #region Inspector 调试
    
    /// <summary>
    /// 在 Inspector 中显示 Blackboard 数据（仅编辑器）
    /// </summary>
    [ContextMenu("显示 Blackboard 数据")]
    private void ShowBlackboardData()
    {
        if (blackboard != null)
        {
            Debug.Log(blackboard.GetDebugInfo());
        }
        else
        {
            Debug.Log($"[BlackboardComponent] {gameObject.name} Blackboard 未初始化");
        }
    }
    
    /// <summary>
    /// 清空 Blackboard 数据（仅编辑器）
    /// </summary>
    [ContextMenu("清空 Blackboard 数据")]
    private void ClearBlackboardData()
    {
        if (blackboard != null)
        {
            int count = blackboard.Count;
            blackboard.Clear();
            Debug.Log($"[BlackboardComponent] {gameObject.name} Blackboard 已清空，移除了 {count} 个数据项");
        }
    }
    
    #endregion
}

