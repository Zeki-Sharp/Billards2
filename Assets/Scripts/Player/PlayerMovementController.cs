using UnityEngine;

/// <summary>
/// 玩家移动控制器 - 处理WASD移动
/// 
/// 【核心职责】：
/// - 处理WASD键盘输入控制的移动
/// - 管理移动的速度和方向控制
/// - 实现移动的物理逻辑和性能优化
/// 
/// 【主要功能】：
/// - WASD移动：实时响应键盘输入，直接设置球体速度
/// - 移动停止：响应蓄力输入，立即停止WASD移动
/// - 移动状态管理：跟踪移动状态和方向变化
/// 
/// 【设计原则】：
/// - 专注移动实现，不处理输入检测（由PlayerInputHandler处理）
/// - 不处理权限检查（由PlayerInputHandler统一管理）
/// - 与物理系统协作，区分WASD移动和物理发射移动
/// - 假设调用者已经验证了移动权限
/// </summary>
public class PlayerMovementController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private PlayerCore playerCore;
    private GameFlowController gameFlowController;
    private PlayerData playerData;
    
    // 移动状态
    private bool isMoving = false;
    private Vector2 lastInputDirection = Vector2.zero;
    
    void Start()
    {
        // 获取组件引用
        playerCore = GetComponent<PlayerCore>();
        gameFlowController = GameFlowController.Instance;
        
        // 获取PlayerData引用
        if (playerCore != null)
        {
            playerData = playerCore.playerData;
        }
    }
    
    #region 移动处理
    
    /// <summary>
    /// 处理移动输入
    /// </summary>
    /// <param name="moveInput">移动输入向量</param>
    /// <param name="isPressed">是否按下移动键</param>
    public void HandleMovement(Vector2 moveInput, bool isPressed)
    {
        // 权限检查由PlayerInputHandler负责，这里直接处理移动逻辑
        
        // 如果有输入，应用微调力
        if (isPressed && moveInput.magnitude > 0.1f)
        {
            ApplyWASDForce(moveInput.normalized);
        }
        else
        {
            // 没有输入时停止球体
            StopMovement();
        }
    }
    
    /// <summary>
    /// 应用WASD方向的速度
    /// </summary>
    void ApplyWASDForce(Vector2 direction)
    {
        if (playerCore == null) return;
        
        // 从PlayerData获取微操速度
        float microMoveSpeed = playerData != null ? playerData.microMoveSpeed : 5f;
        
        // 直接计算目标速度
        Vector2 targetVelocity = direction * microMoveSpeed;
        
        // 检查方向是否改变，或者当前速度与目标速度差距较大
        Vector2 currentVelocity = playerCore.GetVelocity();
        bool directionChanged = Vector2.Distance(direction, lastInputDirection) > 0.1f;
        bool speedChanged = Vector2.Distance(currentVelocity, targetVelocity) > 0.5f;
        
        
        // 如果方向改变或速度差距较大，重新设置速度
        if (directionChanged || speedChanged)
        {
            // 直接设置速度
            playerCore.SetVelocity(targetVelocity);
            
            // 更新上次输入方向
            lastInputDirection = direction;
            
        }
        
        // 更新状态
        isMoving = true;
    }
    
    /// <summary>
    /// 停止移动
    /// </summary>
    void StopMovement()
    {
        if (isMoving)
        {
            isMoving = false;
            lastInputDirection = Vector2.zero;
            
            if (playerCore != null)
            {
                playerCore.SetVelocity(Vector2.zero);
                
            }
        }
    }
    
    /// <summary>
    /// 立即停止WASD移动（由蓄力输入触发）
    /// </summary>
    public void StopWASDMovement()
    {
        if (isMoving)
        {
            isMoving = false;
            lastInputDirection = Vector2.zero;
            
            if (playerCore != null)
            {
                playerCore.SetVelocity(Vector2.zero);
                
            }
        }
    }
    
    #endregion
    
    
    #region 公共属性
    
    /// <summary>
    /// 是否正在进行WASD移动
    /// </summary>
    public bool IsMoving => isMoving;
    
    /// <summary>
    /// 移动最大速度
    /// </summary>
    public float MoveSpeed => playerData != null ? playerData.microMoveSpeed : 5f;
    
    #endregion
}
