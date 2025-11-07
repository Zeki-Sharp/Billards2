using UnityEngine;

/// <summary>
/// 单例 Manager 基类 - 统一的单例实现模式
/// 
/// 【设计目标】：
/// - 消除重复的单例代码
/// - 统一生命周期管理
/// - 提供清晰的初始化/清理接口
/// - 处理场景切换和应用退出
/// 
/// 【使用方式】：
/// public class MyManager : SingletonManager&lt;MyManager&gt;
/// {
///     protected override void OnManagerCreated()
///     {
///         // 初始化逻辑
///     }
/// }
/// 
/// 【特性】：
/// - 自动处理 DontDestroyOnLoad
/// - 自动检测和销毁重复实例
/// - 应用退出时安全处理
/// - 提供虚方法供子类重写
/// </summary>
/// <typeparam name="T">具体的 Manager 类型</typeparam>
public abstract class SingletonManager<T> : MonoBehaviour where T : SingletonManager<T>
{
    #region 单例实现
    
    private static T instance;
    
    /// <summary>
    /// 单例实例
    /// </summary>
    public static T Instance
    {
        get
        {
            // 如果应用正在退出，不再创建新实例
            if (isApplicationQuitting)
            {
                Debug.LogWarning($"[{typeof(T).Name}] 应用正在退出，不创建新实例");
                return null;
            }
            
            // 如果实例不存在，尝试在场景中查找
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();
                
                if (instance != null && instance.EnableDebugLog)
                {
                    Debug.Log($"[{typeof(T).Name}] 在场景中找到现有实例");
                }
            }
            
            return instance;
        }
    }
    
    /// <summary>
    /// 检查实例是否存在
    /// </summary>
    public static bool HasInstance => instance != null && !isApplicationQuitting;
    
    /// <summary>
    /// 应用是否正在退出
    /// </summary>
    private static bool isApplicationQuitting = false;
    
    #endregion
    
    #region 配置选项
    
    /// <summary>
    /// 是否在场景切换时保留（默认：true）
    /// 子类可以重写此属性来改变行为
    /// </summary>
    protected virtual bool PersistAcrossScenes => true;
    
    /// <summary>
    /// 是否启用调试日志（默认：false）
    /// 子类可以重写此属性来改变行为
    /// </summary>
    protected virtual bool EnableDebugLog => false;
    
    #endregion
    
    #region Unity 生命周期
    
    protected virtual void Awake()
    {
        // 检查是否已有实例
        if (instance == null)
        {
            // 设置实例
            instance = this as T;
            
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            // 跨场景保留
            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            if (EnableDebugLog)
            {
                Debug.Log($"[{typeof(T).Name}] 单例创建成功 (跨场景: {PersistAcrossScenes})");
            }
            
            // 调用子类初始化
            OnManagerCreated();
        }
        else if (instance != this)
        {
            // 已有实例存在，销毁当前对象
            if (EnableDebugLog)
            {
                Debug.LogWarning($"[{typeof(T).Name}] 检测到重复实例，销毁当前对象");
            }
            
            Destroy(gameObject);
            return; // ⚠️ 重要：阻止子类 Awake 继续执行
        }
    }
    
    protected virtual void OnDestroy()
    {
        // 只有当前实例是单例实例时才清理
        if (instance == this)
        {
            if (EnableDebugLog && !isApplicationQuitting)
            {
                Debug.Log($"[{typeof(T).Name}] 单例正在销毁");
            }
            
            // 调用子类清理
            OnManagerDestroyed();
            
            // 清空实例引用
            instance = null;
        }
    }
    
    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
        
        if (EnableDebugLog)
        {
            Debug.Log($"[{typeof(T).Name}] 应用退出，标记 isApplicationQuitting");
        }
    }
    
    #endregion
    
    #region 子类重写方法
    
    /// <summary>
    /// Manager 创建时调用
    /// 子类应该在此方法中进行初始化
    /// </summary>
    protected abstract void OnManagerCreated();
    
    /// <summary>
    /// Manager 销毁时调用
    /// 子类应该在此方法中进行清理（取消事件订阅、释放资源等）
    /// </summary>
    protected virtual void OnManagerDestroyed()
    {
        // 默认空实现，子类可选择重写
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 获取或创建实例
    /// 如果场景中没有实例，会创建一个新的 GameObject
    /// </summary>
    /// <returns>Manager 实例</returns>
    public static T GetOrCreateInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }
        
        if (isApplicationQuitting)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 应用正在退出，不创建新实例");
            return null;
        }
        
        // 创建新的 GameObject
        GameObject managerObject = new GameObject($"{typeof(T).Name}");
        T manager = managerObject.AddComponent<T>();
        
        Debug.Log($"[{typeof(T).Name}] 自动创建了新实例");
        
        return manager;
    }
    
    /// <summary>
    /// 强制销毁单例实例
    /// 注意：通常不需要手动调用，仅用于特殊情况
    /// </summary>
    public static void DestroyInstance()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }
    
    #endregion
}

