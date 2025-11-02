using UnityEngine;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using DeepSpaceLabs.SAM;

/// <summary>
/// 墙壁管理器 - 统一管理所有子墙壁的撞墙特效
/// 挂载在墙壁父级对象上，管理所有子墙壁的碰撞检测和特效播放
/// </summary>
public class WallManager : MonoBehaviour
{
    [Header("撞墙特效设置")]
    public float wallHitEffectCooldown = 0.5f; // 撞墙特效冷却时间（秒）
    public float minWallHitSpeed = 1.0f; // 最小撞墙速度阈值
    
    [Header("特效配置")]
    public List<EffectConfig> effects = new List<EffectConfig>();
    
    // 注意：墙面撞击的旋转和位置摇晃 Controller 会自动在子物体中查找，不需要手动配置引用
    
    [Header("调试设置")]
    public bool enableDebugLog = true; // 是否启用调试日志
    
    // 子墙壁列表
    private List<Transform> wallSegments = new List<Transform>();
    
    // 防抖字典：存储每个球体的最后撞墙时间
    private Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();
    
    void Start()
    {
        // 初始化墙壁管理器
        InitializeWallManager();
    }
    
    void OnEnable()
    {
        // 注册特效
        RegisterEffects();
    }
    
    void OnDisable()
    {
        // 注销特效
        UnregisterEffects();
    }
    
    /// <summary>
    /// 初始化墙壁管理器
    /// </summary>
    void InitializeWallManager()
    {
        // 查找所有子墙壁
        FindWallSegments();
        
        // 为每个子墙壁添加碰撞检测
        SetupWallCollisionDetection();
        
        if (enableDebugLog)
        {
            Debug.Log($"WallManager 初始化完成，找到 {wallSegments.Count} 个墙壁段");
        }
    }
    
    /// <summary>
    /// 查找所有子墙壁
    /// </summary>
    void FindWallSegments()
    {
        wallSegments.Clear();
        
        // 遍历所有子对象，查找标记为"Wall"的对象
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Wall"))
            {
                wallSegments.Add(child);
            }
        }
    }
    
    /// <summary>
    /// 为子墙壁设置碰撞检测
    /// </summary>
    void SetupWallCollisionDetection()
    {
        foreach (Transform wallSegment in wallSegments)
        {
            // 为每个墙壁段添加碰撞检测组件
            var detector = wallSegment.gameObject.GetComponent<WallCollisionDetector>();
            if (detector == null)
            {
                detector = wallSegment.gameObject.AddComponent<WallCollisionDetector>();
            }
            
            // 初始化检测器，传入父级管理器引用
            detector.Initialize(this);
        }
    }
    
    /// <summary>
    /// 处理墙壁被撞击（由子墙壁调用）
    /// </summary>
    public void OnWallHit(Collision2D collision, Transform wallTransform)
    {
        GameObject hitObject = collision.gameObject;
        string objectTag = hitObject.tag;
        
        // 只处理球体对象的撞墙
        if (objectTag != "Player" && objectTag != "Enemy")
        {
            return;
        }
        
        // 获取球体的物理组件和速度
        BallPhysics ballPhysics = hitObject.GetComponent<BallPhysics>();
        float currentSpeed = ballPhysics != null ? ballPhysics.GetSpeed() : 0f;
        
        // 计算撞墙信息（所有撞墙都需要，不受防抖影响）
        Vector3 wallHitPosition = collision.contacts[0].point;
        Vector3 wallHitDirection = ((Vector2)hitObject.transform.position - collision.contacts[0].point).normalized;
        Vector3 hitNormal = collision.contacts[0].normal;
        
        // 自动查找并使用现有的 Controller 计算特效数据
        float rotationAngle = 0f;
        Vector3 positionOffset = Vector3.zero;
        
        var rotationController = GetComponentInChildren<WallHitRotationController>();
        if (rotationController != null)
        {
            rotationAngle = rotationController.CalculateRotationAngle(wallHitPosition, hitNormal, currentSpeed);
        }
        
        var positionController = GetComponentInChildren<WallHitPositionController>();
        if (positionController != null)
        {
            positionOffset = positionController.CalculatePositionOffset(wallHitPosition, hitNormal, wallHitDirection, currentSpeed);
        }
        
        // 特效播放
        if (ShouldPlayWallHitEffect(hitObject, currentSpeed))
        {
            // 创建 AttackData（仅用于特效参数，不发布事件）
            var attackData = new AttackData
            {
                AttackType = "Hit",
                Position = wallHitPosition,
                Direction = wallHitDirection,
                Attacker = hitObject,
                Target = wallTransform.gameObject,
                Damage = 0f,
                AttackTime = Time.time,
                AttackerTag = hitObject.tag,
                TargetTag = wallTransform.gameObject.tag,
                HitNormal = hitNormal,
                HitSpeed = currentSpeed,
                WallHitRotationAngle = rotationAngle,
                WallHitPositionOffset = positionOffset
            };
            
            // 播放完整的撞墙特效组
            
            // 1. 攻击者特效（玩家的碰撞特效）
            if (hitObject != null)
            {
                EffectManager.Instance.PlayEffect(hitObject, EffectType.Hit, attackData: attackData);
            }
            
            // 2. 全局特效（镜头震动等）
            if (hitObject.CompareTag("Player"))
            {
                EffectManager.Instance.PlayEffect(EffectManager.Instance.gameObject, EffectType.GlobalHitAttack, attackData: attackData);
            }
            
            // 3. 目标特效（墙壁受击特效 - 位移）
            EffectManager.Instance.PlayEffect(gameObject, EffectType.BeHit, attackData: attackData);
            
            if (enableDebugLog)
            {
                Debug.Log($"触发撞墙完整特效组: {wallTransform.name} <- {hitObject.name}, 速度: {currentSpeed:F2}");
            }
            
            // 更新最后撞墙时间（用于下次防抖判断）
            lastHitTimes[hitObject] = Time.time;
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log($"[WallManager] 防抖拦截特效播放（事件已发布，技能计数不受影响）");
            }
        }
    }
    
    
    /// <summary>
    /// 判断是否应该播放撞墙特效
    /// </summary>
    bool ShouldPlayWallHitEffect(GameObject hitObject, float currentSpeed)
    {
        // 速度阈值检查
        if (currentSpeed < minWallHitSpeed)
        {
            return false;
        }
        
        // 时间间隔检查
        if (lastHitTimes.TryGetValue(hitObject, out float lastHitTime))
        {
            float interval = Time.time - lastHitTime;
            if (interval < wallHitEffectCooldown)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 清理已销毁对象的记录
    /// </summary>
    void Update()
    {
        // 定期清理已销毁对象的记录，避免内存泄漏
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in lastHitTimes)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            lastHitTimes.Remove(key);
        }
    }
    
    /// <summary>
    /// 重置指定对象的撞墙记录
    /// </summary>
    public void ResetWallHitRecord(GameObject obj)
    {
        if (lastHitTimes.ContainsKey(obj))
        {
            lastHitTimes.Remove(obj);
        }
    }
    
    /// <summary>
    /// 重置所有撞墙记录
    /// </summary>
    public void ResetAllWallHitRecords()
    {
        lastHitTimes.Clear();
    }
    
    /// <summary>
    /// 获取墙壁段数量
    /// </summary>
    public int GetWallSegmentCount()
    {
        return wallSegments.Count;
    }
    
    #region 特效管理方法
    
    /// <summary>
    /// 注册所有特效
    /// </summary>
    void RegisterEffects()
    {
        if (EffectManager.Instance == null)
        {
            Debug.LogWarning("WallManager: EffectManager.Instance 为空，跳过特效注册");
            return;
        }
        
        foreach (var effect in effects)
        {
            if (effect.IsValid())
            {
                EffectManager.Instance.RegisterEffect(gameObject, effect.effectType, effect.mmfPlayer);
            }
        }
    }
    
    /// <summary>
    /// 注销所有特效
    /// </summary>
    void UnregisterEffects()
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.UnregisterEffect(gameObject);
        }
    }
    
    #endregion
    
}
