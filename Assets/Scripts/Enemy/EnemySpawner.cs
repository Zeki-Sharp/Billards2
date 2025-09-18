using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;


public class EnemySpawner : MonoBehaviour
{
    [Header("敌人生成设置")]
    public Transform enemyParent; // 敌人父物体（可选）
    
    [Header("生成范围设置")]
    public float minX = -10f; // 生成范围左边界
    public float maxX = 10f;  // 生成范围右边界
    public float minY = -5f;  // 生成范围下边界
    public float maxY = 5f;   // 生成范围上边界
    
    [Header("波次配置")]
    public List<WaveConfig> waveConfigs = new List<WaveConfig>(); // 波次配置列表
    public bool loopWaves = true; // 是否循环波次
    
    [Header("预览特效设置")]
    public GameObject previewEffectPrefab; // 预告特效预制体（包含点位出现和点位消失子物体）
    
    [Header("测试设置")]
    public KeyCode spawnKey = KeyCode.Space; // 手动触发下一波
    public KeyCode toggleKey = KeyCode.T; // 切换生成开关
    
    [Header("事件")]
    public System.Action<List<Vector2>, List<EnemyData>> OnWavePreviewStart; // 波次预演开始事件
    
    private List<Enemy> spawnedEnemies = new List<Enemy>(); // 已生成的敌人列表
    
    // 预告和生成控制
    private List<GameObject> currentPreviewEffects = new List<GameObject>(); // 当前预告特效列表
    private List<Vector2> previewedPositions = new List<Vector2>(); // 预告的位置列表
    private List<EnemyData> previewedEnemyData = new List<EnemyData>(); // 预告的敌人数据列表
    private int currentWaveIndex = 0; // 当前波次索引
    
    void Start()
    {
        if (waveConfigs == null || waveConfigs.Count == 0)
        {
            Debug.LogError("波次配置列表未设置或为空！");
        }
        
        // 订阅敌人阶段事件
        EnemyPhaseController.OnPhaseStart += OnPhaseStart;
        
        Debug.Log($"EnemySpawner初始化完成，生成范围: X({minX}~{maxX}), Y({minY}~{maxY})");
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        EnemyPhaseController.OnPhaseStart -= OnPhaseStart;
    }
    
    /// <summary>
    /// 阶段开始事件处理
    /// </summary>
    void OnPhaseStart(EnemyPhase phase)
    {
        switch (phase)
        {
            case EnemyPhase.Spawn:
                SpawnEnemies();
                break;
            case EnemyPhase.Telegraph:
                StartPreview();
                break;
        }
    }
    
    
    void Update()
    {
        // 保留手动控制（用于测试）
        if (Input.GetKeyDown(spawnKey))
        {
            StartPreview();
        }
    }
    
    /// <summary>
    /// 获取生成位置
    /// </summary>
    Vector2 GetSpawnPosition(bool useRandom, Vector2 customPosition)
    {
        if (useRandom)
        {
            return GenerateRandomPosition();
        }
        else
        {
            return customPosition;
        }
    }
    
    /// <summary>
    /// 获取存活的敌人数量
    /// </summary>
    public int GetAliveEnemyCount()
    {
        int aliveCount = 0;
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                aliveCount++;
            }
        }
        return aliveCount;
    }
    
    /// <summary>
    /// 根据配置数据生成敌人
    /// </summary>
    void SpawnEnemyFromData(EnemyData enemyData, bool useRandomPosition = true, Vector2 customPosition = default)
    {
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogError("敌人配置数据无效！");
            return;
        }
        
        Vector3 spawnPosition;
        if (useRandomPosition)
        {
            spawnPosition = GenerateRandomPosition();
        }
        else
        {
            spawnPosition = customPosition;
        }
        
        GameObject enemyObj = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, enemyParent);
        
        // 获取敌人组件并设置配置数据
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.enemyData = enemyData;
            spawnedEnemies.Add(enemy);
        }
        else
        {
            Debug.LogError("敌人预制体上没有Enemy组件！");
            Destroy(enemyObj);
        }
    }
    
    
    // 在固定范围内生成随机位置
    Vector3 GenerateRandomPosition()
    {
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        return new Vector3(randomX, randomY, 0f);
    }
    
    // 清除所有生成的敌人
    public void ClearAllEnemies()
    {
        foreach (Enemy enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemies.Clear();
        Debug.Log("已清除所有生成的敌人");
    }
    
    // 获取所有敌人
    public List<Enemy> GetAllEnemies()
    {
        return new List<Enemy>(spawnedEnemies);
    }
    
    /// <summary>
    /// 开始预告（由EnemyPhaseController调用）
    /// </summary>
    public void StartPreview()
    {
        Debug.Log("开始预告下一波敌人");
        
        // 预告下一波（currentWaveIndex + 1）
        int previewWaveIndex = currentWaveIndex + 1;
        if (previewWaveIndex >= waveConfigs.Count)
        {
            if (loopWaves)
            {
                previewWaveIndex = 0;
            }
            else
            {
                Debug.Log("所有波次已完成，跳过预告");
                // 通知阶段完成，但不预告
                if (EnemyPhaseController.Instance != null)
                {
                    EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
                }
                return;
            }
        }
        
        WaveConfig previewWave = waveConfigs[previewWaveIndex];
        if (previewWave == null || previewWave.enemySpawns.Count == 0)
        {
            Debug.LogWarning($"第{previewWaveIndex + 1}波配置无效，跳过预告");
            // 通知阶段完成，但不预告
            if (EnemyPhaseController.Instance != null)
            {
                EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
            }
            return;
        }
        
        // 计算生成位置并记录
        previewedPositions.Clear();
        previewedEnemyData.Clear();
        
        foreach (var enemySpawn in previewWave.enemySpawns)
        {
            if (enemySpawn.enemyData == null) continue;
            
            for (int i = 0; i < enemySpawn.count; i++)
            {
                Vector2 spawnPosition;
                if (enemySpawn.useRandomPosition)
                {
                    spawnPosition = GenerateRandomPosition();
                }
                else
                {
                    spawnPosition = enemySpawn.customPosition;
                }
                
                // 记录位置和敌人数据
                previewedPositions.Add(spawnPosition);
                previewedEnemyData.Add(enemySpawn.enemyData);
            }
        }
        
        // 生成预告特效
        if (previewEffectPrefab != null)
        {
            foreach (var position in previewedPositions)
            {
                GameObject previewEffect = Instantiate(previewEffectPrefab, position, Quaternion.identity);
                currentPreviewEffects.Add(previewEffect);
                
                // 播放"点位出现"特效
                Transform appearChild = previewEffect.transform.Find("点位出现");
                if (appearChild != null)
                {
                    var appearMMFPlayer = appearChild.GetComponent<MMF_Player>();
                    if (appearMMFPlayer != null)
                    {
                        appearMMFPlayer.PlayFeedbacks();
                        Debug.Log($"播放点位出现特效 at {position}");
                    }
                }
            }
        }
        
        // 预告阶段立即完成
        if (EnemyPhaseController.Instance != null)
        {
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
        
        Debug.Log("预告完成");
    }
    
    /// <summary>
    /// 生成敌人（由EnemyPhaseController调用）
    /// </summary>
    public void SpawnEnemies()
    {
        Debug.Log("开始生成敌人");
        
        // 播放"点位消失"特效
        foreach (var previewEffect in currentPreviewEffects)
        {
            if (previewEffect != null)
            {
                Transform disappearChild = previewEffect.transform.Find("点位消失");
                if (disappearChild != null)
                {
                    var disappearMMFPlayer = disappearChild.GetComponent<MMF_Player>();
                    if (disappearMMFPlayer != null)
                    {
                        disappearMMFPlayer.PlayFeedbacks();
                        Debug.Log($"播放点位消失特效 at {previewEffect.transform.position}");
                    }
                }
            }
        }
        
        if (previewedPositions.Count == 0)
        {
            // 没有预告位置，不生成敌人
            Debug.Log("没有预告位置，跳过生成");
        }
        else
        {
            // 有预告位置，使用预告的位置生成
            SpawnPreviewedWave();
        }
        
        // 清理预告特效和数据
        ClearPreviewEffects();
        
        // 通知阶段完成
        if (EnemyPhaseController.Instance != null)
        {
            EnemyPhaseController.Instance.OnEnemyPhaseActionComplete();
        }
        
        Debug.Log("敌人生成完成");
    }
    
    
    /// <summary>
    /// 使用预告位置生成敌人
    /// </summary>
    void SpawnPreviewedWave()
    {
        Debug.Log("使用预告位置生成敌人");
        
        for (int i = 0; i < previewedPositions.Count && i < previewedEnemyData.Count; i++)
        {
            Vector2 spawnPosition = previewedPositions[i];
            EnemyData enemyData = previewedEnemyData[i];
            
            SpawnEnemyFromData(enemyData, false, spawnPosition);
            Debug.Log($"在预告位置 {spawnPosition} 生成敌人 {enemyData.name}");
        }
        
        // 切换到下一波
        currentWaveIndex++;
    }
    
    /// <summary>
    /// 清理预告特效和数据
    /// </summary>
    void ClearPreviewEffects()
    {
        foreach (var effect in currentPreviewEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        currentPreviewEffects.Clear();
        
        // 清理记录的数据
        previewedPositions.Clear();
        previewedEnemyData.Clear();
    }

}

