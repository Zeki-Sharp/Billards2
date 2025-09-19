using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 敌人容器管理脚本
/// 管理预告和生成状态，控制子对象的显示/隐藏
/// </summary>
public class EnemyContainer : MonoBehaviour
{
    [Header("子对象引用")]
    public Transform spawnPreview; // 预告子对象
    public Transform enemyItem; // 敌人子对象
    
    [Header("事件")]
    public System.Action<EnemyContainer> OnTelegraphComplete; // 预告完成事件
    public System.Action<EnemyContainer> OnSpawnComplete; // 生成完成事件
    
    private MMF_Player previewAppearPlayer; // 预告出现动画
    private MMF_Player previewDisappearPlayer; // 预告消失动画
    private MMF_Player enemySpawnPlayer; // 敌人生成动画
    
    private bool isTelegraphing = false;
    private bool isSpawning = false;
    
    void Awake()
    {
        // 自动查找子对象
        if (spawnPreview == null)
            spawnPreview = transform.Find("SpawnPreview");
        if (enemyItem == null)
            enemyItem = transform.Find("EnemyItem");
        
        // 获取动画组件
        if (spawnPreview != null)
        {
            previewAppearPlayer = spawnPreview.Find("点位出现")?.GetComponent<MMF_Player>();
            previewDisappearPlayer = spawnPreview.Find("点位消失")?.GetComponent<MMF_Player>();
        }
        
        if (enemyItem != null)
        {
            enemySpawnPlayer = enemyItem.GetComponent<MMF_Player>();
            if (enemySpawnPlayer == null)
                enemySpawnPlayer = enemyItem.GetComponentInChildren<MMF_Player>();
        }
        
        // 初始状态：隐藏所有子对象
        SetInitialState();
    }
    
    /// <summary>
    /// 设置初始状态
    /// </summary>
    void SetInitialState()
    {
        if (spawnPreview != null)
            spawnPreview.gameObject.SetActive(false);
        if (enemyItem != null)
            enemyItem.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 开始预告
    /// </summary>
    public void StartTelegraph()
    {
        if (isTelegraphing) return;
        
        isTelegraphing = true;
        Debug.Log($"EnemyContainer: 开始预告 at {transform.position}");
        
        // 显示预告子对象
        if (spawnPreview != null)
        {
            spawnPreview.gameObject.SetActive(true);
            
            // 播放预告出现动画
            if (previewAppearPlayer != null)
            {
                previewAppearPlayer.Events.OnComplete.AddListener(OnTelegraphAnimationComplete);
                previewAppearPlayer.PlayFeedbacks();
            }
            else
            {
                // 没有动画，立即完成
                OnTelegraphAnimationComplete();
            }
        }
        else
        {
            Debug.LogWarning("EnemyContainer: 没有找到预告子对象");
            OnTelegraphAnimationComplete();
        }
    }
    
    /// <summary>
    /// 开始生成
    /// </summary>
    public void StartSpawn()
    {
        if (isSpawning) return;
        
        isSpawning = true;
        Debug.Log($"EnemyContainer: 开始生成 at {transform.position}");
        
        // 播放预告消失动画
        if (previewDisappearPlayer != null)
        {
            previewDisappearPlayer.Events.OnComplete.AddListener(OnPreviewDisappearComplete);
            previewDisappearPlayer.PlayFeedbacks();
        }
        else
        {
            // 没有消失动画，直接生成敌人
            OnPreviewDisappearComplete();
        }
    }
    
    /// <summary>
    /// 预告动画完成回调
    /// </summary>
    void OnTelegraphAnimationComplete()
    {
        Debug.Log($"EnemyContainer: 预告动画完成 at {transform.position}");
        isTelegraphing = false;
        OnTelegraphComplete?.Invoke(this);
    }
    
    /// <summary>
    /// 预告消失动画完成回调
    /// </summary>
    void OnPreviewDisappearComplete()
    {
        Debug.Log($"EnemyContainer: 预告消失动画完成 at {transform.position}");
        
        // 隐藏预告子对象
        if (spawnPreview != null)
            spawnPreview.gameObject.SetActive(false);
        
        // 显示敌人子对象
        if (enemyItem != null)
        {
            enemyItem.gameObject.SetActive(true);
            
            // 播放敌人生成动画
            if (enemySpawnPlayer != null)
            {
                enemySpawnPlayer.Events.OnComplete.AddListener(OnEnemySpawnAnimationComplete);
                enemySpawnPlayer.PlayFeedbacks();
            }
            else
            {
                // 没有生成动画，立即完成
                OnEnemySpawnAnimationComplete();
            }
        }
        else
        {
            Debug.LogWarning("EnemyContainer: 没有找到敌人子对象");
            OnEnemySpawnAnimationComplete();
        }
    }
    
    /// <summary>
    /// 敌人生成动画完成回调
    /// </summary>
    void OnEnemySpawnAnimationComplete()
    {
        Debug.Log($"EnemyContainer: 敌人生成动画完成 at {transform.position}");
        isSpawning = false;
        OnSpawnComplete?.Invoke(this);
    }
    
    /// <summary>
    /// 获取敌人组件
    /// </summary>
    public Enemy GetEnemy()
    {
        if (enemyItem != null)
            return enemyItem.GetComponent<Enemy>();
        return null;
    }
    
    /// <summary>
    /// 设置敌人数据
    /// </summary>
    public void SetEnemyData(EnemyData enemyData)
    {
        Enemy enemy = GetEnemy();
        if (enemy != null)
        {
            enemy.enemyData = enemyData;
        }
    }
    
    /// <summary>
    /// 检查是否正在预告
    /// </summary>
    public bool IsTelegraphing()
    {
        return isTelegraphing;
    }
    
    /// <summary>
    /// 检查是否正在生成
    /// </summary>
    public bool IsSpawning()
    {
        return isSpawning;
    }
    
    /// <summary>
    /// 清理事件监听
    /// </summary>
    void OnDestroy()
    {
        if (previewAppearPlayer != null)
            previewAppearPlayer.Events.OnComplete.RemoveListener(OnTelegraphAnimationComplete);
        if (previewDisappearPlayer != null)
            previewDisappearPlayer.Events.OnComplete.RemoveListener(OnPreviewDisappearComplete);
        if (enemySpawnPlayer != null)
            enemySpawnPlayer.Events.OnComplete.RemoveListener(OnEnemySpawnAnimationComplete);
    }
}
