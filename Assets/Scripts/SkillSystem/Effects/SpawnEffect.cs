using UnityEngine;

/// <summary>
/// 生成效果 - 空占位符实现
/// 实际功能由掉落系统（DropTableProvider + DeathDropTrigger）处理
/// 此效果仅用于标记技能状态，不执行具体游戏逻辑
/// </summary>
public class SpawnEffect : IEffect
{
    public string EffectName => "SpawnEffect";
    
    // 瞬时效果，总是可以执行
    public bool CanExecute => true;
    
    /// <summary>
    /// 设置是否允许执行（空实现，瞬时效果总是可以执行）
    /// </summary>
    public void SetCanExecute(bool canExecute)
    {
        // 瞬时效果不需要控制执行权限
    }
    
    /// <summary>
    /// 初始化效果（空实现）
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{EffectName}] 初始化完成 - 空占位符效果");
    }
    
    /// <summary>
    /// 执行效果（空实现）
    /// 返回true表示"成功"，实际功能由掉落系统处理
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回true</returns>
    public bool ExecuteEffect(object eventData)
    {
        Debug.Log($"[{EffectName}] 执行效果 - 空占位符，实际功能由掉落系统处理");
        return true;
    }
    
    /// <summary>
    /// 重置效果状态（空实现）
    /// </summary>
    public void RemoveEffect()
    {
        Debug.Log($"[{EffectName}] 重置效果 - 空占位符");
    }
}
