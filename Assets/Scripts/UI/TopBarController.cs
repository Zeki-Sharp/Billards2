using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TopBar控制器 - 管理屏幕顶部常驻UI
/// 
/// 【核心职责】：
/// - 显示玩家血条
/// - 监听血量变化更新UI
/// - 控制TopBar的显示/隐藏（角色选择后显示）
/// - 管理技能按钮（后续添加）
/// </summary>
public class TopBarController : MonoBehaviour
{
    [Header("血条UI元素")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private TextMeshProUGUI totalHealthText;
    
    [Header("技能按钮")]
    [SerializeField] private UnityEngine.UI.Button skillButton;
    
    [Header("显示控制")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Awake()
    {
        // 获取CanvasGroup组件
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        // 初始隐藏TopBar
        SetVisible(false);
    }
    
    void Start()
    {
        // 订阅事件
        SubscribeToEvents();
        
        // 设置按钮事件
        SetupButtonEvents();
        
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 初始化完成，初始状态：隐藏");
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // 移除按钮事件
        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(OnSkillButtonClicked);
        }
    }
    
    #region 事件订阅
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        // 订阅血量变化事件
        GameEventBus.OnHealthChanged += OnHealthChanged;
        
        // 订阅角色选择完成事件（显示TopBar）
        CharacterSelectionManager.OnStartGame += OnGameStarted;
        
        // 订阅游戏重启事件（隐藏TopBar）
        GameEventBus.OnGameRestart += OnGameRestart;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnHealthChanged -= OnHealthChanged;
        CharacterSelectionManager.OnStartGame -= OnGameStarted;
        GameEventBus.OnGameRestart -= OnGameRestart;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 游戏开始事件处理（角色选择完成）
    /// </summary>
    void OnGameStarted(PlayerData character)
    {
        if (showDebugInfo)
        {
            Debug.Log($"TopBarController: 角色选择完成，显示TopBar - {character.playerName}");
        }
        
        // 显示TopBar
        SetVisible(true);
    }
    
    /// <summary>
    /// 游戏重启事件处理
    /// </summary>
    void OnGameRestart()
    {
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 游戏重启，隐藏TopBar");
        }
        
        // 隐藏TopBar
        SetVisible(false);
    }
    
    /// <summary>
    /// 血量变化事件处理
    /// </summary>
    void OnHealthChanged(HealthStateData healthData)
    {
        UpdateHealthDisplay(healthData.CurrentHealth, healthData.MaxHealth);
    }
    
    #endregion
    
    #region 血量显示
    
    /// <summary>
    /// 更新血量显示
    /// </summary>
    void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        // 更新血条填充
        if (healthBarFill != null)
        {
            float fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0;
            healthBarFill.fillAmount = fillAmount;
        }
        
        // 更新当前血量文本
        if (currentHealthText != null)
        {
            currentHealthText.text = Mathf.CeilToInt(currentHealth).ToString();
        }
        
        // 更新最大血量文本
        if (totalHealthText != null)
        {
            totalHealthText.text = Mathf.CeilToInt(maxHealth).ToString();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TopBarController: 血量更新 - {currentHealth}/{maxHealth}");
        }
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtonEvents()
    {
        if (skillButton != null)
        {
            skillButton.onClick.AddListener(OnSkillButtonClicked);
            
            if (showDebugInfo)
            {
                Debug.Log("TopBarController: 技能按钮事件已设置");
            }
        }
        else
        {
            Debug.LogWarning("TopBarController: 技能按钮未配置！");
        }
    }
    
    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    void OnSkillButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 技能按钮被点击");
        }
        
        // 通过UIController显示技能状态面板
        if (UIController.Instance != null)
        {
            // 暂时打印日志，稍后实现SkillStatusPanel
            Debug.Log("TopBarController: 准备打开技能状态面板（待实现）");
            // UIController.Instance.ShowPanel("SkillStatusPanel");
        }
        else
        {
            Debug.LogError("TopBarController: UIController.Instance 为空！");
        }
    }
    
    #endregion
    
    #region 显示控制
    
    /// <summary>
    /// 设置TopBar可见性
    /// </summary>
    void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            // 备用方案：直接控制GameObject
            gameObject.SetActive(visible);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TopBarController: 设置可见性 - {visible}");
        }
    }
    
    /// <summary>
    /// 显示TopBar
    /// </summary>
    public void Show()
    {
        SetVisible(true);
    }
    
    /// <summary>
    /// 隐藏TopBar
    /// </summary>
    public void Hide()
    {
        SetVisible(false);
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("显示TopBar")]
    void DebugShow()
    {
        Show();
    }
    
    [ContextMenu("隐藏TopBar")]
    void DebugHide()
    {
        Hide();
    }
    
    [ContextMenu("显示TopBar状态")]
    void ShowStatus()
    {
        bool isVisible = canvasGroup != null ? canvasGroup.alpha > 0 : gameObject.activeSelf;
        Debug.Log($"TopBarController 状态:\n" +
                 $"可见: {isVisible}\n" +
                 $"Canvas Group: {(canvasGroup != null ? "已配置" : "未配置")}\n" +
                 $"Health Bar Fill: {(healthBarFill != null ? "已配置" : "未配置")}\n" +
                 $"Current Health Text: {(currentHealthText != null ? "已配置" : "未配置")}\n" +
                 $"Total Health Text: {(totalHealthText != null ? "已配置" : "未配置")}\n" +
                 $"Skill Button: {(skillButton != null ? "已配置" : "未配置")}");
    }
    
    #endregion
}

