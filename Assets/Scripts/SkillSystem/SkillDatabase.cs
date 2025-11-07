using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

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
    
    [Header("快速测试开关")]
    [Tooltip("临时禁用的技能列表（不影响 SO 文件，仅用于测试）")]
    [InfoBox("在这里拖入要临时禁用的技能，方便快速测试。不会修改 SkillConfig SO 文件。", InfoMessageType.Info)]
    [SerializeField] private List<SkillConfig> temporarilyDisabledSkills = new List<SkillConfig>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;

    private readonly List<SkillVariant> variantCache = new List<SkillVariant>();
    private readonly Dictionary<string, List<SkillVariant>> variantsByTag = new Dictionary<string, List<SkillVariant>>();
    private bool variantsDirty = true;
    
    private void OnEnable()
    {
        MarkVariantsDirty();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        MarkVariantsDirty();
    }
#endif

    /// <summary>
    /// 获取所有有效的技能配置（排除临时禁用的）
    /// </summary>
    public List<SkillConfig> GetAllSkills()
    {
        return allSkills
            .Where(skill => skill != null && 
                           skill.IsValid() && 
                           skill.isActive &&  // SO 自己的开关
                           !IsTemporarilyDisabled(skill))  // 数据库的临时禁用列表
            .ToList();
    }
    
    /// <summary>
    /// 检查技能是否被临时禁用
    /// </summary>
    private bool IsTemporarilyDisabled(SkillConfig skill)
    {
        return temporarilyDisabledSkills != null && temporarilyDisabledSkills.Contains(skill);
    }

    private static bool HasTag(SkillConfig skill, string tag)
    {
        if (skill == null || string.IsNullOrEmpty(tag))
        {
            return false;
        }

        return skill.EnumerateTags().Contains(tag);
    }

    private static bool HasCommonTag(SkillConfig skill)
    {
        return HasTag(skill, "common");
    }

    private static IEnumerable<string> EnumerateSkillTags(SkillConfig skill)
    {
        return skill?.EnumerateTags() ?? Enumerable.Empty<string>();
    }
    
    private void MarkVariantsDirty()
    {
        variantsDirty = true;
    }

    private void EnsureVariantCache()
    {
        if (!variantsDirty)
        {
            return;
        }

        variantCache.Clear();
        variantsByTag.Clear();

        foreach (var skill in GetAllSkills())
        {
            var tags = EnumerateSkillTags(skill).Distinct().Where(tag => !string.IsNullOrEmpty(tag)).ToList();

            if (tags.Count == 0)
            {
                var fallback = skill.GetPrimaryTag();
                if (!string.IsNullOrEmpty(fallback))
                {
                    tags.Add(fallback);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[SkillDatabase] 构建 variant 缓存: {skill.skillName} -> [{string.Join(",", tags)}] (AllowedTags.Count={skill.AllowedTags?.Count ?? 0})");
            }

            foreach (var tag in tags)
            {
                var variant = new SkillVariant(skill, tag);
                variantCache.Add(variant);

                if (!variantsByTag.TryGetValue(tag, out var list))
                {
                    list = new List<SkillVariant>();
                    variantsByTag.Add(tag, list);
                }

                list.Add(variant);
            }
        }

        variantsDirty = false;

        if (showDebugInfo)
        {
            Debug.Log($"[SkillDatabase] Variant 缓存构建完成：总计 {variantCache.Count} 个副本，标签键数 {variantsByTag.Count}");
        }
    }

    private List<SkillVariant> GetVariantsForTagInternal(string tag)
    {
        EnsureVariantCache();

        if (string.IsNullOrEmpty(tag))
        {
            return variantCache;
        }

        if (variantsByTag.TryGetValue(tag, out var list))
        {
            if (showDebugInfo)
            {
                Debug.Log($"[SkillDatabase] 通过标签 '{tag}' 获取到 {list.Count} 个副本");
            }
            return list;
        }

        if (showDebugInfo)
        {
            Debug.LogWarning($"[SkillDatabase] 标签 '{tag}' 未命中 variant 缓存");
        }

        return new List<SkillVariant>();
    }

    public IReadOnlyList<SkillVariant> GetAllVariants()
    {
        EnsureVariantCache();
        return variantCache;
    }

    public List<SkillVariant> GetVariantsForCharacter(string characterName)
    {
        EnsureVariantCache();

        var result = new List<SkillVariant>();
        var seenVariantIds = new HashSet<string>();
        var seenBaseConfigs = new HashSet<SkillConfig>();

        void AddVariants(IEnumerable<SkillVariant> variants, bool skipIfBaseExists)
        {
            if (variants == null)
            {
                return;
            }

            foreach (var variant in variants)
            {
                if (variant?.BaseConfig == null)
                {
                    continue;
                }

                if (skipIfBaseExists && seenBaseConfigs.Contains(variant.BaseConfig))
                {
                    continue;
                }

                if (seenVariantIds.Add(variant.VariantId))
                {
                    result.Add(variant);
                    seenBaseConfigs.Add(variant.BaseConfig);
                }
            }
        }

        if (string.IsNullOrEmpty(characterName))
        {
            AddVariants(GetVariantsForTagInternal("common"), skipIfBaseExists: false);
            if (showDebugInfo)
            {
                Debug.Log($"[SkillDatabase] 角色为空 => 返回 {result.Count} 个通用副本");
            }
            return result;
        }

        AddVariants(GetVariantsForTagInternal(characterName), skipIfBaseExists: false);
        AddVariants(GetVariantsForTagInternal("common"), skipIfBaseExists: true);

        if (showDebugInfo)
        {
            Debug.Log($"[SkillDatabase] 角色 '{characterName}' 汇总副本数: {result.Count}");
            foreach (var variant in result)
            {
                Debug.Log($"    • {variant.BaseConfig?.skillName ?? "<null>"} (Tag: {variant.Tag})");
            }
        }

        return result;
    }

    public List<SkillConfig> GetSkillsForCharacter(string characterName)
    {
        return GetSkillsForCharacter(characterName, distinct: true);
    }

    public List<SkillConfig> GetSkillsForCharacter(string characterName, bool distinct)
    {
        var variants = GetVariantsForCharacter(characterName);
        var configs = variants
            .Select(variant => variant.BaseConfig)
            .Where(cfg => cfg != null);

        return distinct ? configs.Distinct().ToList() : configs.ToList();
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
                           skill.isActive &&
                           !IsTemporarilyDisabled(skill) &&
                           HasTag(skill, tag))
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
            .Where(skill => skill != null && skill.IsValid())
            .SelectMany(EnumerateSkillTags)
            .Where(tag => !string.IsNullOrEmpty(tag))
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();
    }
    
    public List<SkillVariant> GetVariantsByTag(string tag)
    {
        var variants = GetVariantsForTagInternal(tag);
        return variants == null ? new List<SkillVariant>() : new List<SkillVariant>(variants);
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

        MarkVariantsDirty();
        
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

        MarkVariantsDirty();
        
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
        int disabledCount = temporarilyDisabledSkills != null ? temporarilyDisabledSkills.Count : 0;
        
        // 统计标签分布
        var tagGroups = allSkills
            .Where(s => s != null && s.IsValid())
            .SelectMany(s => EnumerateSkillTags(s).DefaultIfEmpty("<未设置>"), (skill, tag) => new { skill, tag })
            .GroupBy(x => x.tag)
            .OrderByDescending(g => g.Count())
            .ToList();
        
        // 输出验证结果
        string report = $"[SkillDatabase] 验证报告：\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"✅ 有效技能: {validCount}\n" +
                       $"❌ 无效技能: {invalidCount}\n" +
                       $"⚠️  空引用: {nullCount}\n" +
                       $"🔁 重复引用: {duplicateCount}\n" +
                       $"🚫 临时禁用: {disabledCount}\n" +
                       $"📊 总计: {totalCount}\n" +
                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                       $"标签分布：\n";
        
        foreach (var group in tagGroups)
        {
            report += $"  • {group.Key}: {group.Count()} 个技能\n";
        }
        
        if (disabledCount > 0)
        {
            report += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            report += $"临时禁用的技能：\n";
            foreach (var skill in temporarilyDisabledSkills)
            {
                if (skill != null)
                {
                    report += $"  • {skill.skillName}\n";
                }
            }
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

        MarkVariantsDirty();
        
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
            MarkVariantsDirty();
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
            MarkVariantsDirty();
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
        MarkVariantsDirty();
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 按标签排序技能列表
    /// </summary>
    [ContextMenu("按标签排序")]
    public void SortByTag()
    {
        allSkills = allSkills
            .OrderBy(skill =>
            {
                if (skill == null)
                {
                    return string.Empty;
                }

                return EnumerateSkillTags(skill).FirstOrDefault() ?? string.Empty;
            })
            .ThenBy(skill => skill == null ? "" : skill.skillName)
            .ToList();
        
        Debug.Log($"[SkillDatabase] 已按标签和名称排序");
        MarkVariantsDirty();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}

