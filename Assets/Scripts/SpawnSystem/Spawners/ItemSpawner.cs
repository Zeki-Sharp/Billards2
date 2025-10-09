using UnityEngine;

/// <summary>
/// 道具生成器 - 继承BaseSpawner，负责道具的实例化和位置计算
/// 执行层：负责道具的具体生成逻辑
/// </summary>
public class ItemSpawner : BaseSpawner<ItemConfig>
{
    [Header("道具生成设置")]
    [Tooltip("道具生成父对象")]
    public Transform itemParent;
    
    [Header("掉落设置")]
    [Tooltip("是否启用掉落位置偏移")]
    public bool enableDropPositionOffset = true;
    
    [Tooltip("掉落位置偏移范围")]
    public float dropOffsetRange = 0.5f;
    
    [Tooltip("掉落位置偏移模式")]
    public DropOffsetMode offsetMode = DropOffsetMode.Circle;
    
    /// <summary>
    /// 初始化道具生成器
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        
        // 如果没有设置道具父对象，使用spawnParent
        if (itemParent == null)
        {
            itemParent = spawnParent;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemSpawner] 初始化完成，道具父对象: {itemParent.name}");
        }
    }
    
    /// <summary>
    /// 实例化道具对象
    /// </summary>
    /// <param name="itemConfig">道具配置</param>
    /// <param name="position">生成位置</param>
    /// <param name="parent">父对象</param>
    /// <returns>实例化的道具GameObject</returns>
    protected override GameObject InstantiateObject(ItemConfig itemConfig, Vector3 position, Transform parent)
    {
        if (itemConfig == null)
        {
            Debug.LogError("[ItemSpawner] 道具配置为空，无法生成道具");
            return null;
        }
        
        if (itemConfig.itemPrefab == null)
        {
            Debug.LogError($"[ItemSpawner] 道具 {itemConfig.itemName} 的预制体为空");
            return null;
        }
        
        // 实例化道具预制体
        GameObject itemInstance = Instantiate(itemConfig.itemPrefab, position, Quaternion.identity, parent);
        
        // 设置ItemPickup组件
        ItemPickup itemPickup = itemInstance.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemConfig(itemConfig);
            
            if (enableDebugLog)
            {
                Debug.Log($"[ItemSpawner] 设置道具配置: {itemConfig.itemName}");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] 道具预制体 {itemConfig.itemName} 缺少ItemPickup组件");
        }
        
        return itemInstance;
    }
    
    /// <summary>
    /// 生成道具（重写以支持掉落偏移）
    /// </summary>
    /// <param name="itemConfig">道具配置</param>
    /// <param name="position">基础位置（通常是敌人死亡位置）</param>
    public void SpawnItem(ItemConfig itemConfig, Vector3 position)
    {
        Vector3 spawnPosition = CalculateDropPosition(position);
        Spawn(itemConfig, spawnPosition);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemSpawner] 生成道具: {itemConfig.itemName} at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// 批量生成道具
    /// </summary>
    /// <param name="itemConfigs">道具配置列表</param>
    /// <param name="basePosition">基础位置</param>
    public void SpawnItems(ItemConfig[] itemConfigs, Vector3 basePosition)
    {
        if (itemConfigs == null || itemConfigs.Length == 0)
        {
            Debug.LogWarning("[ItemSpawner] 道具配置列表为空");
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemSpawner] 开始批量生成 {itemConfigs.Length} 个道具，基础位置: {basePosition}");
        }
        
        Vector3[] positions = CalculateDropPositions(basePosition, itemConfigs.Length);
        
        for (int i = 0; i < itemConfigs.Length; i++)
        {
            if (itemConfigs[i] != null)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[ItemSpawner] 准备生成道具 {i}: {itemConfigs[i].itemName} at {positions[i]}");
                }
                
                // 使用基类的Spawn方法，它会自动处理位置验证和重试
                Spawn(itemConfigs[i], positions[i]);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[ItemSpawner] ✅ 道具生成请求完成: {itemConfigs[i].itemName}");
                }
            }
            else
            {
                Debug.LogError($"[ItemSpawner] ❌ 道具配置为空: 索引 {i}");
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemSpawner] 批量生成完成，共处理 {itemConfigs.Length} 个道具");
        }
    }
    
    /// <summary>
    /// 计算掉落位置（支持偏移）
    /// </summary>
    /// <param name="basePosition">基础位置</param>
    /// <returns>计算后的位置</returns>
    private Vector3 CalculateDropPosition(Vector3 basePosition)
    {
        if (!enableDropPositionOffset)
        {
            return basePosition;
        }
        
        Vector3 offset = GetRandomOffset();
        return basePosition + offset;
    }
    
    /// <summary>
    /// 计算多个掉落位置（避免重叠）
    /// </summary>
    /// <param name="basePosition">基础位置</param>
    /// <param name="count">数量</param>
    /// <returns>位置数组</returns>
    private Vector3[] CalculateDropPositions(Vector3 basePosition, int count)
    {
        Vector3[] positions = new Vector3[count];
        
        if (!enableDropPositionOffset)
        {
            // 不启用偏移，所有道具都在同一位置
            for (int i = 0; i < count; i++)
            {
                positions[i] = basePosition;
            }
            return positions;
        }
        
        // 启用偏移，分散生成
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = GetRandomOffset();
            positions[i] = basePosition + offset;
        }
        
        return positions;
    }
    
    /// <summary>
    /// 获取随机偏移
    /// </summary>
    /// <returns>随机偏移向量</returns>
    private Vector3 GetRandomOffset()
    {
        switch (offsetMode)
        {
            case DropOffsetMode.Circle:
                return GetRandomCircleOffset();
            case DropOffsetMode.Rectangle:
                return GetRandomRectangleOffset();
            case DropOffsetMode.Ring:
                return GetRandomRingOffset();
            default:
                return Vector3.zero;
        }
    }
    
    /// <summary>
    /// 获取圆形范围内的随机偏移
    /// </summary>
    /// <returns>圆形偏移</returns>
    private Vector3 GetRandomCircleOffset()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0f, dropOffsetRange);
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );
    }
    
    /// <summary>
    /// 获取矩形范围内的随机偏移
    /// </summary>
    /// <returns>矩形偏移</returns>
    private Vector3 GetRandomRectangleOffset()
    {
        return new Vector3(
            Random.Range(-dropOffsetRange, dropOffsetRange),
            Random.Range(-dropOffsetRange, dropOffsetRange),
            0f
        );
    }
    
    /// <summary>
    /// 获取环形范围内的随机偏移
    /// </summary>
    /// <returns>环形偏移</returns>
    private Vector3 GetRandomRingOffset()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(dropOffsetRange * 0.5f, dropOffsetRange);
        
        return new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f
        );
    }
    
    /// <summary>
    /// 生成后处理
    /// </summary>
    /// <param name="spawnedObject">生成的对象</param>
    /// <param name="itemConfig">道具配置</param>
    protected override void OnPostSpawn(GameObject spawnedObject, ItemConfig itemConfig)
    {
        // 可选：添加掉落动画
        // 可选：播放生成音效
        // 可选：添加光效
        
        if (enableDebugLog)
        {
            Debug.Log($"[ItemSpawner] 道具生成完成: {itemConfig.itemName}");
        }
    }
}

/// <summary>
/// 掉落偏移模式枚举
/// </summary>
public enum DropOffsetMode
{
    Circle,      // 圆形范围
    Rectangle,   // 矩形范围
    Ring         // 环形范围
}


