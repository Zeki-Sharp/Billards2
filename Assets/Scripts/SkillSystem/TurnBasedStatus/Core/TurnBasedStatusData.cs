using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合制状态数据 - 配置状态效果的静态数据
/// 
/// 【设计理念】：
/// - ScriptableObject 配置
/// - 纯回合制，不支持实时秒数
/// - 可配置堆叠规则和触发时机
/// 
/// 【使用场景】：
/// - 点燃、中毒、流血等DoT效果
/// - 加速、减速、护盾等Buff/Debuff
/// </summary>
[CreateAssetMenu(fileName = "TurnBasedStatusData", menuName = "Game/Turn Based Status Data")]
public class TurnBasedStatusData : ScriptableObject
{
    [BoxGroup("基本信息")]
    [LabelText("状态ID")]
    [Tooltip("唯一标识符")]
    public string statusID = "Burning";
    
    [BoxGroup("基本信息")]
    [LabelText("显示名称")]
    [Tooltip("UI中显示的名称")]
    public string displayName = "点燃";
    
    [BoxGroup("基本信息")]
    [LabelText("图标")]
    [Tooltip("状态图标（用于UI显示）")]
    [PreviewField(50)]
    public Sprite icon;
    
    [BoxGroup("基本信息")]
    [LabelText("图标颜色")]
    [Tooltip("图标的颜色（白色图标会被染成此颜色）")]
    public Color iconColor = Color.white;  // 默认白色不染色
    
    [BoxGroup("基本信息")]
    [LabelText("描述")]
    [Tooltip("状态效果描述")]
    [TextArea(2, 4)]
    public string description = "持续造成火焰伤害";
    
    [BoxGroup("回合配置")]
    [LabelText("基础持续回合数")]
    [Tooltip("状态效果持续的回合数")]
    [MinValue(1)]
    public int baseDurationInTurns = 2;
    
    [BoxGroup("回合配置")]
    [LabelText("触发阶段")]
    [Tooltip("在哪个阶段触发效果")]
    public GameFlowState triggerPhase = GameFlowState.EnemyPhaseEnd;
    
    [BoxGroup("伤害配置")]
    [LabelText("每回合伤害")]
    [Tooltip("每回合造成的伤害值（DoT类型使用）")]
    [MinValue(0f)]
    public float baseDamagePerTurn = 5f;
    
    [BoxGroup("堆叠配置")]
    [LabelText("最大堆叠层数")]
    [Tooltip("0表示无限堆叠")]
    [MinValue(0)]
    public int maxStacks = 0;
    
    [BoxGroup("视觉效果")]
    [LabelText("特效预制体")]
    [Tooltip("状态激活时显示的粒子特效（可选）")]
    public GameObject vfxPrefab;
    
    [BoxGroup("视觉效果")]
    [LabelText("效果颜色")]
    [Tooltip("UI中显示的颜色标识")]
    public Color effectColor = new Color(1f, 0.5f, 0f, 1f);  // 橙红色（火焰）
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(statusID) && baseDurationInTurns > 0;
    }
}

