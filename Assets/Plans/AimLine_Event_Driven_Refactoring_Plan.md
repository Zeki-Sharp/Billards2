# 瞄准线系统事件驱动重构计划

## 目标
将瞄准线系统从直接依赖改为事件驱动架构，提高系统的解耦性和可维护性。

## 当前问题
- PlayerStateMachine 直接调用 AimController 的 ShowChargingUI/HideChargingUI
- 蓄力状态控制分散在多个系统中，职责不清晰
- 系统间耦合度高，难以扩展和测试

## 重构策略

### 1. 事件发布者
- **PlayerInputHandler**: 作为蓄力事件的唯一发布者
- 检测到蓄力输入时发布 `OnChargingStarted`
- 检测到蓄力释放时发布 `OnChargingStopped`

### 2. 事件订阅者
- **ChargeSystem**: 响应蓄力事件，管理蓄力逻辑
- **AimController**: 响应蓄力事件，控制瞄准线显示
- **ChargeBarUI**: 响应蓄力事件，控制UI显示
- **PlayerStateMachine**: 响应蓄力事件，管理状态切换
- **TimeStopEffect**: 响应蓄力进度事件，控制时停特效
- **TransitionManager**: 响应蓄力停止事件，设置过渡时长
- **PlayerMovementController**: 响应蓄力开始事件，停止移动

### 3. 需要新增的事件
- `OnChargingStopped`: 蓄力停止事件
- `OnChargingReset`: 蓄力重置事件

## 实施步骤

### 阶段1: 事件定义
- 在 GameEventBus 中新增蓄力停止和重置事件
- 更新事件统计信息

### 阶段2: 输入系统改造
- 修改 PlayerInputHandler，直接发布蓄力事件
- 移除对 PlayerStateMachine 的直接调用

### 阶段3: 核心系统改造
- 修改 ChargeSystem，订阅蓄力事件
- 修改 AimController，订阅蓄力事件
- 修改 PlayerStateMachine，订阅蓄力事件

### 阶段4: 辅助系统改造
- 修改 ChargeBarUI，订阅蓄力事件
- 修改 TimeStopEffect，订阅蓄力进度事件
- 修改 TransitionManager，订阅蓄力停止事件
- 修改 PlayerMovementController，订阅蓄力开始事件

### 阶段5: 清理和测试
- 移除所有直接依赖调用
- 测试事件流程
- 验证系统功能正常

## 预期效果
- 系统解耦：各系统独立响应事件，无直接依赖
- 易于扩展：新增系统只需订阅相应事件
- 便于测试：各系统可独立测试
- 维护性提升：事件流向清晰，职责明确

## 风险评估
- 事件订阅/取消订阅需要仔细管理，避免内存泄漏
- 事件顺序可能影响系统行为，需要测试验证
- 调试复杂度增加，需要完善事件日志

## 时间估算
- 阶段1-2: 1天
- 阶段3: 2天  
- 阶段4: 1天
- 阶段5: 1天
- 总计: 5天
