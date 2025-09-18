using UnityEngine;

/// <summary>
/// 玩家移动控制器 - 处理WASD移动和微调移动
/// 
/// 【核心职责】：
/// - 处理WASD键盘输入控制的微调移动
/// - 管理微调移动的速度和方向控制
/// - 与GameFlowController协调，确保移动权限
/// - 提供平滑的微调移动协程
/// 
/// 【主要功能】：
/// - WASD移动：实时响应键盘输入，直接设置球体速度
/// - 微调移动：平滑移动到指定位置（协程实现）
/// - 移动权限：检查游戏状态，确保在正确状态下才能移动
/// - 移动停止：响应蓄力输入，立即停止WASD移动
/// 
/// 【设计原则】：
/// - 专注移动控制，不处理输入检测（由PlayerInputHandler处理）
/// - 与物理系统协作，区分微调移动和物理发射移动
/// - 状态感知：根据游戏状态决定是否允许移动
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
    private bool isMicroMoving = false;
    private float lastMicroMoveTime = 0f;
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
        // 检查是否允许移动
        if (!CanMove())
        {
            return;
        }
        
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
    /// 检查是否可以移动
    /// </summary>
    bool CanMove()
    {
        // 检查游戏状态
        if (gameFlowController == null)
        {
            Debug.LogWarning("PlayerMovementController: GameFlowController实例为空！");
            return false;
        }
        
        // 敌人阶段完全禁用移动
        if (gameFlowController.IsEnemyPhase)
        {
            return false;
        }
        
        // 允许在Normal和Transition状态下移动
        if (!gameFlowController.IsNormalState && !gameFlowController.IsTransitionState)
        {
            return false;
        }
        
        return true;
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
        isMicroMoving = true;
        lastMicroMoveTime = Time.time;
    }
    
    /// <summary>
    /// 停止移动
    /// </summary>
    void StopMovement()
    {
        if (isMicroMoving)
        {
            isMicroMoving = false;
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
        if (isMicroMoving)
        {
            isMicroMoving = false;
            lastInputDirection = Vector2.zero;
            
            if (playerCore != null)
            {
                playerCore.SetVelocity(Vector2.zero);
                
            }
        }
    }
    
    #endregion
    
    #region 微调移动
    
    /// <summary>
    /// 微调移动（由外部调用）
    /// </summary>
    /// <param name="direction">移动方向</param>
    /// <param name="distance">移动距离</param>
    public void MicroMove(Vector2 direction, float distance)
    {
        // 检查方向向量是否有效
        if (direction.magnitude < 0.1f)
        {
            return;
        }
        
        // 如果正在进行微调移动，停止当前的移动
        if (isMicroMoving)
        {
            StopAllCoroutines();
            isMicroMoving = false;
        }
        
        // 如果白球正在物理移动，不能进行微调
        if (playerCore != null && playerCore.IsPhysicsMoving())
        {
            return;
        }
        
        // 获取当前位置
        Vector2 currentPosition = transform.position;
        
        // 计算目标位置
        Vector2 targetPosition = currentPosition + direction.normalized * distance;
        
        
        // 开始平滑移动协程
        StartCoroutine(SmoothMicroMove(targetPosition));
    }
    
    /// <summary>
    /// 平滑微调移动协程
    /// </summary>
    System.Collections.IEnumerator SmoothMicroMove(Vector2 targetPosition)
    {
        isMicroMoving = true;
        Vector2 startPosition = transform.position;
        float distance = Vector2.Distance(startPosition, targetPosition);
        float microMoveSpeed = playerData != null ? playerData.microMoveSpeed : 5f;
        float duration = distance / microMoveSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && isMicroMoving)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 使用平滑插值
            transform.position = Vector2.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }
        
        // 只有在没有被中断的情况下才设置最终位置
        if (isMicroMoving)
        {
            // 确保最终位置准确
            transform.position = targetPosition;
            isMicroMoving = false;
        }
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 是否正在进行微调移动
    /// </summary>
    public bool IsMicroMoving => isMicroMoving;
    
    /// <summary>
    /// 微调移动最大速度
    /// </summary>
    public float MicroMoveSpeed => playerData != null ? playerData.microMoveSpeed : 5f;
    
    #endregion
}
