using UnityEngine;

/// <summary>
/// 生成效果 - 空占位符实现
/// 实际功能由掉落系统（DropTableProvider + DeathDropTrigger）处理
/// 此效果仅用于标记技能状态，不执行具体游戏逻辑
/// </summary>
public class SpawnEffect : IEffect
{
    public string EffectName => "SpawnEffect";
    
    private bool canExecute = true; // 是否允许执行（完全由重置条件控制）
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    public bool CanExecute => canExecute;
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool value)
    {
        canExecute = value;
        Debug.Log($"[{EffectName}] 设置执行权限: {value}");
    }
    
    /// <summary>
    /// ✅ 多角色系统：设置效果的目标角色ID（SpawnEffect 不使用）
    /// </summary>
    /// <param name="characterID">目标角色ID</param>
    public void SetTarget(string characterID)
    {
        // SpawnEffect 是占位符效果，不使用目标角色
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
    public bool ExecuteEffect(SkillArgs args)
    {
        // 检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            Debug.Log($"[{EffectName}] 执行权限被禁止，跳过执行");
            return false;
        }
        
        Debug.Log($"[{EffectName}] 执行效果 - 空占位符，实际功能由掉落系统处理");
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        canExecute = false;
        
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
