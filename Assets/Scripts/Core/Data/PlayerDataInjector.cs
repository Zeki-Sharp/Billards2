using UnityEngine;

/// <summary>
/// 玩家数据注入器 - 在游戏场景中注入选中的角色数据
/// 
/// 【核心职责】：
/// - 在场景加载完成后查找场景中的Player组件
/// - 将选中的角色数据注入到Player组件中
/// - 确保角色选择的结果正确应用到游戏场景
/// </summary>
public class PlayerDataInjector : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private bool injectOnStart = true;
    [SerializeField] private bool injectOnAwake = false;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Awake()
    {
        if (injectOnAwake)
        {
            InjectPlayerData();
        }
    }
    
    void Start()
    {
        if (injectOnStart)
        {
            InjectPlayerData();
        }
    }
    
    /// <summary>
    /// 注入玩家数据到场景中的Player组件
    /// </summary>
    public void InjectPlayerData()
    {
        // 获取选中的角色数据
        PlayerData selectedCharacter = SceneTransitionManager.GetSelectedCharacter();
        
        if (selectedCharacter == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerDataInjector: 没有选中的角色数据，使用默认配置");
            }
            return;
        }
        
        // 查找场景中的Player组件
        Player player = FindFirstObjectByType<Player>();
        
        if (player == null)
        {
            Debug.LogError("PlayerDataInjector: 场景中未找到Player组件！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerDataInjector: 找到Player组件，准备注入角色数据 - {selectedCharacter.info.name}");
        }
        
        // 注入角色数据
        InjectDataToPlayer(player, selectedCharacter);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerDataInjector: 角色数据注入完成 - {selectedCharacter.info.name} (攻击力: {selectedCharacter.attackPower})");
        }
    }
    
    /// <summary>
    /// 将角色数据注入到指定的Player组件
    /// </summary>
    /// <param name="player">目标Player组件</param>
    /// <param name="characterData">要注入的角色数据</param>
    void InjectDataToPlayer(Player player, PlayerData characterData)
    {
        if (player == null || characterData == null)
        {
            Debug.LogError("PlayerDataInjector: Player或角色数据为空，无法注入！");
            return;
        }
        
        // 设置PlayerData
        player.SetPlayerData(characterData);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerDataInjector: 已为Player设置角色数据 - {characterData.info.name}");
        }
    }
    
    /// <summary>
    /// 延迟注入玩家数据（用于异步场景加载后的注入）
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    public void InjectPlayerDataDelayed(float delay = 0.1f)
    {
        StartCoroutine(InjectPlayerDataCoroutine(delay));
    }
    
    /// <summary>
    /// 延迟注入的协程
    /// </summary>
    /// <param name="delay">延迟时间</param>
    System.Collections.IEnumerator InjectPlayerDataCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        InjectPlayerData();
    }
    
    /// <summary>
    /// 强制重新注入数据（用于调试或特殊情况）
    /// </summary>
    [ContextMenu("强制重新注入角色数据")]
    public void ForceReinjectPlayerData()
    {
        if (showDebugInfo)
        {
            Debug.Log("PlayerDataInjector: 强制重新注入角色数据");
        }
        
        InjectPlayerData();
    }
    
    /// <summary>
    /// 显示当前注入状态
    /// </summary>
    [ContextMenu("显示注入状态")]
    public void ShowInjectionStatus()
    {
        PlayerData selectedCharacter = SceneTransitionManager.GetSelectedCharacter();
        Player player = FindFirstObjectByType<Player>();
        
        Debug.Log($"PlayerDataInjector 状态:\n" +
                 $"选中角色: {(selectedCharacter != null ? selectedCharacter.info.name : "无")}\n" +
                 $"场景中的Player: {(player != null ? "找到" : "未找到")}\n" +
                 $"Player的PlayerData: {(player != null && player.GetPlayerData() != null ? player.GetPlayerData().info.name : "无")}");
    }
    
    #region 事件处理
    
    void OnEnable()
    {
        // 订阅场景加载完成事件（如果需要）
        // SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        // 取消订阅
        // SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// 场景加载完成事件处理
    /// </summary>
    /// <param name="scene">加载的场景</param>
    /// <param name="mode">加载模式</param>
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (showDebugInfo)
        {
            Debug.Log($"PlayerDataInjector: 场景加载完成 - {scene.name}");
        }
        
        // 场景加载完成后延迟注入数据
        InjectPlayerDataDelayed(0.2f);
    }
    
    #endregion
}
