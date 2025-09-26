using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入处理器 - 统一处理所有玩家输入和权限控制
/// 
/// 【核心职责】：
/// - 处理WASD移动输入和鼠标攻击输入
/// - 支持New Input System和Legacy Input Manager
/// - 根据玩家状态分发输入到相应组件
/// - 统一管理所有输入权限控制（顶层阶段 + 子阶段）
/// - 主动通知GameFlowController进行状态切换
/// 
/// 【设计原则】：
/// - 作为输入的统一入口，避免其他组件直接检测输入
/// - 作为权限控制的唯一决策点，避免重复的权限检查
/// - 与PlayerStateMachine协作，确保输入与状态一致
/// - 通过PlayerStateMachine触发状态变化，由PlayerPhaseController管理子阶段
/// - 在HandleInput()开始进行统一的顶层阶段权限检查
/// - 在各个输入处理方法中进行具体的子阶段权限检查
/// - WASD移动权限：只在Transition阶段允许
/// - 蓄力输入权限：只在Normal阶段允许
/// 
/// 【权限检查优化】：
/// - 顶层阶段权限检查：在HandleInput()开始统一检查，避免重复
/// - 子阶段权限检查：在具体输入处理方法中检查，逻辑清晰
/// - 消除了重复的GameFlowController检查，提高性能
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("输入设置")]
    [SerializeField] private bool useNewInputSystem = true;
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private PlayerStateMachine stateMachine;
    private PlayerMovementController movementController;
    private PlayerCore playerCore;
    private PlayerInputPermissionManager permissionManager;
    
    // Input System支持
    private InputAction moveAction;
    private InputAction attackAction;
    private InputActionMap inputActionMap;
    
    // 输入状态
    private Vector2 moveInput;
    private bool isMovePressed;
    private bool isAttackPressed;
    private bool isAttackHeld;
    private bool isAttackReleased;
    
    void Start()
    {
        // 获取组件引用
        stateMachine = GetComponent<PlayerStateMachine>();
        movementController = GetComponent<PlayerMovementController>();
        playerCore = GetComponent<PlayerCore>();
        permissionManager = GetComponent<PlayerInputPermissionManager>();
        
        // 确保权限管理器存在
        if (permissionManager == null)
        {
            permissionManager = gameObject.AddComponent<PlayerInputPermissionManager>();
            Debug.LogWarning("PlayerInputHandler: 自动添加PlayerInputPermissionManager组件");
        }
        
        // 初始化输入系统
        InitializeInputSystem();
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerInputHandler: 初始化完成");
        }
    }
    
    void Update()
    {
        
        // 更新输入状态
        UpdateInputState();
        
        // 处理输入
        HandleInput();
    }
    
    void OnEnable()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Enable();
        }
    }
    
    void OnDisable()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Disable();
        }
    }
    
    void OnDestroy()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Dispose();
        }
    }
    
    #region 输入系统初始化
    
    /// <summary>
    /// 初始化输入系统
    /// </summary>
    void InitializeInputSystem()
    {
        if (useNewInputSystem)
        {
            try
            {
                // 创建Input Actions
                inputActionMap = new InputActionMap("Player");
                
                // 创建Move Action
                moveAction = inputActionMap.AddAction("Move", InputActionType.Value, "<Keyboard>/w");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                
                // 创建Attack Action
                attackAction = inputActionMap.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
                
                // 启用Actions
                inputActionMap.Enable();
                
                if (showDebugInfo)
                {
                    Debug.Log("PlayerInputHandler: New Input System初始化完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayerInputHandler: New Input System初始化失败: {e.Message}");
                Debug.Log("PlayerInputHandler: 将使用Legacy Input Manager作为备用方案");
                
                // 清理失败的Input System
                useNewInputSystem = false;
                inputActionMap = null;
                moveAction = null;
                attackAction = null;
            }
        }
    }
    
    #endregion
    
    #region 输入状态更新
    
    /// <summary>
    /// 更新输入状态
    /// </summary>
    void UpdateInputState()
    {
        if (useNewInputSystem && inputActionMap != null)
        {
            // 使用New Input System
            moveInput = moveAction.ReadValue<Vector2>();
            isMovePressed = moveInput.magnitude > 0.1f;
            
            isAttackPressed = attackAction.WasPressedThisFrame();
            isAttackHeld = attackAction.IsPressed();
            isAttackReleased = attackAction.WasReleasedThisFrame();
        }
        else
        {
            // 使用Legacy Input Manager
            moveInput = Vector2.zero;
            isMovePressed = false;
            
            if (Input.GetKey(KeyCode.W)) { moveInput.y += 1; isMovePressed = true; }
            if (Input.GetKey(KeyCode.S)) { moveInput.y -= 1; isMovePressed = true; }
            if (Input.GetKey(KeyCode.A)) { moveInput.x -= 1; isMovePressed = true; }
            if (Input.GetKey(KeyCode.D)) { moveInput.x += 1; isMovePressed = true; }
            
            isAttackPressed = Input.GetMouseButtonDown(0);
            isAttackHeld = Input.GetMouseButton(0);
            isAttackReleased = Input.GetMouseButtonUp(0);
        }
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 统一权限检查：检查顶层游戏阶段
        GameFlowController gameFlowController = GameFlowController.Instance;
        if (gameFlowController == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerInputHandler: GameFlowController实例为空！");
            }
            return;
        }
        
        // 敌人阶段完全禁用所有输入
        if (gameFlowController.IsEnemyPhase)
        {
            return; // 直接返回，不处理任何输入
        }
        
        // 非玩家阶段也禁用所有输入
        if (!gameFlowController.IsPlayerPhase)
        {
            if (showDebugInfo)
            {
                Debug.Log($"PlayerInputHandler: 非玩家阶段，禁用所有输入 - 当前状态: {gameFlowController.CurrentState}");
            }
            return;
        }
        
        // 现在确定在玩家阶段，根据当前状态处理具体输入
        switch (stateMachine.CurrentState)
        {
            case PlayerStateMachine.PlayerState.Idle:
                HandleIdleInput();
                break;
            case PlayerStateMachine.PlayerState.Charging:
                HandleChargingInput();
                break;
            case PlayerStateMachine.PlayerState.Moving:
                // 运动状态不接受任何输入
                break;
        }
    }
    
    /// <summary>
    /// 处理空闲状态输入
    /// </summary>
    void HandleIdleInput()
    {
        // 处理WASD移动 - 使用权限管理器检查
        if (movementController != null && permissionManager.CanMoveInCurrentSubPhase())
        {
            movementController.HandleMovement(moveInput, isMovePressed);
        }
        
        // 检测蓄力输入 - 使用权限管理器检查
        if (isAttackPressed && permissionManager.CanChargeInCurrentSubPhase())
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerInputHandler: 检测到攻击输入，开始蓄力");
            }
            
            // 开始蓄力
            if (stateMachine != null)
            {
                stateMachine.StartCharging();
            }
        }
    }
    
    /// <summary>
    /// 处理蓄力状态输入
    /// </summary>
    void HandleChargingInput()
    {
        // 蓄力状态只处理鼠标释放
        if (isAttackReleased)
        {
            stateMachine.LaunchCharged();
        }
    }
    
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取移动输入
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
    
    /// <summary>
    /// 是否按下移动键
    /// </summary>
    public bool IsMovePressed()
    {
        return isMovePressed;
    }
    
    /// <summary>
    /// 是否按下攻击键
    /// </summary>
    public bool IsAttackPressed()
    {
        return isAttackPressed;
    }
    
    /// <summary>
    /// 是否持续按住攻击键
    /// </summary>
    public bool IsAttackHeld()
    {
        return isAttackHeld;
    }
    
    /// <summary>
    /// 是否释放攻击键
    /// </summary>
    public bool IsAttackReleased()
    {
        return isAttackReleased;
    }
    
    #endregion
}
