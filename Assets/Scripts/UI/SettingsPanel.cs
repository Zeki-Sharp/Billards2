using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 设置界面 - 游戏设置面板
/// 
/// 【核心职责】：
/// - 提供返回游戏功能
/// - 提供返回主界面重新开始功能
/// - 提供退出游戏功能
/// 
/// 【架构说明】：
/// - 继承BasePanel，由UIController统一管理
/// - 显示时暂停游戏，隐藏时恢复游戏
/// - 通过按键（如ESC）触发显示
/// </summary>
public class SettingsPanel : BasePanel
{
    [Header("UI元素")]
    [SerializeField] private Button resumeButton;        // 返回游戏按钮
    [SerializeField] private Button mainMenuButton;      // 回到主界面按钮
    [SerializeField] private Button quitButton;          // 退出游戏按钮
    
    [Header("场景配置")]
    [SerializeField] private string characterSelectionSceneName = "CharacterSelection"; // 角色选择场景名称
    
    #region BasePanel生命周期
    
    /// <summary>
    /// 面板初始化（BasePanel生命周期）
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        // 设置面板类型和配置
        panelType = UIPanelType.Popup;
        pauseGameOnShow = true; // 显示设置时暂停游戏
        
        // 设置按钮事件
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }
        else
        {
            Debug.LogWarning("SettingsPanel: Resume按钮未配置！");
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        else
        {
            Debug.LogWarning("SettingsPanel: MainMenu按钮未配置！");
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        else
        {
            Debug.LogWarning("SettingsPanel: Quit按钮未配置！");
        }
    }
    
    /// <summary>
    /// 面板显示时调用（BasePanel生命周期）
    /// </summary>
    public override void OnShow(UIPanelData data = null)
    {
        base.OnShow(data);
        
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 设置面板已显示");
        }
    }
    
    /// <summary>
    /// 面板隐藏时调用（BasePanel生命周期）
    /// </summary>
    public override void OnHide()
    {
        base.OnHide();
        
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 设置面板已隐藏");
        }
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 返回游戏按钮点击事件
    /// </summary>
    void OnResumeButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 点击返回游戏按钮");
        }
        
        // 隐藏设置面板，自动恢复游戏（由UIController管理）
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
        else
        {
            OnHide();
            // 如果没有UIController，手动恢复游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }
    }
    
    /// <summary>
    /// 回到主界面按钮点击事件
    /// </summary>
    void OnMainMenuButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 点击回到主界面按钮，触发游戏重启");
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
        
        // ✅ 清理会话数据
        GameSession.GetOrCreateInstance()?.Reset();
        SceneTransitionManager.ClearSelectedCharacter();
        
        // 恢复游戏状态
        Time.timeScale = 1f;
        
        // 加载角色选择场景
        LoadCharacterSelectionScene();
    }
    
    /// <summary>
    /// 退出游戏按钮点击事件
    /// </summary>
    void OnQuitButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 点击退出游戏按钮");
        }
        
        // 先隐藏面板
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
        
        // 退出游戏
        QuitGame();
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 加载角色选择场景
    /// </summary>
    void LoadCharacterSelectionScene()
    {
        if (string.IsNullOrEmpty(characterSelectionSceneName))
        {
            Debug.LogError("SettingsPanel: 角色选择场景名称未配置！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SettingsPanel: 加载场景 - {characterSelectionSceneName}");
        }
        
        SceneManager.LoadScene(characterSelectionSceneName);
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    void QuitGame()
    {
        if (showDebugInfo)
        {
            Debug.Log("SettingsPanel: 退出游戏");
        }
        
#if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建版本中退出应用
        Application.Quit();
#endif
    }
    
    #endregion
    
    #region 调试方法
    
    /// <summary>
    /// 测试显示设置面板（仅用于调试）
    /// </summary>
    [ContextMenu("测试显示设置面板")]
    void TestShowSettingsPanel()
    {
        OnShow();
    }
    
    #endregion
}

