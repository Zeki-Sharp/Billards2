using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能数据库 - 集中管理所有可用技能配置
/// 
/// 【核心职责】：
/// - 维护所有技能配置的引用列表
/// - 提供技能查询和筛选接口
/// - 避免使用Resources.LoadAll动态加载
/// - 提供编辑器工具辅助管理
/// 
/// 【使用方式】：
/// 1. 在Project视图中右键 Create → Skill System → Skill Database
/// 2. 在Inspector中将所有SkillConfig拖入allSkills列表（或使用自动发现）
/// 3. 在SkillSelectionManager中引用这个数据库
/// 4. 使用GetSkillsForCharacter()等方法查询技能
/// 
/// 【编辑器工具】：
/// - 右键菜单：自动发现并添加所有技能
/// - 右键菜单：清理无效技能
/// - 右键菜单：验证数据库完整性
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skill System/Skill Database", order = 0)]
public class SkillDatabase : ScriptableObject
{
    [Header("技能列表")]
    [Tooltip("所有可用的技能配置")]
    [SerializeField] private List<SkillConfig> allSkills = new List<SkillConfig>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 获取所有有效的技能配置
    /// </summary>
    public List<SkillConfig> GetAllSkills()
    {
        return allSkills.Where(skill => skill != null && skill.IsValid()).ToList();
    }
    
    /// <summary>
    /// 根据角色名称筛选技能
    /// 返回适合该角色的技能（包含角色专属技能和通用技能）
    /// </summary>
    /// <param name="characterName">角色名称</param>
    /// <returns>适合该角色的技能列表</returns>
    public List<SkillConfig> GetSkillsForCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[SkillDatabase] 角色名称为空，返回通用技能");
            }
            // 角色名称为空时，只返回 common 标签的技能
            return allSkills
                .Where(skill => skill != null && 
                               skill.IsValid() && 
                               skill.skillTag == "common")
                .ToList();
        }
        
        // 【修复】只允许角色专属技能和通用技能
        var filteredSkills = allSkills
            .Where(skill => skill != null && 
                           skill.IsValid() && 
                           (skill.skillTag == characterName ||      // 角色专属技能
                            skill.skillTag == "common"))            // 通用技能
            .ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"[SkillDatabase] 为角色 '{characterName}' 找到 {filteredSkills.Count} 个技能");
            Debug.Log($"[SkillDatabase] 过滤条件: skillTag == '{characterName}' OR skillTag == 'common'");
        }
        
        return filteredSkills;
    }
    
    /// <summary>
    /// 根据标签筛选技能
    /// </summary>
    /// <param name="tag">技能标签</param>
    /// <returns>匹配该标签的技能列表</returns>
    public List<SkillConfig> GetSkillsByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return GetAllSkills();
        }
        
        return allSkills
            .Where(skill => skill != null && 
                           skill.IsValid() && 
                           skill.skillTag == tag)
            .ToList();
    }
    
    /// <summary>
    /// 根据名称查找技能
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <returns>找到的技能配置，未找到返回null</returns>
    public SkillConfig FindSkillByName(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return null;
        }
        
        return allSkills.FirstOrDefault(skill => 
            skill != null && skill.skillName == skillName);
    }
    
    /// <summary>
    /// 检查技能是否存在
    /// </summary>
    /// <param name="skillConfig">要检查的技能配置</param>
    /// <returns>是否存在于数据库中</returns>
    public bool Contains(SkillConfig skillConfig)
    {
        return skillConfig != null && allSkills.Contains(skillConfig);
    }
    
    /// <summary>
    /// 获取有效技能总数
    /// </summary>
    public int GetValidSkillCount()
    {
        return allSkills.Count(skill => skill != null && skill.IsValid());
    }
    
    /// <summary>
    /// 获取所有技能总数（包含无效的）
    /// </summary>
    public int GetTotalSkillCount()
    {
        return allSkills.Count;
    }
    
    /// <summary>
    /// 获取所有使用的标签
    /// </summary>
    public List<string> GetAllTags()
    {
        return allSkills
            .Where(skill => skill != null && !string.IsNullOrEmpty(skill.skillTag))
            .Select(skill => skill.skillTag)
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 自动填充技能列表（编辑器工具）
    /// 从Resources/Data/Skill目录加载所有技能配置
    /// </summary>
    [ContextMenu("自动发现并添加所有技能")]
    public void AutoPopulateSkills()
    {
        // 清空现有列表
        allSkills.Clear();
        
        // 从Resources加载所有技能（仅编辑器使用，用于初始化数据库）
        SkillConfig[] foundSkills = Resources.LoadAll<SkillConfig>("Data/Skill");
        
        if (foundSkills == null || foundSkills.Length == 0)
        {
            // 如果Data/Skill目录为空，尝试从根目录搜索
            foundSkills = Resources.LoadAll<SkillConfig>("");
            foundSkills = foundSkills.Where(s => s != null).ToArray();
        }
        
        // 添加找到的技能
        foreach (var skill in foundSkills)
        {
            if (skill != null && !allSkills.Contains(skill))
            {
                allSkills.Add(skill);
            }
        }
        
        Debug.Log($"[SkillDatabase] 自动发现了 {allSkills.Count} 个技能配置");
        
        // 验证配置
        ValidateDatabase();
        
        // 标记为已修改
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 移除空引用和无效技能
    /// </summary>
    [ContextMenu("清理无效技能")]
    public void CleanupInvalidSkills()
    {
        int beforeCount = allSkills.Count;
        
        // 移除空引用
        allSkills.RemoveAll(skill => skill == null);
        
        // 移除无效技能
        allSkills.RemoveAll(skill => !skill.IsValid());
        
        int afterCount = allSkills.Count;
        int removedCount = beforeCount - afterCount;
        
        if (removedCount > 0)
        {
            Debug.Log($"[SkillDatabase] 清理完成：移除了 {removedCount} 个无效技能");
        }
        else
        {
            Debug.Log($"[SkillDatabase] 清理完成：所有技能都是有效的");
        }
        
        // 标记为已修改
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 验证数据库完整性（编辑器工具）
    /// </summary>
    [ContextMenu("验证数据库完整性")]
    public void ValidateDatabase()
    {
        int totalCount = allSkills.Count;
        int nullCount = allSkills.Count(s => s == null);
        int invalidCount = allSkills.Count(s => s != null && !s.IsValid());
        int validCount = allSkills.Count(s => s != null && s.IsValid());
        int duplicateCount = allSkills.Count - allSkills.Distinct().Count();
        
        // 统计标签分布
        var tagGroups = allSkills
            .Where(s => s != null && s.IsValid())
            .GroupBy(s => string.IsNullOrEmpty(s.skillTag) ? "<未设置>" : s.skillTag)
            .OrderByDescending(g => g.Count())
            .ToList();
        
        // 输出验证结果
        string report = $"[SkillDatabase] 验证报告：\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"✅ 有效技能: {validCount}\n" +
                       $"❌ 无效技能: {invalidCount}\n" +
                       $"⚠️  空引用: {nullCount}\n" +
                       $"🔁 重复引用: {duplicateCount}\n" +
                       $"📊 总计: {totalCount}\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"标签分布：\n";
        
        foreach (var group in tagGroups)
        {
            report += $"  • {group.Key}: {group.Count()} 个技能\n";
        }
        
        Debug.Log(report);
        
        // 检查是否有问题
        if (nullCount > 0 || invalidCount > 0 || duplicateCount > 0)
        {
            Debug.LogWarning($"[SkillDatabase] 发现 {nullCount + invalidCount + duplicateCount} 个问题，建议使用'清理无效技能'功能");
        }
    }
    
    /// <summary>
    /// 添加单个技能到数据库
    /// </summary>
    public void AddSkill(SkillConfig skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("[SkillDatabase] 尝试添加空技能");
            return;
        }
        
        if (allSkills.Contains(skill))
        {
            Debug.LogWarning($"[SkillDatabase] 技能 '{skill.skillName}' 已存在");
            return;
        }
        
        allSkills.Add(skill);
        Debug.Log($"[SkillDatabase] 添加技能: {skill.skillName}");
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 从数据库移除技能
    /// </summary>
    public void RemoveSkill(SkillConfig skill)
    {
        if (skill == null)
        {
            return;
        }
        
        if (allSkills.Remove(skill))
        {
            Debug.Log($"[SkillDatabase] 移除技能: {skill.skillName}");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
    
    /// <summary>
    /// 移除重复的技能引用
    /// </summary>
    [ContextMenu("移除重复技能")]
    public void RemoveDuplicates()
    {
        int beforeCount = allSkills.Count;
        allSkills = allSkills.Distinct().ToList();
        int afterCount = allSkills.Count;
        int removedCount = beforeCount - afterCount;
        
        if (removedCount > 0)
        {
            Debug.Log($"[SkillDatabase] 移除了 {removedCount} 个重复引用");
            UnityEditor.EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.Log($"[SkillDatabase] 没有发现重复引用");
        }
    }
    
    /// <summary>
    /// 按名称排序技能列表
    /// </summary>
    [ContextMenu("按名称排序")]
    public void SortByName()
    {
        allSkills = allSkills
            .OrderBy(skill => skill == null ? "" : skill.skillName)
            .ToList();
        
        Debug.Log($"[SkillDatabase] 已按名称排序");
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 按标签排序技能列表
    /// </summary>
    [ContextMenu("按标签排序")]
    public void SortByTag()
    {
        allSkills = allSkills
            .OrderBy(skill => skill == null ? "" : skill.skillTag)
            .ThenBy(skill => skill == null ? "" : skill.skillName)
            .ToList();
        
        Debug.Log($"[SkillDatabase] 已按标签和名称排序");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

