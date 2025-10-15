using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色选择管理器 - 管理角色选择界面的逻辑和状态
/// </summary>
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private CharacterSelectionData selectionData;
    
    [Header("UI引用")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI selectedCharacterText;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 状态管理
    private PlayerData selectedCharacter;
    private List<CharacterButton> characterButtons = new List<CharacterButton>();
    
    // 事件
    public static event Action<PlayerData> OnCharacterSelected;
    public static event Action<PlayerData> OnStartGame;
    
    void Start()
    {
        InitializeCharacterSelection();
    }
    
    /// <summary>
    /// 初始化角色选择界面
    /// </summary>
    void InitializeCharacterSelection()
    {
        // 验证配置
        if (selectionData == null)
        {
            Debug.LogError("CharacterSelectionManager: 未配置 CharacterSelectionData！");
            return;
        }
        
        if (!selectionData.IsValid())
        {
            Debug.LogError("CharacterSelectionManager: CharacterSelectionData 配置无效！");
            return;
        }
        
        // 设置按钮容器
        if (buttonContainer == null)
        {
            buttonContainer = selectionData.buttonContainer;
        }
        
        if (buttonContainer == null)
        {
            Debug.LogError("CharacterSelectionManager: 按钮容器未配置！");
            return;
        }
        
        // 创建角色按钮
        CreateCharacterButtons();
        
        // 设置开始游戏按钮
        SetupStartGameButton();
        
        // 初始化UI状态
        UpdateUIState();
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 初始化完成，共创建 {characterButtons.Count} 个角色按钮");
        }
    }
    
    /// <summary>
    /// 创建角色按钮
    /// </summary>
    void CreateCharacterButtons()
    {
        // 清理现有按钮
        ClearExistingButtons();
        
        // 获取角色列表
        var characters = selectionData.GetAvailableCharacters();
        
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("CharacterSelectionManager: 没有可用角色！");
            return;
        }
        
        // 为每个角色创建按钮
        foreach (var characterData in characters)
        {
            if (characterData == null)
            {
                Debug.LogWarning("CharacterSelectionManager: 发现空的角色数据，跳过");
                continue;
            }
            
            CreateCharacterButton(characterData);
        }
    }
    
    /// <summary>
    /// 创建单个角色按钮
    /// </summary>
    void CreateCharacterButton(PlayerData characterData)
    {
        if (selectionData.characterButtonPrefab == null)
        {
            Debug.LogError("CharacterSelectionManager: 按钮预制体未配置！");
            return;
        }
        
        // 实例化按钮
        GameObject buttonObj = Instantiate(selectionData.characterButtonPrefab, buttonContainer);
        
        // 获取按钮组件
        CharacterButton characterButton = buttonObj.GetComponent<CharacterButton>();
        if (characterButton == null)
        {
            Debug.LogError("CharacterSelectionManager: 按钮预制体缺少 CharacterButton 组件！");
            Destroy(buttonObj);
            return;
        }
        
        // 设置按钮数据
        characterButton.SetCharacterData(characterData, this);
        
        // 添加到列表
        characterButtons.Add(characterButton);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 创建角色按钮 - {characterData.playerName}");
        }
    }
    
    /// <summary>
    /// 清理现有按钮
    /// </summary>
    void ClearExistingButtons()
    {
        foreach (var button in characterButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        characterButtons.Clear();
    }
    
    /// <summary>
    /// 设置开始游戏按钮
    /// </summary>
    void SetupStartGameButton()
    {
        if (startGameButton == null)
        {
            Debug.LogWarning("CharacterSelectionManager: 开始游戏按钮未配置");
            return;
        }
        
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }
    
    /// <summary>
    /// 选择角色
    /// </summary>
    public void SelectCharacter(PlayerData characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("CharacterSelectionManager: 角色数据为空！");
            return;
        }
        
        // 更新选中状态
        selectedCharacter = characterData;
        
        // 更新所有按钮的选中状态
        UpdateButtonSelectionStates();
        
        // 更新UI状态
        UpdateUIState();
        
        // 触发事件
        OnCharacterSelected?.Invoke(selectedCharacter);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 选择角色 - {selectedCharacter.playerName}");
        }
    }
    
    /// <summary>
    /// 更新按钮选中状态
    /// </summary>
    void UpdateButtonSelectionStates()
    {
        foreach (var button in characterButtons)
        {
            if (button != null)
            {
                bool isSelected = button.GetCharacterData() == selectedCharacter;
                button.SetSelected(isSelected);
            }
        }
    }
    
    /// <summary>
    /// 更新UI状态
    /// </summary>
    void UpdateUIState()
    {
        // 更新选中角色文本
        if (selectedCharacterText != null)
        {
            if (selectedCharacter != null)
            {
                selectedCharacterText.text = $"已选择: {selectedCharacter.playerName}";
            }
            else
            {
                selectedCharacterText.text = "请选择角色";
            }
        }
        
        // 更新开始游戏按钮状态
        if (startGameButton != null)
        {
            startGameButton.interactable = selectedCharacter != null;
        }
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    public void OnStartGameClicked()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("CharacterSelectionManager: 未选择角色，无法开始游戏");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 开始游戏 - {selectedCharacter.playerName}");
        }
        
        // 触发开始游戏事件
        OnStartGame?.Invoke(selectedCharacter);
        
        // 设置选中的角色数据到SceneTransitionManager
        SceneTransitionManager.SetSelectedCharacter(selectedCharacter);
        
        // 加载Level1场景
        LoadLevel1Scene();
    }
    
    /// <summary>
    /// 加载Level1场景
    /// </summary>
    void LoadLevel1Scene()
    {
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 准备加载Level1场景，选中角色: {selectedCharacter.playerName}");
        }
        
        // 获取SceneTransitionManager实例
        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        
        if (transitionManager != null)
        {
            // 使用SceneTransitionManager加载场景
            transitionManager.LoadLevel1();
        }
        else
        {
            Debug.LogError("CharacterSelectionManager: 未找到SceneTransitionManager实例！");
            
            // 备用方案：直接加载场景（不推荐，但作为fallback）
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Level1");
        }
    }
    
    /// <summary>
    /// 获取当前选中的角色
    /// </summary>
    public PlayerData GetSelectedCharacter()
    {
        return selectedCharacter;
    }
    
    /// <summary>
    /// 检查是否已选择角色
    /// </summary>
    public bool HasSelectedCharacter()
    {
        return selectedCharacter != null;
    }
    
    /// <summary>
    /// 重置选择
    /// </summary>
    public void ResetSelection()
    {
        selectedCharacter = null;
        UpdateButtonSelectionStates();
        UpdateUIState();
        
        if (showDebugInfo)
        {
            Debug.Log("CharacterSelectionManager: 重置角色选择");
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }
    }
    
    #region 调试方法
    
    [ContextMenu("重新初始化界面")]
    void ReinitializeUI()
    {
        InitializeCharacterSelection();
    }
    
    [ContextMenu("显示调试信息")]
    void ShowDebugInfo()
    {
        Debug.Log($"CharacterSelectionManager 调试信息:\n" +
                 $"配置数据: {(selectionData != null ? "已配置" : "未配置")}\n" +
                 $"按钮容器: {(buttonContainer != null ? "已配置" : "未配置")}\n" +
                 $"开始游戏按钮: {(startGameButton != null ? "已配置" : "未配置")}\n" +
                 $"角色按钮数量: {characterButtons.Count}\n" +
                 $"选中角色: {(selectedCharacter != null ? selectedCharacter.playerName : "无")}");
    }
    
    #endregion
}
