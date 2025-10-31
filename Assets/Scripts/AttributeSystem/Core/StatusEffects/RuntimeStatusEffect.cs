using UnityEngine;

/// <summary>
/// 运行时状态效果 - 单个状态效果的运行时实例
/// 
/// 【设计理念】：
/// - 引用配置（StatusEffectData）
/// - 管理运行时状态（剩余时间、堆叠层数）
/// - 支持生命周期回调
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 RuntimeStatusEffect
/// - OnStart/OnEnd/WhileActive 回调
/// </summary>
public class RuntimeStatusEffect
{
    #region 核心数据
    
    /// <summary>
    /// 状态效果配置
    /// </summary>
    public StatusEffectData Data { get; private set; }
    
    /// <summary>
    /// 当前堆叠层数
    /// </summary>
    public int StackCount { get; private set; }
    
    /// <summary>
    /// 剩余时间
    /// </summary>
    public float TimeRemaining { get; private set; }
    
    /// <summary>
    /// 效果来源
    /// </summary>
    public object Source { get; private set; }
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时状态效果
    /// </summary>
    public RuntimeStatusEffect(StatusEffectData data, object source = null)
    {
        this.Data = data;
        this.Source = source;
        this.StackCount = 1;
        this.TimeRemaining = data.duration;
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 效果ID
    /// </summary>
    public string EffectID => Data.effectID;
    
    /// <summary>
    /// 是否是永久效果
    /// </summary>
    public bool IsPermanent => Data.duration <= 0f;
    
    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => !IsPermanent && TimeRemaining <= 0f;
    
    /// <summary>
    /// 是否可以堆叠
    /// </summary>
    public bool CanStack => Data.canStack;
    
    /// <summary>
    /// 是否已达到最大堆叠
    /// </summary>
    public bool IsMaxStacked => StackCount >= Data.maxStacks;
    
    #endregion
    
    #region 堆叠管理
    
    /// <summary>
    /// 增加堆叠层数
    /// </summary>
    /// <returns>是否成功增加</returns>
    public bool AddStack()
    {
        if (!CanStack)
        {
            Debug.LogWarning($"[RuntimeStatusEffect] {EffectID} 不支持堆叠");
            return false;
        }
        
        if (IsMaxStacked)
        {
            Debug.LogWarning($"[RuntimeStatusEffect] {EffectID} 已达到最大堆叠层数 {Data.maxStacks}");
            return false;
        }
        
        StackCount++;
        
        // 刷新持续时间
        TimeRemaining = Data.duration;
        
        return true;
    }
    
    /// <summary>
    /// 刷新持续时间
    /// </summary>
    public void RefreshDuration()
    {
        TimeRemaining = Data.duration;
    }
    
    #endregion
    
    #region 生命周期更新
    
    /// <summary>
    /// 更新剩余时间
    /// </summary>
    public void UpdateTime(float deltaTime)
    {
        if (!IsPermanent)
        {
            TimeRemaining -= deltaTime;
            TimeRemaining = Mathf.Max(0f, TimeRemaining);
        }
    }
    
    #endregion
    
    #region 回调接口（供子类实现）
    
    /// <summary>
    /// 效果开始时调用
    /// </summary>
    public virtual void OnStart()
    {
        // 子类可重写实现具体逻辑
    }
    
    /// <summary>
    /// 效果结束时调用
    /// </summary>
    public virtual void OnEnd()
    {
        // 子类可重写实现具体逻辑
    }
    
    /// <summary>
    /// 效果激活期间每帧调用
    /// </summary>
    public virtual void WhileActive(float deltaTime)
    {
        // 子类可重写实现具体逻辑（如持续伤害）
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"[{EffectID}] {Data.displayName}";
        
        if (!IsPermanent)
        {
            info += $" (剩余 {TimeRemaining:F1}s/{Data.duration:F1}s)";
        }
        
        if (CanStack && StackCount > 1)
        {
            info += $" [x{StackCount}]";
        }
        
        return info;
    }
    
    #endregion
}

