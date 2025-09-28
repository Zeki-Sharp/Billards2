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
        
        // 检查防抖条件
        if (ShouldPlayWallHitEffect(hitObject, currentSpeed))
        {
            // 计算撞墙信息
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
                Debug.Log($"🔧 [DEBUG] WallHitRotationController 计算旋转角度: {rotationAngle:F2}°");
            }
            
            var positionController = GetComponentInChildren<WallHitPositionController>();
            if (positionController != null)
            {
                positionOffset = positionController.CalculatePositionOffset(wallHitPosition, hitNormal, wallHitDirection, currentSpeed);
                Debug.Log($"🔧 [DEBUG] WallHitPositionController 计算位置偏移: {positionOffset}");
            }
            
            // 使用 GameEventBus 系统触发墙壁受击特效（带计算器参数）
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
            
            Debug.Log($"🔧 [DEBUG] AttackData 创建完成 - WallHitRotationAngle: {attackData.WallHitRotationAngle:F2}°, " +
                     $"WallHitPositionOffset: {attackData.WallHitPositionOffset}, HitSpeed: {attackData.HitSpeed:F2}");
            
            // 触发全局攻击事件（用于其他对象的受击特效）
            GameEventBus.PublishAttack(attackData);
            
            // 播放墙壁自己的受击特效
            EffectManager.Instance.PlayEffect(gameObject, "Be Hit", attackData);
            
            if (enableDebugLog)
            {
                Debug.Log($"触发墙壁受击特效: {wallTransform.name} <- {hitObject.name}, 速度: {currentSpeed:F2}, 旋转角度: {rotationAngle:F2}, 位置偏移: {positionOffset}");
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"触发撞墙特效: {wallTransform.name} <- {hitObject.name}, 速度: {currentSpeed:F2}, 旋转角度: {rotationAngle:F2}, 位置偏移: {positionOffset}");
            }
            
            // 更新最后撞墙时间
            lastHitTimes[hitObject] = Time.time;
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
            if (Time.time - lastHitTime < wallHitEffectCooldown)
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
        foreach (var effect in effects)
        {
            if (effect.IsValid())
            {
                EffectManager.Instance.RegisterEffect(gameObject, effect.key, effect.mmfPlayer);
            }
        }
    }
    
    /// <summary>
    /// 注销所有特效
    /// </summary>
    void UnregisterEffects()
    {
        EffectManager.Instance.UnregisterEffect(gameObject);
    }
    
    #endregion
    
}
