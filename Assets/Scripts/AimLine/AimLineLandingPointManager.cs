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
        
        // 显示攻击范围圆形（如果启用且角色是范围攻击模式）
        if (showAttackRangeCircle && IsAreaAttackMode())
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
    /// 检查是否为范围攻击模式
    /// </summary>
    /// <returns>是否为范围攻击模式</returns>
    bool IsAreaAttackMode()
    {
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore == null) return false;
        
        Player player = playerCore.GetComponent<Player>();
        if (player == null || player.playerData == null) return false;
        
        return player.playerData.attackMode == PlayerData.AttackMode.Area;
    }
    
    /// <summary>
    /// 获取攻击范围半径
    /// </summary>
    /// <returns>攻击范围半径</returns>
    float GetAttackRangeRadius()
    {
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore == null) return 0f;
        
        PlayerAttackManager attackManager = playerCore.GetComponent<PlayerAttackManager>();
        if (attackManager == null) return 0f;
        
        return attackManager.GetFinalAreaRadius();
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
