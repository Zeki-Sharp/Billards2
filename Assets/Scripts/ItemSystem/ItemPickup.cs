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
    
    // ✅ 拾取者对象缓存（用于获取角色ID）
    private GameObject lastPickerObject;
    
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
        
        // 检查拾取限制
        if (!CanPickup(other.gameObject))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ItemPickup] {other.gameObject.name} 不满足拾取条件：{itemConfig.itemName}");
            }
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
        
        // ✅ 缓存拾取者对象（用于获取角色ID）
        lastPickerObject = picker;
        
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
        
        // 3. 发布拾取事件
        string pickerCharacterID = GetPickerCharacterID();
        if (!string.IsNullOrEmpty(pickerCharacterID))
        {
            GameEventBus.PublishItemPickedUp(pickerCharacterID, itemConfig, transform.position);
        }
        
        // 4. 销毁道具对象
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 应用道具效果
    /// </summary>
    private bool ApplyItemEffect()
    {
        // ✅ 如果没有关联技能，跳过效果应用（如收集者宝石，只用于计数）
        if (itemConfig.itemSkill == null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ItemPickup] 道具 {itemConfig.itemName} 无关联技能，跳过效果应用");
            }
            return true;  // 返回 true，允许拾取继续
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
    /// 
    /// 【多角色系统改进】：
    /// - 根据 targetType 配置，为不同角色执行效果
    /// - 支持：拾取者、全队、指定角色
    /// </summary>
    private bool ExecuteInstantEffect()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 执行一次性效果: {itemConfig.itemSkill.skillName}, 目标类型: {itemConfig.targetType}");
        }
        
        // 创建效果实例（从等级1获取）
        var level1Config = itemConfig.itemSkill.GetLevelConfig(1);
        if (level1Config?.effectConfig == null)
        {
            Debug.LogError($"[ItemPickup] 技能 {itemConfig.itemSkill.skillName} 没有等级1配置");
            return false;
        }
        
        // ✅ 根据目标类型执行效果
        switch (itemConfig.targetType)
        {
            case ItemTargetType.Picker:
                return ExecuteEffectForPicker(level1Config);
                
            case ItemTargetType.AllCharacters:
                return ExecuteEffectForAllCharacters(level1Config);
                
            case ItemTargetType.SpecificCharacter:
                return ExecuteEffectForSpecificCharacter(level1Config);
                
            default:
                Debug.LogError($"[ItemPickup] 未知的目标类型: {itemConfig.targetType}");
                return false;
        }
    }
    
    /// <summary>
    /// ✅ 添加持续效果（如buff）- 多角色系统适配
    /// </summary>
    private bool AddPersistentEffect()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] 添加持续效果: {itemConfig.itemSkill.skillName}");
        }
        
        // 查找SkillManager
        var skillManager = SkillManager.Instance;
        if (skillManager == null)
        {
            Debug.LogError($"[ItemPickup] 未找到SkillManager，无法添加持续效果！");
            return false;
        }
        
        // ✅ 获取拾取者的角色ID
        string pickerCharacterID = GetPickerCharacterID();
        if (string.IsNullOrEmpty(pickerCharacterID))
        {
            Debug.LogError($"[ItemPickup] 无法获取拾取者角色ID，无法添加持续效果！");
            return false;
        }
        
        // ✅ 添加技能到拾取者角色
        skillManager.AddSkillToCharacter(pickerCharacterID, itemConfig.itemSkill);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 持续效果添加成功: {itemConfig.itemSkill.skillName} → 角色 {pickerCharacterID}");
        }
        
        return true;
    }
    
    /// <summary>
    /// ✅ 获取拾取者的角色ID（使用统一接口）
    /// </summary>
    private string GetPickerCharacterID()
    {
        return TriggerHelper.GetCharacterID(lastPickerObject);
    }
    
    /// <summary>
    /// ✅ 检查是否可以拾取（拾取限制）
    /// </summary>
    private bool CanPickup(GameObject picker)
    {
        switch (itemConfig.pickupRestriction)
        {
            case ItemPickupRestriction.None:
                return true;
                
            case ItemPickupRestriction.SpecificCharacter:
                return CanPickupBySpecificCharacter(picker);
                
            case ItemPickupRestriction.HealthBelow50:
                return CanPickupByHealthCondition(picker);
                
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 检查角色ID限制（使用 characterID 而非角色名称）
    /// </summary>
    private bool CanPickupBySpecificCharacter(GameObject picker)
    {
        if (string.IsNullOrEmpty(itemConfig.restrictedCharacterName))
        {
            Debug.LogWarning($"[ItemPickup] 配置了角色限制但未指定角色ID");
            return true;
        }
        
        // 使用 TriggerHelper 获取角色ID（而非角色名称）
        string pickerCharacterID = TriggerHelper.GetCharacterID(picker);
        bool canPickup = pickerCharacterID == itemConfig.restrictedCharacterName;
        
        if (enableDebugLog && !canPickup)
        {
            Debug.Log($"[ItemPickup] 角色 {pickerCharacterID} 不是指定角色 {itemConfig.restrictedCharacterName}，无法拾取");
        }
        
        return canPickup;
    }
    
    /// <summary>
    /// 检查血量条件
    /// </summary>
    private bool CanPickupByHealthCondition(GameObject picker)
    {
        PlayerBehavior playerBehavior = picker.GetComponent<PlayerBehavior>();
        if (playerBehavior == null) return false;
        
        float healthRatio = playerBehavior.GetCurrentHealth() / playerBehavior.GetMaxHealth();
        bool canPickup = healthRatio < 0.5f;
        
        if (enableDebugLog && !canPickup)
        {
            Debug.Log($"[ItemPickup] {picker.name} 血量 {healthRatio * 100:F0}% >= 50%，无法拾取");
        }
        
        return canPickup;
    }
    
    /// <summary>
    /// 获取角色名称（仅用于显示，逻辑判断请使用 TriggerHelper.GetCharacterID）
    /// </summary>
    private string GetCharacterName(GameObject ball)
    {
        if (ball == null) return null;
        
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData != null)
        {
            foreach (var character in teamData.characters)
            {
                if (character.ballInstance == ball)
                {
                    return character.characterData?.info.name;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 为拾取者执行效果
    /// </summary>
    private bool ExecuteEffectForPicker(SkillLevelConfig levelConfig)
    {
        string pickerCharacterID = GetPickerCharacterID();
        if (string.IsNullOrEmpty(pickerCharacterID))
        {
            Debug.LogError($"[ItemPickup] 无法获取拾取者角色ID");
            return false;
        }
        
        var effect = levelConfig.effectConfig.CreateEffect();
        if (effect == null) return false;
        
        effect.Initialize();
        effect.SetTarget(pickerCharacterID);  // ✅ 设置目标
        bool success = effect.ExecuteEffect(null);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 效果应用于拾取者 {pickerCharacterID}: {success}");
        }
        
        return success;
    }
    
    /// <summary>
    /// 为所有角色执行效果
    /// </summary>
    private bool ExecuteEffectForAllCharacters(SkillLevelConfig levelConfig)
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.LogError($"[ItemPickup] TeamData 为空，无法为全队执行效果");
            return false;
        }
        
        int successCount = 0;
        foreach (var character in teamData.characters)
        {
            if (!character.isAlive) continue;
            
            var effect = levelConfig.effectConfig.CreateEffect();
            if (effect == null) continue;
            
            effect.Initialize();
            effect.SetTarget(character.characterID);  // ✅ 设置目标
            if (effect.ExecuteEffect(null))
            {
                successCount++;
                
                if (enableDebugLog)
                {
                    Debug.Log($"[ItemPickup] ✅ 效果应用于角色 {character.characterID}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 团队效果完成，成功 {successCount}/{teamData.characters.Count} 个角色");
        }
        
        return successCount > 0;
    }
    
    /// <summary>
    /// 为指定角色执行效果（使用 characterID）
    /// </summary>
    private bool ExecuteEffectForSpecificCharacter(SkillLevelConfig levelConfig)
    {
        if (string.IsNullOrEmpty(itemConfig.targetCharacterName))
        {
            Debug.LogError($"[ItemPickup] 配置了指定角色但未填写角色ID");
            return false;
        }
        
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.LogError($"[ItemPickup] TeamData 为空");
            return false;
        }
        
        // ✅ 使用 characterID 查找目标角色
        var targetCharacter = teamData.characters.Find(c => 
            c.characterID == itemConfig.targetCharacterName && c.isAlive);
            
        if (targetCharacter == null)
        {
            Debug.LogWarning($"[ItemPickup] 未找到目标角色ID: {itemConfig.targetCharacterName}");
            return false;
        }
        
        var effect = levelConfig.effectConfig.CreateEffect();
        if (effect == null) return false;
        
        effect.Initialize();
        effect.SetTarget(targetCharacter.characterID);
        bool success = effect.ExecuteEffect(null);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemPickup] ✅ 效果应用于角色 {targetCharacter.characterID}: {success}");
        }
        
        return success;
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

