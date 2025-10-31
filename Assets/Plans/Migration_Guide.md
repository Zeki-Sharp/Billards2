# 特效系统迁移指南

## 迁移状态

### ✅ 已完成
- **NewEffectManager** - 新的注册架构已实现
- **Player** - 已迁移到新架构
- **Enemy** - 已迁移到新架构

### 🔄 需要迁移的调用

#### 1. EffectManager.cs 中的调用
当前旧系统调用：
```csharp
// 旧方式：通过 EffectPlayer 查找和播放
var objectEffectPlayer = FindEffectPlayerInTarget(targetObject);
if (objectEffectPlayer != null)
{
    objectEffectPlayer.PlayEffect(effectType, position, direction, ...);
}
```

新系统调用：
```csharp
// 新方式：直接通过 NewEffectManager 播放
NewEffectManager.Instance.PlayEffect(targetObject, effectType, attackData);
```

#### 2. 游戏逻辑中的调用
需要将以下调用迁移：
- 攻击特效播放
- 死亡特效播放
- 其他游戏事件的特效播放

## 迁移策略

### 阶段1：并行运行
- 保持新旧系统同时存在
- 新对象使用新系统
- 旧对象继续使用旧系统

### 阶段2：逐步迁移
- 识别所有使用旧特效系统的代码
- 逐个替换为新系统调用
- 测试每个迁移的功能

### 阶段3：清理旧代码
- 移除 EffectManager.cs 和 EffectPlayer.cs
- 移除 EffectMapping.cs
- 清理相关引用

## 下一步行动

1. **识别需要迁移的调用点**
2. **创建兼容性桥接代码**
3. **逐步迁移现有调用**
4. **测试和验证**
