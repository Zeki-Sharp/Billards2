/// <summary>
/// 敌人状态枚举
/// </summary>
public enum EnemyState
{
    None,       // 无状态
    Telegraphing, // 预告中
    Spawning,   // 生成中
    Active,     // 活跃中
    Dead        // 已死亡
}
