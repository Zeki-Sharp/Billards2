using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色选择按钮组件 - 用于显示单个角色信息并处理点击事件
/// </summary>
public class CharacterButton : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TextMeshProUGUI characterDescriptionText;
    
    [Header("状态显示")]
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color hoverColor = Color.cyan;
    
    // 角色数据
    private PlayerData characterData;
    private CharacterSelectionManager selectionManager;
    
    // 当前状态
    private bool isSelected = false;
    
    void Awake()
    {
        // 自动获取组件引用
        if (button == null)
            button = GetComponent<Button>();
        if (characterNameText == null)
            characterNameText = GetComponentInChildren<TextMeshProUGUI>();
        if (characterIconImage == null)
            characterIconImage = GetComponentInChildren<Image>();
        
        // 订阅按钮事件
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    void Start()
    {
        // 初始化UI状态
        UpdateVisualState();
    }
    
    /// <summary>
    /// 设置角色数据
    /// </summary>
    public void SetCharacterData(PlayerData data, CharacterSelectionManager manager)
    {
        characterData = data;
        selectionManager = manager;
        
        UpdateCharacterDisplay();
    }
    
    /// <summary>
    /// 更新角色显示信息
    /// </summary>
    void UpdateCharacterDisplay()
    {
        if (characterData == null)
        {
            Debug.LogError("CharacterButton: 角色数据为空！");
            return;
        }
        
        // 设置角色名称
        if (characterNameText != null)
        {
            characterNameText.text = characterData.playerName;
        }
        
        // 设置角色图标
        if (characterIconImage != null && characterData.playerIcon != null)
        {
            characterIconImage.sprite = characterData.playerIcon;
            characterIconImage.enabled = true;
        }
        else if (characterIconImage != null)
        {
            characterIconImage.enabled = true;
        }
        
        // 设置角色描述文本
        if (characterDescriptionText != null)
        {
            if (!string.IsNullOrEmpty(characterData.characterDescription))
            {
                characterDescriptionText.text = characterData.characterDescription;
                characterDescriptionText.enabled = true;
            }
            else
            {
                characterDescriptionText.text = "暂无描述";
                characterDescriptionText.enabled = true;
            }
        }
        
        Debug.Log($"CharacterButton: 设置角色 {characterData.playerName}");
    }
    
    
    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }
    
    /// <summary>
    /// 更新视觉状态
    /// </summary>
    void UpdateVisualState()
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        
        if (isSelected)
        {
            colors.normalColor = selectedColor;
            colors.highlightedColor = selectedColor;
        }
        else
        {
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
        }
        
        button.colors = colors;
        
        // 显示选中指示器
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(isSelected);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    void OnButtonClicked()
    {
        if (characterData == null || selectionManager == null)
        {
            Debug.LogError("CharacterButton: 角色数据或选择管理器为空！");
            return;
        }
        
        Debug.Log($"CharacterButton: 选择角色 {characterData.playerName}");
        
        // 通知选择管理器
        selectionManager.SelectCharacter(characterData);
    }
    
    /// <summary>
    /// 获取角色数据
    /// </summary>
    public PlayerData GetCharacterData()
    {
        return characterData;
    }
    
    /// <summary>
    /// 检查是否被选中
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
    
    #region 调试方法
    
    [ContextMenu("测试选中状态")]
    void TestSelectedState()
    {
        SetSelected(!isSelected);
        Debug.Log($"CharacterButton: 测试选中状态 - {isSelected}");
    }
    
    [ContextMenu("显示角色信息")]
    void ShowCharacterInfo()
    {
        if (characterData != null)
        {
            Debug.Log($"角色信息:\n" +
                     $"名称: {characterData.playerName}\n" +
                     $"描述: {characterData.characterDescription}\n" +
                     $"攻击模式: {characterData.attackMode}\n" +
                     $"选中状态: {isSelected}");
        }
        else
        {
            Debug.Log("CharacterButton: 角色数据为空");
        }
    }
    
    #endregion
}
