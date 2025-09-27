using UnityEngine;
using MoreMountains.Feedbacks;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// 对象特效播放器 - 管理单个对象上的所有特效
/// 自动查找子对象中的MMF Player并播放
/// </summary>
public class EffectPlayer : MonoBehaviour
{
    [Header("特效设置")]
    public bool enableDebugLog = true;
    
    // 注意：墙面撞击特效的旋转和位置摇晃现在由 WallManager 通过 WallEffectCalculator 计算，
    // 并通过事件系统传递计算结果，不再需要本地的 Controller 字段
    
    // 使用字典管理所有特效
    private Dictionary<string, MMFeedbacks> effects = new Dictionary<string, MMFeedbacks>();
    
    void Start()
    {
        // 自动查找子对象中的MMF Player
        InitializeEffects();
    }
    
    /// <summary>
    /// 初始化特效引用
    /// </summary>
    void InitializeEffects()
    {
        // 使用EffectMapping自动查找所有特效，只保存找到的特效
        foreach (string eventType in EffectMapping.GetAllEventTypes())
        {
            string mmfObjectName = EffectMapping.GetMMFObjectName(eventType);
            if (!string.IsNullOrEmpty(mmfObjectName))
            {
                var mmfPlayer = FindEffectInChildren(mmfObjectName);
                if (mmfPlayer != null)  // 只有找到特效才添加到字典中
                {
                    effects[eventType] = mmfPlayer;
                    
                    if (enableDebugLog)
                        Debug.Log($"EffectPlayer 找到特效: {eventType} -> {mmfObjectName}");
                }
                else
                {
                    if (enableDebugLog)
                        Debug.LogWarning($"EffectPlayer 未找到特效: {eventType} -> {mmfObjectName}，跳过添加");
                }
            }
        }
        
        if (enableDebugLog)
        {
            var foundEffects = effects.Keys.ToArray();
            Debug.Log($"EffectPlayer 初始化完成 - {gameObject.name}: 找到 {foundEffects.Length} 个特效 - {string.Join(", ", foundEffects)}");
        }
    }
    
    /// <summary>
    /// 在子对象中查找指定名称的MMF Player
    /// </summary>
    private MMFeedbacks FindEffectInChildren(string effectName)
    {
        // 查找指定名称的子对象
        Transform effectTransform = transform.Find(effectName);
        if (effectTransform != null)
        {
            // 获取MMF Player组件
            var mmfPlayer = effectTransform.GetComponent<MMFeedbacks>();
            if (mmfPlayer != null)
            {
                if (enableDebugLog)
                    Debug.Log($"找到 {effectName} 对象和MMFeedbacks组件");
                return mmfPlayer;
            }
            else
            {
                Debug.LogWarning($"找到 {effectName} 对象，但没有MMFeedbacks组件");
            }
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"未找到 {effectName} 子对象，当前对象: {gameObject.name}");
            if (enableDebugLog)
                Debug.Log($"未找到 {effectName} 子对象");
        }
        
        return null;
    }
    
    /// <summary>
    /// 播放指定类型的特效
    /// </summary>
    public void PlayEffect(string effectType, Vector3 position, Vector3 direction = default, Vector3 hitNormal = default, float hitSpeed = 0f, float wallHitRotationAngle = 0f, Vector3 wallHitPositionOffset = default)
    {
        // 直接使用事件类型作为键查找MMF Player
        if (effects.TryGetValue(effectType, out var mmfPlayer) && mmfPlayer != null)
        {
            // 设置特效位置
            mmfPlayer.transform.position = position;
            
            // 如果是 Hit Attack Effect，使用专门的计算器
            if (effectType == "Hit Attack Effect")
            {
                var globalEffect = GameObject.Find("PlayerHitAttackEffect");
                if (globalEffect != null)
                {
                    HitAttackEffectCalculator.SetEffectPosition(globalEffect, position, direction);
                }
                else
                {
                    Debug.LogWarning("未找到全局特效对象");
                }
            }
            
            // 设置特效方向（如果有方向信息）
            if (direction != Vector3.zero)
            {
                mmfPlayer.transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // 如果是撞击相关特效，使用事件中传递的计算结果
            if ((effectType == "Hit Attack Effect" || effectType == "Be Hit Effect") && hitNormal != Vector3.zero)
            {
                // 设置旋转角度（使用事件中的计算结果）
                if (wallHitRotationAngle != 0f)
                {
                    SetMMFRotationAngle(mmfPlayer, wallHitRotationAngle);
                    
                    if (enableDebugLog)
                        Debug.Log($"撞击旋转: 使用事件中的角度={wallHitRotationAngle:F2}");
                }
                
                // 设置位置摇晃（使用事件中的计算结果）
                if (wallHitPositionOffset != Vector3.zero)
                {
                    SetMMFPositionSpringBump(mmfPlayer, wallHitPositionOffset);
                    
                    if (enableDebugLog)
                        Debug.Log($"撞击位置摇晃: 使用事件中的偏移={wallHitPositionOffset}");
                }
            }
            
            // 播放特效
            mmfPlayer.PlayFeedbacks();
            
            if (enableDebugLog)
                Debug.Log($"播放 {effectType} 特效 - {gameObject.name} at {position}");
        }
        else
        {
            Debug.LogWarning($"未找到 {effectType} 特效的MMF Player - {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 设置 MMF 旋转角度
    /// </summary>
    private void SetMMFRotationAngle(MMFeedbacks mmfPlayer, float angle)
    {
        // 将 MMFeedbacks 转换为 MMF_Player 来访问 GetFeedbackOfType 方法
        var mmfPlayerComponent = mmfPlayer as MMF_Player;
        if (mmfPlayerComponent != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"MMF Player 反馈数量: {mmfPlayerComponent.FeedbacksList.Count}");
                for (int i = 0; i < mmfPlayerComponent.FeedbacksList.Count; i++)
                {
                    var feedback = mmfPlayerComponent.FeedbacksList[i];
                    Debug.Log($"反馈 {i}: {feedback.GetType().Name} - 标签: {feedback.Label}");
                }
            }
            
            // 先尝试不使用标签查找
            var rotationFeedback = mmfPlayerComponent.GetFeedbackOfType<MMF_Rotation>();
            if (rotationFeedback != null)
            {
                // 直接将计算出的角度赋值给 RemapCurveOne（它是 float 类型）
                rotationFeedback.RemapCurveOne = angle;
                
                if (enableDebugLog)
                    Debug.Log($"设置 MMF 旋转角度: {angle}");
            }
            else
            {
                Debug.LogWarning($"未找到 MMF_Rotation 反馈，无法设置旋转角度");
            }
        }
        else
        {
            Debug.LogWarning($"MMF Player 不是 MMF_Player 类型，无法设置旋转角度");
        }
    }
    
    /// <summary>
    /// 设置 MMF Position Spring Bump 参数
    /// </summary>
    private void SetMMFPositionSpringBump(MMFeedbacks mmfPlayer, Vector3 positionOffset)
    {
        // 将 MMFeedbacks 转换为 MMF_Player 来访问 GetFeedbackOfType 方法
        var mmfPlayerComponent = mmfPlayer as MMF_Player;
        if (mmfPlayerComponent != null)
        {
            // 先尝试不使用标签查找
            var positionSpringFeedback = mmfPlayerComponent.GetFeedbackOfType<MMF_PositionSpring>();
            if (positionSpringFeedback != null)
            {
                // 设置 Bump Position Min 为 (0,0,0)
                positionSpringFeedback.BumpPositionMin = Vector3.zero;
                
                // 设置 Bump Position Max 为计算出的位置偏移
                positionSpringFeedback.BumpPositionMax = positionOffset;
                
                if (enableDebugLog)
                    Debug.Log($"设置 MMF Position Spring Bump: Min={Vector3.zero}, Max={positionOffset}");
            }
            else
            {
                Debug.LogWarning($"未找到 MMF_PositionSpring 反馈，无法设置位置摇晃");
            }
        }
        else
        {
            Debug.LogWarning($"MMF Player 不是 MMF_Player 类型，无法设置位置摇晃");
        }
    }
    
    
    
}
