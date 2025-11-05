using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 全局输入管理器 - 场景单例
/// 
/// 【核心职责】：
/// - 检测所有原始输入（鼠标点击、滚轮）
/// - 进行射线检测判断点击目标
/// - 发布原始输入事件
/// - 处理UI遮挡检测
/// 
/// 【设计原则】：
/// - 场景中唯一的输入检测入口
/// - 不受球体启用/禁用影响
/// - 只负责输入检测，不处理游戏逻辑
/// - 不管理选中状态
/// - 不处理蓄力逻辑
/// 
/// 【执行顺序】：
/// - BEFORE_CONTROLLER: 输入层，在控制器之前执行
/// - 为控制器提供原始输入事件
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.BEFORE_CONTROLLER)]
public class GlobalInputManager : MonoBehaviour
{
    [Header("检测配置")]
    [SerializeField] 
    [Tooltip("玩家球体的Layer（用于射线检测）")]
    private LayerMask playerBallLayer;
    
    [SerializeField]
    [Tooltip("阻挡点击的UI Layer")]
    private LayerMask blockingUILayers;
    
    [SerializeField]
    [Tooltip("射线检测距离")]
    private float raycastDistance = 100f;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // ⚠️ 输入事件已迁移到 GameEventBus
    // 使用 GameEventBus.PublishBallClicked()、PublishScrollInput() 等方法发布事件
    
    // Input System
    private InputAction leftClickAction;
    private InputAction rightClickAction;
    private InputAction scrollAction;
    private InputAction mousePositionAction;
    private InputActionMap inputActionMap;
    
    // 摄像机引用
    private Camera mainCamera;
    
    // 场景单例
    private static GlobalInputManager instance;
    public static GlobalInputManager Instance => instance;
    
    void Awake()
    {
        // 单例检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning("GlobalInputManager: 场景中存在多个实例，销毁多余实例");
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("GlobalInputManager: 未找到主摄像机！");
        }
        
        // 初始化输入系统
        InitializeInputSystem();
        
        if (showDebugInfo)
        {
            Debug.Log("GlobalInputManager: 初始化完成");
        }
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
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    void Update()
    {
        // 检查游戏是否暂停
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            return;
        }
        
        // ✅ 使用统一权限管理器检查是否在玩家回合
        if (PlayerInputPermissionManager.Instance == null || 
            !PlayerInputPermissionManager.Instance.CanProcessInputInCurrentPhase())
        {
            return; // 不在玩家回合，不处理输入
        }
        
        // 处理输入
        HandleInput();
    }
    
    #region 输入系统初始化
    
    /// <summary>
    /// 初始化输入系统
    /// </summary>
    void InitializeInputSystem()
    {
        inputActionMap = new InputActionMap("GlobalInput");
        
        // 左键
        leftClickAction = inputActionMap.AddAction("LeftClick", InputActionType.Button, "<Mouse>/leftButton");
        
        // 右键
        rightClickAction = inputActionMap.AddAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
        
        // 滚轮
        scrollAction = inputActionMap.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");
        
        // 鼠标位置
        mousePositionAction = inputActionMap.AddAction("MousePosition", InputActionType.Value, "<Mouse>/position");
        
        inputActionMap.Enable();
        
        if (showDebugInfo)
        {
            Debug.Log("GlobalInputManager: Input System 初始化完成");
        }
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 处理滚轮输入
        HandleScrollInput();
        
        // 处理右键取消
        HandleRightClick();
        
        // 处理左键点击
        HandleLeftClick();
    }
    
    /// <summary>
    /// 处理滚轮输入
    /// </summary>
    void HandleScrollInput()
    {
        float scrollDelta = scrollAction.ReadValue<float>();
        
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            GameEventBus.PublishScrollInput(scrollDelta);
            
            if (showDebugInfo)
            {
                Debug.Log($"GlobalInputManager: 检测到滚轮输入 delta={scrollDelta}");
            }
        }
    }
    
    /// <summary>
    /// 处理右键点击
    /// </summary>
    void HandleRightClick()
    {
        if (rightClickAction.WasPressedThisFrame())
        {
            // 检查是否在UI上
            GameObject blockingUI = GetBlockingUI();
            if (blockingUI != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"GlobalInputManager: 右键点击在UI上，忽略 - UI名称: {blockingUI.name}");
                }
                return;
            }
            
            GameEventBus.PublishCancelInput();
            
            if (showDebugInfo)
            {
                Debug.Log("GlobalInputManager: 检测到右键取消输入");
            }
        }
    }
    
    /// <summary>
    /// 处理左键点击
    /// </summary>
    void HandleLeftClick()
    {
        if (leftClickAction.WasPressedThisFrame())
        {
            // 检查是否在UI上
            GameObject blockingUI = GetBlockingUI();
            if (blockingUI != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"GlobalInputManager: 左键点击在UI上，忽略 - UI名称: {blockingUI.name}, Layer: {LayerMask.LayerToName(blockingUI.layer)}");
                }
                return;
            }
            
            // 射线检测
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            GameObject hitBall = RaycastForBall(mousePos);
            
            if (hitBall != null)
            {
                // 点击到球体
                GameEventBus.PublishBallClicked(hitBall);
                
                if (showDebugInfo)
                {
                    Debug.Log($"GlobalInputManager: 点击球体 - {hitBall.name}");
                }
            }
            else
            {
                // 没有点击到球体，可能是发射操作
                GameEventBus.PublishLaunchInput();
                
                if (showDebugInfo)
                {
                    Debug.Log("GlobalInputManager: 左键点击（未击中球体），发布发射输入");
                }
            }
        }
    }
    
    #endregion
    
    #region 射线检测
    
    /// <summary>
    /// 射线检测球体（2D游戏使用2D射线检测）
    /// </summary>
    GameObject RaycastForBall(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("GlobalInputManager: 主摄像机为空，无法进行射线检测");
            return null;
        }
        
        // 将屏幕坐标转换为世界坐标（2D）
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);
        
        // 调试：记录射线信息
        if (showDebugInfo)
        {
            Debug.Log($"GlobalInputManager: 2D射线检测 - 屏幕坐标={screenPosition}, 世界坐标={worldPoint2D}, Layer={playerBallLayer.value}");
        }
        
        // 使用 2D 射线检测（点击检测）
        RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero, 0f, playerBallLayer);
        
        if (hit.collider != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"GlobalInputManager: ✅ 2D射线击中球体 - {hit.collider.gameObject.name}, Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            return hit.collider.gameObject;
        }
        
        // 如果没击中，尝试用OverlapPoint（更适合点击检测）
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPoint2D, playerBallLayer);
        if (colliders.Length > 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"GlobalInputManager: ✅ OverlapPoint检测到球体 - {colliders[0].gameObject.name}");
            }
            return colliders[0].gameObject;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"GlobalInputManager: 2D射线没有击中任何球体");
        }
        
        return null;
    }
    
    #endregion
    
    #region UI检测
    
    /// <summary>
    /// 检测鼠标是否在阻挡点击的UI上
    /// </summary>
    bool IsPointerOverUI()
    {
        return GetBlockingUI() != null;
    }
    
    /// <summary>
    /// 获取阻挡点击的UI对象（用于调试）
    /// </summary>
    GameObject GetBlockingUI()
    {
        if (EventSystem.current == null)
        {
            return null;
        }
        
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = mousePositionAction.ReadValue<Vector2>();
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            if (IsInLayerMask(result.gameObject.layer, blockingUILayers))
            {
                return result.gameObject;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 检查Layer是否在LayerMask中
    /// </summary>
    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask) != 0;
    }
    
    #endregion
}

