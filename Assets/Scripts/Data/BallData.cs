using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BallData", menuName = "Physics/Ball Data")]
public class BallData : ScriptableObject
{
    [BoxGroup("基础物理属性")]
    [LabelText("质量")]
    [MinValue(0.1f)]
    public float mass = 1f;
    
    [BoxGroup("基础物理属性")]
    [LabelText("半径")]
    [MinValue(0.1f)]
    public float radius = 0.5f;
    
    [BoxGroup("碰撞属性")]
    [LabelText("反弹阻尼系数")]
    [Range(0f, 1f)]
    [Tooltip("反弹阻尼系数 (0-1)")]
    public float bounceDamping = 0.8f;
    
    [BoxGroup("碰撞属性")]
    [LabelText("摩擦系数")]
    [MinValue(0f)]
    [Tooltip("摩擦系数")]
    public float friction = 0.1f;
    
    [BoxGroup("碰撞属性")]
    [LabelText("停止阈值")]
    [MinValue(0.01f)]
    [Tooltip("停止阈值，速度低于此值时自动停止")]
    public float stopThreshold = 0.5f;
    
    [BoxGroup("运动属性")]
    [LabelText("线性阻尼")]
    [MinValue(0f)]
    [Tooltip("线性阻尼，让球逐渐减速")]
    public float linearDamping = 0.1f;
    
    [BoxGroup("特殊规则")]
    [LabelText("是否为白球")]
    [Tooltip("是否为白球")]
    public bool isWhiteBall = false;
    
    [BoxGroup("特殊规则")]
    [LabelText("最大速度限制")]
    [MinValue(1f)]
    [Tooltip("最大速度限制")]
    public float maxSpeed = 50f;
    
    [BoxGroup("受击补偿力设置")]
    [LabelText("受击补偿力大小")]
    [MinValue(0f)]
    [Tooltip("受击补偿力大小")]
    public float hitBoostForce = 1f;
    
    [BoxGroup("受击补偿力设置")]
    [LabelText("受击补偿力倍数")]
    [MinValue(0f)]
    [Tooltip("受击补偿力倍数")]
    public float hitBoostMultiplier = 1f;
    
    [BoxGroup("受击补偿力设置")]
    [LabelText("补偿力速度阈值")]
    [MinValue(0f)]
    [Tooltip("获得补偿力的最小速度阈值")]
    public float boostSpeedThreshold = 20f;
    
    [BoxGroup("动态物理参数")]
    [LabelText("速度到弹性曲线")]
    [Tooltip("速度到弹性的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToBounciness = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
    
    [BoxGroup("动态物理参数")]
    [LabelText("速度到阻尼曲线")]
    [Tooltip("速度到阻尼的曲线 (0=静止, 1=最大速度)")]
    public AnimationCurve speedToDamping = AnimationCurve.Linear(0f, 0.8f, 1f, 0.1f);
    
    [BoxGroup("动态参数范围")]
    [LabelText("最小弹性")]
    [Range(0f, 1f)]
    [Tooltip("最小弹性")]
    public float minBounciness = 0.3f;
    
    [BoxGroup("动态参数范围")]
    [LabelText("最大弹性")]
    [Range(0f, 1f)]
    [Tooltip("最大弹性")]
    public float maxBounciness = 1.0f;
    
    [BoxGroup("动态参数范围")]
    [LabelText("最小阻尼")]
    [Range(0f, 1f)]
    [Tooltip("最小阻尼")]
    public float minDamping = 0.1f;
    
    [BoxGroup("动态参数范围")]
    [LabelText("最大阻尼")]
    [Range(0f, 1f)]
    [Tooltip("最大阻尼")]
    public float maxDamping = 0.8f;
    
    [BoxGroup("时间阻尼系统")]
    [LabelText("启用时间阻尼")]
    [Tooltip("是否启用时间阻尼")]
    public bool enableTimeDamping = true;
    
    [BoxGroup("时间阻尼参数")]
    [LabelText("时间阻尼增长速率")]
    [MinValue(0f)]
    [Tooltip("时间阻尼增长速率")]
    public float timeDampingRate = 0.2f;
    
    [BoxGroup("时间阻尼参数")]
    [LabelText("最大时间阻尼值")]
    [MinValue(0f)]
    [Tooltip("最大时间阻尼值")]
    public float maxTimeDamping = 1.5f;
    
    [BoxGroup("时间阻尼参数")]
    [LabelText("时间阻尼开始时间")]
    [MinValue(0f)]
    [Tooltip("时间阻尼开始时间（秒）")]
    public float timeDampingStartTime = 2.0f;
    
    [BoxGroup("性能优化")]
    [LabelText("参数变化阈值")]
    [MinValue(0.01f)]
    [Tooltip("参数变化阈值，避免频繁更新")]
    public float updateThreshold = 0.1f;
    
    [BoxGroup("性能优化")]
    [LabelText("更新间隔")]
    [MinValue(0.001f)]
    [Tooltip("更新间隔（秒）")]
    public float updateInterval = 0.02f;
}
