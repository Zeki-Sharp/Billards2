# WallHitPositionController 使用说明

## 概述
`WallHitPositionController` 是一个专门用于控制墙面撞击时位置摇晃效果的控制器。它类似于 `WallHitRotationController`，但专门管理 `Position Spring` 组件的 `Bump` 模式设置。

## 主要功能

### 1. 位置摇晃计算
- **撞击反方向**：墙面朝着撞击方向的反方向移动（被球推着走）
- **速度系数**：使用曲线控制速度对摇晃强度的影响
- **简单直接**：不考虑法线和撞击位置的复杂计算

### 2. 参数说明

#### 位置摇晃计算
- `maxPositionOffset`：最大位置偏移量（默认：5）
- `minPositionOffset`：最小位置偏移量（默认：1）

#### 速度影响
- `speedToPositionCurve`：速度到位置偏移强度的曲线
- `maxSpeedReference`：最大速度参考值（默认：50）
- `minSpeedMultiplier`：最小速度系数（默认：0.1）
- `maxSpeedMultiplier`：最大速度系数（默认：1.0）

## 使用方法

### 1. 在 EffectPlayer 中配置
```csharp
[Header("墙面撞击位置摇晃")]
public WallHitPositionController wallHitPositionController;
```

### 2. 自动计算位置偏移
当白球撞墙时，系统会自动：
1. 计算基础位置偏移（基于撞击位置）
2. 计算撞击方向影响（如果启用）
3. 应用速度系数
4. 设置 `Position Spring` 的 `Bump` 参数

### 3. 手动调用
```csharp
Vector3 positionOffset = wallHitPositionController.CalculatePositionOffset(
    hitPosition,    // 撞击位置
    hitNormal,      // 墙面法线
    hitDirection,   // 撞击方向
    hitSpeed        // 撞击速度
);
```

## 工作原理

### 1. 撞击方向计算
- 使用撞击方向的反方向作为墙面移动方向（墙面被球推着走）
- 归一化撞击反方向向量
- X和Y方向保持一致

### 2. 速度系数应用
- 将速度归一化到 [0, 1] 范围
- 使用 `speedToPositionCurve` 计算曲线值
- 映射到 `minSpeedMultiplier` 到 `maxSpeedMultiplier` 范围

### 3. 最终偏移计算
- 使用速度系数在 `minPositionOffset` 和 `maxPositionOffset` 之间插值
- 将偏移量乘以归一化的撞击方向

## 调试功能

启用 `enableDebugLog` 可以查看详细的计算过程：
- 撞击方向
- 墙面移动方向（撞击反方向）
- 速度系数
- 偏移量
- 最终位置偏移

## 与 Position Spring 的集成

系统会自动设置 `Position Spring` 组件的以下参数：
- `BumpPositionMin`：设置为 `Vector3.zero`
- `BumpPositionMax`：设置为计算出的位置偏移

## 注意事项

1. 确保 `Position Spring` 组件的 `Mode` 设置为 `Bump`
2. 调整 `Spring Settings` 中的阻尼和频率参数以获得理想的摇晃效果
3. 速度参考值 `maxSpeedReference` 应该与游戏中球的最大速度相匹配
4. 可以通过调整曲线来获得不同的速度响应效果

## 示例配置

```csharp
// 在 Inspector 中设置
maxPositionOffset = 8f;           // 最大偏移
minPositionOffset = 2f;           // 最小偏移
directionInfluence = 1.5f;        // 方向影响强度
maxSpeedReference = 50f;          // 最大速度参考
minSpeedMultiplier = 0.2f;        // 最小速度系数
maxSpeedMultiplier = 1.5f;        // 最大速度系数
```

这样配置后，高速撞击会产生更强烈的摇晃效果，而低速撞击则产生轻微的摇晃。
