using UnityEngine;

/// <summary>
/// 条件类型（简化版）
/// </summary>
public enum ConditionType
{
    Distance,    // 距离条件
    State,       // 状态条件（Blackboard）
    Always       // 永远为真（默认行为）
}

/// <summary>
/// 比较运算符
/// </summary>
public enum ComparisonOperator
{
    LessThan,       // <
    GreaterThan,    // >
    Equal,          // ==
    NotEqual        // !=
}

/// <summary>
/// 行为条件配置类
/// 用于配置化行为条件判断逻辑
/// </summary>
[System.Serializable]
public class BehaviorConditionConfig
{
    [Header("条件类型")]
    [Tooltip("条件类型")]
    public ConditionType type = ConditionType.Distance;
    
    [Header("比较配置")]
    [Tooltip("比较运算符（<, >, ==, !=）")]
    [Sirenix.OdinInspector.ShowIf("@type == ConditionType.Distance || type == ConditionType.State")]
    public ComparisonOperator op = ComparisonOperator.LessThan;
    
    [Tooltip("比较值")]
    [Sirenix.OdinInspector.ShowIf("@type == ConditionType.Distance || type == ConditionType.State")]
    public float value = 2f;
    
    [Header("状态条件配置")]
    [Tooltip("Blackboard 键（用于 State 类型）")]
    [Sirenix.OdinInspector.ShowIf(nameof(type), ConditionType.State)]
    public string blackboardKey = "";
    
    /// <summary>
    /// 评估条件是否满足
    /// </summary>
    public bool Evaluate(Transform enemyTransform, Transform playerTransform, EnemyRuntimeState runtimeState)
    {
        switch (type)
        {
            case ConditionType.Distance:
                return EvaluateDistance(enemyTransform, playerTransform);
                
            case ConditionType.State:
                return EvaluateState(enemyTransform);
                
            case ConditionType.Always:
                return true;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 评估距离条件
    /// </summary>
    private bool EvaluateDistance(Transform enemyTransform, Transform playerTransform)
    {
        float distance = Vector2.Distance(enemyTransform.position, playerTransform.position);
        return CompareValue(distance, value, op);
    }
    
    /// <summary>
    /// 评估状态条件
    /// </summary>
    private bool EvaluateState(Transform enemyTransform)
    {
        var blackboard = enemyTransform.gameObject.TryGetBlackboard();
        if (blackboard == null)
        {
            return false;
        }
        
        if (!blackboard.TryGet<float>(blackboardKey, out var stateValue))
        {
            return false;
        }
        
        return CompareValue(stateValue, value, op);
    }
    
    /// <summary>
    /// 比较两个值
    /// </summary>
    private bool CompareValue(float a, float b, ComparisonOperator operation)
    {
        switch (operation)
        {
            case ComparisonOperator.LessThan:
                return a < b;
                
            case ComparisonOperator.GreaterThan:
                return a > b;
                
            case ComparisonOperator.Equal:
                return Mathf.Approximately(a, b);
                
            case ComparisonOperator.NotEqual:
                return !Mathf.Approximately(a, b);
                
            default:
                return false;
        }
    }
}

