using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 场景切换管理器 - 负责处理场景切换和数据传递
/// 
/// 【核心职责】：
/// - 管理场景之间的切换
/// - 传递选中的角色数据到游戏场景
/// - 提供统一的场景加载接口
/// 
/// 【执行顺序】：LEVEL 层 (-30)
/// 【依赖】：SYSTEM 层
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.LEVEL)]
public class SceneTransitionManager : SingletonManager<SceneTransitionManager>
{
    
    [Header("场景配置")]
    [SerializeField] private string level1SceneName = "Level1";
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 静态数据存储 - 用于场景间传递数据
    private static PlayerData selectedCharacterData;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        if (showDebugInfo)
        {
            Debug.Log("SceneTransitionManager: 单例创建成功（LEVEL 层），将跨场景保留");
        }
    }
    
    #endregion
    
    /// <summary>
    /// 设置选中的角色数据
    /// </summary>
    /// <param name="characterData">选中的角色数据</param>
    public static void SetSelectedCharacter(PlayerData characterData)
    {
        selectedCharacterData = characterData;
        
        if (Instance != null && Instance.showDebugInfo)
        {
            Debug.Log($"SceneTransitionManager: 设置选中角色 - {characterData?.playerName ?? "null"}");
        }
    }
    
    /// <summary>
    /// 获取选中的角色数据
    /// </summary>
    /// <returns>选中的角色数据，如果没有则返回null</returns>
    public static PlayerData GetSelectedCharacter()
    {
        return selectedCharacterData;
    }
    
    /// <summary>
    /// 清除选中的角色数据
    /// </summary>
    public static void ClearSelectedCharacter()
    {
        selectedCharacterData = null;
        
        if (Instance != null && Instance.showDebugInfo)
        {
            Debug.Log("SceneTransitionManager: 清除选中角色数据");
        }
    }
    
    /// <summary>
    /// 加载Level1场景
    /// </summary>
    public void LoadLevel1()
    {
        if (selectedCharacterData == null)
        {
            Debug.LogError("SceneTransitionManager: 未选择角色数据，无法加载Level1！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SceneTransitionManager: 开始加载Level1场景，选中角色: {selectedCharacterData.playerName}");
        }
        
        // 异步加载场景
        SceneManager.LoadSceneAsync(level1SceneName);
    }
    
    /// <summary>
    /// 加载指定场景
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void LoadScene(string sceneName)
    {
        if (showDebugInfo)
        {
            Debug.Log($"SceneTransitionManager: 开始加载场景 - {sceneName}");
        }
        
        SceneManager.LoadSceneAsync(sceneName);
    }
    
    /// <summary>
    /// 返回主菜单（角色选择界面）
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (showDebugInfo)
        {
            Debug.Log("SceneTransitionManager: 返回主菜单");
        }
        
        // 清除选中的角色数据
        ClearSelectedCharacter();
        
        // 加载主菜单场景（这里假设是角色选择场景）
        // 需要根据实际的主菜单场景名称调整
        SceneManager.LoadSceneAsync("MainMenu"); // 需要替换为实际的主菜单场景名
    }
    
    #region 调试方法
    
    [ContextMenu("显示当前选中角色")]
    void ShowSelectedCharacter()
    {
        if (selectedCharacterData != null)
        {
            Debug.Log($"当前选中角色: {selectedCharacterData.playerName} (攻击模式: {selectedCharacterData.attackMode})");
        }
        else
        {
            Debug.Log("当前没有选中角色");
        }
    }
    
    [ContextMenu("测试加载Level1")]
    void TestLoadLevel1()
    {
        // 创建一个测试角色数据
        if (selectedCharacterData == null)
        {
            Debug.LogWarning("SceneTransitionManager: 没有选中角色数据，无法测试加载Level1");
            return;
        }
        
        LoadLevel1();
    }
    
    #endregion
}
