using UnityEngine;

/// <summary>
/// 角色选择控制器 - 管理当前选中的角色状态
/// 
/// 【核心职责】：
/// - 维护当前选中的角色ID
/// - 响应GlobalInputManager的点击球体事件
/// - 判断是否允许切换选择
/// - 发布具体角色的选择/取消选择事件
/// 
/// 【设计原则】：
/// - 不检测输入（由GlobalInputManager负责）
/// - 不处理蓄力（由ChargeController负责）
/// - 只负责管理选中状态
/// 
/// 【执行顺序】：
/// - CONTROLLER: 控制器层，处理选择逻辑
/// - 在 OnEnable 中订阅 GameEventBus 事件（完全解耦，不依赖 GlobalInputManager）
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class CharacterSelectionController : MonoBehaviour
{
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前选中的角色ID
    private string selectedCharacterID;
    private GameObject selectedBallObject;
    
    // 场景单例
    private static CharacterSelectionController instance;
    public static CharacterSelectionController Instance => instance;
    
    // 公共接口
    public string SelectedCharacterID => selectedCharacterID;
    public GameObject SelectedBallObject => selectedBallObject;
    public bool HasSelection => !string.IsNullOrEmpty(selectedCharacterID);
    
    void Awake()
    {
        // 单例检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning("CharacterSelectionController: 场景中存在多个实例，销毁多余实例");
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        if (showDebugInfo)
        {
            Debug.Log("CharacterSelectionController: 初始化完成");
        }
    }
    
    void OnEnable()
    {
        // 订阅 GameEventBus 输入事件
        GameEventBus.OnBallClicked += HandleBallClicked;
        GameEventBus.OnCancelInput += HandleCancelInput;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnBallClicked -= HandleBallClicked;
        GameEventBus.OnCancelInput -= HandleCancelInput;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log("CharacterSelectionController: 初始化完成，已通过 OnEnable 订阅 GameEventBus 输入事件");
        }
    }
    
    #region 输入响应
    
    /// <summary>
    /// 处理球体点击
    /// </summary>
    void HandleBallClicked(GameObject ballObject)
    {
        if (ballObject == null) return;
        
        // ✅ 使用统一权限管理器检查选择权限（阶段 + 发射次数）
        if (PlayerInputPermissionManager.Instance == null)
        {
            Debug.LogError("[CharacterSelectionController] PlayerInputPermissionManager.Instance 为 null！请在场景中添加该组件");
            return;
        }
        
        if (!PlayerInputPermissionManager.Instance.CanSelectCharacter())
        {
            // 权限管理器已经输出了详细的拒绝原因日志
            return;
        }
        
        // 从球体获取角色ID
        string characterID = GetCharacterIDFromBall(ballObject);
        if (string.IsNullOrEmpty(characterID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CharacterSelectionController: 无法从球体获取角色ID - {ballObject.name}");
            }
            return;
        }
        
        // ✅ 检查角色是否已完成发射（查询 PlayerTurnManager）
        if (PlayerTurnManager.Instance != null && PlayerTurnManager.Instance.IsCharacterCompleted(characterID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CharacterSelectionController: 角色 {characterID} 已完成发射，无法再选中");
            }
            return;
        }
        
        // ✅ 检查角色是否死亡
        if (IsCharacterDead(characterID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"CharacterSelectionController: 角色 {characterID} 已死亡，无法选中");
            }
            return;
        }
        
        // 检查是否点击的是已选中的角色
        if (selectedCharacterID == characterID)
        {
            if (showDebugInfo)
            {
                Debug.Log($"CharacterSelectionController: 点击已选中角色 {characterID}，保持选中状态");
            }
            // 不做任何操作，保持选中状态
            return;
        }
        
        // 检查是否可以切换选择
        if (!CanSwitchSelection())
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("CharacterSelectionController: 当前无法切换选择（可能正在蓄力中）");
            }
            return;
        }
        
        // 执行选择
        SelectCharacter(characterID, ballObject);
    }
    
    /// <summary>
    /// 处理取消输入
    /// </summary>
    void HandleCancelInput()
    {
        if (HasSelection)
        {
            DeselectCharacter();
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("CharacterSelectionController: 当前无选中角色，忽略取消操作");
            }
        }
    }
    
    #endregion
    
    #region 选择逻辑
    
    /// <summary>
    /// 选择角色
    /// </summary>
    void SelectCharacter(string characterID, GameObject ballObject)
    {
        // 取消上一个选中（如果有）
        if (HasSelection)
        {
            DeselectCharacter();
        }
        
        // 设置新选中
        selectedCharacterID = characterID;
        selectedBallObject = ballObject;
        
        // 发布选中事件
        GameEventBus.PublishCharacterSelected(characterID);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionController: ✅ 选中角色 {characterID} ({ballObject.name})");
        }
    }
    
    /// <summary>
    /// 取消选中
    /// </summary>
    void DeselectCharacter()
    {
        if (!HasSelection) return;
        
        string oldCharacterID = selectedCharacterID;
        
        // 清空选中状态
        selectedCharacterID = null;
        selectedBallObject = null;
        
        // 发布取消选中事件
        GameEventBus.PublishCharacterDeselected(oldCharacterID);
        
        if (showDebugInfo)
        {
            Debug.Log($"CharacterSelectionController: ❌ 取消选中角色 {oldCharacterID}");
        }
    }
    
    /// <summary>
    /// 检查是否可以切换选择
    /// </summary>
    bool CanSwitchSelection()
    {
        // ✅ 方案B：统一门槛值逻辑
        // 力度 < 门槛值：可以切换（Selected 状态）
        // 力度 >= 门槛值：不能切换（Charging 状态）
        if (ChargeController.Instance != null && ChargeController.Instance.IsCharging())
        {
            float currentForce = ChargeController.Instance.GetCurrentForce();
            
            // 从 ChargeController 的当前 ChargeSystem 获取门槛值
            // 暂时硬编码门槛值为 2f（与 ChargeSystem 默认值一致）
            float threshold = 2f;
            
            if (currentForce >= threshold)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"CharacterSelectionController: 蓄力已达门槛（{currentForce:F2} >= {threshold}），无法切换选择");
                }
                return false;
            }
        }
        
        return true;
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 从球体GameObject获取角色ID
    /// </summary>
    string GetCharacterIDFromBall(GameObject ballObject)
    {
        // 从GameSession的TeamData中查找匹配的角色
        var session = GameSession.GetOrCreateInstance();
        if (session != null && session.HasTeamData())
        {
            var teamData = session.GetTeamData();
            foreach (var character in teamData.characters)
            {
                if (character.ballInstance == ballObject)
                {
                    return character.characterID;
                }
            }
        }
        
        // 如果没找到，记录警告
        if (showDebugInfo)
        {
            Debug.LogWarning($"CharacterSelectionController: 无法为球体 {ballObject.name} 找到对应的角色ID");
        }
        
        return null;
    }
    
    /// <summary>
    /// ✅ 多角色系统：检查角色是否死亡
    /// </summary>
    bool IsCharacterDead(string characterID)
    {
        var session = GameSession.GetOrCreateInstance();
        if (session != null && session.HasTeamData())
        {
            var teamData = session.GetTeamData();
            var character = teamData.characters.Find(c => c.characterID == characterID);
            if (character != null)
            {
                return !character.isAlive;
            }
        }
        return false;
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 强制取消选中（供其他系统调用，例如角色死亡）
    /// </summary>
    public void ForceDeselect()
    {
        DeselectCharacter();
    }
    
    /// <summary>
    /// 检查指定角色是否被选中
    /// </summary>
    public bool IsCharacterSelected(string characterID)
    {
        return selectedCharacterID == characterID;
    }
    
    #endregion
}

