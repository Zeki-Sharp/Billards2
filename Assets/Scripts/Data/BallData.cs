using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BallData", menuName = "Physics/Ball Data")]
public class BallData : ScriptableObject
{
    // 一、基础物理
    [BoxGroup("基础物理")]
    [LabelText("质量")]
    [MinValue(0.1f)]
    public float mass = 1f;
    
    [BoxGroup("基础物理")]
    [LabelText("最大速度限制")]
    [MinValue(1f)]
    [Tooltip("最大速度限制")]
    public float maxSpeed = 50f;
    
    [BoxGroup("基础物理")]
    [LabelText("停止阈值")]
    [MinValue(0.01f)]
    [Tooltip("速度低于此值时强制判定为停止")]
    public float stopThreshold = 0.5f;
    
    // 二、碰撞材质
    [BoxGroup("碰撞材质")]
    [LabelText("反弹阻尼系数")]
    [Range(0f, 1f)]
    [Tooltip("反弹阻尼系数 (0-1)")]
    public float bounceDamping = 0.8f;
    
    [BoxGroup("碰撞材质")]
    [LabelText("摩擦系数")]
    [MinValue(0f)]
    [Tooltip("摩擦系数")]
    public float friction = 0.1f;
    
    // 三、运动阻尼（已废弃，保留用于兼容）
    [HideInInspector]
    public float linearDamping = 0.1f;
    
    // 四、动态调控（速度曲线）（已废弃，保留用于兼容）
    [HideInInspector]
    public AnimationCurve speedToBounciness = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
    
    [HideInInspector]
    public AnimationCurve speedToDamping = AnimationCurve.Linear(0f, 0.8f, 1f, 0.1f);
    
    // 五、动态参数范围（已废弃，保留用于兼容）
    [HideInInspector]
    public float minBounciness = 0.3f;
    
    [HideInInspector]
    public float maxBounciness = 1.0f;
    
    [HideInInspector]
    public float minDamping = 0.1f;
    
    [HideInInspector]
    public float maxDamping = 0.8f;
    
    // 六、时间阻尼系统（已废弃，保留用于兼容）
    [HideInInspector]
    public bool enableTimeDamping = true;
    
    [HideInInspector]
    public float timeDampingStartTime = 2.0f;
    
    [HideInInspector]
    public float timeDampingRate = 0.2f;
    
    [HideInInspector]
    public float maxTimeDamping = 1.5f;
    
    // 七、受击补偿（已废弃，保留用于兼容）
    [HideInInspector]
    public float hitBoostForce = 1f;
    
    [HideInInspector]
    public float hitBoostMultiplier = 1f;
    
    [HideInInspector]
    public float boostSpeedThreshold = 20f;
    
    // 九、几何物理参数（新物理系统）
    [BoxGroup("几何物理")]
    [BoxGroup("几何物理/基础")]
    [LabelText("停止阈值")]
    [MinValue(0f)]
    [Tooltip("几何模拟速度低于该值时视为停止")]
    public float geometryMinSpeedThreshold = 0.2f;
    
    [BoxGroup("几何物理/分段阻尼")]
    [LabelText("高速阶段持续时间（秒）")]
    [MinValue(0f)]
    [Tooltip("在此时间内速度衰减很小，保持较高速度")]
    public float geometryHighSpeedPhaseDuration = 0.5f;
    
    [BoxGroup("几何物理/分段阻尼")]
    [LabelText("高速阶段线性阻尼")]
    [MinValue(0f)]
    [Tooltip("高速阶段的线性衰减系数（建议较小）")]
    public float geometryHighPhaseDamping = 0.2f;
    
    [BoxGroup("几何物理/分段阻尼")]
    [LabelText("低速阶段线性阻尼")]
    [MinValue(0f)]
    [Tooltip("减速阶段的线性衰减系数（建议明显大于高速阶段）")]
    public float geometryLowPhaseDamping = 40f;
    
    [BoxGroup("几何物理/碰撞")]
    [LabelText("墙体速度保留比例")]
    [Range(0f, 1f)]
    [Tooltip("墙体碰撞后速度保留比例")]
    public float geometryWallBounceFactor = 0.95f;
    
    [BoxGroup("几何物理/碰撞")]
    [LabelText("球体速度保留比例")]
    [Range(0f, 1f)]
    [Tooltip("球↔球碰撞后速度保留比例（全局弹性）")]
    public float geometryBallBounceFactor = 0.98f;
    
    [BoxGroup("几何物理/碰撞")]
    [LabelText("Knockback 缩放")]
    [Tooltip("自身被击中时速度缩放（1=标准质量，<1=更重，>1=更轻）")]
    public float geometryKnockbackScale = 1f;
    
    // 八、性能与刷新
    [BoxGroup("性能与刷新")]
    [LabelText("参数变化阈值")]
    [MinValue(0.01f)]
    [Tooltip("参数变化阈值，避免频繁更新")]
    public float updateThreshold = 0.1f;
    
    [BoxGroup("性能与刷新")]
    [LabelText("更新间隔")]
    [MinValue(0.001f)]
    [Tooltip("更新间隔（秒）")]
    public float updateInterval = 0.02f;
}
