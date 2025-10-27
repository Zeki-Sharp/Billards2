using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// UI中央管理器 - 统一管理所有UI面板
/// 
/// 【核心职责】：
/// - 单例模式，跨场景保持
/// - 管理所有UI面板的显示/隐藏
/// - 处理面板互斥和层级
/// - 统一控制游戏暂停/恢复
/// - 订阅游戏事件并响应
/// 
/// 【设计模式】：
/// - 单例模式：全局唯一实例
/// - 中介者模式：协调各面板交互
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    
    [Header("预放置面板")]
    [SerializeField] private TopBarController topBarController;
    [SerializeField] private SkillSelectionUI skillSelectionUI;
    
    [Header("Canvas容器")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Canvas fullScreenCanvas;
    [SerializeField] private Canvas popupCanvas;
    
    [Header("动态加载配置")]
    [SerializeField] private string victoryPanelPath = "UI/Popups/VictoryPanel";
    [SerializeField] private string gameOverPanelPath = "UI/Popups/GameOverPanel";
    [SerializeField] private string skillStatusPanelPath = "UI/Popups/SkillStatusPanel";
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 动态加载的面板缓存
    private Dictionary<string, BasePanel> loadedPanels = new Dictionary<string, BasePanel>();
    
    // 当前显示的面板（用于互斥管理）
    private BasePanel currentPopupPanel = null;
    private BasePanel currentFullScreenPanel = null;
    
    void Awake()
    {
        InitializeSingleton();
    }
    
    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        // 取消订阅游戏重启事件
        GameEventBus.OnGameRestart -= ResetState;
        
        UnsubscribeFromEvents();
    }
    
    #region 单例和初始化
    
    /// <summary>
    /// 初始化单例
    /// </summary>
    void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 订阅游戏重启事件
            GameEventBus.OnGameRestart += ResetState;
            
            if (showDebugInfo)
            {
                Debug.Log("UIController: 单例创建，设置为DontDestroyOnLoad");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("UIController: 发现重复实例，销毁当前对象");
            }
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 初始化UI系统
    /// </summary>
    void InitializeUI()
    {
        // 确保有EventSystem
        EnsureEventSystem();
        
        // 初始化预放置面板
        InitializePreloadedPanels();
        
        if (showDebugInfo)
        {
            Debug.Log("UIController: UI系统初始化完成");
        }
    }
    
    /// <summary>
    /// 确保场景中有EventSystem
    /// </summary>
    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            eventSystemObj.transform.SetParent(transform);
            
            if (showDebugInfo)
            {
                Debug.Log("UIController: 创建EventSystem");
            }
        }
    }
    
    /// <summary>
    /// 初始化预放置面板
    /// </summary>
    void InitializePreloadedPanels()
    {
        // 查找TopBar（不是BasePanel，有自己的Awake/Start）
        if (topBarController == null)
        {
            topBarController = FindFirstObjectByType<TopBarController>();
        }
        
        // 查找SkillSelectionUI
        if (skillSelectionUI == null)
        {
            skillSelectionUI = FindFirstObjectByType<SkillSelectionUI>();
        }
        
        // 初始化SkillSelectionUI（继承BasePanel，需要手动初始化）
        if (skillSelectionUI != null)
        {
            skillSelectionUI.OnInit();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"UIController: 预放置面板初始化 - TopBar: {topBarController != null}, SkillSelectionUI: {skillSelectionUI != null}");
        }
    }
    
    #endregion
    
    #region 事件订阅
    
    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnGameOver += OnGameOver;
        GameEventBus.OnGameCompleted += OnGameCompleted;
        
        if (showDebugInfo)
        {
            Debug.Log("UIController: 已订阅游戏事件");
        }
    }
    
    /// <summary>
    /// 取消订阅游戏事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnGameOver -= OnGameOver;
        GameEventBus.OnGameCompleted -= OnGameCompleted;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 游戏失败事件处理
    /// </summary>
    void OnGameOver()
    {
        if (showDebugInfo)
        {
            Debug.Log("UIController: 收到游戏失败事件，显示GameOverPanel");
        }
        
        ShowGameOverPanel();
    }
    
    /// <summary>
    /// 游戏完成事件处理
    /// </summary>
    void OnGameCompleted()
    {
        if (showDebugInfo)
        {
            Debug.Log("UIController: 收到游戏完成事件，显示VictoryPanel");
        }
        
        ShowVictoryPanel();
    }
    
    #endregion
    
    #region 面板显示管理
    
    /// <summary>
    /// 显示面板（通用方法）
    /// </summary>
    /// <param name="panel">要显示的面板</param>
    /// <param name="data">传递给面板的数据</param>
    public void ShowPanel(BasePanel panel, UIPanelData data = null)
    {
        if (panel == null)
        {
            Debug.LogError("UIController: 面板为空，无法显示");
            return;
        }
        
        // 检查是否已经显示（避免重复暂停）
        bool wasVisible = panel.IsVisible;
        
        if (wasVisible && showDebugInfo)
        {
            Debug.LogWarning($"UIController: 面板 {panel.GetType().Name} 已经显示，跳过暂停游戏");
        }
        
        // 处理面板互斥
        HandlePanelExclusivity(panel);
        
        // 显示面板
        panel.OnShow(data);
        
        // 只有在面板从隐藏变为显示时才暂停游戏
        if (!wasVisible && panel.PauseGameOnShow && GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
            
            if (showDebugInfo)
            {
                Debug.Log($"UIController: 面板 {panel.GetType().Name} 需要暂停游戏");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"UIController: 显示面板 - {panel.GetType().Name}");
        }
    }
    
    /// <summary>
    /// 隐藏面板（通用方法）
    /// </summary>
    /// <param name="panel">要隐藏的面板</param>
    public void HidePanel(BasePanel panel)
    {
        if (panel == null)
            return;
        
        // 检查是否已经隐藏（避免重复处理）
        if (!panel.IsVisible)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"UIController: 面板 {panel.GetType().Name} 已经隐藏，跳过");
            }
            return;
        }
        
        // 先根据配置恢复游戏（在 OnHide 设置 IsVisible = false 之前）
        if (panel.PauseGameOnShow && GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
            
            if (showDebugInfo)
            {
                Debug.Log($"UIController: 面板 {panel.GetType().Name} 隐藏，恢复游戏");
            }
        }
        
        // 隐藏面板（会设置 IsVisible = false）
        panel.OnHide();
        
        // 更新当前面板追踪
        if (currentPopupPanel == panel)
            currentPopupPanel = null;
        if (currentFullScreenPanel == panel)
            currentFullScreenPanel = null;
        
        if (showDebugInfo)
        {
            Debug.Log($"UIController: 隐藏面板 - {panel.GetType().Name}");
        }
    }
    
    /// <summary>
    /// 处理面板互斥逻辑
    /// </summary>
    void HandlePanelExclusivity(BasePanel panel)
    {
        switch (panel.PanelType)
        {
            case UIPanelType.Popup:
                // 隐藏之前的Popup面板
                if (currentPopupPanel != null && currentPopupPanel != panel)
                {
                    HidePanel(currentPopupPanel);
                }
                currentPopupPanel = panel;
                break;
            
            case UIPanelType.FullScreen:
                // 隐藏之前的FullScreen面板
                if (currentFullScreenPanel != null && currentFullScreenPanel != panel)
                {
                    HidePanel(currentFullScreenPanel);
                }
                currentFullScreenPanel = panel;
                break;
            
            case UIPanelType.HUD:
            case UIPanelType.Tips:
                // HUD和Tips类型不互斥
                break;
        }
    }
    
    #endregion
    
    #region 具体面板显示方法
    
    /// <summary>
    /// 显示胜利面板
    /// </summary>
    public void ShowVictoryPanel()
    {
        BasePanel victoryPanel = LoadPanel(victoryPanelPath, popupCanvas);
        if (victoryPanel != null)
        {
            ShowPanel(victoryPanel);
        }
    }
    
    /// <summary>
    /// 显示失败面板
    /// </summary>
    public void ShowGameOverPanel()
    {
        BasePanel gameOverPanel = LoadPanel(gameOverPanelPath, popupCanvas);
        if (gameOverPanel != null)
        {
            ShowPanel(gameOverPanel);
        }
    }
    
    /// <summary>
    /// 显示技能状态面板
    /// </summary>
    public void ShowSkillStatusPanel()
    {
        BasePanel skillStatusPanel = LoadPanel(skillStatusPanelPath, popupCanvas);
        if (skillStatusPanel != null)
        {
            ShowPanel(skillStatusPanel);
        }
    }
    
    #endregion
    
    #region 动态加载
    
    /// <summary>
    /// 加载面板（从Resources）
    /// </summary>
    BasePanel LoadPanel(string path, Canvas parentCanvas)
    {
        // 检查缓存
        if (loadedPanels.ContainsKey(path))
        {
            return loadedPanels[path];
        }
        
        // 从Resources加载
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"UIController: 无法加载面板 - {path}");
            return null;
        }
        
        // 实例化
        GameObject panelObj = Instantiate(prefab, parentCanvas.transform);
        BasePanel panel = panelObj.GetComponent<BasePanel>();
        
        if (panel == null)
        {
            Debug.LogError($"UIController: 面板没有BasePanel组件 - {path}");
            Destroy(panelObj);
            return null;
        }
        
        // 初始化并缓存
        // OnInit() 会通过 CanvasGroup 隐藏面板，不需要 SetActive(false)
        panel.OnInit();
        loadedPanels[path] = panel;
        
        if (showDebugInfo)
        {
            Debug.Log($"UIController: 加载面板成功 - {path}");
        }
        
        return panel;
    }
    
    #endregion
    
    #region 状态重置
    
    /// <summary>
    /// 重置UI控制器状态（游戏重启时调用）
    /// </summary>
    public void ResetState()
    {
        // 销毁所有动态加载的面板
        foreach (var panel in loadedPanels.Values)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        loadedPanels.Clear();
        
        // 重置面板追踪
        currentPopupPanel = null;
        currentFullScreenPanel = null;
        
        if (showDebugInfo)
        {
            Debug.Log("UIController: 重置完成 - 动态面板已清理，状态已重置");
        }
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 获取TopBar引用
    /// </summary>
    public TopBarController TopBar => topBarController;
    
    /// <summary>
    /// 获取SkillSelectionUI引用
    /// </summary>
    public SkillSelectionUI SkillSelectionUI => skillSelectionUI;
    
    #endregion
}
