using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    /// <summary>
    /// 地图场景控制器 - 管理地图场景的生命周期和状态
    /// 
    /// 【核心职责】：
    /// - 检测是否从战斗场景返回
    /// - 首次进入时生成新地图
    /// - 战斗返回时恢复地图状态
    /// - 解锁地图节点供玩家继续选择
    /// </summary>
    public class MapSceneController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private MapPlayerTracker mapPlayerTracker;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        
        void Start()
        {
            InitializeMapScene();
        }
        
        /// <summary>
        /// 初始化地图场景
        /// </summary>
        void InitializeMapScene()
        {
            if (showDebugInfo)
            {
                Debug.Log("MapSceneController: 初始化地图场景");
            }
            
            // 检查是否从战斗场景返回
            if (GameRuntimeData.IsFromMapSystem())
            {
                OnReturnFromCombat();
            }
            else
            {
                OnFirstEnter();
            }
        }
        
        /// <summary>
        /// 首次进入地图场景（从角色选择或新游戏）
        /// </summary>
        void OnFirstEnter()
        {
            if (showDebugInfo)
            {
                Debug.Log("MapSceneController: 首次进入地图场景，生成新地图");
            }
            
            // 强制生成新地图（清除旧数据）
            if (mapManager != null)
            {
                // 清除旧地图数据
                PlayerPrefs.DeleteKey("Map");
                
                // 生成新地图
                mapManager.GenerateNewMap();
                
                if (showDebugInfo)
                {
                    Debug.Log("MapSceneController: 新地图生成完成");
                }
            }
            
            // 解锁地图供玩家选择
            if (mapPlayerTracker != null)
            {
                mapPlayerTracker.Locked = false;
            }
        }
        
        /// <summary>
        /// 从战斗场景返回地图
        /// </summary>
        void OnReturnFromCombat()
        {
            if (showDebugInfo)
            {
                Debug.Log("MapSceneController: 从战斗返回地图场景");
            }
            
            // 清除"从地图系统"标记
            GameRuntimeData.ClearFromMapSystem();
            
            // 解锁地图供玩家继续选择下一个节点
            if (mapPlayerTracker != null)
            {
                mapPlayerTracker.Locked = false;
                
                if (showDebugInfo)
                {
                    Debug.Log("MapSceneController: 地图已解锁，玩家可以继续选择节点");
                }
            }
            
            // 地图状态会从PlayerPrefs自动恢复（MapManager.Start()中处理）
        }
        
        /// <summary>
        /// 清理场景（可选）
        /// </summary>
        void OnDestroy()
        {
            if (showDebugInfo)
            {
                Debug.Log("MapSceneController: 清理地图场景");
            }
        }
        
        #region 调试方法
        
        [ContextMenu("显示地图状态")]
        void ShowMapStatus()
        {
            if (mapManager != null && mapManager.CurrentMap != null)
            {
                Debug.Log($"MapSceneController 状态:\n" +
                         $"当前地图: {mapManager.CurrentMap.configName}\n" +
                         $"已访问节点数: {mapManager.CurrentMap.path.Count}\n" +
                         $"是否从战斗返回: {GameRuntimeData.IsFromMapSystem()}\n" +
                         $"地图已锁定: {(mapPlayerTracker != null ? mapPlayerTracker.Locked : false)}");
            }
            else
            {
                Debug.LogWarning("MapSceneController: 地图尚未生成");
            }
        }
        
        [ContextMenu("强制解锁地图")]
        void ForceUnlockMap()
        {
            if (mapPlayerTracker != null)
            {
                mapPlayerTracker.Locked = false;
                Debug.Log("MapSceneController: 地图已强制解锁");
            }
        }
        
        #endregion
    }
}

