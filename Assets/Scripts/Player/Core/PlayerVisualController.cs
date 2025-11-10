using UnityEngine;

/// <summary>
/// 玩家视觉表现控制器
/// 
/// 【核心职责】：
/// - 管理玩家角色的视觉表现（图标、颜色、特效等）
/// - 从 PlayerData 加载并应用视觉配置
/// - 与 Player 核心逻辑解耦，专注视觉层
/// 
/// 【使用方法】：
/// 1. 将此组件添加到 Player 预制体
/// 2. 在 Inspector 中拖拽 Image(1) 上的 SpriteRenderer 组件到 characterSpriteRenderer 字段
/// 3. Player.SetPlayerData() 时会自动调用 ApplyVisuals()
/// 
/// 【扩展性】：
/// - 可添加动画播放
/// - 可添加特效管理
/// - 可添加颜色渐变等效果
/// </summary>
public class PlayerVisualController : MonoBehaviour
{
    [Header("视觉组件引用")]
    [Tooltip("角色图标显示组件（拖拽 Image(1) 上的 SpriteRenderer 组件）")]
    [SerializeField] private SpriteRenderer characterSpriteRenderer;
    [Tooltip("角色动画控制器（挂在 Image(1) 上的 Animator 组件）")]
    [SerializeField] private Animator characterAnimator;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] [Tooltip("默认攻击动画触发冷却，未配置时使用")] 
    private float defaultAttackTriggerCooldown = 0.1f;
    
    private const string AnimatorParamIsMoving = "IsMoving";
    private const string AnimatorTriggerAttack = "AttackTrigger";
    
    private RuntimeAnimatorController defaultController;
    private float activeAttackTriggerCooldown;
    private PlayerStateMachine stateMachine;
    private PlayerBehavior playerBehavior;
    private bool hasSubscribedEvents;
    private float lastAttackTriggerTime = -999f;
    
    private void Awake()
    {
        CacheComponents();
        if (characterAnimator != null)
        {
            defaultController = characterAnimator.runtimeAnimatorController;
        }
    }
    
    private void OnEnable()
    {
        CacheComponents();
        SubscribeEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeEvents();
    }
    
    /// <summary>
    /// 应用角色视觉配置
    /// </summary>
    /// <param name="playerData">玩家配置数据</param>
    public void ApplyVisuals(PlayerData playerData)
    {
        if (playerData == null)
        {
            Debug.LogWarning("PlayerVisualController: PlayerData 为空，无法应用视觉配置");
            return;
        }
        
        // 应用角色图标
        ApplyCharacterIcon(playerData);
        
        // 应用动画
        ApplyCharacterAnimation(playerData);
        
        // 可扩展：应用角色颜色
        // ApplyCharacterColor(playerData);
        
        // 可扩展：播放出场动画
        // PlaySpawnAnimation();
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerVisualController: 已应用角色视觉 - {playerData.info.name}");
        }
    }
    
    /// <summary>
    /// 应用角色图标
    /// </summary>
    private void ApplyCharacterIcon(PlayerData playerData)
    {
        if (characterSpriteRenderer == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerVisualController: characterSpriteRenderer 未设置，跳过图标更新");
            }
            return;
        }
        
        if (playerData.info == null)
        {
            Debug.LogWarning("PlayerVisualController: PlayerData.info 为空");
            return;
        }
        
        if (playerData.info.icon != null)
        {
            characterSpriteRenderer.sprite = playerData.info.icon;
            
            if (showDebugInfo)
            {
                Debug.Log($"PlayerVisualController: 已设置角色图标 - {playerData.info.icon.name}");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"PlayerVisualController: 角色 {playerData.info.name} 没有配置图标");
            }
        }
    }
    
    /// <summary>
    /// 应用角色颜色（可选功能，暂时保留）
    /// </summary>
    private void ApplyCharacterColor(PlayerData playerData)
    {
        if (characterSpriteRenderer == null || playerData.info == null)
            return;
        
        // 可以应用到图标的颜色叠加
        // characterSpriteRenderer.color = playerData.info.color;
        
        // 或者应用到其他视觉元素（边框、光晕等）
    }
    
    #region 公共接口
    
    /// <summary>
    /// 更新角色图标（运行时动态更换）
    /// </summary>
    public void UpdateIcon(Sprite newIcon)
    {
        if (characterSpriteRenderer != null && newIcon != null)
        {
            characterSpriteRenderer.sprite = newIcon;
        }
    }
    
    /// <summary>
    /// 应用角色动画
    /// </summary>
    private void ApplyCharacterAnimation(PlayerData playerData)
    {
        if (characterAnimator == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("PlayerVisualController: characterAnimator 未设置，跳过动画配置");
            }
            return;
        }
        
        RuntimeAnimatorController overrideController = playerData.animatorController;
        activeAttackTriggerCooldown = Mathf.Max(0f, playerData.attackTriggerCooldown);
        
        if (overrideController != null)
        {
            characterAnimator.runtimeAnimatorController = overrideController;
            
            if (showDebugInfo)
            {
                Debug.Log($"PlayerVisualController: 应用动画 Override - {overrideController.name}");
            }
        }
        else
        {
            characterAnimator.runtimeAnimatorController = defaultController;
            
            if (showDebugInfo && defaultController != null)
            {
                Debug.Log($"PlayerVisualController: 使用默认动画控制器 - {defaultController.name}");
            }
        }
        
        ResetAnimatorParameters();
    }
    
    /// <summary>
    /// 重置动画参数
    /// </summary>
    private void ResetAnimatorParameters()
    {
        if (characterAnimator == null) return;
        
        characterAnimator.ResetTrigger(AnimatorTriggerAttack);
        characterAnimator.SetBool(AnimatorParamIsMoving, false);
        lastAttackTriggerTime = -999f;
    }
    
    /// <summary>
    /// 更新图标颜色
    /// </summary>
    public void UpdateIconColor(Color color)
    {
        if (characterSpriteRenderer != null)
        {
            characterSpriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// 重置视觉表现
    /// </summary>
    public void ResetVisuals()
    {
        if (characterSpriteRenderer != null)
        {
            characterSpriteRenderer.sprite = null;
            characterSpriteRenderer.color = Color.white;
        }
    }
    
    #endregion
    
    #region Inspector 验证
    
    private void OnValidate()
    {
        // Inspector 中验证配置
        if (characterSpriteRenderer == null)
        {
            Debug.LogWarning("PlayerVisualController: 未设置 characterSpriteRenderer，请在 Inspector 中拖拽 Image(1) 上的 SpriteRenderer 组件");
        }
        
        if (characterAnimator == null)
        {
            Debug.LogWarning("PlayerVisualController: 未设置 characterAnimator，请在 Inspector 中拖拽 Animator 组件");
        }
    }
    
    #endregion
    
    private void CacheComponents()
    {
        if (stateMachine == null)
        {
            stateMachine = GetComponent<PlayerStateMachine>();
        }
        
        if (playerBehavior == null)
        {
            playerBehavior = GetComponent<PlayerBehavior>();
        }
    }
    
    private void SubscribeEvents()
    {
        if (hasSubscribedEvents) return;
        
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += HandleStateChanged;
        }
        
        GameEventBus.OnBallStarted += HandleBallStarted;
        GameEventBus.OnBallStopped += HandleBallStopped;
        GameEventBus.OnDamage += HandleDamageEvent;
        
        hasSubscribedEvents = true;
    }
    
    private void UnsubscribeEvents()
    {
        if (!hasSubscribedEvents) return;
        
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= HandleStateChanged;
        }
        
        GameEventBus.OnBallStarted -= HandleBallStarted;
        GameEventBus.OnBallStopped -= HandleBallStopped;
        GameEventBus.OnDamage -= HandleDamageEvent;
        
        hasSubscribedEvents = false;
    }
    
    private void HandleStateChanged(PlayerStateMachine.PlayerState newState, PlayerStateMachine.PlayerState oldState)
    {
        bool shouldMove = newState == PlayerStateMachine.PlayerState.Moving;
        UpdateMovementState(shouldMove);
    }
    
    private void HandleBallStarted(BallPhysics ball)
    {
        if (!IsMyBall(ball)) return;
        UpdateMovementState(true);
    }
    
    private void HandleBallStopped(BallPhysics ball)
    {
        if (!IsMyBall(ball)) return;
        UpdateMovementState(false);
    }
    
    private void HandleDamageEvent(DamageEvent damageEvent)
    {
        if (damageEvent.Source != gameObject) return;
        
        float cooldown = activeAttackTriggerCooldown > 0f ? activeAttackTriggerCooldown : defaultAttackTriggerCooldown;
        if (Time.time - lastAttackTriggerTime < cooldown)
        {
            return;
        }
        
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(AnimatorTriggerAttack);
            lastAttackTriggerTime = Time.time;
            
            if (showDebugInfo)
            {
                Debug.Log("PlayerVisualController: 触发 Attack 动画");
            }
        }
    }
    
    private void UpdateMovementState(bool isMoving)
    {
        if (characterAnimator == null) return;
        if (characterAnimator.GetBool(AnimatorParamIsMoving) == isMoving) return;
        
        characterAnimator.SetBool(AnimatorParamIsMoving, isMoving);
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerVisualController: 设置动画 IsMoving = {isMoving}");
        }
    }
    
    private bool IsMyBall(BallPhysics ball)
    {
        if (ball == null) return false;
        
        if (playerBehavior != null)
        {
            return playerBehavior.IsMyBall(ball);
        }
        
        return ball.gameObject == gameObject;
    }
}

