using System.Collections.Generic;

/// <summary>
/// 生成策略接口 - 决定生成什么内容
/// 
/// 【核心职责】：
/// - 根据配置数据决定要生成的对象列表
/// - 不关心何时生成、在哪生成、如何生成
/// - 纯逻辑层，无状态，可序列化配置
/// 
/// 【设计原则】：
/// - 单一职责：只管"生成什么"，不管其他
/// - 无状态：每次调用独立，不保存状态
/// - 可配置：通过配置数据驱动行为
/// </summary>
/// <typeparam name="T">生成对象的数据类型（如ItemConfig、EnemyData）</typeparam>
public interface ISpawnStrategy<T>
{
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的数据列表</returns>
    List<T> GetSpawnList();
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    int GetSpawnCount();
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    bool ValidateConfig();
}
