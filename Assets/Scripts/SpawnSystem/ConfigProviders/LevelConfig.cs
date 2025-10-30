using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 关卡配置 ScriptableObject
/// 存储整个关卡的波次序列和其他关卡相关配置
/// 支持多关卡配置复用，便于关卡设计师管理
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [BoxGroup("关卡基本信息")]
    [LabelText("关卡名称")]
    [Tooltip("关卡的显示名称")]
    public string levelName = "关卡1";
    
    [BoxGroup("关卡基本信息")]
    [LabelText("关卡描述")]
    [Tooltip("关卡的描述信息")]
    [TextArea(2, 4)]
    public string description = "第一关";
    
    [BoxGroup("关卡基本信息")]
    [LabelText("关卡图标")]
    [Tooltip("关卡的图标")]
    public Sprite levelIcon;
    
    [BoxGroup("初始敌人配置")]
    [LabelText("生成初始敌人")]
    [Tooltip("游戏开始时是否生成初始敌人")]
    public bool generateInitialEnemies = true;
    
    [BoxGroup("初始敌人配置")]
    [LabelText("初始敌人列表")]
    [Tooltip("游戏开始时生成的敌人配置")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<EnemySpawn> initialEnemies = new List<EnemySpawn>();
    
    [BoxGroup("初始敌人配置")]
    [LabelText("初始敌人生成范围")]
    [Tooltip("初始敌人生成的范围配置")]
    [SerializeField]
    public SpawnRangeConfig initialEnemySpawnRange;
    
    [BoxGroup("波次配置")]
    [LabelText("波次列表")]
    [Tooltip("关卡的波次序列")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<WaveConfig> waves = new List<WaveConfig>();
    
    [BoxGroup("波次配置")]
    [LabelText("波次敌人生成范围")]
    [Tooltip("波次敌人生成的范围配置")]
    [SerializeField]
    public SpawnRangeConfig waveEnemySpawnRange;
    
    [BoxGroup("波次配置")]
    [LabelText("是否循环波次")]
    [Tooltip("当所有波次完成后是否重新开始")]
    public bool loopWaves = true;
    
    [BoxGroup("回合限制配置")]
    [LabelText("启用回合限制")]
    [Tooltip("是否启用回合限制惩罚系统（超过回合数后开始扣血）")]
    public bool enableTurnPenalty = false;
    
    [BoxGroup("回合限制配置")]
    [LabelText("开始惩罚回合数")]
    [Tooltip("超过此回合数后开始扣血（例如：设置为20，则从第21回合开始扣血）")]
    [ShowIf("enableTurnPenalty")]
    [MinValue(1)]
    public int turnPenaltyThreshold = 20;
    
    [BoxGroup("回合限制配置")]
    [LabelText("每回合扣血量")]
    [Tooltip("超过回合限制后，每个玩家回合扣除的血量")]
    [ShowIf("enableTurnPenalty")]
    [MinValue(0.1f)]
    public float damagePerTurn = 5f;
    
    [BoxGroup("回合限制配置")]
    [LabelText("是否递增伤害")]
    [Tooltip("每回合扣血量是否递增（如第21回合扣5点，第22回合扣6点）")]
    [ShowIf("enableTurnPenalty")]
    public bool increasingDamage = false;
    
    [BoxGroup("回合限制配置")]
    [LabelText("伤害递增量")]
    [Tooltip("每回合增加的伤害量（仅在启用递增伤害时有效）")]
    [ShowIf("@enableTurnPenalty && increasingDamage")]
    [MinValue(0)]
    public float damageIncrement = 1f;
    
    
    /// <summary>
    /// 获取指定索引的波次配置
    /// </summary>
    /// <param name="index">波次索引</param>
    /// <returns>波次配置</returns>
    public WaveConfig GetWave(int index)
    {
        if (index >= 0 && index < waves.Count)
        {
            return waves[index];
        }
        return null;
    }
    
    /// <summary>
    /// 获取波次总数
    /// </summary>
    /// <returns>波次总数</returns>
    public int GetWaveCount()
    {
        return waves.Count;
    }
    
    /// <summary>
    /// 计算关卡总敌人数
    /// </summary>
    /// <returns>总敌人数</returns>
    public int GetTotalEnemyCount()
    {
        int totalCount = 0;
        
        // 计算初始敌人数
        if (generateInitialEnemies)
        {
            foreach (var enemySpawn in initialEnemies)
            {
                totalCount += enemySpawn.count;
            }
        }
        
        // 计算波次敌人数
        foreach (var wave in waves)
        {
            foreach (var enemySpawn in wave.enemySpawns)
            {
                totalCount += enemySpawn.count;
            }
        }
        return totalCount;
    }
    
    /// <summary>
    /// 计算关卡总波次数
    /// </summary>
    /// <returns>总波次数</returns>
    public int GetTotalWaveCount()
    {
        return waves.Count;
    }
    
    /// <summary>
    /// 验证关卡配置的有效性
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError($"[LevelConfig] 关卡名称不能为空: {name}");
            return false;
        }
        
        // 验证初始敌人配置
        if (generateInitialEnemies && initialEnemies.Count == 0)
        {
            Debug.LogWarning($"[LevelConfig] 关卡启用了初始敌人生成但没有配置初始敌人: {levelName}");
        }
        
        for (int i = 0; i < initialEnemies.Count; i++)
        {
            if (initialEnemies[i] == null)
            {
                Debug.LogError($"[LevelConfig] 初始敌人配置 {i} 为空: {levelName}");
                return false;
            }
            
            if (initialEnemies[i].enemyData == null)
            {
                Debug.LogError($"[LevelConfig] 初始敌人配置 {i} 没有设置敌人数据: {levelName}");
                return false;
            }
        }
        
        if (waves.Count == 0)
        {
            Debug.LogWarning($"[LevelConfig] 关卡没有配置波次: {levelName}");
            return false;
        }
        
        // 验证每个波次配置
        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i] == null)
            {
                Debug.LogError($"[LevelConfig] 波次 {i} 配置为空: {levelName}");
                return false;
            }
            
            
            if (waves[i].enemySpawns.Count == 0)
            {
                Debug.LogWarning($"[LevelConfig] 波次 {i} 没有配置敌人: {levelName}");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取初始敌人生成范围配置
    /// </summary>
    /// <returns>生成范围配置</returns>
    public SpawnRangeConfig GetInitialEnemySpawnRange()
    {
        return initialEnemySpawnRange;
    }
    
    /// <summary>
    /// 获取波次敌人生成范围配置
    /// </summary>
    /// <returns>生成范围配置</returns>
    public SpawnRangeConfig GetWaveEnemySpawnRange()
    {
        return waveEnemySpawnRange;
    }
    
    /// <summary>
    /// 获取关卡的调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string info = $"关卡: {levelName}, 波次数: {waves.Count}, 总敌人数: {GetTotalEnemyCount()}\n";
        info += $"初始敌人生成范围: {initialEnemySpawnRange.GetDebugInfo()}\n";
        info += $"波次敌人生成范围: {waveEnemySpawnRange.GetDebugInfo()}";
        return info;
    }
}
