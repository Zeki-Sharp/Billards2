using UnityEngine;

/// <summary>
/// 瞄准线落点管理器 - 管理瞄准线上的落点显示
/// 
/// 【核心职责】：
/// - 管理落点预制体的显示/隐藏
/// - 更新落点位置
/// - 处理落点生命周期
/// - 提供落点状态查询
/// 
/// 【设计原则】：
/// - MonoBehaviour组件，可在Inspector中配置
/// - 职责单一，专注落点管理
/// - 与瞄准线系统松耦合
/// - 支持动态落点位置更新
/// </summary>
public class AimLineLandingPointManager : MonoBehaviour
{
    #region 配置参数
    
    [Header("落点设置")]
    [Tooltip("落点图片")]
    public Sprite landingPointSprite;
    
    [Tooltip("落点大小")]
    public float landingPointSize = 0.3f;
    
    [Tooltip("落点颜色")]
    public Color landingPointColor = Color.red;
    
    [Tooltip("落点排序顺序")]
    public int landingPointSortingOrder = 11;
    
    [Tooltip("是否显示落点")]
    public bool showLandingPoint = true;
    
    [Header("攻击范围圆形设置")]
    [Tooltip("是否显示攻击范围圆形")]
    public bool showAttackRangeCircle = true;
    
    [Tooltip("攻击范围圆形图片")]
    public Sprite attackRangeCircleSprite;
    
    [Tooltip("攻击范围圆形颜色")]
    public Color attackRangeCircleColor = new Color(1f, 0.5f, 0f, 0.3f);
    
    [Tooltip("攻击范围圆形排序顺序")]
    public int attackRangeCircleSortingOrder = 10;
    
    [Header("调试设置")]
    [Tooltip("是否显示调试信息")]
    public bool showDebugInfo = false;
    
    #endregion
    
    #region 私有变量
    
    // 当前落点对象
    private GameObject currentLandingPoint;
    
    // 当前攻击范围圆形对象
    private GameObject currentAttackRangeCircle;
    
    // 落点状态
    private bool isLandingPointVisible = false;
    private Vector3 lastLandingPosition;
    
    // 落点容器（参考转折点的indicatorContainer）
    private GameObject landingPointContainer;
    
    // ✅ 多角色系统改造：存储父物体的 PlayerBehavior 引用
    private PlayerBehavior playerBehavior;
    
    #endregion
    
    #region Unity生命周期
    
    /// <summary>
    /// 初始化
    /// </summary>
    void Start()
    {
        InitializeLandingPointManager();
    }
    
    /// <summary>
    /// 清理
    /// </summary>
    void OnDestroy()
    {
        HideLandingPoint();
        
        // 清理容器
        if (landingPointContainer != null)
        {
            DestroyImmediate(landingPointContainer);
        }
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化落点管理器
    /// </summary>
    void InitializeLandingPointManager()
    {
        // ✅ 多角色系统改造：从父物体获取 PlayerBehavior
        // 结构：Player (PlayerBehavior) -> AimController (本组件挂在这里)
        if (transform.parent != null)
        {
            playerBehavior = transform.parent.GetComponent<PlayerBehavior>();
            if (playerBehavior == null)
            {
                Debug.LogWarning($"[AimLineLandingPointManager] 未找到父物体 {transform.parent.name} 的 PlayerBehavior 组件");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[AimLineLandingPointManager] ✅ 找到 PlayerBehavior: {playerBehavior.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("[AimLineLandingPointManager] 没有父物体！");
        }
        
        // 创建落点容器
        CreateLandingPointContainer();
        
        if (showDebugInfo)
        {
            Debug.Log("[AimLineLandingPointManager] 初始化完成");
        }
    }
    
    /// <summary>
    /// 创建落点容器
    /// </summary>
    void CreateLandingPointContainer()
    {
        if (landingPointContainer != null)
        {
            DestroyImmediate(landingPointContainer);
        }
        
        landingPointContainer = new GameObject("LandingPointContainer");
        landingPointContainer.transform.SetParent(transform);
    }
    
    #endregion
    
    #region 主要接口
    
    /// <summary>
    /// 显示落点
    /// </summary>
    /// <param name="position">落点位置</param>
    public void ShowLandingPoint(Vector3 position)
    {
        if (!showLandingPoint)
        {
            return;
        }
        
        // 如果落点已显示且位置相同，无需更新
        if (isLandingPointVisible && Vector3.Distance(position, lastLandingPosition) < 0.01f)
        {
            return;
        }
        
        // 创建或更新落点
        CreateOrUpdateLandingPoint(position);
        
        // 更新状态
        isLandingPointVisible = true;
        lastLandingPosition = position;
        
        // ✅ 多角色系统改造：显示攻击范围圆形（撞击和三角形角色显示，范围攻击不显示）
        if (showAttackRangeCircle && ShouldShowLandingPointCircle())
        {
            float radius = GetAttackRangeRadius();
            if (radius > 0f)
            {
                ShowAttackRangeCircle(position);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AimLineLandingPointManager] 显示落点: {position}");
        }
    }
    
    /// <summary>
    /// 隐藏落点
    /// </summary>
    public void HideLandingPoint()
    {
        if (!isLandingPointVisible)
        {
            return;
        }
        
        // 销毁落点对象
        if (currentLandingPoint != null)
        {
            DestroyImmediate(currentLandingPoint);
            currentLandingPoint = null;
        }
        
        // 销毁攻击范围圆形对象
        if (currentAttackRangeCircle != null)
        {
            DestroyImmediate(currentAttackRangeCircle);
            currentAttackRangeCircle = null;
        }
        
        // 更新状态
        isLandingPointVisible = false;
        
        if (showDebugInfo)
        {
            Debug.Log("[AimLineLandingPointManager] 隐藏落点");
        }
    }
    
    /// <summary>
    /// 更新落点位置
    /// </summary>
    /// <param name="position">新位置</param>
    public void UpdateLandingPointPosition(Vector3 position)
    {
        if (isLandingPointVisible)
        {
            ShowLandingPoint(position);
        }
    }
    
    /// <summary>
    /// 检查落点是否可见
    /// </summary>
    /// <returns>是否可见</returns>
    public bool IsLandingPointVisible()
    {
        return isLandingPointVisible;
    }
    
    /// <summary>
    /// 获取当前落点位置
    /// </summary>
    /// <returns>落点位置</returns>
    public Vector3 GetCurrentLandingPosition()
    {
        return isLandingPointVisible ? lastLandingPosition : Vector3.zero;
    }
    
    #endregion
    
    #region 内部方法
    
    /// <summary>
    /// 创建或更新落点
    /// </summary>
    /// <param name="position">位置</param>
    void CreateOrUpdateLandingPoint(Vector3 position)
    {
        // 如果落点对象不存在，创建新的
        if (currentLandingPoint == null)
        {
            CreateLandingPointObject();
        }
        
        // 更新位置
        if (currentLandingPoint != null)
        {
            currentLandingPoint.transform.position = position;
        }
    }
    
    /// <summary>
    /// 创建落点对象
    /// </summary>
    void CreateLandingPointObject()
    {
        GameObject indicatorObj = new GameObject("LandingPoint");
        indicatorObj.transform.SetParent(landingPointContainer.transform);
        
        // 添加SpriteRenderer组件
        SpriteRenderer spriteRenderer = indicatorObj.AddComponent<SpriteRenderer>();
        
        // 设置图片
        if (landingPointSprite != null)
        {
            spriteRenderer.sprite = landingPointSprite;
        }
        else
        {
            // 如果没有设置图片，使用默认的白色方块
            spriteRenderer.sprite = CreateDefaultLandingPointSprite();
        }
        
        // 设置属性
        spriteRenderer.color = landingPointColor;
        spriteRenderer.sortingOrder = landingPointSortingOrder;
        spriteRenderer.transform.localScale = Vector3.one * landingPointSize;
        
        currentLandingPoint = indicatorObj;
        
        if (showDebugInfo)
        {
            Debug.Log("[AimLineLandingPointManager] 创建落点对象");
        }
    }
    
    /// <summary>
    /// 创建默认落点图片（当没有设置自定义图片时）
    /// </summary>
    /// <returns>默认的白色方块Sprite</returns>
    Sprite CreateDefaultLandingPointSprite()
    {
        // 创建一个简单的白色方块作为默认落点
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    
    #endregion
    
    #region 公共方法
    
    
    /// <summary>
    /// 设置落点图片
    /// </summary>
    /// <param name="sprite">落点图片</param>
    public void SetLandingPointSprite(Sprite sprite)
    {
        landingPointSprite = sprite;
    }
    
    /// <summary>
    /// 设置落点大小
    /// </summary>
    /// <param name="size">落点大小</param>
    public void SetLandingPointSize(float size)
    {
        landingPointSize = Mathf.Max(0.1f, size);
    }
    
    /// <summary>
    /// 设置落点颜色
    /// </summary>
    /// <param name="color">落点颜色</param>
    public void SetLandingPointColor(Color color)
    {
        landingPointColor = color;
    }
    
    /// <summary>
    /// 设置是否显示落点
    /// </summary>
    /// <param name="show">是否显示</param>
    public void SetShowLandingPoint(bool show)
    {
        showLandingPoint = show;
        if (!show && isLandingPointVisible)
        {
            HideLandingPoint();
        }
    }
    
    /// <summary>
    /// 在Inspector中测试显示落点
    /// </summary>
    [ContextMenu("测试显示落点")]
    void TestShowLandingPoint()
    {
        if (Application.isPlaying)
        {
            Vector3 testPosition = transform.position + Vector3.right * 2f;
            ShowLandingPoint(testPosition);
        }
    }
    
    /// <summary>
    /// 在Inspector中测试隐藏落点
    /// </summary>
    [ContextMenu("测试隐藏落点")]
    void TestHideLandingPoint()
    {
        if (Application.isPlaying)
        {
            HideLandingPoint();
        }
    }
    
    #endregion
    
    #region 攻击范围圆形
    
    /// <summary>
    /// ✅ 多角色系统改造：检查是否应该显示落点范围圈
    /// 范围攻击角色显示（需要看到攻击范围），撞击和三角形角色不显示
    /// </summary>
    /// <returns>是否应该显示落点范围圈</returns>
    bool ShouldShowLandingPointCircle()
    {
        if (playerBehavior == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AimLineLandingPointManager] playerBehavior 为 null，无法判断是否显示落点范围圈");
            }
            return false;
        }
        
        Player player = playerBehavior.GetComponent<Player>();
        if (player == null || player.playerData == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AimLineLandingPointManager] Player 或 playerData 为 null");
            }
            return false;
        }
        
        // ✅ 检查角色是否配置了 Stopped 圆形范围攻击规则
        bool shouldShow = HasStoppedCircleRangeAttack(player.playerData);
        
        if (showDebugInfo)
        {
            Debug.Log($"[AimLineLandingPointManager] 是否有范围攻击: {shouldShow}");
        }
        
        return shouldShow;
    }
    
    /// <summary>
    /// 检查角色是否配置了 Stopped 圆形范围攻击规则
    /// </summary>
    bool HasStoppedCircleRangeAttack(PlayerData data)
    {
        if (data == null || data.damageProfiles == null) return false;
        
        foreach (var profile in data.damageProfiles)
        {
            if (profile == null || profile.rules == null) continue;
            
            foreach (var rule in profile.rules)
            {
                if (rule != null && 
                    rule.triggerType == DamageTriggerType.Stopped && 
                    rule.rangeShape == RangeShapeType.Circle)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// ✅ 获取攻击范围半径（使用当前角色的 PlayerBehavior）
    /// </summary>
    /// <returns>攻击范围半径</returns>
    float GetAttackRangeRadius()
    {
        // ✅ 使用已存储的 playerBehavior（当前角色），而不是 FindFirstObjectByType（会找到错误的角色）
        if (playerBehavior == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AimLineLandingPointManager] playerBehavior 为 null，无法获取攻击范围");
            }
            return 0f;
        }
        
        PlayerAttackManager attackManager = playerBehavior.GetComponent<PlayerAttackManager>();
        if (attackManager == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[AimLineLandingPointManager] PlayerAttackManager 为 null");
            }
            return 0f;
        }
        
        float radius = attackManager.GetFinalAreaRadius();
        
        if (showDebugInfo)
        {
            Debug.Log($"[AimLineLandingPointManager] 获取攻击范围半径: {radius} (角色: {playerBehavior.gameObject.name})");
        }
        
        return radius;
    }
    
    /// <summary>
    /// 显示攻击范围圆形
    /// </summary>
    /// <param name="position">位置</param>
    void ShowAttackRangeCircle(Vector3 position)
    {
        // 获取攻击范围半径
        float radius = GetAttackRangeRadius();
        if (radius <= 0f)
        {
            return;
        }
        
        // 如果攻击范围圆形已存在，直接更新位置和大小
        if (currentAttackRangeCircle != null)
        {
            currentAttackRangeCircle.transform.position = position;
            currentAttackRangeCircle.transform.localScale = Vector3.one * radius * 2f; // 直径
            return;
        }
        
        // 创建攻击范围圆形对象
        GameObject circleObj = new GameObject("AttackRangeCircle");
        circleObj.transform.SetParent(landingPointContainer.transform);
        circleObj.transform.position = position;
        circleObj.transform.localScale = Vector3.one * radius * 2f; // 直径
        
        // 添加SpriteRenderer组件
        SpriteRenderer spriteRenderer = circleObj.AddComponent<SpriteRenderer>();
        
        // 设置图片
        if (attackRangeCircleSprite != null)
        {
            spriteRenderer.sprite = attackRangeCircleSprite;
        }
        else
        {
            // 如果没有设置图片，使用默认的白色圆形
            spriteRenderer.sprite = CreateDefaultCircleSprite();
        }
        
        // 设置属性
        spriteRenderer.color = attackRangeCircleColor;
        spriteRenderer.sortingOrder = attackRangeCircleSortingOrder;
        
        currentAttackRangeCircle = circleObj;
    }
    
    /// <summary>
    /// 创建默认圆形图片（当没有设置自定义图片时）
    /// </summary>
    /// <returns>默认的白色圆形Sprite</returns>
    Sprite CreateDefaultCircleSprite()
    {
        // 创建一个简单的白色圆形作为默认攻击范围圆形
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        Vector2 center = new Vector2(32, 32);
        float radius = 30f;
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * 64 + x] = Color.white;
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 获取当前落点对象
    /// </summary>
    public GameObject CurrentLandingPoint => currentLandingPoint;
    
    /// <summary>
    /// 获取落点是否可见
    /// </summary>
    public bool IsVisible => isLandingPointVisible;
    
    /// <summary>
    /// 获取上次落点位置
    /// </summary>
    public Vector3 LastPosition => lastLandingPosition;
    
    /// <summary>
    /// 获取当前攻击范围圆形对象
    /// </summary>
    public GameObject CurrentAttackRangeCircle => currentAttackRangeCircle;
    
    #endregion
}
