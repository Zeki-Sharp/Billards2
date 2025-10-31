/// <summary>
/// 数据提取器类型枚举
/// 用于指定从事件数据中提取哪种类型的数据
/// </summary>
public enum DataExtractorType
{
    Health,     // 生命值百分比（0-1）
    Attack,     // 攻击力
    Defense,    // 防御力
    Speed,      // 移动速度
    Mana        // 魔法值
}

