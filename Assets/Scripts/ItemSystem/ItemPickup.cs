using UnityEngine;

/// <summary>
/// 场景道具实体 - 处理道具的拾取和效果触发
/// 职责：碰撞检测、效果应用、视听反馈、自身销毁
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("道具配置")]
    [Tooltip("道具配置数据")]
    public ItemConfig itemConfig;
    
    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool enableDebugLog = true;
    
    private bool isPickedUp = false; // 防止重复拾取（仅限当前实例）
    private string instanceId; // 实例唯一标识
    
    #region Unity生命周期
    
    void Start()
    {
        // 生成实例唯一ID
        instanceId = System.Guid.NewGuid().ToString();
        
        // 验证配置
        if (itemConfig == null)
        {
            Debug.LogError($"[ItemPickup] {gameObject.name} 未设置ItemConfig！");
            return;
        }
        
        if (!itemConfig.IsValid())
        {
            Debug.LogError($"[ItemPickup] {gameObject.name} 的ItemConfig配置无效！");
            return;
        }
        
        // 确保Collider是触发器
        var collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            Debug.LogWarning($"[ItemPickup] {gameObject.name} 的Collider不是触发器，已自动设置为触发器");
            collider.isTrigger = true;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 道具初始化成功: {itemConfig.itemName} (实例ID: {instanceId})");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 只响应玩家
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // 防止重复拾取
        if (isPickedUp)
        {
            return;
        }
        
        // 执行拾取
        PickupItem(other.gameObject);
    }
    
    #endregion
    
    #region 拾取逻辑
    
    /// <summary>
    /// 执行道具拾取
    /// </summary>
    private void PickupItem(GameObject picker)
    {
        isPickedUp = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 🎁 玩家拾取道具: {itemConfig.itemName} (实例ID: {instanceId})");
        }
        
        // 1. 应用道具效果
        bool effectApplied = ApplyItemEffect();
        
        if (!effectApplied)
        {
            Debug.LogError($"[ItemPickup] 道具效果应用失败: {itemConfig.itemName}");
            isPickedUp = false; // 允许重新拾取
            return;
        }
        
        // 2. 播放拾取反馈
        PlayPickupFeedback();
        
        // 3. 发布拾取事件（暂时注释，等事件系统集成时启用）
        // GameEventBus.PublishItemPickedUp(itemConfig, transform.position, picker);
        
        // 4. 销毁道具对象
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 应用道具效果
    /// </summary>
    private bool ApplyItemEffect()
    {
        if (itemConfig.itemSkill == null)
        {
            Debug.LogError($"[ItemPickup] 道具 {itemConfig.itemName} 未设置关联技能！");
            return false;
        }
        
        // 根据是否为一次性效果选择不同的应用方式
        if (itemConfig.isInstantEffect)
        {
            // 一次性效果 - 立即执行Effect
            return ExecuteInstantEffect();
        }
        else
        {
            // 持续效果 - 添加到SkillManager（阶段5实现）
            return AddPersistentEffect();
        }
    }
    
    /// <summary>
    /// 执行一次性效果（如治疗）
    /// </summary>
    private bool ExecuteInstantEffect()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 执行一次性效果: {itemConfig.itemSkill.skillName}");
        }
        
        // 创建效果实例（从等级1获取）
        var level1Config = itemConfig.itemSkill.GetLevelConfig(1);
        if (level1Config?.effectConfig == null)
        {
            Debug.LogError($"[ItemPickup] 技能 {itemConfig.itemSkill.skillName} 没有等级1配置");
            return false;
        }
        
        var effect = level1Config.effectConfig.CreateEffect();
        
        if (effect == null)
        {
            Debug.LogError($"[ItemPickup] 创建效果失败: {itemConfig.itemSkill.skillName}");
            return false;
        }
        
        // 初始化并执行效果
        effect.Initialize();
        bool success = effect.ExecuteEffect(null);
        
        if (success && enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 一次性效果执行成功: {itemConfig.itemSkill.skillName}");
        }
        
        return success;
    }
    
    /// <summary>
    /// 添加持续效果（如buff）
    /// </summary>
    private bool AddPersistentEffect()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 添加持续效果: {itemConfig.itemSkill.skillName}");
        }
        
        // 查找SkillManager
        var skillManager = FindFirstObjectByType<SkillManager>();
        if (skillManager == null)
        {
            Debug.LogError($"[ItemPickup] 未找到SkillManager，无法添加持续效果！");
            return false;
        }
        
        // 添加技能到管理器
        skillManager.AddSkill(itemConfig.itemSkill);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 持续效果添加成功: {itemConfig.itemSkill.skillName}");
        }
        
        return true;
    }
    
    #endregion
    
    #region 视听反馈
    
    /// <summary>
    /// 播放拾取反馈效果
    /// </summary>
    private void PlayPickupFeedback()
    {
        // 播放拾取特效
        if (itemConfig.pickupEffect != null)
        {
            Instantiate(itemConfig.pickupEffect, transform.position, Quaternion.identity);
            
            if (enableDebugLog)
            {
                Debug.Log($"[ItemPickup] 播放拾取特效");
            }
        }
        
        // 播放拾取音效
        if (itemConfig.pickupSound != null)
        {
            // 音效系统待集成
            // AudioManager.PlaySound(itemConfig.pickupSound, transform.position);
            
            if (enableDebugLog)
            {
                Debug.Log($"[ItemPickup] 播放拾取音效: {itemConfig.pickupSound.name}");
            }
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 设置道具配置（用于动态生成道具时）
    /// </summary>
    public void SetItemConfig(ItemConfig config)
    {
        itemConfig = config;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 设置道具配置: {config?.itemName ?? "null"}");
        }
    }
    
    /// <summary>
    /// 获取道具配置
    /// </summary>
    public ItemConfig GetItemConfig()
    {
        return itemConfig;
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 在Scene视图中显示道具信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (itemConfig == null) return;
        
        // 绘制拾取范围
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        var collider = GetComponent<Collider2D>();
        if (collider is CircleCollider2D circleCollider)
        {
            Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
        }
    }
    
    #endregion
}

