using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 弱点攻击效果配置 - 在敌人身上标记弱点，命中弱点造成额外伤害
/// </summary>
[System.Serializable]
public class WeakPointEffectConfig : EffectBase
{
    [BoxGroup("弱点攻击配置")]
    [LabelText("弱点标记预制体")]
    [Tooltip("弱点标记的UI预制体，将显示在敌人身上")]
    [AssetsOnly]
    [Required]
    public GameObject weakPointMarkerPrefab;

    [BoxGroup("弱点攻击配置")]
    [LabelText("判定半径")]
    [Tooltip("弱点判定的半径（单位）")]
    [Range(0.1f, 2f)]
    public float weakPointRadius = 0.5f;

    [BoxGroup("弱点攻击配置")]
    [LabelText("伤害倍率")]
    [Tooltip("命中弱点时的伤害倍率")]
    [Range(1.0f, 5.0f)]
    public float weakPointDamageMultiplier = 1.5f;

    [BoxGroup("弱点攻击配置")]
    [LabelText("击中后刷新")]
    [Tooltip("命中弱点后是否立即刷新位置")]
    public bool weakPointRefreshOnHit = true;

    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var weakPointEffect = new WeakPointEffect();
        
        // 设置弱点攻击专用参数
        weakPointEffect.SetParameters(
            weakPointMarkerPrefab,
            weakPointRadius,
            weakPointDamageMultiplier,
            weakPointRefreshOnHit
        );
        
        // 弱点效果是持续效果，生命周期由技能管理
        return weakPointEffect;
    }

    public override string GetDebugInfo()
    {
        return $"弱点攻击: {weakPointDamageMultiplier:F1}x伤害 (半径:{weakPointRadius:F1})";
    }
}

