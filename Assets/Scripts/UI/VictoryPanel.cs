using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 胜利界面 - 显示游戏完成统计信息
/// 
/// 【核心职责】：
/// - 显示总击杀敌人数和通过关卡数
/// - 处理重新开始按钮点击
/// - 返回角色选择场景
/// 
/// 【架构说明】：
/// - 继承BasePanel，由UIController统一管理
/// - 不自己订阅事件，由UIController触发显示
/// - 不自己控制暂停，由UIController统一管理
/// </summary>
public class VictoryPanel : BasePanel
{
    [Header("UI元素")]
    [SerializeField] private TextMeshProUGUI enemyCountText;     // 敌人数统计文本
    [SerializeField] private TextMeshProUGUI levelCountText;     // 关卡数统计文本
    [SerializeField] private Button restartButton;               // 重新开始按钮
    
    [Header("场景配置")]
    [SerializeField] private string characterSelectionSceneName = "CharacterSelection"; // 角色选择场景名称
    
    #region BasePanel生命周期
    
    /// <summary>
    /// 面板初始化（BasePanel生命周期）
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        // 设置面板类型和配置（BasePanel中已有默认值，这里确保正确）
        panelType = UIPanelType.Popup;
        pauseGameOnShow = true; // 确保显示时暂停游戏
        
        // 设置按钮事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else
        {
            Debug.LogWarning("VictoryPanel: Restart按钮未配置！");
        }
    }
    
    /// <summary>
    /// 面板显示时调用（BasePanel生命周期）
    /// </summary>
    public override void OnShow(UIPanelData data = null)
    {
        base.OnShow(data);
        
        // 更新统计数据
        UpdateStatistics();
    }
    
    /// <summary>
    /// 面板隐藏时调用（BasePanel生命周期）
    /// </summary>
    public override void OnHide()
    {
        base.OnHide();
        
        // 清理逻辑（如果需要）
    }
    
    #endregion
    
    /// <summary>
    /// 更新统计数据
    /// </summary>
    void UpdateStatistics()
    {
        // 获取总击杀数
        int totalKills = GameRuntimeData.GetTotalEnemyKills();
        
        // 获取通过关卡数
        int levelsPassed = 0;
        if (LevelManager.Instance != null)
        {
            // 当前关卡索引 + 1 = 通过的关卡数
            levelsPassed = LevelManager.Instance.GetCurrentLevelIndex() + 1;
        }
        else
        {
            Debug.LogWarning("VictoryPanel: LevelManager.Instance 为空，无法获取关卡数！");
        }
        
        // 更新UI文本
        if (enemyCountText != null)
        {
            enemyCountText.text = $"消灭敌人数: {totalKills}";
        }
        else
        {
            Debug.LogWarning("VictoryPanel: 敌人数统计文本未配置！");
        }
        
        if (levelCountText != null)
        {
            levelCountText.text = $"通过关卡数: {levelsPassed}";
        }
        else
        {
            Debug.LogWarning("VictoryPanel: 关卡数统计文本未配置！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"VictoryPanel: 统计数据更新 - 击杀: {totalKills}, 关卡: {levelsPassed}");
        }
    }
    
    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    void OnRestartButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("VictoryPanel: 点击重新开始按钮，触发游戏重启");
        }
        
        // 先隐藏当前面板
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
        else
        {
            OnHide();
        }
        
        // 发布游戏重启事件（让所有DontDestroyOnLoad管理器重置状态）
        GameEventBus.PublishGameRestart();
        
        // 清理静态数据
        GameRuntimeData.ClearAllData();
        SceneTransitionManager.ClearSelectedCharacter();
        
        // 恢复游戏状态（双保险，UIController.ResetState也会做）
        Time.timeScale = 1f;
        
        // 加载角色选择场景
        LoadCharacterSelectionScene();
    }
    
    /// <summary>
    /// 加载角色选择场景
    /// </summary>
    void LoadCharacterSelectionScene()
    {
        if (string.IsNullOrEmpty(characterSelectionSceneName))
        {
            Debug.LogError("VictoryPanel: 角色选择场景名称未配置！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"VictoryPanel: 加载场景 - {characterSelectionSceneName}");
        }
        
        SceneManager.LoadScene(characterSelectionSceneName);
    }
    
    #region 调试方法
    
    /// <summary>
    /// 测试显示胜利界面（仅用于调试）
    /// </summary>
    [ContextMenu("测试显示胜利界面")]
    void TestShowVictoryPanel()
    {
        OnShow();
    }
    
    #endregion
}

