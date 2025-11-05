using UnityEngine;

/// <summary>
/// ✅ 多角色系统：触发器辅助类
/// 提供角色ID验证和获取的通用方法
/// </summary>
public static class TriggerHelper
{
    /// <summary>
    /// 从 GameObject 获取角色ID
    /// 
    /// 【查询策略】：
    /// 1. 优先从 Player 组件读取（O(1)，快速）
    /// 2. Fallback：遍历 TeamData（O(n)，兼容性）
    /// 
    /// 【设计说明】：
    /// - Player 组件是场景对象层的唯一ID持有者
    /// - 统一通过此方法查询，避免直接遍历 TeamData
    /// </summary>
    /// <param name="gameObject">游戏对象</param>
    /// <returns>角色ID，如果未找到返回 null</returns>
    public static string GetCharacterID(GameObject gameObject)
    {
        if (gameObject == null) return null;
        
        // 策略1：优先从 Player 组件直接读取（高效：O(1)）
        Player player = gameObject.GetComponent<Player>();
        if (player != null && !string.IsNullOrEmpty(player.CharacterID))
        {
            return player.CharacterID;
        }
        
        // 策略2：Fallback - 通过 GameSession.TeamData 查找（兼容旧代码：O(n)）
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData != null)
        {
            foreach (var character in teamData.characters)
            {
                if (character.ballInstance == gameObject)
                {
                    return character.characterID;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 检查 GameObject 是否属于指定角色
    /// </summary>
    /// <param name="gameObject">游戏对象</param>
    /// <param name="ownerCharacterID">归属角色ID</param>
    /// <returns>是否匹配</returns>
    public static bool IsOwner(GameObject gameObject, string ownerCharacterID)
    {
        // 如果没有指定归属角色，则不过滤（全局技能）
        if (string.IsNullOrEmpty(ownerCharacterID))
        {
            return true;
        }
        
        string characterID = GetCharacterID(gameObject);
        return !string.IsNullOrEmpty(characterID) && characterID == ownerCharacterID;
    }
    
    /// <summary>
    /// 检查事件来源是否属于指定角色
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="ownerCharacterID">归属角色ID</param>
    /// <param name="showDebugLog">是否显示调试日志</param>
    /// <returns>是否匹配</returns>
    public static bool CheckEventSource(object eventData, string ownerCharacterID, bool showDebugLog = false)
    {
        // 如果没有指定归属角色，则不过滤（全局技能）
        if (string.IsNullOrEmpty(ownerCharacterID))
        {
            if (showDebugLog)
            {
                Debug.Log("[TriggerHelper] 全局技能，不过滤角色");
            }
            return true;
        }
        
        GameObject source = null;
        string eventType = eventData?.GetType().Name ?? "null";
        
        // 从不同类型的事件中提取 source
        if (eventData is CollisionEvent collisionEvent)
        {
            source = collisionEvent.Source;
        }
        else if (eventData is StoppedEvent stoppedEvent)
        {
            source = stoppedEvent.Source;
        }
        else if (eventData is BallPhysics ballPhysics)
        {
            source = ballPhysics.gameObject;
        }
        else if (eventData is DeathData deathData)
        {
            // ⚠️ DeathData 目前没有击杀者信息，暂时不过滤击杀事件
            // 所有角色的击杀都会触发（待扩展 DeathData 添加 Attacker 字段）
            if (showDebugLog)
            {
                Debug.LogWarning("[TriggerHelper] DeathData 缺少击杀者信息，击杀触发器暂不支持角色过滤");
            }
            return true;  // 不过滤，所有击杀都触发
        }
        
        if (source == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[TriggerHelper] 事件 {eventType} 没有来源对象");
            }
            return false;
        }
        
        bool isMatch = IsOwner(source, ownerCharacterID);
        
        if (showDebugLog)
        {
            string sourceCharacterID = GetCharacterID(source);
            Debug.Log($"[TriggerHelper] 事件来源: {source.name} (角色ID: {sourceCharacterID}), 技能归属: {ownerCharacterID}, 匹配: {isMatch}");
        }
        
        return isMatch;
    }
}

