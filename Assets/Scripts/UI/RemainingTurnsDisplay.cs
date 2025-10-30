using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 剩余回合数显示组件 - 专门负责回合数的显示和管理
/// 
/// 【核心职责】：
/// - 监听回合事件，实时更新显示
/// - 根据场景类型控制自身显示/隐藏
/// - 根据回合状态切换显示模式（倒计时/伤害/无限）
/// - 实现颜色预警系统
/// 
/// 【设计原则】：
/// - 单一职责：只处理回合数显示
/// - 事件驱动：响应回合和场景事件
/// - 自我管理：独立控制显示/隐藏
/// - 解耦设计：不依赖TopBarController
/// 
/// 【显示模式】：
/// - 倒计时模式：剩余回合 > 0，显示"剩余: X回合"
/// - 伤害模式：剩余回合 = 0，显示"伤害: X/回合"
/// - 无限模式：无回合限制，显示"回合: ∞"
/// </summary>
public class RemainingTurnsDisplay : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] 
    [Tooltip("显示回合信息的文本组件")]
    private TextMeshProUGUI turnsText;
    
    [SerializeField] 
    [Tooltip("控制整体显示/隐藏（可选）")]
    private CanvasGroup canvasGroup;
    
    [Header("场景显示控制")]
    [SerializeField] 
    [Tooltip("在地图场景是否显示")]
    private bool showInMapScene = false;
    
    [SerializeField] 
    [Tooltip("在关卡场景是否显示")]
    private bool showInLevelScene = true;
    
    [SerializeField]
    [Tooltip("地图场景名称列表（用于判断）")]
    private string[] mapSceneNames = { "MapScene" };
    
    [Header("颜色配置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private int warningThreshold = 5;
    [SerializeField] private int dangerThreshold = 3;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 显示模式枚举
    private enum DisplayMode
    {
        Countdown,      // 倒计时模式：显示剩余回合数
        Damage,         // 伤害模式：显示每回合伤害
        Infinite        // 无限模式：显示∞
    }
    
    private DisplayMode currentMode = DisplayMode.Countdown;
    private bool isInitialized = false;
    
    void Awake()
    {
        // 初始化
        if (turnsText == null)
        {
            turnsText = GetComponent<TextMeshProUGUI>();
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
    
    void Start()
    {
        // 订阅事件
        SubscribeToEvents();
        
        // 初始化显示状态（根据当前场景）
        UpdateVisibilityByScene(SceneManager.GetActiveScene().name);
        
        // 初始化回合数显示
        UpdateTurnDisplay();
        
        isInitialized = true;
        
        if (showDebugInfo)
        {
            Debug.Log("RemainingTurnsDisplay: 初始化完成");
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #region 事件订阅
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        // 订阅玩家回合开始事件
        GameEventBus.OnPlayerPlayingPhaseStarted += OnPlayerTurnStarted;
        
        // 订阅关卡开始事件
        GameEventBus.OnLevelStarted += OnLevelStarted;
        
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnPlayerPlayingPhaseStarted -= OnPlayerTurnStarted;
        GameEventBus.OnLevelStarted -= OnLevelStarted;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 玩家回合开始事件处理
    /// </summary>
    void OnPlayerTurnStarted()
    {
        UpdateTurnDisplay();
    }
    
    /// <summary>
    /// 关卡开始事件处理
    /// </summary>
    void OnLevelStarted(int levelIndex, LevelConfig levelConfig)
    {
        // 关卡开始时显示（如果配置允许）
        if (showInLevelScene)
        {
            SetVisible(true);
        }
        
        // 初始化回合数显示
        UpdateTurnDisplay();
        
        if (showDebugInfo)
        {
            Debug.Log($"RemainingTurnsDisplay: 关卡 {levelIndex} 开始，初始化回合数显示");
        }
    }
    
    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateVisibilityByScene(scene.name);
    }
    
    #endregion
    
    #region 显示控制
    
    /// <summary>
    /// 根据场景名称更新显示状态
    /// </summary>
    void UpdateVisibilityByScene(string sceneName)
    {
        // 判断是否是地图场景
        bool isMapScene = System.Array.Exists(mapSceneNames, name => sceneName.Contains(name));
        
        if (isMapScene)
        {
            // 地图场景
            SetVisible(showInMapScene);
            
            if (showDebugInfo)
            {
                Debug.Log($"RemainingTurnsDisplay: 检测到地图场景 ({sceneName})，显示状态: {showInMapScene}");
            }
        }
        else
        {
            // 关卡场景
            SetVisible(showInLevelScene);
            
            if (showDebugInfo)
            {
                Debug.Log($"RemainingTurnsDisplay: 检测到关卡场景 ({sceneName})，显示状态: {showInLevelScene}");
            }
        }
    }
    
    /// <summary>
    /// 设置可见性
    /// </summary>
    void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else if (turnsText != null)
        {
            turnsText.gameObject.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
    
    #endregion
    
    #region 回合数显示
    
    /// <summary>
    /// 更新回合数显示
    /// </summary>
    void UpdateTurnDisplay()
    {
        if (turnsText == null)
        {
            return;
        }
        
        // 获取TurnPenaltyManager实例
        if (TurnPenaltyManager.Instance == null)
        {
            // 如果Manager还未初始化，显示占位符
            turnsText.text = "回合: --";
            turnsText.color = normalColor;
            return;
        }
        
        // 获取剩余回合数
        int remainingTurns = TurnPenaltyManager.Instance.GetRemainingTurns();
        
        // 判断显示模式
        if (remainingTurns == -1)
        {
            // 无限模式
            currentMode = DisplayMode.Infinite;
            UpdateDisplayInfiniteMode();
        }
        else if (remainingTurns > 0)
        {
            // 倒计时模式
            currentMode = DisplayMode.Countdown;
            UpdateDisplayCountdownMode(remainingTurns);
        }
        else
        {
            // 伤害模式（剩余回合为0）
            currentMode = DisplayMode.Damage;
            UpdateDisplayDamageMode();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"RemainingTurnsDisplay: 更新显示 - 模式: {currentMode}, 剩余: {remainingTurns}");
        }
    }
    
    /// <summary>
    /// 更新显示：无限模式
    /// </summary>
    void UpdateDisplayInfiniteMode()
    {
        turnsText.text = "回合: ∞";
        turnsText.color = normalColor;
    }
    
    /// <summary>
    /// 更新显示：倒计时模式
    /// </summary>
    void UpdateDisplayCountdownMode(int remainingTurns)
    {
        turnsText.text = $"剩余: {remainingTurns}回合";
        
        // 根据剩余回合数设置颜色
        if (remainingTurns <= dangerThreshold)
        {
            turnsText.color = dangerColor; // 紧急：红色
        }
        else if (remainingTurns <= warningThreshold)
        {
            turnsText.color = warningColor; // 警告：黄色
        }
        else
        {
            turnsText.color = normalColor; // 正常：白色
        }
    }
    
    /// <summary>
    /// 更新显示：伤害模式
    /// </summary>
    void UpdateDisplayDamageMode()
    {
        // 获取下一回合的伤害
        float nextDamage = TurnPenaltyManager.Instance.GetNextTurnPenaltyDamage();
        
        turnsText.text = $"本回合伤害: {nextDamage:F0}";
        turnsText.color = dangerColor; // 伤害模式显示红色
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 手动刷新显示
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateTurnDisplay();
    }
    
    /// <summary>
    /// 手动设置可见性
    /// </summary>
    public void Show()
    {
        SetVisible(true);
    }
    
    /// <summary>
    /// 手动隐藏
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("刷新显示")]
    void DebugRefresh()
    {
        RefreshDisplay();
    }
    
    [ContextMenu("显示组件")]
    void DebugShow()
    {
        Show();
    }
    
    [ContextMenu("隐藏组件")]
    void DebugHide()
    {
        Hide();
    }
    
    [ContextMenu("显示状态")]
    void DebugShowStatus()
    {
        bool isVisible = canvasGroup != null ? canvasGroup.alpha > 0 : (turnsText != null ? turnsText.gameObject.activeSelf : gameObject.activeSelf);
        
        Debug.Log($"RemainingTurnsDisplay 状态:\n" +
                 $"可见: {isVisible}\n" +
                 $"当前模式: {currentMode}\n" +
                 $"Text组件: {(turnsText != null ? "已配置" : "未配置")}\n" +
                 $"CanvasGroup: {(canvasGroup != null ? "已配置" : "未配置")}\n" +
                 $"当前文本: {(turnsText != null ? turnsText.text : "N/A")}\n" +
                 $"地图场景显示: {showInMapScene}\n" +
                 $"关卡场景显示: {showInLevelScene}");
    }
    
    #endregion
}

