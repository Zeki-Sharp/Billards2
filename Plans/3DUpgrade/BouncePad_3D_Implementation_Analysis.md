# 弹簧垫 3D 实现分析

## 🎯 核心问题

**当前实现的问题：**
- ❌ 在碰撞**后**修改速度（`SetVelocity`）
- ❌ 时机不对：碰撞已处理完，再改速度可能冲突
- ❌ 不符合物理直觉：应该影响碰撞时的能量损失

**正确的实现方式：**
- ✅ 在碰撞**时**修改反弹系数（`bounceFactor`）
- ✅ 使用接口系统，避免硬编码

## 📋 现有脚本分析

### BouncePad.cs（2D 版本）
- 继承自 `BaseLevelHazard`
- 在碰撞后使用 `SetVelocity()` 增强速度
- 参数：`bounceMultiplier`（1.5）、`minBounceSpeed`、`maxBounceSpeed`

### BallPhysics.cs（3D 物理系统）
- 使用几何模拟（Geometry Simulation）
- 在 `HandleGeometryWallCollision()` 中处理墙体碰撞
- 当前逻辑：`geometrySpeed *= geometryWallBounceFactor`（默认 0.95）

## ✅ 推荐方案：接口系统

### 核心思路

**不修改速度，而是修改反弹系数：**
- 正常墙体：`speed *= 0.95`（损失 5%）
- 弹簧垫：`speed *= 1.5`（增加 50%）

**使用接口系统解耦：**
- 创建 `IWallCollisionModifier` 接口
- 障碍物实现接口，提供修改碰撞参数的方法
- `BallPhysics` 只查找接口，不关心具体障碍物类型

### 接口设计

```csharp
public interface IWallCollisionModifier
{
    float? ModifyBounceFactor(GameObject ball, float currentSpeed, float defaultBounceFactor);
    bool CanModify(GameObject ball);
    void OnCollisionModified(GameObject ball);
}
```

### 实现步骤

1. **创建接口文件**：`IWallCollisionModifier.cs`
   - 定义接口方法

2. **修改 BouncePad.cs**
   - 实现 `IWallCollisionModifier` 接口
   - 移除 `SetVelocity` 逻辑
   - 在 `ModifyBounceFactor()` 中返回反弹系数

3. **修改 BallPhysics.cs**
   - 在 `HandleGeometryWallCollision()` 中查找 `IWallCollisionModifier` 接口
   - 调用接口方法修改反弹系数

4. **修改 BaseLevelHazard.cs**（可选）
   - 支持 3D Collider

## 🔄 工作流程

```
球体碰撞 (BallPhysics)
  ↓
HandleGeometryWallCollision()
  ↓
查找 IWallCollisionModifier 接口
  ↓
调用 ModifyBounceFactor()
  ↓
应用反弹系数 (speed *= bounceFactor)
  ↓
调用 OnCollisionModified() (播放特效)
```

## 🎯 优势

- ✅ **解耦**：`BallPhysics` 不依赖具体障碍物类型
- ✅ **扩展性**：任何障碍物都可以实现接口
- ✅ **时机正确**：在碰撞处理时应用
- ✅ **符合物理直觉**：反弹系数是标准参数

## 📝 需要修改的文件

1. **IWallCollisionModifier.cs** - 新建接口文件
2. **BouncePad.cs** - 实现接口
3. **BallPhysics.cs** - 使用接口
4. **BaseLevelHazard.cs** - 支持 3D Collider（可选）
