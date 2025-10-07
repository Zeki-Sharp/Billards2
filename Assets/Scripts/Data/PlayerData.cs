using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("玩家基本信息")]
    public string playerName;
    public GameObject playerPrefab;
    public Sprite playerIcon;
    
    [Header("物理数据")]
    public BallData ballData;
    
    [Header("战斗配置 - 基础属性")]
    [Tooltip("基础最大血量")]
    public float baseMaxHealth = 100f;
    [Tooltip("基础攻击力")]
    public float baseDamage = 10f;
    
    [Header("移动配置 - 基础属性")]
    [Tooltip("基础微调移动速度")]
    public float baseMicroMoveSpeed = 5f;
    
    [Header("向后兼容属性（只读）")]
    [Tooltip("最大血量 - 通过PlayerStatsManager获取最终值")]
    public float maxHealth => baseMaxHealth;
    [Tooltip("攻击力 - 通过PlayerStatsManager获取最终值")]
    public float damage => baseDamage;
    [Tooltip("微调移动速度 - 通过PlayerStatsManager获取最终值")]
    public float microMoveSpeed => baseMicroMoveSpeed;
    
    [Header("玩家特有配置")]
    public bool canLevelUp = true;
    public int startingLevel = 1;
    public int maxLevel = 100;
    public float experienceMultiplier = 1f;
}