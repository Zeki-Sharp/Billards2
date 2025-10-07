using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 伤害数字配置数据类
/// 包含伤害数字系统的各种配置参数
/// </summary>
[CreateAssetMenu(fileName = "DamageTextConfig", menuName = "DamageText/DamageText Config")]
public class DamageTextConfig : ScriptableObject
{
    [BoxGroup("颜色设置")]
    [LabelText("伤害数字颜色")]
    [Tooltip("伤害数字颜色")]
    public Color damageColor = Color.white;
    
    [BoxGroup("文本设置")]
    [LabelText("伤害数字前缀")]
    [Tooltip("伤害数字前缀（如：-、+、暴击等）")]
    public string damagePrefix = "-";
    
    [BoxGroup("文本设置")]
    [LabelText("伤害数字后缀")]
    [Tooltip("伤害数字后缀（如：伤害、治疗等）")]
    public string damageSuffix = "";
    
    [BoxGroup("对象池设置")]
    [LabelText("对象池大小")]
    [MinValue(1)]
    [Tooltip("对象池大小")]
    public int poolSize = 30;
    
    [BoxGroup("对象池设置")]
    [LabelText("自动扩展对象池")]
    [Tooltip("是否自动扩展对象池")]
    public bool autoExpandPool = true;
    
    [BoxGroup("对象池设置")]
    [LabelText("最大对象池大小")]
    [MinValue(1)]
    [Tooltip("最大对象池大小")]
    public int maxPoolSize = 100;
    
    [BoxGroup("字体设置")]
    [LabelText("字体大小")]
    [MinValue(1f)]
    [Tooltip("伤害数字字体大小")]
    public float fontSize = 24f;
    
    [BoxGroup("字体设置")]
    [LabelText("启用字体描边")]
    [Tooltip("是否启用字体描边")]
    public bool enableOutline = true;
    
    [BoxGroup("字体设置")]
    [LabelText("字体描边颜色")]
    [Tooltip("字体描边颜色")]
    public Color outlineColor = Color.black;
    
    [BoxGroup("字体设置")]
    [LabelText("字体描边宽度")]
    [MinValue(0f)]
    [Tooltip("字体描边宽度")]
    public float outlineWidth = 2f;

}