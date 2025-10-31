using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 碰撞触发器配置
/// </summary>
[System.Serializable]
public class CollisionTriggerConfig : TriggerBase
{
    /// <summary>
    /// 获取可用的 Tag 列表
    /// </summary>
    private static IEnumerable<ValueDropdownItem<string>> GetAvailableTags()
    {
        return new ValueDropdownList<string>
        {
            { "玩家 (Player)", "Player" },
            { "敌人 (Enemy)", "Enemy" },
            { "墙壁 (Wall)", "Wall" },
            { "洞 (Hole)", "Hole" },
            { "范围 (Range)", "Range" },
            { "陷阱 (Trap)", "Trap" },
            { "物品 (Item)", "Item" }
        };
    }

    [LabelText("碰撞目标标签")]
    [Tooltip("检测与哪个标签的物体碰撞")]
    [ValueDropdown("GetAvailableTags")]
    public string targetTag = "Enemy";
    
    public override ITrigger CreateTrigger()
    {
        var trigger = new CollisionTrigger();
        trigger.SetTargetTag(targetTag);
        return trigger;
    }
}

