using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

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
public class TurnBasedStatusData : SerializedScriptableObject
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
    [LabelText("触发阶段")]
    [Tooltip("在哪个阶段触发效果")]
    public GameFlowState triggerPhase = GameFlowState.EnemyPhaseEnd;
    
    [BoxGroup("视觉效果")]
    [LabelText("特效预制体")]
    [Tooltip("状态激活时显示的粒子特效（可选）")]
    public GameObject vfxPrefab;
    
    [BoxGroup("视觉效果")]
    [LabelText("效果颜色")]
    [Tooltip("UI中显示的颜色标识")]
    public Color effectColor = new Color(1f, 0.5f, 0f, 1f);  // 橙红色（火焰）

    [SerializeField, HideInInspector]
    private int legacyBaseDurationInTurns = 2;

    [SerializeField, HideInInspector]
    private float legacyBaseDamagePerTurn = 5f;

    [SerializeField, HideInInspector]
    private int legacyMaxStacks = 0;

    [SerializeField, HideInInspector]
    private bool behaviourConfigInitialized = false;

    [SerializeField, HideInInspector]
    private string behaviourConfigTypeName = null;

    [BoxGroup("行为配置")]
    [LabelText("状态类型与参数")]
    [Tooltip("选择具体状态类型，并配置对应的堆叠与伤害规则")]
    [InlineProperty]
    [HideLabel]
    [OdinSerialize]
    private TurnBasedStatusBehaviourConfig behaviourConfig;
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        EnsureBehaviourConfig();
        return !string.IsNullOrEmpty(statusID);
    }

    /// <summary>
    /// 获取要附加的运行时组件类型
    /// </summary>
    public System.Type GetComponentType()
    {
        EnsureBehaviourConfig();
        EnsureBehaviourConfig();
        return behaviourConfig?.ComponentType ?? typeof(BurningStatus);
    }

    /// <summary>
    /// 初始化运行时组件
    /// </summary>
    public void ApplyInitialValues(TurnBasedStatusComponent component)
    {
        EnsureBehaviourConfig();
        behaviourConfig?.ApplyInitialValues(this, component);
    }

    /// <summary>
    /// 当状态再次被施加时调用，处理堆叠等逻辑
    /// </summary>
    public void OnStackApplied(TurnBasedStatusComponent component)
    {
        EnsureBehaviourConfig();
        behaviourConfig?.OnStackApplied(this, component);
    }

    /// <summary>
    /// 每次回合结算后调用，允许修改栈数或持续时间
    /// </summary>
    public void OnTurnResolved(TurnBasedStatusComponent component)
    {
        EnsureBehaviourConfig();
        behaviourConfig?.OnTurnResolved(this, component);
    }

    public string GetDebugDescription()
    {
        EnsureBehaviourConfig();
        return behaviourConfig?.GetDebugDescription(this) ?? displayName;
    }

    private void EnsureBehaviourConfig()
    {
        if (behaviourConfig == null)
        {
            behaviourConfig = new BurningStatusBehaviourConfig();
        }

        var currentTypeName = behaviourConfig.GetType().FullName;

        if (!behaviourConfigInitialized || behaviourConfigTypeName != currentTypeName)
        {
            behaviourConfigInitialized = true;
            behaviourConfigTypeName = currentTypeName;
            behaviourConfig?.SyncLegacyValues(this);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureBehaviourConfig();
    }
#endif

    #region Legacy字段访问

    public int LegacyBaseDurationInTurns => legacyBaseDurationInTurns;
    public float LegacyBaseDamagePerTurn => legacyBaseDamagePerTurn;
    public int LegacyMaxStacks => legacyMaxStacks;

    #endregion
}

