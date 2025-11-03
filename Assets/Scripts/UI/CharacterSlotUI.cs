using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 角色槽位UI组件 - 独立管理单个角色的UI显示
/// 
/// 【设计原则】：
/// - 组件化：每个槽位GameObject挂载一个此组件
/// - 自我管理：只负责自己的UI更新
/// - 松耦合：通过公共方法接收外部调用
/// 
/// 【职责】：
/// - 显示角色头像、名称、血量
/// - 管理选中高亮效果
/// - 管理死亡状态显示
/// </summary>
public class CharacterSlotUI : MonoBehaviour
{
    #region UI元素引用
    
    [Header("必需UI元素")]
    [Tooltip("角色头像")]
    [SerializeField] private Image avatarImage;
    
    [Tooltip("血条填充")]
    [SerializeField] private Image healthBarFill;
    
    [Header("可选UI元素")]
    [Tooltip("角色名称")]
    [SerializeField] private TextMeshProUGUI nameText;
    
    [Header("血量文本（支持两种模式）")]
    [Tooltip("模式1：单文本组件显示（如：100/100）")]
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Tooltip("模式2：拆分文本 - 当前血量")]
    [SerializeField] private TextMeshProUGUI currentHealthText;
    
    [Tooltip("模式2：拆分文本 - 总血量")]
    [SerializeField] private TextMeshProUGUI totalHealthText;
    
    [Header("其他可选元素")]
    [Tooltip("选中高亮边框")]
    [SerializeField] private GameObject highlightBorder;
    
    [Tooltip("死亡遮罩")]
    [SerializeField] private Image deathOverlay;
    
    #endregion
    
    #region 颜色配置
    
    [Header("颜色配置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color deadColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    #endregion
    
    #region 调试
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    #endregion
    
    #region 生命周期
    
    void Awake()
    {
        // 初始化状态
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(false);
        }
        
        if (deathOverlay != null)
        {
            deathOverlay.enabled = false;
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 更新角色完整信息
    /// </summary>
    public void UpdateCharacterInfo(CharacterInstance character)
    {
        if (character == null || character.characterData == null)
        {
            ClearSlot();
            return;
        }
        
        // 更新头像
        UpdateAvatar(character.characterData.info.icon);
        
        // 更新名称
        UpdateName(character.characterData.info.name);
        
        // 更新血量
        UpdateHealth(character.currentHealth, character.maxHealth);
        
        // 更新死亡状态
        SetDead(!character.isAlive);
        
        if (showDebugInfo)
        {
            Debug.Log($"[CharacterSlotUI] 更新角色: {character.characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 更新头像
    /// </summary>
    public void UpdateAvatar(Sprite avatarSprite)
    {
        if (avatarImage != null)
        {
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
                avatarImage.enabled = true;
            }
            else
            {
                avatarImage.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// 更新名称
    /// </summary>
    public void UpdateName(string characterName)
    {
        if (nameText != null)
        {
            nameText.text = characterName;
        }
    }
    
    /// <summary>
    /// 更新血量显示（支持单文本和拆分文本两种模式）
    /// </summary>
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        // 更新血条
        if (healthBarFill != null)
        {
            float healthRatio = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = healthRatio;
        }
        
        // 模式1：单文本模式（如：100/100）
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
        
        // 模式2：拆分文本模式
        if (currentHealthText != null)
        {
            currentHealthText.text = Mathf.CeilToInt(currentHealth).ToString();
        }
        
        if (totalHealthText != null)
        {
            totalHealthText.text = Mathf.CeilToInt(maxHealth).ToString();
        }
    }
    
    /// <summary>
    /// 设置选中高亮
    /// </summary>
    public void SetHighlight(bool isHighlighted)
    {
        // 高亮边框
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isHighlighted);
        }
        
        // 头像颜色
        if (avatarImage != null)
        {
            avatarImage.color = isHighlighted ? highlightColor : normalColor;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[CharacterSlotUI] 设置高亮: {isHighlighted}");
        }
    }
    
    /// <summary>
    /// 设置死亡状态
    /// </summary>
    public void SetDead(bool isDead)
    {
        // 死亡遮罩
        if (deathOverlay != null)
        {
            deathOverlay.enabled = isDead;
        }
        
        // 颜色变灰
        if (isDead)
        {
            if (avatarImage != null) avatarImage.color = deadColor;
            if (nameText != null) nameText.color = deadColor;
        }
        else
        {
            if (avatarImage != null) avatarImage.color = normalColor;
            if (nameText != null) nameText.color = Color.white;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[CharacterSlotUI] 设置死亡状态: {isDead}");
        }
    }
    
    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        if (avatarImage != null) avatarImage.enabled = false;
        if (nameText != null) nameText.text = "";
        if (healthBarFill != null) healthBarFill.fillAmount = 0f;
        
        // 清空血量文本（两种模式）
        if (healthText != null) healthText.text = "";
        if (currentHealthText != null) currentHealthText.text = "";
        if (totalHealthText != null) totalHealthText.text = "";
        
        SetHighlight(false);
        SetDead(false);
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("测试 - 设置满血")]
    void TestFullHealth()
    {
        UpdateHealth(100f, 100f);
    }
    
    [ContextMenu("测试 - 设置半血")]
    void TestHalfHealth()
    {
        UpdateHealth(50f, 100f);
    }
    
    [ContextMenu("测试 - 切换高亮")]
    void TestToggleHighlight()
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(!highlightBorder.activeSelf);
        }
    }
    
    [ContextMenu("测试 - 切换死亡")]
    void TestToggleDead()
    {
        if (deathOverlay != null)
        {
            SetDead(!deathOverlay.enabled);
        }
    }
    
    [ContextMenu("清空槽位")]
    void TestClear()
    {
        ClearSlot();
    }
    
    #endregion
}

