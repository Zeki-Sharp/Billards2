using UnityEngine;

[CreateAssetMenu(fileName = "BallData", menuName = "Physics/Ball Data")]
public class BallData : ScriptableObject
{
    [Header("基础物理属性")]
    public float mass = 1f;
    public float radius = 0.5f;
    
    [Header("碰撞属性")]
    public float bounceDamping = 0.8f; // 反弹阻尼系数 (0-1)
    public float friction = 0.1f; // 摩擦系数
    public float stopThreshold = 0.5f; // 停止阈值，速度低于此值时自动停止
    
    [Header("运动属性")]
    public float linearDamping = 0.1f; // 线性阻尼，让球逐渐减速
    
    [Header("特殊规则")]
    public bool isWhiteBall = false; // 是否为白球
    public float maxSpeed = 50f; // 最大速度限制
    
    [Header("受击补偿力设置")]
    [Tooltip("受击补偿力大小")]
    public float hitBoostForce = 1f;
    [Tooltip("受击补偿力倍数")]
    public float hitBoostMultiplier = 1f;
    [Tooltip("获得补偿力的最小速度阈值")]
    public float boostSpeedThreshold = 20f;
    
    [Header("动态物理参数")]
    [Tooltip("速度到弹性的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToBounciness = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
    [Tooltip("速度到阻尼的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToDamping = AnimationCurve.Linear(0f, 0.8f, 1f, 0.1f);
    
    [Header("动态参数范围")]
    public float minBounciness = 0.3f; // 最小弹性
    public float maxBounciness = 1.0f; // 最大弹性
    public float minDamping = 0.1f; // 最小阻尼
    public float maxDamping = 0.8f; // 最大阻尼
    
    
    [Header("时间阻尼系统")]
    [Tooltip("是否启用时间阻尼")]
    public bool enableTimeDamping = true;
    
    [Header("时间阻尼参数")]
    [Tooltip("时间阻尼增长速率")]
    public float timeDampingRate = 0.2f;
    [Tooltip("最大时间阻尼值")]
    public float maxTimeDamping = 1.5f;
    [Tooltip("时间阻尼开始时间（秒）")]
    public float timeDampingStartTime = 2.0f;
    
    
    [Header("性能优化")]
    public float updateThreshold = 0.1f; // 参数变化阈值，避免频繁更新
    public float updateInterval = 0.02f; // 更新间隔（秒）
}
