using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 技能参数容器 - 统一的参数传递系统
/// 
/// 【设计目标】：
/// - 替代 object 类型的参数传递
/// - 提供类型安全的访问接口
/// - 内置组件缓存机制，避免重复 GetComponent
/// - 为后续系统（三层属性）打下基础
/// 
/// 【核心优势】：
/// - ✅ 类型安全（无需手动类型转换）
/// - ✅ 性能提升（组件缓存）
/// - ✅ 代码可读性增强
/// - ✅ 更好的 IDE 支持
/// </summary>
public class SkillArgs
{
    #region 核心字段
    
    /// <summary>
    /// 事件发起者（例如：发动技能的玩家）
    /// </summary>
    public GameObject Source { get; private set; }
    
    /// <summary>
    /// 事件目标（例如：被攻击的敌人）
    /// </summary>
    public GameObject Target { get; private set; }
    
    /// <summary>
    /// 事件数据（例如：AttackData, DeathData 等）
    /// </summary>
    public object EventData { get; private set; }
    
    #endregion
    
    #region 组件缓存
    
    /// <summary>
    /// Source 的组件缓存
    /// </summary>
    private Dictionary<Type, Component> sourceComponentCache;
    
    /// <summary>
    /// Target 的组件缓存
    /// </summary>
    private Dictionary<Type, Component> targetComponentCache;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建 SkillArgs（最简版本，只有事件数据）
    /// </summary>
    public SkillArgs(object eventData)
    {
        this.Source = null;
        this.Target = null;
        this.EventData = eventData;
        
        InitializeCaches();
    }
    
    /// <summary>
    /// 创建 SkillArgs（包含 Source 和事件数据）
    /// </summary>
    public SkillArgs(GameObject source, object eventData)
    {
        this.Source = source;
        this.Target = null;
        this.EventData = eventData;
        
        InitializeCaches();
    }
    
    /// <summary>
    /// 创建 SkillArgs（完整版本）
    /// </summary>
    public SkillArgs(GameObject source, GameObject target, object eventData)
    {
        this.Source = source;
        this.Target = target;
        this.EventData = eventData;
        
        InitializeCaches();
    }
    
    /// <summary>
    /// 初始化缓存字典
    /// </summary>
    private void InitializeCaches()
    {
        sourceComponentCache = new Dictionary<Type, Component>();
        targetComponentCache = new Dictionary<Type, Component>();
    }
    
    #endregion
    
    #region 类型安全的数据访问
    
    /// <summary>
    /// 获取类型安全的事件数据
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    /// <returns>转换后的事件数据，失败返回 default(T)</returns>
    public T GetEventData<T>()
    {
        if (EventData is T data)
        {
            return data;
        }
        
        Debug.LogWarning($"[SkillArgs] 无法将事件数据转换为类型 {typeof(T).Name}，实际类型: {EventData?.GetType().Name ?? "null"}");
        return default(T);
    }
    
    /// <summary>
    /// 尝试获取事件数据
    /// </summary>
    /// <typeparam name="T">事件数据类型</typeparam>
    /// <param name="data">输出参数</param>
    /// <returns>是否成功获取</returns>
    public bool TryGetEventData<T>(out T data)
    {
        if (EventData is T convertedData)
        {
            data = convertedData;
            return true;
        }
        
        data = default(T);
        return false;
    }
    
    #endregion
    
    #region 组件访问（带缓存）
    
    /// <summary>
    /// 从 Source 获取组件（带缓存）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>组件实例，未找到返回 null</returns>
    public T GetSourceComponent<T>() where T : Component
    {
        if (Source == null)
        {
            Debug.LogWarning($"[SkillArgs] Source 为空，无法获取组件 {typeof(T).Name}");
            return null;
        }
        
        return GetComponentWithCache<T>(Source, sourceComponentCache);
    }
    
    /// <summary>
    /// 从 Target 获取组件（带缓存）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>组件实例，未找到返回 null</returns>
    public T GetTargetComponent<T>() where T : Component
    {
        if (Target == null)
        {
            Debug.LogWarning($"[SkillArgs] Target 为空，无法获取组件 {typeof(T).Name}");
            return null;
        }
        
        return GetComponentWithCache<T>(Target, targetComponentCache);
    }
    
    /// <summary>
    /// 通用的组件获取方法（带缓存）
    /// </summary>
    private T GetComponentWithCache<T>(GameObject gameObject, Dictionary<Type, Component> cache) where T : Component
    {
        Type componentType = typeof(T);
        
        // 检查缓存
        if (cache.TryGetValue(componentType, out Component cachedComponent))
        {
            return cachedComponent as T;
        }
        
        // 未缓存，从 GameObject 获取
        T component = gameObject.GetComponent<T>();
        
        // 缓存结果（即使是 null 也缓存，避免重复查找）
        cache[componentType] = component;
        
        return component;
    }
    
    #endregion
    
    #region 便捷方法
    
    /// <summary>
    /// 检查 Source 是否存在
    /// </summary>
    public bool HasSource => Source != null;
    
    /// <summary>
    /// 检查 Target 是否存在
    /// </summary>
    public bool HasTarget => Target != null;
    
    /// <summary>
    /// 检查 EventData 是否存在
    /// </summary>
    public bool HasEventData => EventData != null;
    
    /// <summary>
    /// 清空缓存（通常不需要手动调用）
    /// </summary>
    public void ClearCache()
    {
        sourceComponentCache.Clear();
        targetComponentCache.Clear();
    }
    
    #endregion
    
    #region 调试信息
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"SkillArgs:\n" +
               $"  Source: {(Source != null ? Source.name : "null")}\n" +
               $"  Target: {(Target != null ? Target.name : "null")}\n" +
               $"  EventData: {(EventData != null ? EventData.GetType().Name : "null")}\n" +
               $"  Source Cache Count: {sourceComponentCache.Count}\n" +
               $"  Target Cache Count: {targetComponentCache.Count}";
    }
    
    #endregion
    
    #region 静态工厂方法（便捷创建）
    
    /// <summary>
    /// 从 AttackData 创建 SkillArgs
    /// </summary>
    public static SkillArgs FromAttackData(AttackData attackData)
    {
        return new SkillArgs(
            source: attackData.Attacker,
            target: attackData.Target,
            eventData: attackData
        );
    }
    
    /// <summary>
    /// 从 DeathData 创建 SkillArgs
    /// </summary>
    public static SkillArgs FromDeathData(DeathData deathData)
    {
        return new SkillArgs(
            source: null, // DeathData 不包含 Killer 信息
            target: deathData.DeadObject,
            eventData: deathData
        );
    }
    
    /// <summary>
    /// 从任意事件数据创建 SkillArgs（简化版本）
    /// </summary>
    public static SkillArgs FromEventData(object eventData)
    {
        return new SkillArgs(eventData);
    }
    
    #endregion
}

