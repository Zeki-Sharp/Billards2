using UnityEngine;

[CreateAssetMenu(fileName = "BallData", menuName = "Physics/Ball Data")]
public class BallPhysicsData : ScriptableObject
{
    // 一、基础物理
    [Header("基础物理")]
    public float mass = 1f;
    public float maxSpeed = 10f; // 最大速度限制
    public float stopThreshold = 0.5f; // 停止阈值
    
    // 二、碰撞材质
    [Header("碰撞材质")]
    public float bounceDamping = 0.8f; // 反弹阻尼系数 (0-1)
    public float friction = 0.1f; // 摩擦系数
    
    // 三、运动阻尼
    [Header("运动阻尼")]
    public float linearDamping = 0.5f; // 线性阻尼，让球逐渐减速
    
    // 四、动态调控（速度曲线）
    [Header("动态调控（速度曲线）")]
    [Tooltip("速度到弹性的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToBounciness = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
    [Tooltip("速度到阻尼的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToDamping = AnimationCurve.Linear(0f, 0.8f, 1f, 0.1f);
    
    // 五、动态参数范围（夹紧）
    [Header("动态参数范围")]
    public float minBounciness = 0.3f; // 最小弹性
    public float maxBounciness = 1.0f; // 最大弹性
    public float minDamping = 0.1f; // 最小阻尼
    public float maxDamping = 0.8f; // 最大阻尼
    
    // 六、时间阻尼系统
    [Header("时间阻尼系统")]
    [Tooltip("是否启用时间阻尼")]
    public bool enableTimeDamping = true;
    [Tooltip("时间阻尼开始时间（秒）")]
    public float timeDampingStartTime = 2.0f;
    [Tooltip("时间阻尼增长速率")]
    public float timeDampingRate = 0.2f;
    [Tooltip("最大时间阻尼值")]
    public float maxTimeDamping = 1.5f;
    
    // 七、受击补偿（撞墙充能）
    [Header("受击补偿（撞墙充能）")]
    [Tooltip("受击补偿力大小")]
    public float hitBoostForce = 1f;
    [Tooltip("受击补偿力倍数")]
    public float hitBoostMultiplier = 1f;
    [Tooltip("获得补偿力的最小速度阈值")]
    public float boostSpeedThreshold = 20f;
    
    // 八、性能与刷新
    [Header("性能与刷新")]
    public float updateThreshold = 0.1f; // 参数变化阈值，避免频繁更新
    public float updateInterval = 0.02f; // 更新间隔（秒）
}
