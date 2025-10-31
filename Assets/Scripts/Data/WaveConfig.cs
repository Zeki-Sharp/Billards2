using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 波次配置 - 存储单个波次的敌人生成配置
/// 专注于敌人生成，不包含奖励和描述信息
/// </summary>
[System.Serializable]
public class WaveConfig
{
    [LabelText("敌人列表")]
    [Tooltip("这个波次要生成的敌人配置")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();
    
    /// <summary>
    /// 获取波次的总敌人数
    /// </summary>
    /// <returns>总敌人数</returns>
    public int GetTotalEnemyCount()
    {
        int totalCount = 0;
        foreach (var enemySpawn in enemySpawns)
        {
            totalCount += enemySpawn.count;
        }
        return totalCount;
    }
    
    /// <summary>
    /// 验证波次配置的有效性
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool IsValid()
    {
        if (enemySpawns.Count == 0)
        {
            Debug.LogWarning("[WaveConfig] 波次没有配置敌人");
            return false;
        }
        
        // 验证每个敌人生成配置
        for (int i = 0; i < enemySpawns.Count; i++)
        {
            if (enemySpawns[i] == null)
            {
                Debug.LogError($"[WaveConfig] 波次的敌人配置 {i} 为空");
                return false;
            }
            
            if (enemySpawns[i].enemyData == null)
            {
                Debug.LogError($"[WaveConfig] 波次的敌人配置 {i} 没有设置敌人数据");
                return false;
            }
        }
        
        return true;
    }
}

/// <summary>
/// 敌人生成配置 - 专注于敌人数据、等级和数量配置
/// </summary>
[System.Serializable]
public class EnemySpawn
{
    [LabelText("敌人数据")]
    [HorizontalGroup("Main")]
    [LabelWidth(60)]
    public EnemyData enemyData;
    
    [LabelText("等级")]
    [HorizontalGroup("Main")]
    [LabelWidth(40)]
    [Tooltip("敌人等级（1, 2, 3...）。如果敌人没有配置此等级，将使用 Level 1")]
    [MinValue(1)]
    public int level = 1;
    
    [LabelText("数量")]
    [HorizontalGroup("Main")]
    [LabelWidth(40)]
    [MinValue(1)]
    public int count = 1;
    
    /// <summary>
    /// 重写ToString方法，用于在Inspector中显示
    /// </summary>
    public override string ToString()
    {
        if (enemyData == null)
        {
            return "未设置敌人数据";
        }
        return $"{enemyData.enemyName} Lv{level} x{count}";
    }
}