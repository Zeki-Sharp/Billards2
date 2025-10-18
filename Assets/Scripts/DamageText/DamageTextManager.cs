using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

/// <summary>
/// 伤害数字管理器
/// 负责全局伤害数字的生成、回收和对象池管理
/// 监听攻击事件系统，当有伤害值时显示伤害数字
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }
    
    [Header("配置")]
    [Tooltip("伤害数字配置")]
    public DamageTextConfig config;
    
    [Header("位置偏移设置")]
    [Tooltip("向上偏移量（世界单位）")]
    public float upwardOffset = 0f;
    [Tooltip("向右偏移量（世界单位）")]
    public float rightwardOffset = 0.6f;
    
    [Header("预制体")]
    [Tooltip("伤害数字预制体")]
    public GameObject damageTextPrefab;
    
    [Header("Canvas 设置")]
    [Tooltip("Canvas 排序顺序")]
    public int canvasSortOrder = 100;
    [Tooltip("Canvas 参考分辨率")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Header("对象池设置")]
    [Tooltip("对象池大小（已弃用，现在每次都创建新对象）")]
    public int poolSize = 30;
    [Tooltip("是否自动扩展对象池（已弃用）")]
    public bool autoExpandPool = true;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;
    
    // 对象池（已弃用，现在每次都创建新对象）
    // private Queue<DamageText> damageTextPool = new Queue<DamageText>();
    // private List<DamageText> activeDamageTexts = new List<DamageText>();
    
    // 相机引用
    private Camera targetCamera;
    
    // Canvas 管理
    private Canvas damageTextCanvas;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        // 订阅伤害处理完成事件 - 使用最终处理后的伤害值
        GameEventBus.OnDamageProcessed += HandleDamageProcessed;
    }
    
    void OnDisable()
    {
        // 取消订阅伤害处理完成事件
        GameEventBus.OnDamageProcessed -= HandleDamageProcessed;
    }
    
    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        // 创建或获取 Canvas
        CreateDamageTextCanvas();
        
        // 获取相机引用
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }
        
        // 对象池已弃用，不再预创建
        
        if (enableDebugLog)
        {
            Debug.Log($"DamageTextManager 初始化完成，使用直接创建模式");
        }
    }
    
    /// <summary>
    /// 创建伤害数字 Canvas
    /// </summary>
    private void CreateDamageTextCanvas()
    {
        // 查找现有的伤害数字 Canvas
        damageTextCanvas = GameObject.Find("DamageTextCanvas")?.GetComponent<Canvas>();
        
        if (damageTextCanvas == null)
        {
            // 创建新的 Canvas
            GameObject canvasGO = new GameObject("DamageTextCanvas");
            damageTextCanvas = canvasGO.AddComponent<Canvas>();
            damageTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            damageTextCanvas.sortingOrder = canvasSortOrder;
            
            // 添加 CanvasScaler
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // 添加 GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // 设置为 DontDestroyOnLoad
            DontDestroyOnLoad(canvasGO);
            
            if (enableDebugLog)
            {
                Debug.Log("DamageTextManager: 创建了伤害数字 Canvas");
            }
        }
    }
    
    // PreCreatePool 方法已删除，现在每次都创建新对象
    
    // CreateDamageTextInstance 方法已删除，现在每次都创建新对象
    
    /// <summary>
    /// 创建新的伤害数字实例（每次都创建新对象）
    /// </summary>
    /// <returns>伤害数字实例</returns>
    public DamageText GetDamageText()
    {
        if (damageTextCanvas == null)
        {
            Debug.LogError("DamageTextManager: Canvas 未初始化！");
            return null;
        }
        
        // 直接创建新实例
        GameObject instance = Instantiate(damageTextPrefab, damageTextCanvas.transform);
        DamageText damageText = instance.GetComponent<DamageText>();
        
        if (damageText == null)
        {
            Debug.LogError("DamageTextManager: 预制体缺少 DamageText 组件！");
            Destroy(instance);
            return null;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageTextManager] 创建新的伤害数字实例");
        }
        
        return damageText;
    }
    
    // ReturnDamageText 方法已删除，现在由MMF直接销毁对象
    
    /// <summary>
    /// 显示伤害数字
    /// </summary>
    /// <param name="position">显示位置（世界坐标）</param>
    /// <param name="damage">伤害数值</param>
    /// <param name="target">目标对象</param>
    public void ShowDamageText(Vector3 position, float damage, GameObject target)
    {
        // 获取实例
        DamageText damageText = GetDamageText();
        if (damageText == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("DamageTextManager: 无法获取伤害数字实例，对象池可能已满");
            }
            return;
        }
        
        // 获取最终显示位置（屏幕坐标）
        Vector3 screenPosition = GetFinalScreenPosition(position, target);
        
        // 初始化伤害数字
        damageText.Initialize(damage, screenPosition, config);
        
        // 播放 MMF 动画
        PlayDamageTextAnimation(damageText);
        
        if (enableDebugLog)
        {
            Debug.Log($"DamageTextManager: 显示伤害数字 {damage} 在屏幕位置 {screenPosition}");
        }
    }
    
    /// <summary>
    /// 播放伤害数字动画
    /// </summary>
    /// <param name="damageText">伤害数字实例</param>
    private void PlayDamageTextAnimation(DamageText damageText)
    {
        // 获取 MMF Player 组件
        var mmfPlayer = damageText.GetComponent<MMF_Player>();
        if (mmfPlayer != null)
        {
            // 播放动画
            mmfPlayer.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning("DamageTextManager: 伤害数字预制体缺少 MMF_Player 组件！");
        }
    }
    
    /// <summary>
    /// 获取最终屏幕位置
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    /// <param name="target">目标对象</param>
    /// <returns>屏幕坐标位置</returns>
    private Vector3 GetFinalScreenPosition(Vector3 worldPosition, GameObject target)
    {
        // 使用目标对象的固定位置，而不是攻击位置
        Vector3 finalWorldPosition;
        if (target != null)
        {
            // 优先使用enemyItem的位置（实际可见的敌人物体）
            Vector3 targetPosition = target.transform.position;
            
            // 检查是否是敌人，如果是则使用enemyItem位置
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null && enemy.enemyItem != null)
            {
                targetPosition = enemy.enemyItem.position;
                if (enableDebugLog)
                {
                    Debug.Log($"DamageTextManager: 使用enemyItem位置 {targetPosition} 而不是根物体位置 {target.transform.position}");
                }
            }
            
            // 使用目标对象的中心位置，添加可调整的偏移
            finalWorldPosition = targetPosition;
            finalWorldPosition.y += upwardOffset; // 可调整的向上偏移
            finalWorldPosition.x += rightwardOffset; // 可调整的向右偏移
        }
        else
        {
            // 如果没有目标对象，使用传入的世界位置
            finalWorldPosition = worldPosition;
            finalWorldPosition.y += upwardOffset;
            finalWorldPosition.x += rightwardOffset;
        }
        
        // 转换为屏幕坐标
        Vector3 screenPosition = targetCamera.WorldToScreenPoint(finalWorldPosition);
        
        
        
        return screenPosition;
    }
    
    /// <summary>
    /// 事件监听 - 处理伤害处理完成事件中的伤害数字显示
    /// </summary>
    /// <param name="processedData">处理完成的伤害数据</param>
    private void HandleDamageProcessed(ProcessedDamageData processedData)
    {
        // 检查是否有伤害值且大于0
        if (processedData.FinalDamage > 0f)
        {
            // 显示伤害数字 - 使用最终处理后的伤害值
            ShowDamageText(processedData.OriginalData.Position, processedData.FinalDamage, processedData.OriginalData.Target);
            
            if (enableDebugLog)
            {
                Debug.Log($"DamageTextManager: 显示最终伤害数字 {processedData.FinalDamage} (原始: {processedData.OriginalData.Damage})");
            }
        }
    }
    
    // ClearAllDamageTexts 方法已删除，现在不需要管理活跃对象列表
    
    /// <summary>
    /// 获取伤害数字实例（供 MMF 使用）
    /// </summary>
    /// <returns>伤害数字实例</returns>
    public DamageText GetDamageTextInstance()
    {
        return GetDamageText();
    }
    
    /// <summary>
    /// 获取伤害数字 Canvas
    /// </summary>
    /// <returns>Canvas 组件</returns>
    public Canvas GetDamageTextCanvas()
    {
        return damageTextCanvas;
    }
    
    // GetPoolStatus 方法已删除，不再使用对象池
    
    void OnDestroy()
    {
        // 不再需要清理，对象由MMF自动销毁
    }
}