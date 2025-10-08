using UnityEngine;
using System.Collections.Generic;

namespace Game.SpawnSystem.Triggers
{
    /// <summary>
    /// 波次生成触发器 - 连接配置层和执行层
    /// 
    /// 【核心职责】：
    /// - 监听游戏事件（如回合开始）
    /// - 查询WaveConfigProvider获取当前波次数据
    /// - 调用EnemySpawner执行实际生成
    /// - 处理初始敌人和波次敌人生成
    /// </summary>
    public class WaveSpawnTrigger : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private WaveConfigProvider configProvider;
        [SerializeField] private EnemySpawner enemySpawner;
        
        [Header("触发器设置")]
        [SerializeField] private bool generateInitialEnemies = true;
        [SerializeField] private bool generateWaveEnemies = true;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        
        // 状态管理
        private bool hasGeneratedInitialEnemies = false;
        private bool isFirstWave = true;
        
        void Start()
        {
            InitializeTrigger();
            
            // 在游戏开始时生成初始敌人
            GenerateInitialEnemies();
        }
        
        /// <summary>
        /// 初始化触发器
        /// </summary>
        void InitializeTrigger()
        {
            // 验证依赖引用
            if (configProvider == null)
            {
                Debug.LogError("[WaveSpawnTrigger] WaveConfigProvider 未设置！");
                return;
            }
            
            if (enemySpawner == null)
            {
                Debug.LogError("[WaveSpawnTrigger] EnemySpawner 未设置！");
                return;
            }
            
            // 初始化配置提供者
            configProvider.Initialize();
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 初始化完成");
            }
        }
        
        /// <summary>
        /// 生成初始敌人（游戏开始时调用）
        /// </summary>
        public void GenerateInitialEnemies()
        {
            if (!generateInitialEnemies || hasGeneratedInitialEnemies)
            {
                return;
            }
            
            if (configProvider == null)
            {
                Debug.LogError("[WaveSpawnTrigger] WaveConfigProvider 为空，无法生成初始敌人");
                return;
            }
            
            if (!configProvider.ShouldGenerateInitialEnemies())
            {
                if (showDebugInfo)
                {
                    Debug.Log("[WaveSpawnTrigger] 配置为不生成初始敌人");
                }
                hasGeneratedInitialEnemies = true; // 标记为已处理，避免重复调用
                return;
            }
            
            List<EnemySpawn> initialEnemies = configProvider.GetInitialSpawnData();
            if (initialEnemies.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[WaveSpawnTrigger] 没有配置初始敌人");
                }
                hasGeneratedInitialEnemies = true;
                return;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[WaveSpawnTrigger] 开始生成初始敌人，数量: {initialEnemies.Count}");
            }
            
            // 调用EnemySpawner生成初始敌人
            enemySpawner.GenerateEnemiesFromList(initialEnemies);
            hasGeneratedInitialEnemies = true;
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 初始敌人生成完成");
            }
        }
        
        /// <summary>
        /// 生成当前波次敌人（回合开始时调用）
        /// </summary>
        public void GenerateCurrentWave()
        {
            if (!generateWaveEnemies)
            {
                return;
            }
            
            if (configProvider == null)
            {
                Debug.LogError("[WaveSpawnTrigger] WaveConfigProvider 为空，无法生成波次敌人");
                return;
            }
            
            if (!configProvider.ShouldSpawn())
            {
                if (showDebugInfo)
                {
                    Debug.Log("[WaveSpawnTrigger] 没有更多波次需要生成");
                }
                return;
            }
            
            List<EnemySpawn> currentWaveEnemies = configProvider.GetSpawnData();
            if (currentWaveEnemies.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("[WaveSpawnTrigger] 当前波次没有配置敌人");
                }
                // 仍然推进到下一波次
                configProvider.AdvanceToNextWave();
                return;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[WaveSpawnTrigger] 开始生成当前波次敌人，数量: {currentWaveEnemies.Count}");
            }
            
            // 调用EnemySpawner生成波次敌人
            enemySpawner.GenerateEnemiesFromList(currentWaveEnemies);
            
            // 推进到下一波次
            configProvider.AdvanceToNextWave();
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 当前波次敌人生成完成");
            }
        }
        
        /// <summary>
        /// 重置触发器状态
        /// </summary>
        public void ResetTriggerState()
        {
            hasGeneratedInitialEnemies = false;
            isFirstWave = true;
            
            if (configProvider != null)
            {
                configProvider.Reset();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 触发器状态已重置");
            }
        }
        
        /// <summary>
        /// 检查是否应该生成波次敌人
        /// </summary>
        public bool ShouldGenerateWaveEnemies()
        {
            if (generateWaveEnemies && configProvider != null && configProvider.ShouldSpawn())
            {
                return true; // 需要生成波次敌人
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取当前波次信息（调试用）
        /// </summary>
        public string GetCurrentWaveInfo()
        {
            if (configProvider == null)
            {
                return "配置提供者未设置";
            }
            
            return $"波次索引: {configProvider.GetCurrentWaveIndex()}, 总波次数: {configProvider.GetTotalWaveCount()}";
        }
    }
}
