using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 波次配置 - 临时空脚本
/// </summary>
[System.Serializable]
public class WaveConfig
{
    [Header("波次信息")]
    public string waveName = "Wave";
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();
}

/// <summary>
/// 敌人生成配置 - 临时空脚本
/// </summary>
[System.Serializable]
public class EnemySpawn
{
    public EnemyData enemyData;
    public int count = 1;
    public bool useRandomPosition = true;
    public Vector2 customPosition = Vector2.zero;
}