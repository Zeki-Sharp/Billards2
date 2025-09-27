// using UnityEngine;

// public class Hole : MonoBehaviour
// {
//     // 事件：白球进洞
//     public System.Action<Player> OnPlayerInHole;
    
//     void OnTriggerEnter2D(Collider2D other)
//     {
//         // 检测敌人进入hole
//         if (other.CompareTag("Enemy"))
//         {
//             Enemy enemy = other.GetComponent<Enemy>();
//             if (enemy != null)
//             {
//                 Debug.Log($"敌人 {enemy.name} 进入hole，直接死亡");
                
//                 // 触发敌人进洞特效事件
//                 GameEventBus.PublishEffectEvent("HoleEnter", other.transform.position, Vector3.zero, other.gameObject);
                
//                 // 通过伤害系统处理，造成足够大的伤害直接死亡
//                 float maxHealth = enemy.playerData != null ? enemy.playerData.maxHealth : 100f;
//                 enemy.TakeDamage(maxHealth);
//             }
//         }
        
//         // 检测白球进入hole
//         if (other.CompareTag("Player"))
//         {
//             Player player = other.GetComponent<Player>();
//             if (player != null)
//             {
//                 Debug.Log("玩家进入hole，触发事件");
                
//                 // 触发玩家进洞特效事件
//                 GameEventBus.PublishEffectEvent("HoleEnter", other.transform.position, Vector3.zero, other.gameObject);
                
//                 Debug.Log($"OnPlayerInHole事件订阅者数量: {OnPlayerInHole?.GetInvocationList()?.Length ?? 0}");
//                 // 触发事件，让GameManager处理
//                 OnPlayerInHole?.Invoke(player);
//                 Debug.Log("事件已触发");
//             }
//         }
//     }
    
//     // 检查洞口上是否有球
//     public bool HasBallOnHole()
//     {
//         // 获取洞口的碰撞器
//         Collider2D holeCollider = GetComponent<Collider2D>();
//         if (holeCollider == null) return false;
        
//         // 检查洞口范围内是否有球
//         Collider2D[] colliders = Physics2D.OverlapAreaAll(
//             holeCollider.bounds.min, 
//             holeCollider.bounds.max
//         );
        
//         foreach (Collider2D col in colliders)
//         {
//             // 检查是否有球（Player或Enemy标签）
//             if (col.CompareTag("Player") || col.CompareTag("Enemy"))
//             {
//                 Debug.Log($"洞口 {transform.parent.name} 上有球: {col.name}");
//                 return true;
//             }
//         }
        
//         return false;
//     }
// }
