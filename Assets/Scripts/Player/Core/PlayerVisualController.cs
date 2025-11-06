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
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
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
    }
    
    #endregion
}

