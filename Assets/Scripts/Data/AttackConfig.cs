using UnityEngine;

/// <summary>
/// 远程攻击配置
/// </summary>
[System.Serializable]
public class RangedAttackConfig
{
    [Header("远程攻击配置")]
    [Tooltip("检测玩家的范围")]
    public float detectionRange = 8f;
    
    [Tooltip("投射到玩家附近的距离")]
    public float projectionDistance = 2f;
    
    [Tooltip("攻击冷却时间")]
    public float cooldown = 2f;
    
    [Header("随机偏移设置")]
    [Tooltip("是否使用随机偏移")]
    public bool useRandomOffset = true;
    
    [Tooltip("随机偏移范围")]
    public float randomOffsetRange = 1f;
    
    [Header("抛物线指示器")]
    [Tooltip("是否显示抛物线指示器（需要在AttackRange上添加ParabolicIndicator组件）")]
    public bool showParabolicIndicator = true;
}

/// <summary>
/// 棘刺攻击配置
/// </summary>
[System.Serializable]
public class ThornAttackConfig
{
    [Header("回合设置")]
    [Tooltip("棘刺激活持续回合数（从预告到下次预告算1回合）")]
    public int activeRounds = 1;
    
    [Tooltip("棘刺冷却回合数（0表示每回合都攻击）")]
    public int cooldownRounds = 0;
    
    [Header("伤害设置")]
    [Tooltip("棘刺伤害间隔（秒）")]
    public float damageInterval = 0.5f;
    
    [Header("视觉效果")]
    [Tooltip("是否显示冷却状态")]
    public bool showCooldownState = true;
    
    [Tooltip("激活状态颜色")]
    public Color activeColor = new Color(1f, 0.2f, 0.2f, 0.8f);  // 红色
    
    [Tooltip("冷却状态颜色")]
    public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // 灰色
}
