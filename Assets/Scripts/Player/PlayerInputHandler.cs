using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入处理器 - 统一处理所有玩家输入
/// 
/// 【核心职责】：
/// - 处理WASD移动输入和鼠标攻击输入
/// - 支持New Input System和Legacy Input Manager
/// - 根据玩家状态分发输入到相应组件
/// - 主动通知GameFlowController进行状态切换
/// 
/// 【设计原则】：
/// - 作为输入的统一入口，避免其他组件直接检测输入
/// - 与PlayerStateMachine协作，确保输入与状态一致
/// - 通过GameFlowController.RequestChargingState()主动触发游戏状态变化
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
    private GameFlowController gameFlowController;
    
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
        gameFlowController = GameFlowController.Instance;
        
        // 初始化输入系统
        InitializeInputSystem();
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerInputHandler: 初始化完成");
        }
    }
    
    void Update()
    {
        // 检查T键（测试功能）- 只在敌人阶段生效
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (gameFlowController != null && gameFlowController.IsEnemyPhase)
            {
                if (showDebugInfo)
                {
                    Debug.Log("PlayerInputHandler: 检测到T键，从敌人阶段回到玩家阶段");
                }
                gameFlowController.RequestNormalState(); // 回到玩家阶段
            }
        }
        
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
        // 敌人阶段完全禁用所有输入
        if (gameFlowController != null && gameFlowController.IsEnemyPhase)
        {
            return; // 直接返回，不处理任何输入
        }
        
        // 根据当前状态处理输入
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
        // 处理WASD移动
        if (movementController != null)
        {
            movementController.HandleMovement(moveInput, isMovePressed);
        }
        
        // 检测蓄力输入，先检查游戏状态和能量门槛
        if (isAttackPressed)
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerInputHandler: 检测到攻击输入，开始检查蓄力条件");
            }
            
            // 检查游戏状态，只能在Normal状态下蓄力
            if (gameFlowController != null && !gameFlowController.IsNormalState)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"PlayerInputHandler: 游戏状态不是Normal，无法蓄力 - 当前状态: {gameFlowController.CurrentState}");
                }
                return;
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
