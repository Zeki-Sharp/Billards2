using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    public class MapPlayerTracker : MonoBehaviour
    {
        public bool lockAfterSelecting = false;
        public float enterNodeDelay = 1f;
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked) return;

            // Debug.Log("Selected node: " + mapNode.Node.point);

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // player has not selected the node yet, he can select any of the nodes with y = 0
                if (mapNode.Node.point.y == 0)
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
            else
            {
                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                if (currentNode != null && currentNode.outgoing.Any(point => point.Equals(mapNode.Node.point)))
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
        }

        private void SendPlayerToNode(MapNode mapNode)
        {
            Locked = lockAfterSelecting;
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();

            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() => EnterNode(mapNode));
        }

        private static void EnterNode(MapNode mapNode)
        {
            // we have access to blueprint name here as well
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            
            // 根据节点类型处理
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                case NodeType.EliteEnemy:
                case NodeType.Boss:
                    // 战斗类节点：加载对应的战斗场景
                    LoadCombatScene(mapNode);
                    break;
                    
                case NodeType.RestSite:
                    // 休息节点：显示休息UI（暂未实现）
                    Debug.Log("RestSite节点：功能暂未实现");
                    Instance.Locked = false; // 解锁地图
                    break;
                    
                case NodeType.Treasure:
                    // 宝箱节点：显示宝箱UI（暂未实现）
                    Debug.Log("Treasure节点：功能暂未实现");
                    Instance.Locked = false; // 解锁地图
                    break;
                    
                case NodeType.Store:
                    // 商店节点：显示商店UI（暂未实现）
                    Debug.Log("Store节点：功能暂未实现");
                    Instance.Locked = false; // 解锁地图
                    break;
                    
                case NodeType.Mystery:
                    // 神秘节点：显示事件UI（暂未实现）
                    Debug.Log("Mystery节点：功能暂未实现");
                    Instance.Locked = false; // 解锁地图
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// 加载战斗场景
        /// </summary>
        /// <param name="mapNode">地图节点</param>
        private static void LoadCombatScene(MapNode mapNode)
        {
            // 获取节点层级（y坐标）
            int layer = mapNode.Node.point.y;
            
            // 根据层级确定场景名称
            // Layer 0 使用 3D 版第一关，其余保持原有命名约定：
            // Layer 0 → Level1_3D, Layer 1 → Level2, ..., Layer 4 → Level5
            string sceneName = layer == 0 ? "Level1" : $"Level{layer + 1}";
            
            Debug.Log($"MapPlayerTracker: 加载战斗场景 - {sceneName} (Layer {layer}, NodeType: {mapNode.Node.nodeType})");
            
            // ✅ 保存地图系统数据到 GameSession
            var session = GameSession.GetOrCreateInstance();
            if (session != null)
            {
                session.State.SetMapSystemState(true, layer);
            }
            
            // 加载战斗场景
            SceneManager.LoadScene(sceneName);
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}