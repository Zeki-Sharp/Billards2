using UnityEngine;

/// <summary>
/// 墙壁脚本 - 统一处理撞墙特效和防抖逻辑
/// 避免在每个球体对象中重复实现防抖代码
/// </summary>
public class Wall : MonoBehaviour
{
    [Header("撞墙特效设置")]
    public float wallHitEffectCooldown = 0.5f; // 撞墙特效冷却时间（秒）
    public float minWallHitSpeed = 1.0f; // 最小撞墙速度阈值
    
    // 防抖字典：存储每个对象的最后撞墙时间
    private System.Collections.Generic.Dictionary<GameObject, float> lastHitTimes = new System.Collections.Generic.Dictionary<GameObject, float>();
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 只处理球体对象的撞墙
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleWallHit(collision);
        }
    }
    
    /// <summary>
    /// 处理撞墙逻辑
    /// </summary>
    void HandleWallHit(Collision2D collision)
    {
        GameObject hitObject = collision.gameObject;
        string objectTag = hitObject.tag;
        
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
            
            // 撞墙特效由攻击者（白球）触发，这里不需要重复触发
            // 玩家会在 PlayerCore.cs 中调用 GameEventBus.PublishAttack(...)
            
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
        var keysToRemove = new System.Collections.Generic.List<GameObject>();
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
}
