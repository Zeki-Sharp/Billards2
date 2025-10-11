using UnityEngine;
using System.Collections.Generic;

namespace Game.SpawnSystem.Triggers
{
    /// <summary>
    /// 波次生成触发器 - 使用新架构的生成策略层
    /// 
    /// 【核心职责】：
    /// - 使用WaveListSpawnStrategy获取生成数据
    /// - 调用EnemySpawner执行实际生成
    /// - 处理初始敌人和波次敌人生成
    /// </summary>
    public class WaveSpawnTrigger : SpawnTrigger<EnemyData>
    {
        [Header("生成策略")]
        [SerializeField] private WaveListSpawnStrategy spawnStrategy;
        
        [Header("触发器设置")]
        [SerializeField] private bool generateInitialEnemies = true;
        [SerializeField] private bool generateWaveEnemies = true;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        
        /// <summary>
        /// 初始化触发器
        /// </summary>
        protected override void Initialize()
        {
            // 跳过基类的Initialize()，使用自定义初始化逻辑
            // base.Initialize();
            
            if (spawnStrategy == null)
            {
                Debug.LogError("[WaveSpawnTrigger] spawnStrategy 未设置！");
                return;
            }
            
            if (spawner == null)
            {
                Debug.LogError("[WaveSpawnTrigger] spawner 未设置！");
                return;
            }
            
            // 初始化 WaveConfigProvider
            if (spawnStrategy.configProvider != null)
            {
                spawnStrategy.configProvider.Initialize();
                
                if (showDebugInfo)
                {
                    Debug.Log("[WaveSpawnTrigger] WaveConfigProvider 初始化完成");
                }
            }
            
            // 设置生成策略的生成模式
            spawnStrategy.SetGenerationMode(generateInitialEnemies, generateWaveEnemies);
            spawnStrategy.enableDebugLog = showDebugInfo;
            
            // 验证策略配置
            if (!spawnStrategy.ValidateConfig())
            {
                Debug.LogError("[WaveSpawnTrigger] spawnStrategy 配置无效！");
                return;
            }
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 初始化完成");
            }
        }
        
        /// <summary>
        /// 订阅事件 - 波次生成通常由外部调用，不需要订阅事件
        /// </summary>
        protected override void SubscribeEvents()
        {
            // 波次生成通常由游戏逻辑主动调用，不需要订阅事件
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 使用主动调用模式，无需订阅事件");
            }
        }
        
        /// <summary>
        /// 取消事件订阅
        /// </summary>
        protected override void UnsubscribeEvents()
        {
            // 无需取消订阅，因为没有订阅任何事件
        }
        
        /// <summary>
        /// 游戏开始时自动生成初始敌人
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
            // 延迟一帧生成初始敌人，确保所有系统都已初始化
            StartCoroutine(GenerateInitialEnemiesDelayed());
        }
        
        /// <summary>
        /// 延迟生成初始敌人的协程
        /// </summary>
        private System.Collections.IEnumerator GenerateInitialEnemiesDelayed()
        {
            yield return null; // 等待一帧
            
            GenerateInitialEnemies();
        }
        
        /// <summary>
        /// 生成初始敌人（游戏开始时调用）
        /// </summary>
        public void GenerateInitialEnemies()
        {
            if (!generateInitialEnemies)
            {
                return;
            }
            
            // 设置策略只生成初始敌人
            spawnStrategy.SetGenerationMode(true, false);
            
            // 获取生成列表并执行生成
            ExecuteSpawnFromStrategy();
            
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
            
            // 设置策略只生成波次敌人
            spawnStrategy.SetGenerationMode(false, true);
            
            // 获取生成列表并执行生成
            ExecuteSpawnFromStrategy();
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 当前波次敌人生成完成");
            }
        }
        
        /// <summary>
        /// 从策略执行生成
        /// </summary>
        private void ExecuteSpawnFromStrategy()
        {
            if (spawnStrategy == null || spawner == null)
            {
                Debug.LogError("[WaveSpawnTrigger] spawnStrategy 或 spawner 为空！");
                return;
            }
            
            // 获取要生成的对象列表
            List<EnemyData> enemiesToSpawn = spawnStrategy.GetSpawnList();
            
            if (enemiesToSpawn.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[WaveSpawnTrigger] 没有敌人需要生成");
                }
                return;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[WaveSpawnTrigger] 开始生成敌人，数量: {enemiesToSpawn.Count}");
            }
            
            // 逐个生成敌人
            foreach (EnemyData enemyData in enemiesToSpawn)
            {
                spawner.Spawn(enemyData);
            }
        }
        
        /// <summary>
        /// 重置触发器状态
        /// </summary>
        public void ResetTriggerState()
        {
            if (spawnStrategy != null)
            {
                spawnStrategy.ResetState();
            }
            
            if (showDebugInfo)
            {
                Debug.Log("[WaveSpawnTrigger] 触发器状态已重置");
            }
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetDebugInfo()
        {
            string info = $"WaveSpawnTrigger:\n";
            info += $"- SpawnStrategy: {(spawnStrategy != null ? "已设置" : "未设置")}\n";
            info += $"- Spawner: {(spawner != null ? "已设置" : "未设置")}\n";
            info += $"- 初始敌人: {(generateInitialEnemies ? "启用" : "禁用")}\n";
            info += $"- 波次敌人: {(generateWaveEnemies ? "启用" : "禁用")}\n";
            
            return info;
        }
    }
}
