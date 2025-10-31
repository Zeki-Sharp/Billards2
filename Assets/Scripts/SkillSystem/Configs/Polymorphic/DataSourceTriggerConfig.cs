using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 数据源触发器配置
/// </summary>
[System.Serializable]
public class DataSourceTriggerConfig : TriggerBase
{
    [LabelText("数据提取器类型")]
    [Tooltip("从事件数据中提取什么类型的数据")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;
    
    public override ITrigger CreateTrigger()
    {
        var trigger = new DataSourceTrigger();
        trigger.SetDataExtractorType(dataExtractorType);
        return trigger;
    }
}

