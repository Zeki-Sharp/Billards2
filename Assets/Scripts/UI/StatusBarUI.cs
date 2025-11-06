using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 状态栏UI管理器
/// 
/// 【职责】：
/// - 管理一个角色身上所有状态图标的显示
/// - 维护图标对象池（性能优化）
/// - 监听状态变化事件，自动更新UI
/// 
/// 【挂载位置】：
/// - Enemy预制体的 HealthBar 下方（World Space Canvas）
/// - 或任何需要显示状态的UI容器上
/// 
/// 【工作流程】：
/// 1. 监听 TurnBasedStatusComponent.OnStatusChanged 事件
/// 2. 收集该GameObject上所有的 TurnBasedStatusComponent
/// 3. 为每个状态创建/更新图标
/// 4. 自动排列（使用 HorizontalLayoutGroup）
/// </summary>
public class StatusBarUI : MonoBehaviour
{
    #region 配置
    
    [Header("容器配置")]
    [SerializeField] private Transform iconContainer;   // 图标容器（应带 HorizontalLayoutGroup）
    [SerializeField] private GameObject iconPrefab;     // 状态图标预制体
    [SerializeField] private int maxIconCount = 8;      // 最多显示的图标数量
    
    [Header("目标配置")]
    [Tooltip("自动从父级查找目标GameObject，如果为空")]
    [SerializeField] private GameObject targetGameObject; // 监听的目标GameObject
    
    [Header("调试")]
    [SerializeField] private bool showDebugLog = false;
    
    #endregion
    
    #region 运行时数据
    
    private Dictionary<string, StatusIconUI> activeIcons = new Dictionary<string, StatusIconUI>();
    private Queue<StatusIconUI> iconPool = new Queue<StatusIconUI>();
    
    #endregion
    
    #region Unity生命周期
    
    void Awake()
    {
        // 自动查找容器
        if (iconContainer == null)
        {
            iconContainer = transform;
        }
        
        // 自动查找目标GameObject
        if (targetGameObject == null)
        {
            // 尝试从父级的父级查找（因为 StatusBarUI 通常在 HUD 下）
            GameObject candidate = transform.parent?.parent?.gameObject;
            
            if (candidate != null)
            {
                // ✅ 方法1：如果找到的对象没有 IDamageable，向上查找
                var damageable = candidate.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    // 可能是 enemyItem 子对象，向上找到有 IDamageable 的根物体
                    damageable = candidate.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        targetGameObject = (damageable as MonoBehaviour)?.gameObject;
                        Debug.Log($"[StatusBarUI] {gameObject.name} 通过 IDamageable 查找到根物体: {targetGameObject?.name}");
                    }
                }
                
                // 如果没有找到 IDamageable，使用原始 candidate
                if (targetGameObject == null)
                {
                    targetGameObject = candidate;
                }
            }
            
        }
        
        // 预热对象池
        PrewarmPool(3);
    }
    
    void OnEnable()
    {
        // 订阅 GameEventBus 状态变化事件
        GameEventBus.OnTurnBasedStatusChanged += OnStatusChanged;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnTurnBasedStatusChanged -= OnStatusChanged;
    }
    
    void Start()
    {
        // 初始化时刷新一次
        RefreshAllIcons();
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 响应状态变化事件
    /// </summary>
    void OnStatusChanged(GameObject target, TurnBasedStatusData statusData, int remainingTurns)
    {
        // 只处理自己监听的目标
        if (target != targetGameObject)
        {
            return;
        }
        
        // 全量刷新（简单但可靠）
        RefreshAllIcons();
    }
    
    #endregion
    
    #region 核心方法
    
    /// <summary>
    /// 刷新所有状态图标（从 TurnBasedStatusComponent 读取）
    /// </summary>
    public void RefreshAllIcons()
    {
        if (targetGameObject == null)
        {
            return;
        }
        
        // 收集所有状态组件（包括子对象）
        var statusComponents = targetGameObject.GetComponentsInChildren<TurnBasedStatusComponent>();
        
        // 过滤出有效的状态
        var validStatuses = statusComponents
            .Where(s => s != null && s.StatusData != null && s.RemainingTurns > 0)
            .ToList();
        
        // 清空现有图标
        ClearAllIcons();
        
        // 限制显示数量
        int displayCount = Mathf.Min(validStatuses.Count, maxIconCount);
        
        // 为每个状态创建图标
        for (int i = 0; i < displayCount; i++)
        {
            var status = validStatuses[i];
            var statusData = status.StatusData;
            
            // 从对象池获取图标
            StatusIconUI icon = GetIconFromPool();
            
            // 设置图标数据
            icon.SetData(
                statusData.icon,
                statusData.iconColor,
                status.RemainingTurns,
                true,  // TODO: 根据状态类型判断
                statusData.statusID
            );
            
            // 记录激活的图标
            activeIcons[statusData.statusID] = icon;
        }
        
        // ✅ 修复：不禁用 GameObject，而是控制可见性（避免取消事件订阅）
        bool shouldBeVisible = activeIcons.Count > 0;
        
        // 使用 CanvasGroup 控制可见性，而不是 SetActive
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = shouldBeVisible ? 1f : 0f;
        canvasGroup.blocksRaycasts = shouldBeVisible;
        canvasGroup.interactable = shouldBeVisible;
    }
    
    /// <summary>
    /// 清空所有图标（回收到对象池）
    /// </summary>
    void ClearAllIcons()
    {
        foreach (var icon in activeIcons.Values)
        {
            ReturnIconToPool(icon);
        }
        
        activeIcons.Clear();
    }
    
    #endregion
    
    #region 对象池管理
    
    /// <summary>
    /// 预热对象池
    /// </summary>
    void PrewarmPool(int count)
    {
        if (iconPrefab == null)
        {
            Debug.LogWarning("[StatusBarUI] iconPrefab 未配置！");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            CreateNewIcon();
        }
    }
    
    /// <summary>
    /// 从对象池获取图标
    /// </summary>
    StatusIconUI GetIconFromPool()
    {
        if (iconPool.Count > 0)
        {
            var icon = iconPool.Dequeue();
            icon.gameObject.SetActive(true);
            return icon;
        }
        else
        {
            return CreateNewIcon();
        }
    }
    
    /// <summary>
    /// 归还图标到对象池
    /// </summary>
    void ReturnIconToPool(StatusIconUI icon)
    {
        icon.Clear();
        icon.gameObject.SetActive(false);
        iconPool.Enqueue(icon);
    }
    
    /// <summary>
    /// 创建新图标
    /// </summary>
    StatusIconUI CreateNewIcon()
    {
        GameObject iconObj = Instantiate(iconPrefab, iconContainer);
        StatusIconUI icon = iconObj.GetComponent<StatusIconUI>();
        
        if (icon == null)
        {
            Debug.LogError("[StatusBarUI] iconPrefab 上没有 StatusIconUI 组件！");
            icon = iconObj.AddComponent<StatusIconUI>();
        }
        
        icon.Clear();
        icon.gameObject.SetActive(false);
        iconPool.Enqueue(icon);
        
        return icon;
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 手动设置目标GameObject
    /// </summary>
    public void SetTarget(GameObject target)
    {
        targetGameObject = target;
        RefreshAllIcons();
    }
    
    /// <summary>
    /// 获取当前显示的状态数量
    /// </summary>
    public int ActiveIconCount => activeIcons.Count;
    
    #endregion
}

