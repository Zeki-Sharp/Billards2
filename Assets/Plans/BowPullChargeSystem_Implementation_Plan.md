# 拉弓式蓄力系统实现计划

## 需求背景

### 当前系统问题
1. **被动体验**：当前蓄力系统基于时间自动变化，玩家只需等待时机，缺乏主动操作感
2. **技巧性不足**：蓄力强度自动循环变化，玩家只需在合适时间点松开鼠标，技巧要求低
3. **反馈不直观**：需要额外的UI提示才能了解当前蓄力状态

### 期望改进
- 玩家通过**拉动鼠标距离**主动控制力度，类似拉弓射箭的操作感
- 视觉直观：鼠标拉得越远，力度越大
- 提高技巧性：需要玩家判断合适的拉弓距离

## 核心设计

### 交互流程
1. **按下鼠标左键**：开始蓄力状态
2. **拖动鼠标**：
   - 鼠标与球的距离决定发射力度
   - 球指向鼠标的反方向为发射方向（拉弓感）
   - 实时显示瞄准线和力度UI
3. **松开鼠标**：按当前方向和力度发射球体

### 力度计算公式
```
distance = Vector2.Distance(鼠标世界坐标, 球位置)
chargingPower = Clamp01(distance / maxPullDistance)
currentForce = Lerp(minForce, maxForce, chargingPower)
```

### 方向计算
```
发射方向 = (球位置 - 鼠标位置).normalized
```
与原系统相反（原系统是从球指向鼠标）

## 实现方案

### 阶段一：ChargeSystem 核心改造

#### 1. 添加新配置参数
```csharp
[Header("拉弓式蓄力设置")]
[SerializeField] private bool useBowPullMode = false;        // 是否使用拉弓模式
[SerializeField] private float maxPullDistance = 8f;        // 最大拉弓距离（世界单位）
[SerializeField] private float minPullDistance = 1f;        // 最小拉弓距离
```

#### 2. 修改 UpdateChargingProgress() 方法
**位置**：`ChargeSystem.cs` 第126-168行

**修改内容**：
- 添加模式判断：`if (useBowPullMode)`
- 获取鼠标世界坐标（需要引用PlayerCore获取球位置）
- 计算距离：`float distance = Vector2.Distance(mouseWorldPos, ballPos)`
- 距离映射到进度：`chargingPower = Mathf.Clamp01((distance - minPullDistance) / (maxPullDistance - minPullDistance))`

#### 3. 添加组件引用
```csharp
[Header("组件引用")]
[SerializeField] private PlayerCore playerCore;  // 用于获取球位置
[SerializeField] private Camera targetCamera;    // 用于坐标转换
```

#### 4. 鼠标坐标转换
**问题**：需要将屏幕坐标转换为世界坐标

**方案A（推荐）**：复用AimController的转换逻辑
- 从 `AimController.GetMouseWorldPosition()` 提取为工具方法

**方案B**：使用Unity内置方法
```csharp
Vector3 mouseScreenPos = Input.mousePosition;
Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
mouseWorldPos.z = 0;
```

### 阶段二：AimController 方向反转

#### 修改瞄准方向计算
**位置**：`AimController.cs` 第160-177行

**修改内容**：
```csharp
// 原代码（第172行）：
Vector3 direction = mouseWorldPos - playerCore.transform.position;

// 修改为：
Vector3 direction;
if (chargeSystem != null && chargeSystem.UseBowPullMode) 
{
    // 拉弓模式：从鼠标指向球（发射方向）
    direction = playerCore.transform.position - mouseWorldPos;
}
else
{
    // 原模式：从球指向鼠标
    direction = mouseWorldPos - playerCore.transform.position;
}
```

**注意**：需要添加 `ChargeSystem` 引用到 `AimController`

### 阶段三：工具方法提取（可选优化）

#### 创建 InputUtility 工具类
**目的**：避免在多个类中重复实现鼠标坐标转换

**位置**：`Assets/Scripts/Utilities/InputUtility.cs`（新建）

```csharp
public static class InputUtility
{
    /// <summary>
    /// 获取鼠标的世界坐标（2D）
    /// </summary>
    public static Vector3 GetMouseWorldPosition2D(Camera camera)
    {
        if (camera == null) camera = Camera.main;
        
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseScreenPos = new Vector3(mousePos.x, mousePos.y, 0f);
        
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float cameraSize = camera.orthographicSize;
        float aspectRatio = (float)screenWidth / screenHeight;
        
        float worldX = (mouseScreenPos.x / screenWidth - 0.5f) * cameraSize * aspectRatio * 2f;
        float worldY = (mouseScreenPos.y / screenHeight - 0.5f) * cameraSize * 2f;
        
        return new Vector3(worldX, worldY, 0f);
    }
}
```

**使用位置**：
- `ChargeSystem.UpdateChargingProgress()`
- `AimController.UpdateAimDirection()`

## 参数配置建议

### 距离参数参考值

#### 小型场景（20x20单位）
- `minPullDistance = 1.5f`  // 约1.5个球的半径
- `maxPullDistance = 6f`    // 约场景宽度的30%

#### 中型场景（40x40单位）
- `minPullDistance = 2f`
- `maxPullDistance = 10f`

#### 大型场景（60x60单位）
- `minPullDistance = 3f`
- `maxPullDistance = 15f`

### 力度参数
保持当前配置：
- `minForce = 5f`
- `maxForce = 25f`

### 模式切换
通过Inspector勾选框切换：
- `useBowPullMode = true`  // 拉弓模式
- `useBowPullMode = false` // 原时间蓄力模式

## 需要注意的问题

### 问题1：最小距离过小导致无法发射
**现象**：鼠标距离球太近（< minPullDistance），力度为0无法发射

**解决方案**：
1. 在 `PlayerCore.LaunchCharged()` 中添加最小力度检查：
   ```csharp
   if (currentForce < minForce * 0.1f) 
   {
       Debug.LogWarning("力度过小，无法发射");
       return;
   }
   ```
2. UI提示：距离不足时，瞄准线显示为红色（暂不实现）

### 问题2：最大距离限制
**现象**：鼠标拉出屏幕外，距离无限增大

**解决方案**：
使用 `Clamp01` 自动限制：
```csharp
chargingPower = Mathf.Clamp01((distance - minPullDistance) / (maxPullDistance - minPullDistance));
```
超过 `maxPullDistance` 后，力度锁定为最大值

### 问题3：坐标系一致性
**注意**：确保 `ChargeSystem` 和 `AimController` 使用相同的坐标转换方法

**验证方式**：
- 在两个组件中都输出鼠标世界坐标
- 确认数值完全一致

### 问题4：相机引用
**问题**：多个组件都需要Camera引用

**解决方案**：
- 优先使用 `Camera.main`
- 如需特定相机，在Inspector中手动设置 `targetCamera`

## 测试要点

### 功能测试
1. **基础功能**
   - [ ] 按住鼠标，拖动时力度UI实时更新
   - [ ] 松开鼠标，球按正确方向和力度发射
   - [ ] 瞄准线方向正确（从球指向鼠标反方向）

2. **边界测试**
   - [ ] 鼠标距离 < minPullDistance：力度为最小值
   - [ ] 鼠标距离 > maxPullDistance：力度为最大值
   - [ ] 鼠标在球内部：正常计算方向和距离

3. **极限测试**
   - [ ] 鼠标拖出屏幕外：力度正常限制在最大值
   - [ ] 快速移动鼠标：方向和力度平滑跟随
   - [ ] 按住不动（距离为0）：无法发射或使用最小力度

### 性能测试
- [ ] Update中计算鼠标位置不影响帧率
- [ ] 坐标转换无GC Alloc

### 兼容性测试
- [ ] 切换回时间蓄力模式（`useBowPullMode = false`）功能正常
- [ ] 原有循环蓄力模式（`useCyclingCharge`）不受影响

## 实现优先级

### 第一阶段：核心功能（必须实现）
1. **ChargeSystem改造**
   - 添加拉弓模式开关
   - 实现距离计算逻辑
   - 添加必要的组件引用

2. **AimController调整**
   - 方向反转逻辑
   - 模式适配

3. **基础测试**
   - 验证方向、力度计算正确性

**预计工作量**：1-2小时

### 第二阶段：优化完善（推荐实现）
1. **工具类提取**
   - 创建 `InputUtility`
   - 统一坐标转换逻辑

2. **边界处理**
   - 最小距离检查
   - 异常情况提示

3. **参数调优**
   - 测试不同场景的最佳距离参数

**预计工作量**：0.5-1小时

### 第三阶段：扩展功能（暂不实现，作为参考）
见下方"后续可选增强"部分

## 代码改动清单

### 必改文件
| 文件 | 改动行数 | 主要内容 |
|------|---------|---------|
| `ChargeSystem.cs` | ~30行 | 添加拉弓模式逻辑 |
| `AimController.cs` | ~10行 | 方向反转 + 模式判断 |

### 可选新建文件
| 文件 | 行数 | 说明 |
|------|------|------|
| `InputUtility.cs` | ~20行 | 坐标转换工具类 |

**总改动量**：40-60行

## 后续可选增强（暂不实现）

以下功能作为参考，暂时不实现，可根据后续需求选择性添加：

### 增强视觉反馈

#### 1. 拉力指示线
**效果**：在球和鼠标之间绘制弹簧/橡皮筋效果
**实现**：
- 使用LineRenderer绘制弧线
- 根据力度调整线条粗细/颜色
- 位置：`AimController` 或新建 `BowPullVisualizer`

#### 2. 力度颜色分段
**效果**：
- 绿色：低力度（0-30%）
- 黄色：中力度（30-70%）
- 红色：高力度（70-100%）

**实现**：
- 修改 `AimLineRenderer` 的颜色逻辑
- 订阅 `GameEventBus.OnForceChanged` 事件

#### 3. 球体形变
**效果**：力度越大，球在发射方向上稍微压扁
**实现**：
- 在 `PlayerCore` 中添加 `Transform.localScale` 动画
- 使用DOTween制作弹性效果

#### 4. 粒子特效
**效果**：拉弓时球周围显示能量聚集
**实现**：
- 创建粒子系统Prefab
- 在 `ChargeSystem.StartCharging()` 时生成
- 粒子数量/速度随力度变化

### 增强音效反馈

#### 1. 音效分层
**效果**：根据拉弓距离，音效音调逐渐升高
**实现**：
- 在 `ChargeSystem` 中添加 `AudioSource`
- `pitch = Mathf.Lerp(0.8f, 1.2f, chargingPower)`

#### 2. 松手音效
**效果**：松开鼠标时播放"嗖"的发射音效
**实现**：
- 在 `PlayerCore.LaunchCharged()` 中触发
- 音量根据力度调整

### 游戏性扩展

#### 1. 完美发射判定
**设计**：某个距离区间（如80%-90%）触发"完美发射"
**效果**：
- 力度额外增加10%
- 播放特殊特效和音效
- UI显示"Perfect!"

**实现位置**：`ChargeSystem.CalculateCurrentForce()`

#### 2. 过充惩罚机制
**设计**：超过最大距离后，力度开始衰减
**公式**：
```csharp
if (distance > maxPullDistance) 
{
    float overcharge = (distance - maxPullDistance) / maxPullDistance;
    currentForce *= Mathf.Lerp(1f, 0.7f, overcharge); // 最多降低到70%
}
```

#### 3. 蓄力超时机制
**设计**：按住超过一定时间后，力度开始衰减
**目的**：鼓励快速决策

**实现**：
```csharp
[SerializeField] private float chargeTimeoutDuration = 5f;
[SerializeField] private AnimationCurve chargeTimeoutCurve;

float holdTime = Time.time - chargingStartTime;
if (holdTime > chargeTimeoutDuration)
{
    float decay = chargeTimeoutCurve.Evaluate((holdTime - chargeTimeoutDuration) / 3f);
    currentForce *= decay;
}
```

#### 4. 距离预览刻度
**效果**：在瞄准线上显示距离刻度标记
**实现**：
- 每隔一定距离放置一个刻度标记（小圆点/数字）
- 在Editor中配置刻度间距

#### 5. 手感微调选项
**配置**：
```csharp
[Header("手感调整")]
[SerializeField] private AnimationCurve distanceToPowerCurve; // 距离到力度的非线性映射
[SerializeField] private float mouseDistanceMultiplier = 1f;  // 鼠标距离倍率
```

**用途**：
- 曲线可以设置为EaseOut：拉近了需要更大的距离才能达到满力
- 曲线设置为EaseIn：容易达到满力
- 倍率可以放大/缩小整体灵敏度

### UI增强

#### 1. 力度数值显示
**效果**：在鼠标旁边显示当前力度百分比
**实现**：
- 创建UI Text跟随鼠标
- 显示 `chargingPower * 100%`

#### 2. 最小/最大距离提示圈
**效果**：在球周围绘制两个圆圈
- 内圈：`minPullDistance`（红色虚线）
- 外圈：`maxPullDistance`（绿色虚线）

**实现**：
- 使用 `Gizmos.DrawWireSphere()` 在Scene中显示
- 运行时使用LineRenderer绘制

#### 3. 轨迹预测增强
**效果**：瞄准线长度根据力度动态调整
**实现**：
- 在 `AimLineReflectionCalculator` 中传入力度参数
- `maxDistance = baseDistance * chargingPower`

## 版本规划

### v1.0 - 基础版（本次实现）
- ✅ 距离计算蓄力
- ✅ 方向反转
- ✅ 力度UI更新
- ✅ 模式切换开关

### v1.1 - 优化版（可选）
- 工具类提取
- 边界情况处理
- 参数精细调优

### v2.0 - 增强版（未来参考）
- 拉力指示线
- 音效反馈
- 粒子特效
- 力度颜色分段

### v3.0 - 深度版（未来参考）
- 完美发射判定
- 过充惩罚
- 距离预览刻度
- 手感曲线配置

## 总结

### 实现难度评估
- **技术难度**：⭐⭐ (简单)
- **改动风险**：⭐ (低风险)
- **测试复杂度**：⭐⭐ (中等)
- **用户体验提升**：⭐⭐⭐⭐⭐ (显著)

### 关键成功因素
1. **参数调优**：`minPullDistance` 和 `maxPullDistance` 需要根据场景精心调整
2. **手感测试**：需要实际游玩测试，找到最舒适的拉弓距离
3. **UI反馈**：虽然暂不实现复杂视觉效果，但现有力度UI必须准确反映距离

### 后续考虑
本计划实现基础功能，确保玩法改动成功。后续可根据玩家反馈，从"可选增强"章节中选择合适的功能逐步添加，持续优化游戏体验。

