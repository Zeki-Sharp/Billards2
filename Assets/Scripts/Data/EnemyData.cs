using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("敌人基本信息")]
    public string enemyName;
    public GameObject enemyPrefab;
    public Sprite enemyIcon;
    
    [Header("物理数据")]
    public BallData ballData;                   // 打包的物理数据
    
    [Header("战斗配置")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float attackCooldown = 1f;
    public float moveSpeed = 2f;
    
    [Header("AI配置")]
    public bool enableAI = true;
    public AttackType attackType = AttackType.Contact;
    public MovementType movementType = MovementType.FollowPlayer;
    
    [Header("攻击配置")]
    public float attackRange = 3f;
    public float contactRadius = 1f;
    
    [Header("远程攻击配置")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletDamage = 8f;
    public float bulletLifetime = 3f;
    public Vector2 bulletOffset = Vector2.zero;
    
    [Header("跟随移动配置")]
    public float followMinDistance = 1f;
    public float followMaxDistance = 5f;
    public bool maintainDistance = false;
    
    [Header("巡逻移动配置")]
    public float patrolSpeedMultiplier = 0.8f;
    public float stuckDetectionTime = 1.5f;
    public float bounceRandomOffset = 8f;
    public float minMoveDistance = 0.1f;
    
    [Header("生成配置")]
    public int spawnWeight = 1;
    public int spawnCost = 1;
    public bool isBoss = false;
    public int experienceValue = 10;
}
