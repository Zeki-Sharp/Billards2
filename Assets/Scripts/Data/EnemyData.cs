using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [BoxGroup("显示信息")]
    [LabelText("敌人信息")]
    [Tooltip("显示信息（名称、图标、描述等）")]
    [InlineProperty]
    [HideLabel]
    public EnemyInfo info = new EnemyInfo();
    
    [BoxGroup("敌人基本信息")]
    [LabelText("敌人预制体")]
    [Tooltip("敌人容器预制体（包含预告和敌人）")]
    [Required]
    public GameObject enemyContainerPrefab;
    
    [BoxGroup("等级配置")]
    [LabelText("敌人等级列表")]
    [Tooltip("敌人的所有等级配置。等级编号根据列表位置自动确定：第1位=Level 1，第2位=Level 2")]
    [InfoBox("等级编号根据列表位置自动确定：第1位=Level 1，第2位=Level 2，以此类推", InfoMessageType.Info)]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 3)]
    public List<EnemyLevelConfig> enemyLevels = new List<EnemyLevelConfig>
    {
        new EnemyLevelConfig { level = 1 }  // ✅ 默认有一个 Level 1
    };
    
    /// <summary>
    /// 编辑器中自动同步等级编号
    /// </summary>
    private void OnValidate()
    {
        #if UNITY_EDITOR
        bool assetDirty = false;
        
        if (enemyLevels != null && enemyLevels.Count > 0)
        {
            for (int i = 0; i < enemyLevels.Count; i++)
            {
                if (enemyLevels[i] != null)
                {
                    enemyLevels[i].level = i + 1;
                }
            }
        }
        
        if (legacyDamageProfile != null)
        {
            if (damageProfiles == null)
            {
                damageProfiles = new List<DamageProfile>();
            }
            
            if (!damageProfiles.Contains(legacyDamageProfile))
            {
                damageProfiles.Insert(0, legacyDamageProfile);
            }
            
            legacyDamageProfile = null;
            assetDirty = true;
        }
        
        if (damageProfiles != null && damageProfiles.RemoveAll(p => p == null) > 0)
        {
            assetDirty = true;
        }
        
        if (assetDirty)
        {
            EditorUtility.SetDirty(this);
        }
        #endif
    }
    
    
    [BoxGroup("共享配置")]
    [LabelText("球体数据")]
    [Tooltip("打包的物理数据（所有等级共享）")]
    [Required]
    public BallData ballData;
    
    [BoxGroup("新伤害系统配置")]
    [LabelText("伤害配置列表")]
    [Tooltip("敌人的伤害规则配置列表（新伤害系统），支持组合多个 Profile 以实现更复杂的攻击逻辑")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<DamageProfile> damageProfiles = new List<DamageProfile>();
    
    [FormerlySerializedAs("damageProfile")]
    [SerializeField, HideInInspector]
    private DamageProfile legacyDamageProfile;
    
    #region 多等级配置管理
    
    /// <summary>
    /// 获取指定等级的配置
    /// </summary>
    public EnemyLevelConfig GetLevelConfig(int level)
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return null;
        }
        
        // 根据列表索引获取配置：Level 1 = Index 0, Level 2 = Index 1, ...
        int index = level - 1;
        if (index >= 0 && index < enemyLevels.Count && enemyLevels[index] != null)
        {
            // 确保 level 字段与列表位置一致
            enemyLevels[index].level = level;
            return enemyLevels[index];
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取最高可用等级
    /// </summary>
    public int GetMaxLevel()
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return 1;
        }
        
        // 列表数量即最高等级
        return enemyLevels.Count;
    }
    
    /// <summary>
    /// 获取所有可用等级
    /// </summary>
    public List<int> GetAvailableLevels()
    {
        if (enemyLevels == null || enemyLevels.Count == 0)
        {
            return new List<int> { 1 };
        }
        
        var levels = new List<int>();
        for (int i = 0; i < enemyLevels.Count; i++)
        {
            levels.Add(i + 1);  // 列表索引 + 1 = 等级
        }
        
        return levels;
    }
    
    /// <summary>
    /// 获取所有有效的伤害配置（运行时自动过滤空引用）
    /// </summary>
    public List<DamageProfile> GetDamageProfiles()
    {
        if (damageProfiles == null)
        {
            damageProfiles = new List<DamageProfile>();
        }
        
        damageProfiles.RemoveAll(p => p == null);
        return damageProfiles;
    }
    
    #endregion
}
