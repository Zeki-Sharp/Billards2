// using UnityEngine;
// using System.Collections.Generic;

// public class HoleManager : MonoBehaviour
// {
//     [Header("Hole管理设置")]
//     public int activeHoleCount = 2; // 每回合激活的Hole数量
    
//     private List<GameObject> allHoles = new List<GameObject>();
//     private List<GameObject> activeHoles = new List<GameObject>();
//     private List<Hole> holeScripts = new List<Hole>(); // 存储所有Hole脚本
    
//     // 事件：白球进洞
//     public System.Action<Player> OnPlayerInHole;
    
//     void Start()
//     {
//         // 查找所有Hole对象
//         FindAllHoles();
        
//         // 初始时所有Hole都是非激活状态
//         DeactivateAllHoles();
//     }
    
//     void FindAllHoles()
//     {
//         // 查找所有有Hole脚本的对象（子物体Trigger）
//         Hole[] foundHoleScripts = FindObjectsByType<Hole>(FindObjectsSortMode.None);
//         allHoles.Clear();
//         holeScripts.Clear();
        
//         foreach (Hole holeScript in foundHoleScripts)
//         {
//             // 获取父物体Hole
//             GameObject parentHole = holeScript.transform.parent.gameObject;
//             allHoles.Add(parentHole);
//             holeScripts.Add(holeScript);
            
//             // 订阅这个Hole的事件
//             holeScript.OnPlayerInHole += HandlePlayerInHole;
            
//             Debug.Log($"找到Hole对象: {parentHole.name} (脚本在子物体: {holeScript.name})");
//         }
        
//         Debug.Log($"总共找到 {allHoles.Count} 个有效的Hole对象");
//     }
    
//     void HandlePlayerInHole(Player player)
//     {
//         Debug.Log("HoleManager接收到玩家进洞事件");
//         // 转发事件给GameManager
//         OnPlayerInHole?.Invoke(player);
//     }
    
//     public void ActivateRandomHoles()
//     {
//         // 先关闭所有Hole
//         DeactivateAllHoles();
        
//         // 如果Hole数量不足，激活所有Hole
//         if (allHoles.Count <= activeHoleCount)
//         {
//             foreach (GameObject hole in allHoles)
//             {
//                 hole.SetActive(true);
//                 activeHoles.Add(hole);
//             }
//             Debug.Log($"Hole数量不足，激活所有 {allHoles.Count} 个Hole");
//             return;
//         }
        
//         // 随机选择指定数量的Hole激活，但跳过有球的洞口
//         List<GameObject> availableHoles = new List<GameObject>(allHoles);
//         activeHoles.Clear();
        
//         int attempts = 0;
//         int maxAttempts = allHoles.Count * 2; // 防止无限循环
        
//         for (int i = 0; i < activeHoleCount && attempts < maxAttempts; attempts++)
//         {
//             if (availableHoles.Count == 0) break;
            
//             int randomIndex = Random.Range(0, availableHoles.Count);
//             GameObject selectedHole = availableHoles[randomIndex];
            
//             // 获取对应的Hole脚本
//             Hole holeScript = selectedHole.GetComponentInChildren<Hole>();
//             if (holeScript != null && !holeScript.HasBallOnHole())
//             {
//                 // 洞口上没有球，可以激活
//                 selectedHole.SetActive(true);
//                 activeHoles.Add(selectedHole);
//                 availableHoles.RemoveAt(randomIndex);
//                 i++;
                
//                 Debug.Log($"激活Hole: {selectedHole.name} (无球)");
//             }
//             else
//             {
//                 // 洞口上有球，跳过这个洞口
//                 Debug.Log($"跳过Hole: {selectedHole.name} (有球)");
//                 availableHoles.RemoveAt(randomIndex);
//             }
//         }
        
//         Debug.Log($"本回合激活了 {activeHoles.Count} 个Hole (尝试了 {attempts} 次)");
//     }
    
//     public void DeactivateAllHoles()
//     {
//         foreach (GameObject hole in allHoles)
//         {
//             hole.SetActive(false);
//         }
//         activeHoles.Clear();
//         Debug.Log("所有Hole已关闭");
//     }
    
//     public List<GameObject> GetActiveHoles()
//     {
//         return new List<GameObject>(activeHoles);
//     }
    
//     public int GetActiveHoleCount()
//     {
//         return activeHoles.Count;
//     }
// }
