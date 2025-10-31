using UnityEngine;

/// <summary>
/// 游戏统计数据
/// 记录一局游戏中的各种统计信息
/// </summary>
[System.Serializable]
public class GameStatistics
{
    #region 战斗统计
    
    /// <summary>
    /// 总击杀敌人数（跨所有关卡）
    /// </summary>
    public int totalEnemyKills = 0;
    
    /// <summary>
    /// 通过的关卡数
    /// </summary>
    public int levelsCompleted = 0;
    
    /// <summary>
    /// 总受伤次数
    /// </summary>
    public int totalDamageTaken = 0;
    
    #endregion
    
    #region 游戏进度
    
    /// <summary>
    /// 游戏时长（秒）
    /// </summary>
    public float gameTime = 0f;
    
    /// <summary>
    /// 当前回合数
    /// </summary>
    public int currentTurn = 0;
    
    #endregion
    
    #region 管理方法
    
    /// <summary>
    /// 增加击杀数
    /// </summary>
    public void AddKill()
    {
        totalEnemyKills++;
    }
    
    /// <summary>
    /// 增加关卡完成数
    /// </summary>
    public void AddLevelCompleted()
    {
        levelsCompleted++;
    }
    
    /// <summary>
    /// 增加受伤次数
    /// </summary>
    public void AddDamageTaken()
    {
        totalDamageTaken++;
    }
    
    /// <summary>
    /// 更新游戏时间
    /// </summary>
    public void UpdateGameTime(float deltaTime)
    {
        gameTime += deltaTime;
    }
    
    /// <summary>
    /// 重置所有统计数据
    /// </summary>
    public void Clear()
    {
        totalEnemyKills = 0;
        levelsCompleted = 0;
        totalDamageTaken = 0;
        gameTime = 0f;
        currentTurn = 0;
    }
    
    #endregion
}

