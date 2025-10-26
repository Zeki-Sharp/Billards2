using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 模拟对象复制器 - 负责将主场景对象复制到影子场景
/// 
/// 【核心职责】：
/// - 扫描主场景中的动态物体（Player、Enemy）和静态边界（Wall）
/// - 复制物理相关组件到影子场景
/// - 配置模拟模式和物理参数
/// - 管理复制对象的生命周期
/// 
/// 【设计原则】：
/// - 只复制物理组件，不复制渲染/UI/音效
/// - 区分动态物体和静态边界，针对性处理
/// - 支持标签扩展，易于添加新类型
/// </summary>
public class SimulationObjectReplicator : MonoBehaviour
{
    #region 配置
    
    [Header("对象标签配置")]
    [Tooltip("动态物体标签（需要模拟运动）")]
    [SerializeField] private string[] dynamicObjectTags = { "Player", "Enemy" };
    
    [Tooltip("静态边界标签（只提供碰撞）")]
    [SerializeField] private string[] staticObjectTags = { "Wall" };
    
    [Header("复制设置")]
    [Tooltip("是否复制BallPhysics组件")]
    [SerializeField] private bool replicateBallPhysics = true;
    
    [Tooltip("是否复制物理材质")]
    [SerializeField] private bool replicatePhysicsMaterial = true;
    
    [Header("调试")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLog = true;
    
    #endregion
    
    #region 私有字段
    
    // 场景引用
    private Scene simulationScene;
    
    // 复制对象映射（主场景对象 -> 影子场景对象）
    private Dictionary<GameObject, GameObject> replicatedObjects = new Dictionary<GameObject, GameObject>();
    
    // 分类存储
    private List<GameObject> replicatedDynamicObjects = new List<GameObject>();
    private List<GameObject> replicatedStaticObjects = new List<GameObject>();
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 复制所有对象到影子场景
    /// </summary>
    /// <param name="targetScene">目标影子场景</param>
    public void ReplicateAllObjects(Scene targetScene)
    {
        if (!targetScene.IsValid())
        {
            Debug.LogError("SimulationObjectReplicator: 目标场景无效！");
            return;
        }
        
        simulationScene = targetScene;
        
        if (showDebugLog)
        {
            Debug.Log($"SimulationObjectReplicator: 开始复制对象到场景 {simulationScene.name}");
        }
        
        // 清空之前的复制记录
        ClearReplicatedObjects();
        
        // 复制动态物体
        foreach (string tag in dynamicObjectTags)
        {
            ReplicateDynamicObjects(tag);
        }
        
        // 复制静态边界
        foreach (string tag in staticObjectTags)
        {
            ReplicateStaticObjects(tag);
        }
        
        if (showDebugLog)
        {
            Debug.Log($"SimulationObjectReplicator: 复制完成");
            Debug.Log($"  动态物体: {replicatedDynamicObjects.Count} 个");
            Debug.Log($"  静态边界: {replicatedStaticObjects.Count} 个");
            Debug.Log($"  总计: {replicatedObjects.Count} 个");
        }
    }
    
    /// <summary>
    /// 清空所有复制的对象
    /// </summary>
    public void ClearReplicatedObjects()
    {
        foreach (var kvp in replicatedObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        
        replicatedObjects.Clear();
        replicatedDynamicObjects.Clear();
        replicatedStaticObjects.Clear();
        
        if (showDebugLog)
        {
            Debug.Log("SimulationObjectReplicator: 已清空所有复制对象");
        }
    }
    
    /// <summary>
    /// 获取原始对象对应的复制对象
    /// </summary>
    public GameObject GetReplicatedObject(GameObject original)
    {
        if (replicatedObjects.TryGetValue(original, out GameObject replicated))
        {
            return replicated;
        }
        return null;
    }
    
    /// <summary>
    /// 获取所有复制的动态物体
    /// </summary>
    public List<GameObject> GetReplicatedDynamicObjects()
    {
        return new List<GameObject>(replicatedDynamicObjects);
    }
    
    /// <summary>
    /// 获取所有复制的静态对象
    /// </summary>
    public List<GameObject> GetReplicatedStaticObjects()
    {
        return new List<GameObject>(replicatedStaticObjects);
    }
    
    #endregion
    
    #region 私有方法 - 动态物体复制
    
    /// <summary>
    /// 复制动态物体（Player、Enemy）
    /// </summary>
    private void ReplicateDynamicObjects(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        
        if (showDebugLog)
        {
            Debug.Log($"SimulationObjectReplicator: 找到 {objects.Length} 个 [{tag}] 对象");
        }
        
        foreach (GameObject original in objects)
        {
            GameObject replicated = ReplicateDynamicObject(original);
            if (replicated != null)
            {
                replicatedObjects[original] = replicated;
                replicatedDynamicObjects.Add(replicated);
            }
        }
    }
    
    /// <summary>
    /// 复制单个动态物体
    /// </summary>
    private GameObject ReplicateDynamicObject(GameObject original)
    {
        // 创建新对象
        GameObject replicated = new GameObject($"Sim_{original.name}");
        
        // 复制Transform
        replicated.transform.position = original.transform.position;
        replicated.transform.rotation = original.transform.rotation;
        replicated.transform.localScale = original.transform.localScale;
        
        // 复制Rigidbody2D
        Rigidbody2D originalRb = original.GetComponent<Rigidbody2D>();
        if (originalRb != null)
        {
            Rigidbody2D replicatedRb = replicated.AddComponent<Rigidbody2D>();
            CopyRigidbody2DProperties(originalRb, replicatedRb);
        }
        else
        {
            Debug.LogWarning($"SimulationObjectReplicator: {original.name} 没有 Rigidbody2D 组件");
            Destroy(replicated);
            return null;
        }
        
        // 复制Collider2D
        Collider2D originalCollider = original.GetComponent<Collider2D>();
        if (originalCollider != null)
        {
            CopyCollider2D(originalCollider, replicated);
        }
        else
        {
            Debug.LogWarning($"SimulationObjectReplicator: {original.name} 没有 Collider2D 组件");
        }
        
        // 复制BallPhysics（如果启用）
        if (replicateBallPhysics)
        {
            BallPhysics originalPhysics = original.GetComponent<BallPhysics>();
            if (originalPhysics != null)
            {
                BallPhysics replicatedPhysics = replicated.AddComponent<BallPhysics>();
                ConfigureBallPhysicsForSimulation(originalPhysics, replicatedPhysics);
            }
        }
        
        // 移动到影子场景
        SceneManager.MoveGameObjectToScene(replicated, simulationScene);
        
        // 配置动态物体（根据Tag区分Player和Enemy）
        ConfigureDynamicObject(replicated, originalRb, original.tag);
        
        if (showDebugLog)
        {
            Debug.Log($"  ✓ 复制动态物体: {original.name} -> {replicated.name}");
        }
        
        return replicated;
    }
    
    /// <summary>
    /// 复制Rigidbody2D属性
    /// </summary>
    private void CopyRigidbody2DProperties(Rigidbody2D from, Rigidbody2D to)
    {
        to.bodyType = from.bodyType;
        to.mass = from.mass;
        to.linearDamping = from.linearDamping;
        to.angularDamping = from.angularDamping;
        to.gravityScale = from.gravityScale;
        to.constraints = from.constraints;
        to.freezeRotation = from.freezeRotation;
        
        // 物理材质
        if (replicatePhysicsMaterial && from.sharedMaterial != null)
        {
            to.sharedMaterial = from.sharedMaterial;
        }
    }
    
    /// <summary>
    /// 复制Collider2D
    /// </summary>
    private void CopyCollider2D(Collider2D originalCollider, GameObject target)
    {
        if (originalCollider is CircleCollider2D circleCollider)
        {
            CircleCollider2D newCollider = target.AddComponent<CircleCollider2D>();
            newCollider.radius = circleCollider.radius;
            newCollider.offset = circleCollider.offset;
            newCollider.isTrigger = circleCollider.isTrigger;
            if (replicatePhysicsMaterial && circleCollider.sharedMaterial != null)
            {
                newCollider.sharedMaterial = circleCollider.sharedMaterial;
            }
        }
        else if (originalCollider is BoxCollider2D boxCollider)
        {
            BoxCollider2D newCollider = target.AddComponent<BoxCollider2D>();
            newCollider.size = boxCollider.size;
            newCollider.offset = boxCollider.offset;
            newCollider.isTrigger = boxCollider.isTrigger;
            if (replicatePhysicsMaterial && boxCollider.sharedMaterial != null)
            {
                newCollider.sharedMaterial = boxCollider.sharedMaterial;
            }
        }
        else if (originalCollider is EdgeCollider2D edgeCollider)
        {
            EdgeCollider2D newCollider = target.AddComponent<EdgeCollider2D>();
            newCollider.points = edgeCollider.points;
            newCollider.offset = edgeCollider.offset;
            newCollider.isTrigger = edgeCollider.isTrigger;
            if (replicatePhysicsMaterial && edgeCollider.sharedMaterial != null)
            {
                newCollider.sharedMaterial = edgeCollider.sharedMaterial;
            }
        }
        else if (originalCollider is PolygonCollider2D polygonCollider)
        {
            PolygonCollider2D newCollider = target.AddComponent<PolygonCollider2D>();
            newCollider.points = polygonCollider.points;
            newCollider.offset = polygonCollider.offset;
            newCollider.isTrigger = polygonCollider.isTrigger;
            if (replicatePhysicsMaterial && polygonCollider.sharedMaterial != null)
            {
                newCollider.sharedMaterial = polygonCollider.sharedMaterial;
            }
        }
        else
        {
            Debug.LogWarning($"SimulationObjectReplicator: 不支持的碰撞器类型 {originalCollider.GetType().Name}");
        }
    }
    
    /// <summary>
    /// 配置BallPhysics为模拟模式
    /// </summary>
    private void ConfigureBallPhysicsForSimulation(BallPhysics original, BallPhysics replicated)
    {
        // 复制BallData引用
        replicated.ballData = original.ballData;
        
        // 启用模拟模式
        replicated.isSimulationMode = true;
        
        // 初始化模拟状态
        replicated.InitializeSimulationState();
    }
    
    /// <summary>
    /// 配置动态物体（Player: 运动球，Enemy: 静止球需防休眠）
    /// </summary>
    private void ConfigureDynamicObject(GameObject replicated, Rigidbody2D rb, string originalTag)
    {
        if (originalTag == "Player")
        {
            // 玩家球：保持Dynamic，将接收初始速度
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            
            if (showDebugLog)
            {
                Debug.Log($"    配置为玩家球（运动球）");
            }
        }
        else if (originalTag == "Enemy")
        {
            // 敌人球：Dynamic + 防休眠配置
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            // 防止休眠导致碰撞检测失效
            rb.WakeUp();
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            
            if (showDebugLog)
            {
                Debug.Log($"    配置为敌人球（静止球+防休眠）");
            }
        }
    }
    
    #endregion
    
    #region 私有方法 - 静态边界复制
    
    /// <summary>
    /// 复制静态边界（Wall）
    /// </summary>
    private void ReplicateStaticObjects(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        
        if (showDebugLog)
        {
            Debug.Log($"SimulationObjectReplicator: 找到 {objects.Length} 个 [{tag}] 对象");
        }
        
        foreach (GameObject original in objects)
        {
            GameObject replicated = ReplicateStaticObject(original);
            if (replicated != null)
            {
                replicatedObjects[original] = replicated;
                replicatedStaticObjects.Add(replicated);
            }
        }
    }
    
    /// <summary>
    /// 复制单个静态边界
    /// </summary>
    private GameObject ReplicateStaticObject(GameObject original)
    {
        // 创建新对象
        GameObject replicated = new GameObject($"Sim_{original.name}");
        
        // 复制Transform
        replicated.transform.position = original.transform.position;
        replicated.transform.rotation = original.transform.rotation;
        replicated.transform.localScale = original.transform.localScale;
        
        // 复制Collider2D（静态边界只需要碰撞器）
        Collider2D originalCollider = original.GetComponent<Collider2D>();
        if (originalCollider != null)
        {
            CopyCollider2D(originalCollider, replicated);
        }
        else
        {
            Debug.LogWarning($"SimulationObjectReplicator: {original.name} 没有 Collider2D 组件");
            Destroy(replicated);
            return null;
        }
        
        // 静态边界不需要Rigidbody2D（或者添加Static类型）
        // 如果需要物理材质反弹效果，可以添加Static Rigidbody2D
        Rigidbody2D originalRb = original.GetComponent<Rigidbody2D>();
        if (originalRb != null && originalRb.bodyType == RigidbodyType2D.Static)
        {
            Rigidbody2D replicatedRb = replicated.AddComponent<Rigidbody2D>();
            replicatedRb.bodyType = RigidbodyType2D.Static;
            if (replicatePhysicsMaterial && originalRb.sharedMaterial != null)
            {
                replicatedRb.sharedMaterial = originalRb.sharedMaterial;
            }
        }
        
        // 移动到影子场景
        SceneManager.MoveGameObjectToScene(replicated, simulationScene);
        
        if (showDebugLog)
        {
            Debug.Log($"  ✓ 复制静态边界: {original.name} -> {replicated.name}");
        }
        
        return replicated;
    }
    
    #endregion
    
    #region Inspector测试方法
    
    /// <summary>
    /// [测试] 复制所有对象
    /// </summary>
    [ContextMenu("测试/复制所有对象到影子场景")]
    private void Test_ReplicateAll()
    {
        Scene simScene = TrajectorySimulationManager.Instance.GetSimulationScene();
        
        if (!simScene.IsValid())
        {
            Debug.LogError("影子场景无效，请先创建影子场景！");
            return;
        }
        
        Debug.Log("========== 测试：复制所有对象 ==========");
        ReplicateAllObjects(simScene);
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 清空复制的对象
    /// </summary>
    [ContextMenu("测试/清空复制对象")]
    private void Test_ClearReplicated()
    {
        Debug.Log("========== 测试：清空复制对象 ==========");
        ClearReplicatedObjects();
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 打印复制统计
    /// </summary>
    [ContextMenu("测试/打印复制统计")]
    private void Test_PrintStats()
    {
        Debug.Log("========== 复制对象统计 ==========");
        Debug.Log($"动态物体: {replicatedDynamicObjects.Count} 个");
        foreach (var obj in replicatedDynamicObjects)
        {
            if (obj != null)
            {
                Debug.Log($"  - {obj.name}");
            }
        }
        
        Debug.Log($"静态边界: {replicatedStaticObjects.Count} 个");
        foreach (var obj in replicatedStaticObjects)
        {
            if (obj != null)
            {
                Debug.Log($"  - {obj.name}");
            }
        }
        
        Debug.Log($"总计: {replicatedObjects.Count} 个映射关系");
        Debug.Log("================================");
    }
    
    #endregion
}

