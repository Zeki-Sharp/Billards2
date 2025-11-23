using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家生成器 - 负责在关卡中生成玩家队伍的所有角色
/// 
/// 【核心职责】：
/// - 从 GameSession 读取队伍数据
/// - 为每个角色生成球体实例
/// - 管理生成位置分配（防重叠）
/// - 将场景实例引用回写到 CharacterInstance
/// 
/// 【生成流程】：
/// 1. 从 GameSession.TeamData 读取角色列表
/// 2. 为每个角色获取随机位置（防重叠）
/// 3. 实例化对应的球体预制体
/// 4. 设置 PlayerData 到 Player 组件
/// 5. 保存实例引用到 CharacterInstance
/// 
/// 【设计原则】：
/// - 复用 SpawnRangeConfig 定义生成区域
/// - 使用防重叠算法确保角色间距
/// - 统一的生成接口，易于调用
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("生成配置")]
    [SerializeField] 
    [Tooltip("玩家生成区域配置")]
    private SpawnRangeConfig spawnRange;
    
    [SerializeField] 
    [Tooltip("玩家球体的父对象（组织层级）")]
    private Transform playerParent;
    
    [Header("重叠检测")]
    [SerializeField] 
    [Tooltip("球体间的最小安全距离")]
    private float minDistanceBetweenBalls = 2f;
    
    [SerializeField] 
    [Tooltip("最大尝试次数（防止无限循环）")]
    private int maxSpawnAttempts = 20;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    /// <summary>
    /// 为角色添加初始技能
    /// </summary>
    /// <param name="character">角色实例</param>
    private void AddInitialSkills(CharacterInstance character)
    {
        if (character == null || character.characterData == null)
        {
            return;
        }
        
        // 检查是否配置了初始技能
        if (character.characterData.initialSkills == null || character.characterData.initialSkills.Count == 0)
        {
            return;
        }
        
        // 获取 SkillManager
        var skillManager = SkillManager.Instance;
        if (skillManager == null)
        {
            Debug.LogWarning($"PlayerSpawner: SkillManager 不存在，无法添加初始技能");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerSpawner: 为角色 {character.characterID} 添加 {character.characterData.initialSkills.Count} 个初始技能");
        }
        
        // 添加所有初始技能到角色
        foreach (var skillConfig in character.characterData.initialSkills)
        {
            if (skillConfig == null)
            {
                Debug.LogWarning($"PlayerSpawner: 角色 {character.characterID} 的初始技能列表中有空引用，跳过");
                continue;
            }
            
            // 使用 SkillManager 添加技能
            skillManager.AddSkillToCharacter(character.characterID, skillConfig);
            
            if (showDebugInfo)
            {
                Debug.Log($"PlayerSpawner: ✅ 为 {character.characterID} 添加初始技能: {skillConfig.skillName}");
            }
        }
    }
    
    void Start()
    {
        // 验证配置
        if (spawnRange == null)
        {
            Debug.LogError("PlayerSpawner: 未配置 SpawnRangeConfig！");
        }
        
        if (playerParent == null)
        {
            playerParent = transform;
            Debug.LogWarning("PlayerSpawner: 未配置 playerParent，使用当前对象作为父对象");
        }
    }
    
    /// <summary>
    /// 生成队伍（从 GameSession 读取角色数据）
    /// </summary>
    /// <returns>生成的球体列表</returns>
    public List<GameObject> SpawnTeam()
    {
        // 从 GameSession 获取队伍数据
        var session = GameSession.GetOrCreateInstance();
        if (session == null)
        {
            Debug.LogError("PlayerSpawner: 无法获取 GameSession！");
            return new List<GameObject>();
        }
        
        TeamData teamData = session.GetTeamData();
        if (teamData == null || !teamData.IsValid())
        {
            Debug.LogError("PlayerSpawner: TeamData 无效或为空！");
            return new List<GameObject>();
        }
        
        return SpawnTeam(teamData);
    }
    
    /// <summary>
    /// 生成队伍（指定队伍数据）
    /// </summary>
    /// <param name="teamData">队伍数据</param>
    /// <returns>生成的球体列表</returns>
    public List<GameObject> SpawnTeam(TeamData teamData)
    {
        if (teamData == null || teamData.characters.Count == 0)
        {
            Debug.LogError("PlayerSpawner: TeamData 为空或没有角色！");
            return new List<GameObject>();
        }
        
        List<GameObject> spawnedBalls = new List<GameObject>();
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerSpawner: 开始生成队伍，共 {teamData.characters.Count} 个角色");
        }
        
        // 为每个角色生成球体
        for (int i = 0; i < teamData.characters.Count; i++)
        {
            CharacterInstance character = teamData.characters[i];
            
            if (character == null || character.characterData == null)
            {
                Debug.LogWarning($"PlayerSpawner: 角色 {i + 1} 数据为空，跳过");
                continue;
            }
            
            // 获取有效的生成位置（防重叠）
            Vector3 spawnPosition = GetValidSpawnPosition(spawnedBalls);
            
            // 生成球体
            GameObject ball = SpawnCharacter(character, spawnPosition);
            
            if (ball != null)
            {
                spawnedBalls.Add(ball);
                
                if (showDebugInfo)
                {
                    Debug.Log($"PlayerSpawner: 成功生成角色 [{character.positionIndex}号位] {character.characterData.info.name} at {spawnPosition}");
                }
            }
            else
            {
                Debug.LogError($"PlayerSpawner: 生成角色 {character.characterData.info.name} 失败！");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerSpawner: 队伍生成完成，成功生成 {spawnedBalls.Count}/{teamData.characters.Count} 个角色");
        }
        
        return spawnedBalls;
    }
    
    /// <summary>
    /// 生成单个角色
    /// </summary>
    /// <param name="character">角色实例数据</param>
    /// <param name="position">生成位置</param>
    /// <returns>生成的球体GameObject</returns>
    private GameObject SpawnCharacter(CharacterInstance character, Vector3 position)
    {
        if (character.characterData == null || character.characterData.playerPrefab == null)
        {
            Debug.LogError($"PlayerSpawner: 角色 {character.characterID} 的 playerPrefab 为空！");
            return null;
        }
        
        // 实例化预制体
        GameObject ball = Instantiate(
            character.characterData.playerPrefab,
            position,
            Quaternion.identity,
            playerParent
        );
        
        // 设置球体名称（便于调试）
        ball.name = $"Player_{character.positionIndex}_{character.characterData.info.name}";
        
        // ✅ 确保有 GroundAlignAnchor 组件（如果没有则自动添加）
        GroundAlignAnchor alignAnchor = ball.GetComponent<GroundAlignAnchor>();
        if (alignAnchor == null)
        {
            alignAnchor = ball.AddComponent<GroundAlignAnchor>();
            if (showDebugInfo)
            {
                Debug.Log($"PlayerSpawner: 自动添加 GroundAlignAnchor 组件到 {ball.name}");
            }
        }
        
        // 获取 Player 组件并设置数据和ID
        Player player = ball.GetComponent<Player>();
        if (player != null)
        {
            player.SetCharacterID(character.characterID);
            player.SetPlayerData(character.characterData);
        }
        else
        {
            Debug.LogWarning($"PlayerSpawner: 球体预制体缺少 Player 组件！");
        }
        
        // 保存场景实例引用到 CharacterInstance
        character.ballInstance = ball;
        
        // ✅ 添加初始技能（如果 PlayerData 中配置了）
        AddInitialSkills(character);
        
        return ball;
    }
    
    /// <summary>
    /// 获取有效的生成位置（防重叠）
    /// </summary>
    /// <param name="existingBalls">已生成的球体列表</param>
    /// <returns>有效的生成位置</returns>
    private Vector3 GetValidSpawnPosition(List<GameObject> existingBalls)
    {
        if (spawnRange == null)
        {
            Debug.LogWarning("PlayerSpawner: SpawnRangeConfig 未配置，使用默认位置");
            return transform.position;
        }
        
        // 尝试多次生成位置，直到找到不重叠的位置
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // ✅ 自动避开障碍物（墙体/玩家/敌人）
            // checkObstacles = true 时使用 Physics2D 检测，false 时使用随机位置
            Vector3 candidatePosition = spawnRange.GetValidRandomPosition();
            
            // ✅ 仍然检查是否与现有球体距离过近（双重保险，更精确的间距控制）
            if (IsPositionValid(candidatePosition, existingBalls))
            {
                return candidatePosition;
            }
            
            if (showDebugInfo && attempt > 5)
            {
                Debug.Log($"PlayerSpawner: 位置重叠，重试 {attempt + 1}/{maxSpawnAttempts}");
            }
        }
        
        // 如果多次尝试失败，返回随机位置（可能重叠，但总比不生成好）
        Debug.LogWarning($"PlayerSpawner: 经过 {maxSpawnAttempts} 次尝试未找到不重叠位置，使用可能重叠的随机位置");
        return spawnRange.GetValidRandomPosition();
    }
    
    /// <summary>
    /// 检查位置是否有效（不与现有球体重叠）
    /// </summary>
    /// <param name="position">候选位置</param>
    /// <param name="existingBalls">已生成的球体列表</param>
    /// <returns>是否有效</returns>
    private bool IsPositionValid(Vector3 position, List<GameObject> existingBalls)
    {
        // 检查与所有已生成球体的距离
        foreach (GameObject ball in existingBalls)
        {
            if (ball == null) continue;
            
            float distance = Vector3.Distance(position, ball.transform.position);
            if (distance < minDistanceBetweenBalls)
            {
                // 距离太近，位置无效
                return false;
            }
        }
        
        // 没有重叠，位置有效
        return true;
    }
    
    /// <summary>
    /// 清除所有生成的球体（用于重置关卡）
    /// </summary>
    public void ClearAllBalls()
    {
        // 清除所有子对象
        if (playerParent != null)
        {
            foreach (Transform child in playerParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerSpawner: 已清除所有球体");
        }
    }
    
    #region 调试方法
    
    /// <summary>
    /// 在Scene视图中绘制生成范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (spawnRange == null) return;
        
        // 绘制生成范围
        Gizmos.color = Color.cyan;
        
        if (spawnRange.rangeShape == SpawnRangeShape.Rectangle)
        {
            // 绘制矩形范围
            Vector3 center = spawnRange.worldCenter;
            Vector2 size = spawnRange.worldSize;
            
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.1f));
        }
        else if (spawnRange.rangeShape == SpawnRangeShape.Circle)
        {
            // 绘制圆形范围
            Vector3 center = spawnRange.worldCenter;
            float radius = spawnRange.worldRadius;
            
            DrawCircle(center, radius, 32);
        }
        
        // 绘制最小距离指示
        Gizmos.color = Color.yellow;
        DrawCircle(transform.position, minDistanceBetweenBalls, 16);
    }
    
    /// <summary>
    /// 绘制圆形（Gizmos辅助方法）
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    [ContextMenu("测试生成队伍")]
    void TestSpawnTeam()
    {
        var balls = SpawnTeam();
        Debug.Log($"PlayerSpawner: 测试生成完成，生成了 {balls.Count} 个球体");
    }
    
    [ContextMenu("清除所有球体")]
    void DebugClearAllBalls()
    {
        ClearAllBalls();
    }
    
    [ContextMenu("显示配置信息")]
    void ShowConfigInfo()
    {
        Debug.Log($"PlayerSpawner 配置信息:\n" +
                 $"生成范围: {(spawnRange != null ? "已配置" : "未配置")}\n" +
                 $"父对象: {(playerParent != null ? playerParent.name : "未配置")}\n" +
                 $"最小距离: {minDistanceBetweenBalls}\n" +
                 $"最大尝试次数: {maxSpawnAttempts}");
        
        if (spawnRange != null)
        {
            Debug.Log("生成范围详情:\n" + spawnRange.GetDebugInfo());
        }
    }
    
    #endregion
}

