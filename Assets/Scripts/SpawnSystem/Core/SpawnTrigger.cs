using UnityEngine;

/// <summary>
/// 生成触发器抽象基类 - 决策层核心基类
/// 定义触发器的统一接口和通用逻辑
/// 负责监听游戏事件、查询配置、决定生成
/// </summary>
/// <typeparam name="T">生成数据类型</typeparam>
public abstract class SpawnTrigger<T> : MonoBehaviour
{
    [Header("配置和生成器引用")]
    [SerializeField] protected SpawnConfigProvider<T> configProvider;
    [SerializeField] protected BaseSpawner<T> spawner;
    
    [Header("触发器状态")]
    [SerializeField] protected bool isActive = true;
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    protected virtual void Start()
    {
        Initialize();
        SubscribeEvents();
    }
    
    /// <summary>
    /// 销毁时清理
    /// </summary>
    protected virtual void OnDestroy()
    {
        UnsubscribeEvents();
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    protected virtual void Initialize()
    {
        if (configProvider == null)
        {
            Debug.LogError($"[{GetType().Name}] configProvider 未设置！");
        }
        
        if (spawner == null)
        {
            Debug.LogError($"[{GetType().Name}] spawner 未设置！");
        }
        
        // 初始化配置提供者
        configProvider?.Initialize();
        
        Debug.Log($"[{GetType().Name}] 初始化完成");
    }
    
    /// <summary>
    /// 订阅游戏事件（抽象方法，子类实现）
    /// </summary>
    protected abstract void SubscribeEvents();
    
    /// <summary>
    /// 取消事件订阅（抽象方法，子类实现）
    /// </summary>
    protected abstract void UnsubscribeEvents();
    
    /// <summary>
    /// 请求生成单个对象
    /// </summary>
    /// <param name="data">生成数据</param>
    /// <param name="position">生成位置</param>
    protected void RequestSpawn(T data, Vector3 position)
    {
        if (!isActive || spawner == null) return;
        
        spawner.Spawn(data, position);
    }
    
    /// <summary>
    /// 请求批量生成对象
    /// </summary>
    /// <param name="dataList">生成数据列表</param>
    /// <param name="positions">生成位置列表</param>
    protected void RequestSpawnBatch(System.Collections.Generic.List<T> dataList, System.Collections.Generic.List<Vector3> positions)
    {
        if (!isActive || spawner == null) return;
        
        spawner.SpawnBatch(dataList, positions);
    }
    
    /// <summary>
    /// 设置触发器激活状态
    /// </summary>
    /// <param name="active">是否激活</param>
    public void SetActive(bool active)
    {
        isActive = active;
        Debug.Log($"[{GetType().Name}] 激活状态设置为: {active}");
    }
    
    /// <summary>
    /// 获取触发器激活状态
    /// </summary>
    /// <returns>是否激活</returns>
    public bool IsActive()
    {
        return isActive;
    }
}
