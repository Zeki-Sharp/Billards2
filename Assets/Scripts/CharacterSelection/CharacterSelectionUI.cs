using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色选择UI控制器 - 管理角色选择界面的UI显示和交互
/// </summary>
public class CharacterSelectionUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Transform characterButtonsContainer;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI startGameButtonText;
    [SerializeField] private TextMeshProUGUI selectedCharacterText;
    [SerializeField] private TextMeshProUGUI selectedCountText; // ✅ 新增：已选角色计数显示
    [SerializeField] private Button backButton;
    
    [Header("UI设置")]
    [SerializeField] private string titleTextContent = "选择角色";
    [SerializeField] private string instructionTextContent = "请选择你要使用的角色";
    [SerializeField] private string startGameButtonTextContent = "开始游戏";
    [SerializeField] private string noCharacterSelectedText = "请选择角色";
    
    [Header("动画设置")]
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float buttonAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private CharacterSelectionManager selectionManager;
    
    // 动画相关
    private Coroutine currentAnimation;
    
    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    void InitializeUI()
    {
        // 获取选择管理器
        selectionManager = GetComponent<CharacterSelectionManager>();
        if (selectionManager == null)
        {
            selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
        }
        
        if (selectionManager == null)
        {
            Debug.LogError("CharacterSelectionUI: 未找到 CharacterSelectionManager！");
            return;
        }
        
        // 设置UI文本
        SetupUITexts();
        
        // 设置按钮事件
        SetupButtons();
        
        // 初始化UI状态
        UpdateUIState();
        
        if (showDebugInfo)
        {
            Debug.Log("CharacterSelectionUI: 初始化完成");
        }
    }
    
    /// <summary>
    /// 设置UI文本
    /// </summary>
    void SetupUITexts()
    {
        if (titleText != null)
        {
            titleText.text = titleTextContent;
        }
        
        if (instructionText != null)
        {
            instructionText.text = instructionTextContent;
        }
        
        if (startGameButtonText != null)
        {
            startGameButtonText.text = startGameButtonTextContent;
        }
        
        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = noCharacterSelectedText;
        }
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtons()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        CharacterSelectionManager.OnCharacterSelected += OnCharacterSelected;
        CharacterSelectionManager.OnCharacterDeselected += OnCharacterDeselected; // ✅ 订阅取消选择事件
        CharacterSelectionManager.OnStartGame += OnStartGame;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        CharacterSelectionManager.OnCharacterSelected -= OnCharacterSelected;
        CharacterSelectionManager.OnCharacterDeselected -= OnCharacterDeselected; // ✅ 取消订阅
        CharacterSelectionManager.OnStartGame -= OnStartGame;
    }
    
    /// <summary>
    /// 角色选择事件处理（多选模式）
    /// </summary>
    void OnCharacterSelected(PlayerData characterData, int positionIndex)
    {
        if (characterData == null) return;
        
        // ✅ 更新UI状态（包括已选计数）
        UpdateUIState();
        
        // 播放选择动画
        if (enableAnimations)
        {
            PlaySelectionAnimation();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionUI: 角色选择 [{positionIndex}号位] - {characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 角色取消选择事件处理
    /// </summary>
    void OnCharacterDeselected(PlayerData characterData)
    {
        if (characterData == null) return;
        
        // ✅ 更新UI状态（包括已选计数）
        UpdateUIState();
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionUI: 取消选择 - {characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 开始游戏事件处理（多选模式）
    /// </summary>
    void OnStartGame(System.Collections.Generic.List<PlayerData> selectedCharacters)
    {
        if (selectedCharacters == null || selectedCharacters.Count == 0) return;
        
        // 播放开始游戏动画
        if (enableAnimations)
        {
            PlayStartGameAnimation();
        }
        
        if (showDebugInfo)
        {
            string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
            Debug.Log($"CharacterSelectionUI: 开始游戏 - 队伍: {characterNames}");
        }
    }
    
    /// <summary>
    /// 更新UI状态（多选模式）
    /// </summary>
    void UpdateUIState()
    {
        if (selectionManager == null) return;
        
        bool hasSelectedFullTeam = selectionManager.HasSelectedFullTeam();
        int selectedCount = selectionManager.GetSelectedCount();
        
        // 更新开始游戏按钮状态
        if (startGameButton != null)
        {
            startGameButton.interactable = hasSelectedFullTeam;
        }
        
        // ✅ 更新已选角色计数显示（独立的Text）
        if (selectedCountText != null)
        {
            selectedCountText.text = $"已选角色：{selectedCount}/{TeamData.TEAM_SIZE}";
        }
        
        // 更新选中角色文本（显示详细信息）
        if (selectedCharacterText != null)
        {
            if (selectedCount == 0)
            {
                selectedCharacterText.text = noCharacterSelectedText;
            }
            else
            {
                var selectedCharacters = selectionManager.GetSelectedCharacters();
                string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
                selectedCharacterText.text = $"已选择: {characterNames}";
            }
        }
    }
    
    /// <summary>
    /// 播放选择动画
    /// </summary>
    void PlaySelectionAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(AnimateSelectedCharacterText());
    }
    
    /// <summary>
    /// 播放开始游戏动画
    /// </summary>
    void PlayStartGameAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(AnimateStartGameButton());
    }
    
    /// <summary>
    /// 选中角色文本动画
    /// </summary>
    System.Collections.IEnumerator AnimateSelectedCharacterText()
    {
        if (selectedCharacterText == null) yield break;
        
        Vector3 originalScale = selectedCharacterText.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float elapsed = 0f;
        
        // 放大
        while (elapsed < buttonAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (buttonAnimationDuration / 2);
            float curveValue = buttonAnimationCurve.Evaluate(progress);
            selectedCharacterText.transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            yield return null;
        }
        
        // 缩小回原尺寸
        elapsed = 0f;
        while (elapsed < buttonAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (buttonAnimationDuration / 2);
            float curveValue = buttonAnimationCurve.Evaluate(progress);
            selectedCharacterText.transform.localScale = Vector3.Lerp(targetScale, originalScale, curveValue);
            yield return null;
        }
        
        selectedCharacterText.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 开始游戏按钮动画
    /// </summary>
    System.Collections.IEnumerator AnimateStartGameButton()
    {
        if (startGameButton == null) yield break;
        
        Vector3 originalScale = startGameButton.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float elapsed = 0f;
        
        // 放大
        while (elapsed < buttonAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (buttonAnimationDuration / 2);
            float curveValue = buttonAnimationCurve.Evaluate(progress);
            startGameButton.transform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            yield return null;
        }
        
        // 缩小回原尺寸
        elapsed = 0f;
        while (elapsed < buttonAnimationDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (buttonAnimationDuration / 2);
            float curveValue = buttonAnimationCurve.Evaluate(progress);
            startGameButton.transform.localScale = Vector3.Lerp(targetScale, originalScale, curveValue);
            yield return null;
        }
        
        startGameButton.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    void OnStartGameButtonClicked()
    {
        if (selectionManager != null)
        {
            selectionManager.OnStartGameClicked();
        }
    }
    
    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    void OnBackButtonClicked()
    {
        Debug.Log("CharacterSelectionUI: 返回按钮点击");
        // 功能待实现
    }
    
    /// <summary>
    /// 显示/隐藏UI
    /// </summary>
    public void SetUIVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 重置UI状态
    /// </summary>
    public void ResetUI()
    {
        if (selectionManager != null)
        {
            selectionManager.ResetSelection();
        }
        
        UpdateUIState();
        
        if (showDebugInfo)
        {
            Debug.Log("CharacterSelectionUI: UI状态已重置");
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        UnsubscribeFromEvents();
        
        // 取消按钮事件
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }
    
    #region 调试方法
    
    [ContextMenu("更新UI状态")]
    void UpdateUIStateDebug()
    {
        UpdateUIState();
        Debug.Log("CharacterSelectionUI: 手动更新UI状态");
    }
    
    [ContextMenu("重置UI")]
    void ResetUIDebug()
    {
        ResetUI();
    }
    
    [ContextMenu("显示调试信息")]
    void ShowDebugInfo()
    {
        Debug.Log($"CharacterSelectionUI 调试信息:\n" +
                 $"选择管理器: {(selectionManager != null ? "已连接" : "未连接")}\n" +
                 $"标题文本: {(titleText != null ? "已配置" : "未配置")}\n" +
                 $"说明文本: {(instructionText != null ? "已配置" : "未配置")}\n" +
                 $"按钮容器: {(characterButtonsContainer != null ? "已配置" : "未配置")}\n" +
                 $"开始游戏按钮: {(startGameButton != null ? "已配置" : "未配置")}\n" +
                 $"已选计数文本: {(selectedCountText != null ? "已配置" : "未配置")}\n" +
                 $"选中角色文本: {(selectedCharacterText != null ? "已配置" : "未配置")}");
    }
    
    #endregion
}
