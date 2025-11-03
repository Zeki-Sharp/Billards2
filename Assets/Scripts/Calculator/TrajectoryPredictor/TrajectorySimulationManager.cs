using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 轨迹模拟管理器 - 负责创建和管理影子场景
/// 
/// 【核心职责】：
/// - 创建独立的物理场景（影子场景）
/// - 管理场景生命周期
/// - 提供PhysicsScene2D引用供轨迹预测使用
/// - 确保主场景和影子场景互不干扰
/// 
/// 【设计原则】：
/// - 单例模式：全局唯一的影子场景管理器（继承 SingletonManager）
/// - 跨场景持久化：DontDestroyOnLoad
/// - 延迟创建：首次访问时才创建场景
/// - 自动清理：销毁时清理影子场景
/// 
/// 【执行顺序】：SYSTEM (-100)，早于所有其他组件
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class TrajectorySimulationManager : SingletonManager<TrajectorySimulationManager>
{
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;  // 跨场景持久化
    
    protected override void OnManagerCreated()
    {
        // ✅ 获取对象复制器组件
        objectReplicator = GetComponent<SimulationObjectReplicator>();
        if (objectReplicator == null)
        {
            Debug.LogWarning("TrajectorySimulationManager: 没有找到 SimulationObjectReplicator 组件");
        }
        
        // 可选：启动时创建场景
        if (createOnAwake)
        {
            CreateSimulationScene();
            
            // ✅ 检查场景是否创建成功后再复制对象
            if (objectReplicator != null && isSceneCreated && simulationScene.IsValid())
            {
                objectReplicator.ReplicateAllObjects(simulationScene);
                
                if (showDebugLog)
                {
                    Debug.Log("TrajectorySimulationManager: 已复制场景对象到影子场景");
                }
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"TrajectorySimulationManager: 跳过对象复制 - isSceneCreated={isSceneCreated}, sceneValid={simulationScene.IsValid()}");
            }
        }
        
        if (showDebugLog)
        {
            Debug.Log("TrajectorySimulationManager: 初始化完成");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 清理影子场景
        DestroySimulationScene();
    }
    
    #endregion
    
    #region 字段
    
    [Header("影子场景设置")]
    [Tooltip("影子场景名称")]
    [SerializeField] private string simulationSceneName = "TrajectorySimulationScene";
    
    [Tooltip("是否在启动时立即创建场景")]
    [SerializeField] private bool createOnAwake = true;
    
    [Header("调试")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLog = true;
    
    // 影子场景引用
    private Scene simulationScene;
    private PhysicsScene2D simulationPhysicsScene;
    
    // 场景状态
    private bool isSceneCreated = false;
    
    // ✅ 多角色系统改造：内部管理对象复制器
    private SimulationObjectReplicator objectReplicator;
    
    #endregion
    
    // ✅ 继承 SingletonManager 后，不再需要手动实现 Awake/OnDestroy
    // 逻辑已移到 OnManagerCreated/OnManagerDestroyed
    
    #region 公共方法
    
    /// <summary>
    /// 创建影子场景（如果尚未创建）
    /// </summary>
    public void CreateSimulationScene()
    {
        if (isSceneCreated)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("TrajectorySimulationManager: 影子场景已存在，跳过创建");
            }
            return;
        }
        
        try
        {
            // 创建独立的物理场景
            // LocalPhysicsMode.Physics2D 确保这个场景有独立的2D物理引擎
            CreateSceneParameters parameters = new CreateSceneParameters(LocalPhysicsMode.Physics2D);
            simulationScene = SceneManager.CreateScene(simulationSceneName, parameters);
            
            // 获取物理场景引用
            simulationPhysicsScene = simulationScene.GetPhysicsScene2D();
            
            // 验证场景创建成功
            if (!simulationScene.IsValid())
            {
                Debug.LogError("TrajectorySimulationManager: 影子场景创建失败！");
                return;
            }
            
            // 验证物理场景有效
            if (!simulationPhysicsScene.IsValid())
            {
                Debug.LogError("TrajectorySimulationManager: 影子场景的物理引擎无效！");
                return;
            }
            
            isSceneCreated = true;
            
            if (showDebugLog)
            {
                Debug.Log($"TrajectorySimulationManager: 影子场景创建成功 - {simulationSceneName}");
                Debug.Log($"  场景句柄: {simulationScene.handle}");
                Debug.Log($"  物理场景有效: {simulationPhysicsScene.IsValid()}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TrajectorySimulationManager: 创建影子场景时出错 - {e.Message}");
            isSceneCreated = false;
        }
    }
    
    /// <summary>
    /// 销毁影子场景
    /// </summary>
    public void DestroySimulationScene()
    {
        if (!isSceneCreated)
        {
            return;
        }
        
        try
        {
            if (simulationScene.IsValid())
            {
                SceneManager.UnloadSceneAsync(simulationScene);
                
                if (showDebugLog)
                {
                    Debug.Log("TrajectorySimulationManager: 影子场景已销毁");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TrajectorySimulationManager: 销毁影子场景时出错 - {e.Message}");
        }
        finally
        {
            isSceneCreated = false;
        }
    }
    
    /// <summary>
    /// 获取影子场景引用
    /// </summary>
    public Scene GetSimulationScene()
    {
        // ✅ 检查场景有效性（可能失效）
        if (!IsSceneValid())
        {
            Debug.LogWarning("TrajectorySimulationManager: 影子场景无效或未创建，正在创建...");
            CreateSimulationScene();
            
            // 创建后复制对象
            if (objectReplicator != null && isSceneCreated)
            {
                objectReplicator.ReplicateAllObjects(simulationScene);
            }
        }
        
        return simulationScene;
    }
    
    /// <summary>
    /// 获取影子场景的物理引擎引用
    /// </summary>
    public PhysicsScene2D GetPhysicsScene()
    {
        // ✅ 检查场景有效性（可能失效）
        if (!IsSceneValid())
        {
            Debug.LogWarning("TrajectorySimulationManager: 影子场景无效或未创建，正在创建...");
            CreateSimulationScene();
            
            // 创建后复制对象
            if (objectReplicator != null && isSceneCreated)
            {
                objectReplicator.ReplicateAllObjects(simulationScene);
            }
        }
        
        return simulationPhysicsScene;
    }
    
    /// <summary>
    /// 检查影子场景是否已创建且有效
    /// </summary>
    public bool IsSceneValid()
    {
        // ✅ 检测场景失效：如果标记为已创建，但实际无效，重置标记
        if (isSceneCreated && !simulationScene.IsValid())
        {
            Debug.LogWarning("TrajectorySimulationManager: 检测到场景失效（可能被卸载），重置标记");
            isSceneCreated = false;
            return false;
        }
        
        return isSceneCreated && simulationScene.IsValid() && simulationPhysicsScene.IsValid();
    }
    
    /// <summary>
    /// 重新创建影子场景（先销毁再创建）
    /// </summary>
    public void RecreateSimulationScene()
    {
        if (showDebugLog)
        {
            Debug.Log("TrajectorySimulationManager: 重新创建影子场景");
        }
        
        DestroySimulationScene();
        CreateSimulationScene();
    }
    
    /// <summary>
    /// 更新影子场景中的动态对象（多角色系统 - 每回合或敌人移动时调用）
    /// </summary>
    public void UpdateDynamicObjects()
    {
        if (!isSceneCreated)
        {
            Debug.LogWarning("TrajectorySimulationManager: 影子场景未创建，无法更新对象");
            return;
        }
        
        if (objectReplicator != null)
        {
            objectReplicator.ReplicateAllObjects(simulationScene);
            
            if (showDebugLog)
            {
                Debug.Log("TrajectorySimulationManager: 已更新影子场景中的动态对象");
            }
        }
        else
        {
            Debug.LogWarning("TrajectorySimulationManager: ObjectReplicator 为 null，无法更新对象");
        }
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 创建一个简单的圆形Sprite（用于可视化测试球）
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        float center = size / 2f;
        float radius = size / 2f - 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    #endregion
    
    #region 调试方法
    
    /// <summary>
    /// 获取场景信息（用于调试）
    /// </summary>
    public string GetSceneInfo()
    {
        if (!isSceneCreated)
        {
            return "影子场景未创建";
        }
        
        int objectCount = simulationScene.rootCount;
        
        return $"影子场景信息:\n" +
               $"  名称: {simulationScene.name}\n" +
               $"  句柄: {simulationScene.handle}\n" +
               $"  有效: {simulationScene.IsValid()}\n" +
               $"  物理场景有效: {simulationPhysicsScene.IsValid()}\n" +
               $"  根对象数: {objectCount}";
    }
    
    /// <summary>
    /// [测试] 打印场景信息
    /// </summary>
    [ContextMenu("测试/打印场景信息")]
    private void Test_PrintSceneInfo()
    {
        Debug.Log("========== 影子场景信息 ==========");
        Debug.Log(GetSceneInfo());
        Debug.Log("================================");
    }
    
    /// <summary>
    /// [测试] 手动创建场景
    /// </summary>
    [ContextMenu("测试/创建影子场景")]
    private void Test_CreateScene()
    {
        Debug.Log("========== 测试：创建影子场景 ==========");
        CreateSimulationScene();
        Debug.Log(GetSceneInfo());
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 销毁场景
    /// </summary>
    [ContextMenu("测试/销毁影子场景")]
    private void Test_DestroyScene()
    {
        Debug.Log("========== 测试：销毁影子场景 ==========");
        DestroySimulationScene();
        Debug.Log("场景已销毁");
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 重新创建场景
    /// </summary>
    [ContextMenu("测试/重新创建场景")]
    private void Test_RecreateScene()
    {
        Debug.Log("========== 测试：重新创建场景 ==========");
        RecreateSimulationScene();
        Debug.Log(GetSceneInfo());
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 在影子场景中创建测试球
    /// </summary>
    [ContextMenu("测试/在影子场景创建测试球")]
    private void Test_CreateTestBall()
    {
        // 确保场景有效
        if (!IsSceneValid())
        {
            Debug.LogWarning("影子场景未创建或无效，正在创建...");
            CreateSimulationScene();
            
            // 再次检查
            if (!IsSceneValid())
            {
                Debug.LogError("❌ 无法创建影子场景！测试中止。");
                return;
            }
        }
        
        Debug.Log("========== 测试：创建测试球 ==========");
        Debug.Log($"影子场景状态: {simulationScene.name}, 有效={simulationScene.IsValid()}");
        
        // 创建纯GameObject（不使用CreatePrimitive，避免3D/2D物理冲突）
        GameObject testBall = new GameObject("TestBall_Simulation");
        testBall.transform.position = Vector3.zero;
        testBall.transform.localScale = Vector3.one * 0.5f;
        
        Debug.Log($"测试球创建完成，当前场景: {testBall.scene.name}");
        
        // 添加2D物理组件
        Rigidbody2D rb = testBall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        
        CircleCollider2D collider = testBall.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加SpriteRenderer用于可视化（可选）
        SpriteRenderer renderer = testBall.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = Color.cyan;
        
        Debug.Log($"已添加组件: Rigidbody2D={rb!=null}, CircleCollider2D={collider!=null}");
        
        // 移动到影子场景（关键步骤）
        Debug.Log($"准备移动球到影子场景: {simulationScene.name}");
        SceneManager.MoveGameObjectToScene(testBall, simulationScene);
        
        // 验证移动结果
        if (testBall.scene.name == simulationScene.name)
        {
            Debug.Log($"✅ 测试球已成功移动到影子场景");
        }
        else
        {
            Debug.LogError($"❌ 移动失败！球仍在场景: {testBall.scene.name}");
        }
        
        Debug.Log($"  位置: {testBall.transform.position}");
        Debug.Log($"  场景: {testBall.scene.name}");
        Debug.Log($"  场景句柄: {testBall.scene.handle}");
        Debug.Log(GetSceneInfo());
        Debug.Log("====================================");
    }
    
    /// <summary>
    /// [测试] 模拟物理步进
    /// </summary>
    [ContextMenu("测试/模拟10步物理")]
    private void Test_SimulatePhysics()
    {
        if (!IsSceneValid())
        {
            Debug.LogError("影子场景无效，请先创建场景！");
            return;
        }
        
        Debug.Log("========== 测试：模拟物理步进 ==========");
        
        // 查找影子场景中的所有Rigidbody2D
        GameObject[] rootObjects = simulationScene.GetRootGameObjects();
        int ballCount = 0;
        
        foreach (GameObject obj in rootObjects)
        {
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 给球一个初始速度
                rb.linearVelocity = new Vector2(5f, 3f);
                ballCount++;
                Debug.Log($"  给 {obj.name} 设置初始速度: (5, 3)");
            }
        }
        
        if (ballCount == 0)
        {
            Debug.LogWarning("影子场景中没有找到物理对象，请先创建测试球！");
            return;
        }
        
        // 模拟10步物理
        Debug.Log($"\n开始模拟10步物理（每步 {Time.fixedDeltaTime:F3} 秒）:");
        for (int i = 0; i < 10; i++)
        {
            simulationPhysicsScene.Simulate(Time.fixedDeltaTime);
            
            // 打印第一个球的位置
            foreach (GameObject obj in rootObjects)
            {
                if (obj.GetComponent<Rigidbody2D>() != null)
                {
                    Debug.Log($"  步骤 {i + 1}: 位置 = {obj.transform.position}");
                    break;
                }
            }
        }
        
        Debug.Log("模拟完成！");
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 清空影子场景中的所有对象
    /// </summary>
    [ContextMenu("测试/清空影子场景对象")]
    private void Test_ClearSceneObjects()
    {
        if (!IsSceneValid())
        {
            Debug.LogWarning("影子场景无效");
            return;
        }
        
        Debug.Log("========== 测试：清空场景对象 ==========");
        
        GameObject[] rootObjects = simulationScene.GetRootGameObjects();
        int count = rootObjects.Length;
        
        foreach (GameObject obj in rootObjects)
        {
            Destroy(obj);
        }
        
        Debug.Log($"已删除 {count} 个对象");
        Debug.Log(GetSceneInfo());
        Debug.Log("======================================");
    }
    
    #endregion
}

