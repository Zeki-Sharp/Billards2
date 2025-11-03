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
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI selectedCharacterText;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 状态管理 - 多选模式
    private List<PlayerData> selectedCharacters = new List<PlayerData>(TeamData.TEAM_SIZE);
    private List<CharacterButton> characterButtons = new List<CharacterButton>();
    
    // 事件
    public static event Action<PlayerData, int> OnCharacterSelected; // 参数：角色数据, 位置索引
    public static event Action<PlayerData> OnCharacterDeselected;
    public static event Action<List<PlayerData>> OnStartGame; // 改为传递角色列表
    
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
        
        // 验证UI组件配置
        if (characterButtonPrefab == null)
        {
            Debug.LogError("CharacterSelectionManager: 角色按钮预制体未配置！");
            return;
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
        // 实例化按钮
        GameObject buttonObj = Instantiate(characterButtonPrefab, buttonContainer);
        
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
            Debug.Log($"CharacterSelectionManager: 创建角色按钮 - {characterData.info.name}");
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
    /// 选择角色（多选模式）
    /// </summary>
    public void SelectCharacter(PlayerData characterData)
    {
        if (characterData == null)
        {
            Debug.LogError("CharacterSelectionManager: 角色数据为空！");
            return;
        }
        
        // 检查是否已选中（如果已选中则取消选中）
        if (selectedCharacters.Contains(characterData))
        {
            DeselectCharacter(characterData);
            return;
        }
        
        // 检查是否已满（最多3个）
        if (selectedCharacters.Count >= TeamData.TEAM_SIZE)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CharacterSelectionManager: 已选择{TeamData.TEAM_SIZE}个角色，无法继续添加");
            }
            return;
        }
        
        // 添加到选中列表
        selectedCharacters.Add(characterData);
        int positionIndex = selectedCharacters.Count; // 1-based
        
        // 更新所有按钮的选中状态
        UpdateButtonSelectionStates();
        
        // 更新UI状态
        UpdateUIState();
        
        // 触发事件
        OnCharacterSelected?.Invoke(characterData, positionIndex);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 选择角色 [{positionIndex}号位] - {characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 取消选择角色
    /// </summary>
    public void DeselectCharacter(PlayerData characterData)
    {
        if (characterData == null || !selectedCharacters.Contains(characterData))
        {
            return;
        }
        
        selectedCharacters.Remove(characterData);
        
        // 更新所有按钮的选中状态
        UpdateButtonSelectionStates();
        
        // 更新UI状态
        UpdateUIState();
        
        // 触发事件
        OnCharacterDeselected?.Invoke(characterData);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionManager: 取消选择角色 - {characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 更新按钮选中状态（多选模式）
    /// </summary>
    void UpdateButtonSelectionStates()
    {
        foreach (var button in characterButtons)
        {
            if (button != null)
            {
                bool isSelected = selectedCharacters.Contains(button.GetCharacterData());
                button.SetSelected(isSelected);
            }
        }
    }
    
    /// <summary>
    /// 更新UI状态（多选模式）
    /// </summary>
    void UpdateUIState()
    {
        // 更新选中角色文本
        if (selectedCharacterText != null)
        {
            if (selectedCharacters.Count == 0)
            {
                selectedCharacterText.text = $"请选择角色 (0/{TeamData.TEAM_SIZE})";
            }
            else if (selectedCharacters.Count < TeamData.TEAM_SIZE)
            {
                string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
                selectedCharacterText.text = $"已选择 ({selectedCharacters.Count}/{TeamData.TEAM_SIZE}): {characterNames}";
            }
            else
            {
                string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
                selectedCharacterText.text = $"队伍已满 ({selectedCharacters.Count}/{TeamData.TEAM_SIZE}): {characterNames}";
            }
        }
        
        // 更新开始游戏按钮状态（只有选满3个才能开始）
        if (startGameButton != null)
        {
            startGameButton.interactable = selectedCharacters.Count == TeamData.TEAM_SIZE;
        }
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件（多选模式）
    /// </summary>
    public void OnStartGameClicked()
    {
        if (selectedCharacters.Count != TeamData.TEAM_SIZE)
        {
            Debug.LogWarning($"CharacterSelectionManager: 未选满{TeamData.TEAM_SIZE}个角色，无法开始游戏");
            return;
        }
        
        if (showDebugInfo)
        {
            string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
            Debug.Log($"CharacterSelectionManager: 开始游戏 - 队伍: {characterNames}");
        }
        
        // 触发开始游戏事件
        OnStartGame?.Invoke(selectedCharacters);
        
        // 创建队伍数据并保存到 GameSession
        CreateAndSaveTeamData();
        
        // 加载地图场景
        LoadMapScene();
    }
    
    /// <summary>
    /// 创建队伍数据并保存到 GameSession
    /// </summary>
    void CreateAndSaveTeamData()
    {
        // 创建队伍数据
        TeamData teamData = new TeamData(selectedCharacters);
        
        // 保存到 GameSession
        var session = GameSession.GetOrCreateInstance();
        if (session != null)
        {
            session.SetTeamData(teamData);
            
            if (showDebugInfo)
            {
                Debug.Log($"CharacterSelectionManager: 队伍数据已保存到 GameSession");
                teamData.PrintDebugInfo();
            }
        }
        else
        {
            Debug.LogError("CharacterSelectionManager: 无法获取 GameSession！");
        }
    }
    
    /// <summary>
    /// 加载地图场景（使用地图系统）
    /// </summary>
    void LoadMapScene()
    {
        if (showDebugInfo)
        {
            string characterNames = string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name));
            Debug.Log($"CharacterSelectionManager: 准备加载地图场景，队伍: {characterNames}");
        }
        
        // ✅ 清除地图系统标记（确保MapSceneController识别为首次进入）
        var session = GameSession.GetOrCreateInstance();
        if (session != null)
        {
            session.State.ClearMapSystemFlag();
        }
        
        // 加载MapScene（MapSceneController会处理新地图生成）
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }
    
    /// <summary>
    /// 获取当前选中的角色列表
    /// </summary>
    public List<PlayerData> GetSelectedCharacters()
    {
        return new List<PlayerData>(selectedCharacters);
    }
    
    /// <summary>
    /// 检查是否已选满角色
    /// </summary>
    public bool HasSelectedFullTeam()
    {
        return selectedCharacters.Count == TeamData.TEAM_SIZE;
    }
    
    /// <summary>
    /// 检查角色是否已被选中
    /// </summary>
    public bool IsCharacterSelected(PlayerData characterData)
    {
        return selectedCharacters.Contains(characterData);
    }
    
    /// <summary>
    /// 获取已选角色数量
    /// </summary>
    public int GetSelectedCount()
    {
        return selectedCharacters.Count;
    }
    
    /// <summary>
    /// 重置选择
    /// </summary>
    public void ResetSelection()
    {
        selectedCharacters.Clear();
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
        string selectedInfo = selectedCharacters.Count > 0
            ? string.Join(", ", selectedCharacters.ConvertAll(c => c.info.name))
            : "无";
            
        Debug.Log($"CharacterSelectionManager 调试信息:\n" +
                 $"配置数据: {(selectionData != null ? "已配置" : "未配置")}\n" +
                 $"按钮预制体: {(characterButtonPrefab != null ? "已配置" : "未配置")}\n" +
                 $"按钮容器: {(buttonContainer != null ? "已配置" : "未配置")}\n" +
                 $"开始游戏按钮: {(startGameButton != null ? "已配置" : "未配置")}\n" +
                 $"角色按钮数量: {characterButtons.Count}\n" +
                 $"已选角色数: {selectedCharacters.Count}/{TeamData.TEAM_SIZE}\n" +
                 $"选中角色: {selectedInfo}");
    }
    
    #endregion
}
