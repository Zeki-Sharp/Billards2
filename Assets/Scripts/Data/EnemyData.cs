using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("敌人基本信息")]
    public string enemyName;
    public GameObject enemyContainerPrefab; // 敌人容器预制体（包含预告和敌人）
    public Sprite enemyIcon;
    
    [Header("物理数据")]
    public BallData ballData;                   // 打包的物理数据
    
    [Header("战斗配置")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float attackCooldown = 1f;
    public float moveSpeed = 2f;
    
    [Header("攻击配置")]
    public float attackRange = 3f;  // 保留用于其他用途，如检测范围
    public AttackType attackType = AttackType.Melee;  // 攻击类型
    
    [ConditionalField("attackType", false, false, AttackType.Ranged)]
    public RangedAttackConfig rangedConfig = new RangedAttackConfig();
    
    [Header("AI配置")]
    public bool enableAI = true;
    public MovementType movementType = MovementType.FollowPlayer;
    

    
    [ConditionalField("movementType", false, false, MovementType.FollowPlayer)]
    public FollowMovementConfig followConfig = new FollowMovementConfig();
    
    [ConditionalField("movementType", false, false, MovementType.Flee)]
    public FleeMovementConfig fleeConfig = new FleeMovementConfig();
    
    [Header("生成配置")]
    public int spawnWeight = 1;
    public int spawnCost = 1;
    public bool isBoss = false;
    public int experienceValue = 10;
}
