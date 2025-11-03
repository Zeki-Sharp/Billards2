using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TopBar控制器 - 管理屏幕顶部常驻UI（容器控制器）
/// 
/// 【核心职责】：
/// - 显示玩家血条
/// - 控制TopBar的显示/隐藏（角色选择后显示）
/// - 管理技能按钮和设置按钮
/// - 管理子组件引用
/// 
/// 【子组件】：
/// - RemainingTurnsDisplay: 剩余回合数显示（独立组件）
/// 
/// 【设计原则】：
/// - 容器职责：只负责整体显示和按钮管理
/// - 业务逻辑：由子组件独立处理
/// - 松耦合：子组件自我管理
/// </summary>
public class TopBarController : MonoBehaviour
{
    [Header("✅ 多角色系统 - 队伍状态显示")]
    [Tooltip("1号位角色的UI槽位组件")]
    [SerializeField] private CharacterSlotUI slot1;
    
    [Tooltip("2号位角色的UI槽位组件")]
    [SerializeField] private CharacterSlotUI slot2;
    
    [Tooltip("3号位角色的UI槽位组件")]
    [SerializeField] private CharacterSlotUI slot3;
    
    // ⚠️ 旧版单角色UI（保留兼容，建议使用上面的槽位）
    [Header("旧版单角色UI（不推荐）")]
    [SerializeField] private Image playerPortrait;  // 角色头像
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI currentHealthText;
    [SerializeField] private TextMeshProUGUI totalHealthText;
    
    [Header("子组件")]
    [SerializeField] 
    [Tooltip("剩余回合数显示组件（独立管理）")]
    private RemainingTurnsDisplay turnsDisplay;
    
    [Header("按钮")]
    [SerializeField] private UnityEngine.UI.Button skillButton;      // 技能按钮（书本图标）
    [SerializeField] private UnityEngine.UI.Button settingsButton;   // 设置按钮（齿轮图标）
    
    [Header("显示控制")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 槽位列表（方便遍历）
    private System.Collections.Generic.List<CharacterSlotUI> slots = new System.Collections.Generic.List<CharacterSlotUI>();
    
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
        // ✅ 初始化槽位列表
        InitializeSlots();
        
        // 订阅事件
        SubscribeToEvents();
        
        // 设置按钮事件
        SetupButtonEvents();
        
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 初始化完成，初始状态：隐藏");
        }
    }
    
    /// <summary>
    /// ✅ 多角色系统：初始化槽位列表
    /// </summary>
    void InitializeSlots()
    {
        slots.Clear();
        if (slot1 != null) slots.Add(slot1);
        if (slot2 != null) slots.Add(slot2);
        if (slot3 != null) slots.Add(slot3);
        
        if (showDebugInfo)
        {
            Debug.Log($"[TopBarController] 初始化 {slots.Count} 个角色槽位");
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
        
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        }
    }
    
    #region 事件订阅
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        // ✅ 多角色系统：订阅角色特定事件
        GameEventBus.OnCharacterSelected += OnCharacterSelected;
        GameEventBus.OnCharacterDeselected += OnCharacterDeselected;
        GameEventBus.OnCharacterDamaged += OnCharacterDamaged;
        GameEventBus.OnCharacterHealed += OnCharacterHealed;
        GameEventBus.OnCharacterDied += OnCharacterDied;
        
        // 旧版单角色事件（向后兼容）
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
        // ✅ 多角色系统事件
        GameEventBus.OnCharacterSelected -= OnCharacterSelected;
        GameEventBus.OnCharacterDeselected -= OnCharacterDeselected;
        GameEventBus.OnCharacterDamaged -= OnCharacterDamaged;
        GameEventBus.OnCharacterHealed -= OnCharacterHealed;
        GameEventBus.OnCharacterDied -= OnCharacterDied;
        
        // 旧版事件
        GameEventBus.OnHealthChanged -= OnHealthChanged;
        CharacterSelectionManager.OnStartGame -= OnGameStarted;
        GameEventBus.OnGameRestart -= OnGameRestart;
    }
    
    #endregion
    
    #region 事件处理
    
    #region 多角色系统事件
    
    /// <summary>
    /// ✅ 多角色系统：角色被选中
    /// </summary>
    void OnCharacterSelected(string characterID)
    {
        UpdateSelectionHighlight(characterID, true);
    }
    
    /// <summary>
    /// ✅ 多角色系统：角色被取消选中
    /// </summary>
    void OnCharacterDeselected(string characterID)
    {
        UpdateSelectionHighlight(characterID, false);
    }
    
    /// <summary>
    /// ✅ 多角色系统：角色受伤
    /// </summary>
    void OnCharacterDamaged(string characterID, float damage, string sourceID)
    {
        UpdateCharacterHealth(characterID);
    }
    
    /// <summary>
    /// ✅ 多角色系统：角色治疗
    /// </summary>
    void OnCharacterHealed(string characterID, float amount)
    {
        UpdateCharacterHealth(characterID);
    }
    
    /// <summary>
    /// ✅ 多角色系统：角色死亡
    /// </summary>
    void OnCharacterDied(string characterID)
    {
        UpdateCharacterDeath(characterID);
    }
    
    #endregion
    
    /// <summary>
    /// 游戏开始事件处理（角色选择完成）- 多角色模式
    /// </summary>
    void OnGameStarted(System.Collections.Generic.List<PlayerData> selectedCharacters)
    {
        if (selectedCharacters == null || selectedCharacters.Count == 0)
        {
            Debug.LogWarning("TopBarController: 未选择任何角色！");
            return;
        }
        
        if (showDebugInfo)
        {
            string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
            Debug.Log($"[TopBarController] ✅ 角色选择完成，显示TopBar - 队伍: {characterNames}");
        }
        
        // ✅ 刷新所有角色槽位
        RefreshAllSlots();
        
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
            Debug.Log("TopBarController: 游戏重启，隐藏TopBar并重置显示");
        }
        
        // 重置角色头像
        if (playerPortrait != null)
        {
            playerPortrait.sprite = null;
            playerPortrait.enabled = false;
        }
        
        // 重置血量显示为空（避免显示旧数据）
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f; // 重置为满血状态
        }
        
        if (currentHealthText != null)
        {
            currentHealthText.text = "0";
        }
        
        if (totalHealthText != null)
        {
            totalHealthText.text = "0";
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
    
    #region 角色信息显示
    
    /// <summary>
    /// 更新角色头像
    /// </summary>
    void UpdatePlayerPortrait(PlayerData character)
    {
        if (playerPortrait != null && character != null)
        {
            if (character.info.icon != null)
            {
                playerPortrait.sprite = character.info.icon;
                playerPortrait.enabled = true;
                playerPortrait.gameObject.SetActive(true);
                
                if (showDebugInfo)
                {
                    Debug.Log($"TopBarController: 角色头像已更新 - {character.info.name}");
                }
            }
            else
            {
                playerPortrait.enabled = false;
                Debug.LogWarning($"TopBarController: 角色 {character.info.name} 没有配置头像");
            }
        }
        else if (playerPortrait == null)
        {
            Debug.LogWarning("TopBarController: playerPortrait 未配置！请在 Inspector 中拖入头像 Image 组件");
        }
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
        // 设置技能按钮
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
        
        // 设置设置按钮
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            
            if (showDebugInfo)
            {
                Debug.Log("TopBarController: 设置按钮事件已设置");
            }
        }
        else
        {
            Debug.LogWarning("TopBarController: 设置按钮未配置！");
        }
    }
    
    /// <summary>
    /// 技能按钮点击事件
    /// </summary>
    void OnSkillButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 技能按钮被点击，打开技能状态面板");
        }
        
        // 通过UIController显示技能状态面板
        if (UIController.Instance != null)
        {
            UIController.Instance.ShowSkillStatusPanel();
        }
        else
        {
            Debug.LogError("TopBarController: UIController.Instance 为空！");
        }
    }
    
    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    void OnSettingsButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("TopBarController: 设置按钮被点击，打开设置面板");
        }
        
        // 通过UIController显示设置面板
        if (UIController.Instance != null)
        {
            UIController.Instance.ShowSettingsPanel();
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
                 $"Player Portrait: {(playerPortrait != null ? "已配置" : "未配置")}\n" +
                 $"Health Bar Fill: {(healthBarFill != null ? "已配置" : "未配置")}\n" +
                 $"Current Health Text: {(currentHealthText != null ? "已配置" : "未配置")}\n" +
                 $"Total Health Text: {(totalHealthText != null ? "已配置" : "未配置")}\n" +
                 $"Turns Display: {(turnsDisplay != null ? "已配置" : "未配置")}\n" +
                 $"Skill Button: {(skillButton != null ? "已配置" : "未配置")}\n" +
                 $"Settings Button: {(settingsButton != null ? "已配置" : "未配置")}");
    }
    
    #endregion
    
    #region 多角色UI更新方法
    
    /// <summary>
    /// ✅ 刷新所有槽位
    /// </summary>
    void RefreshAllSlots()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[TopBarController] TeamData 为空，无法刷新UI");
            }
            return;
        }
        
        for (int i = 0; i < slots.Count && i < teamData.characters.Count; i++)
        {
            var character = teamData.characters[i];
            var slot = slots[i];
            
            if (slot != null)
            {
                slot.UpdateCharacterInfo(character);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[TopBarController] 刷新槽位 {i + 1}: {character.characterData?.info.name ?? "未知"}");
                }
            }
        }
    }
    
    /// <summary>
    /// ✅ 更新选中高亮
    /// </summary>
    void UpdateSelectionHighlight(string characterID, bool isSelected)
    {
        var slot = FindSlotByCharacterID(characterID);
        if (slot != null)
        {
            slot.SetHighlight(isSelected);
        }
    }
    
    /// <summary>
    /// ✅ 更新角色血量显示
    /// </summary>
    void UpdateCharacterHealth(string characterID)
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return;
        
        var character = teamData.characters.Find(c => c.characterID == characterID);
        if (character == null) return;
        
        var slot = FindSlotByCharacterID(characterID);
        if (slot != null)
        {
            slot.UpdateHealth(character.currentHealth, character.maxHealth);
        }
    }
    
    /// <summary>
    /// ✅ 更新角色死亡状态
    /// </summary>
    void UpdateCharacterDeath(string characterID)
    {
        var slot = FindSlotByCharacterID(characterID);
        if (slot != null)
        {
            slot.SetDead(true);
        }
    }
    
    /// <summary>
    /// ✅ 根据角色ID查找槽位
    /// </summary>
    CharacterSlotUI FindSlotByCharacterID(string characterID)
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return null;
        
        for (int i = 0; i < teamData.characters.Count && i < slots.Count; i++)
        {
            if (teamData.characters[i].characterID == characterID)
            {
                return slots[i];
            }
        }
        
        return null;
    }
    
    #endregion
}
