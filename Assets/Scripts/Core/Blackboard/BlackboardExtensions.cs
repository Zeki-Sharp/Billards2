using UnityEngine;

/// <summary>
/// Blackboard 扩展方法
/// 为 MonoBehaviour 提供便捷的 Blackboard 访问
/// 
/// 【使用方式】：
/// - this.GetBlackboard() - 获取或创建 Blackboard
/// - this.GetBlackboard().Set("Key", value)
/// - var value = this.GetBlackboard().Get<T>("Key")
/// </summary>
public static class BlackboardExtensions
{
    // 存储每个 GameObject 的 Blackboard 实例
    private static readonly System.Collections.Generic.Dictionary<int, Blackboard> blackboards 
        = new System.Collections.Generic.Dictionary<int, Blackboard>();
    
    /// <summary>
    /// 获取 GameObject 的 Blackboard 实例
    /// 如果不存在则自动创建
    /// </summary>
    public static Blackboard GetBlackboard(this MonoBehaviour mono)
    {
        return GetBlackboard(mono.gameObject);
    }
    
    /// <summary>
    /// 获取 GameObject 的 Blackboard 实例
    /// 如果不存在则自动创建
    /// </summary>
    public static Blackboard GetBlackboard(this GameObject go)
    {
        if (go == null)
        {
            Debug.LogError("[BlackboardExtensions] GameObject 为空");
            return null;
        }
        
        int instanceId = go.GetInstanceID();
        
        if (!blackboards.TryGetValue(instanceId, out Blackboard blackboard))
        {
            blackboard = new Blackboard();
            blackboard.SetOwner(go);  // 设置 Owner 用于调试
            blackboards[instanceId] = blackboard;
        }
        
        return blackboard;
    }
    
    /// <summary>
    /// 尝试获取 GameObject 的 Blackboard 实例
    /// 如果不存在则返回 null（不自动创建）
    /// </summary>
    public static Blackboard TryGetBlackboard(this GameObject go)
    {
        if (go == null) return null;
        
        int instanceId = go.GetInstanceID();
        blackboards.TryGetValue(instanceId, out Blackboard blackboard);
        return blackboard;
    }
    
    /// <summary>
    /// 检查 GameObject 是否有 Blackboard
    /// </summary>
    public static bool HasBlackboard(this GameObject go)
    {
        if (go == null) return false;
        return blackboards.ContainsKey(go.GetInstanceID());
    }
    
    /// <summary>
    /// 移除 GameObject 的 Blackboard
    /// （对象销毁时调用，避免内存泄漏）
    /// </summary>
    public static void RemoveBlackboard(this GameObject go)
    {
        if (go == null) return;
        blackboards.Remove(go.GetInstanceID());
    }
    
    /// <summary>
    /// 清理所有 Blackboard（场景切换时调用）
    /// </summary>
    public static void ClearAllBlackboards()
    {
        blackboards.Clear();
    }
    
    /// <summary>
    /// 获取当前 Blackboard 数量（调试用）
    /// </summary>
    public static int GetBlackboardCount()
    {
        return blackboards.Count;
    }
}

