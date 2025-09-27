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
    [Tooltip("对象池大小")]
    public int poolSize = 30;
    [Tooltip("是否自动扩展对象池")]
    public bool autoExpandPool = true;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = true;
    
    // 对象池
    private Queue<DamageText> damageTextPool = new Queue<DamageText>();
    private List<DamageText> activeDamageTexts = new List<DamageText>();
    
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
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        // 订阅攻击事件
        GameEventBus.OnAttack += HandleAttack;
    }
    
    void OnDisable()
    {
        // 取消订阅攻击事件
        GameEventBus.OnAttack -= HandleAttack;
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
        
        // 使用配置数据
        if (config != null)
        {
            poolSize = config.poolSize;
            autoExpandPool = config.autoExpandPool;
        }
        
        // 预创建对象池
        PreCreatePool();
        
        if (enableDebugLog)
        {
            Debug.Log($"DamageTextManager 初始化完成，对象池大小: {poolSize}");
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
    
    /// <summary>
    /// 预创建对象池
    /// </summary>
    private void PreCreatePool()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogError("DamageTextManager: 伤害数字预制体未设置！");
            return;
        }
        
        for (int i = 0; i < poolSize; i++)
        {
            CreateDamageTextInstance();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"DamageTextManager: 预创建了 {poolSize} 个伤害数字实例");
        }
    }
    
    /// <summary>
    /// 创建伤害数字实例
    /// </summary>
    private void CreateDamageTextInstance()
    {
        if (damageTextCanvas == null)
        {
            Debug.LogError("DamageTextManager: Canvas 未初始化！");
            return;
        }
        
        GameObject instance = Instantiate(damageTextPrefab, damageTextCanvas.transform);
        DamageText damageText = instance.GetComponent<DamageText>();
        
        if (damageText == null)
        {
            Debug.LogError("DamageTextManager: 预制体缺少 DamageText 组件！");
            Destroy(instance);
            return;
        }
        
        instance.SetActive(false);
        damageTextPool.Enqueue(damageText);
    }
    
    /// <summary>
    /// 从对象池获取伤害数字实例
    /// </summary>
    /// <returns>伤害数字实例</returns>
    public DamageText GetDamageText()
    {
        DamageText damageText = null;
        
        // 尝试从对象池获取
        if (damageTextPool.Count > 0)
        {
            damageText = damageTextPool.Dequeue();
        }
        // 如果对象池为空且允许自动扩展
        else if (autoExpandPool)
        {
            CreateDamageTextInstance();
            if (damageTextPool.Count > 0)
            {
                damageText = damageTextPool.Dequeue();
            }
        }
        
        if (damageText != null)
        {
            damageText.gameObject.SetActive(true);
            activeDamageTexts.Add(damageText);
        }
        
        return damageText;
    }
    
    /// <summary>
    /// 回收到对象池
    /// </summary>
    /// <param name="damageText">要回收的伤害数字</param>
    public void ReturnDamageText(DamageText damageText)
    {
        if (damageText == null) return;
        
        // 从活跃列表中移除
        activeDamageTexts.Remove(damageText);
        
        // 重置状态
        damageText.gameObject.SetActive(false);
        damageText.transform.SetParent(damageTextCanvas.transform);
        
        // 回收到对象池
        damageTextPool.Enqueue(damageText);
    }
    
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
            // 使用目标对象的中心位置，添加可调整的偏移
            finalWorldPosition = target.transform.position;
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
    /// 事件监听 - 处理攻击事件中的伤害数字事件
    /// </summary>
    /// <param name="attackData">攻击数据</param>
    private void HandleAttack(AttackData attackData)
    {
        
        // 检查是否有伤害值且大于0
        if (attackData.Damage > 0f)
        {
            // 显示伤害数字
            ShowDamageText(attackData.Position, attackData.Damage, attackData.Target);
            
        }
        else
        {
        }
    }
    
    /// <summary>
    /// 清理所有活跃的伤害数字
    /// </summary>
    public void ClearAllDamageTexts()
    {
        for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
        {
            if (activeDamageTexts[i] != null)
            {
                activeDamageTexts[i].ForceReturnToPool();
            }
        }
        activeDamageTexts.Clear();
    }
    
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
    
    /// <summary>
    /// 获取对象池状态信息
    /// </summary>
    /// <returns>状态信息字符串</returns>
    public string GetPoolStatus()
    {
        return $"对象池状态 - 可用: {damageTextPool.Count}, 活跃: {activeDamageTexts.Count}, 总计: {damageTextPool.Count + activeDamageTexts.Count}";
    }
    
    void OnDestroy()
    {
        // 清理所有伤害数字
        ClearAllDamageTexts();
    }
}