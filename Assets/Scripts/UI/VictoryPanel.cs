using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 胜利界面 - 显示游戏完成统计信息
/// 
/// 【核心职责】：
/// - 订阅游戏完成事件（OnGameCompleted）
/// - 显示总击杀敌人数和通过关卡数
/// - 处理重新开始按钮点击
/// - 返回角色选择场景
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] private TextMeshProUGUI enemyCountText;     // 敌人数统计文本
    [SerializeField] private TextMeshProUGUI levelCountText;     // 关卡数统计文本
    [SerializeField] private Button restartButton;               // 重新开始按钮
    
    [Header("场景配置")]
    [SerializeField] private string characterSelectionSceneName = "CharacterSelection"; // 角色选择场景名称
    
    [Header("游戏暂停")]
    [SerializeField] private bool pauseGameOnShow = true;        // 显示时是否暂停游戏
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        InitializePanel();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    void InitializePanel()
    {
        // 设置初始状态为隐藏
        gameObject.SetActive(false);
        
        // 订阅游戏完成事件
        SubscribeToEvents();
        
        // 设置按钮事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else
        {
            Debug.LogWarning("VictoryPanel: Restart按钮未配置！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log("VictoryPanel: 初始化完成");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnGameCompleted += OnGameCompleted;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnGameCompleted -= OnGameCompleted;
        
        // 移除按钮事件
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        }
    }
    
    /// <summary>
    /// 游戏完成事件处理
    /// </summary>
    void OnGameCompleted()
    {
        ShowVictoryPanel();
    }
    
    /// <summary>
    /// 显示胜利界面
    /// </summary>
    void ShowVictoryPanel()
    {
        // 激活面板
        gameObject.SetActive(true);
        
        // 更新统计数据
        UpdateStatistics();
        
        // 暂停游戏（可选）
        if (pauseGameOnShow)
        {
            Time.timeScale = 0f;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("VictoryPanel: 显示胜利界面");
        }
    }
    
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
            Debug.Log("VictoryPanel: 点击重新开始按钮");
        }
        
        // 恢复时间缩放
        Time.timeScale = 1f;
        
        // 清理所有运行时数据
        GameRuntimeData.ClearAllData();
        
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
        ShowVictoryPanel();
    }
    
    #endregion
}

