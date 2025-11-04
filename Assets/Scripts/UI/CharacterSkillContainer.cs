using UnityEngine;
using TMPro;

/// <summary>
/// 角色技能容器组件 - 管理单个角色的技能显示区域
/// 
/// 【核心职责】：
/// - 提供UI元素的引用（Header、SkillList、NoSkillHint）
/// - 管理角色名称显示
/// - 管理"暂无技能"提示的显示/隐藏
/// - 简化 SkillStatusPanel 的引用逻辑
/// </summary>
public class CharacterSkillContainer : MonoBehaviour
{
    #region UI元素引用
    
    [Header("UI元素")]
    [Tooltip("角色名称文本（Header）")]
    public TextMeshProUGUI headerText;
    
    [Tooltip("技能列表容器（SkillList）")]
    public Transform skillListContainer;
    
    [Tooltip("暂无技能提示（NoSkillHint）")]
    public GameObject noSkillHint;
    
    #endregion
    
    #region 运行时数据
    
    // 当前角色ID
    private string currentCharacterID;
    
    #endregion
    
    #region 初始化
    
    void Awake()
    {
        // 默认隐藏 NoSkillHint
        if (noSkillHint != null)
        {
            noSkillHint.SetActive(false);
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 设置角色信息
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="characterName">角色名称</param>
    /// <param name="isAlive">是否存活</param>
    public void SetCharacterInfo(string characterID, string characterName, bool isAlive)
    {
        currentCharacterID = characterID;
        
        // 更新角色名称
        if (headerText != null)
        {
            headerText.text = characterName;
            headerText.color = isAlive ? Color.white : Color.gray;
        }
    }
    
    /// <summary>
    /// 显示/隐藏"暂无技能"提示
    /// </summary>
    /// <param name="show">是否显示</param>
    public void ShowNoSkillHint(bool show)
    {
        if (noSkillHint != null)
        {
            noSkillHint.SetActive(show);
        }
    }
    
    /// <summary>
    /// 清空技能列表
    /// </summary>
    public void ClearSkillList()
    {
        if (skillListContainer == null) return;
        
        foreach (Transform child in skillListContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    /// <summary>
    /// 获取当前角色ID
    /// </summary>
    public string GetCharacterID()
    {
        return currentCharacterID;
    }
    
    #endregion
    
    #region 验证
    
    void OnValidate()
    {
        // 编辑器中验证引用是否配置
        if (headerText == null)
        {
            Debug.LogWarning($"[CharacterSkillContainer] {gameObject.name} 缺少 headerText 引用！", this);
        }
        
        if (skillListContainer == null)
        {
            Debug.LogWarning($"[CharacterSkillContainer] {gameObject.name} 缺少 skillListContainer 引用！", this);
        }
        
        if (noSkillHint == null)
        {
            Debug.LogWarning($"[CharacterSkillContainer] {gameObject.name} 缺少 noSkillHint 引用！", this);
        }
    }
    
    #endregion
}

