# 辅助瞄准与手柄视角固定实现说明

本文档说明 `Player -> Combat` 页面中 **手柄视角固定 (Controller Aim Lock)** 与 **键鼠自动瞄准 (Keyboard Aim Assist)** 的实现机制与架构设计。

---

## 1. 手柄固定视角 (Controller Aim Lock)

### 需求与 ETG 输入路径

`Controller Aim Lock` 的目标是：固定屏幕相机的手柄视角偏移，同时保留角色和枪械使用手柄右摇杆正常瞄准的能力。

ETG 对手柄输入实际上有两条独立路径：
1. `PlayerController.DetermineAimPointInWorld()` 读取 `ActiveActions.Aim.Vector`，生成角色/枪械的瞄准点，传给 `Gun.HandleAimRotation()` 决定角色面向和枪口方向。
2. `CameraController.GetCoreOffset()` 再次直接读取同一个 `Aim.Vector`，按照 `controllerCamera.AimContribution` 计算屏幕相机偏移。右摇杆停止后，该偏移通过平滑插值回到零。

因此，单纯固定 `DetermineAimPointInWorld()` 会同时锁死角色瞄准。

### 插件实现

* `ControllerAimLockService` 保存全局使能开关，持久化到配置项 `[Combat] ControllerAimLockEnabled`。
* `ControllerAimLockHooks` 对 `CameraController.GetCoreOffset()` 添加 Harmony Prefix/Postfix。
* 当处于手柄模式且 P1 开启开关时，Prefix 临时将相机公开属性 `PreventAimLook` 设为 `true`，使 ETG 跳过相机的 aim-look 偏移；Postfix 恢复原有属性值，避免影响其他场景流程。
* 键鼠模式下不触发相机抑制。

---

## 2. 键鼠自动瞄准 (Keyboard Aim Assist)

### 实现机制与目标选择

键鼠自动瞄准在仅使用键盘和鼠标操作时生效，通过 Harmony Postfix 拦截 `PlayerController.DetermineAimPointInWorld()` 并重新计算瞄准点。

具体流程由 `KeyboardAimAssistService` 与 `KeyboardAimAssistTargetSelector` 协同处理：

1. **输入与状态判定**：检查当前输入设备是否为键鼠 (`IsKeyboardAndMouse()`)、游戏未暂停且玩家未被强制指定瞄准点。
2. **目标检索与视野过滤**：从 `PlayerController.CurrentRoom.GetAutoAimTargets()` 获取当前房间内所有可自动瞄准的目标，并通过 `MainCameraController.PointIsVisible()` 过滤超出视野屏幕的目标。
3. **弹道预判 (Lead Prediction)**：获取当前枪械主弹道子弹飞行速度 (`projectileSpeed`)，根据玩家中心点到目标的距离计算飞行时间 `leadTime = distance / projectileSpeed`，预测目标未来的移动位置 `predictedPoint = targetCenter + target.Velocity * leadTime`。
4. **射线遮挡检测 (Raycast Check)**：通过 `PhysicsEngine.Instance.Raycast` 沿目标方向发射测试射线，防止隔墙或隔着障碍物锁定不可达的目标。
5. **角度判定与吸附**：计算原始鼠标瞄准方向与目标预测位置之间的夹角，若在当前有效辅助角度 (`AimAssistDegrees`) 范围内，且为所有有效目标中夹角最小者，则将最终瞄准点调整吸附至该目标。

### 自瞄模式、倍率与角度计算

面板提供两个循环切换按钮：

* **键鼠自瞄模式 (`Mode`)**：
  * `关闭` (`Off`)：不启用辅助自瞄。
  * `普通自动瞄准` (`Auto Aim`)：基础锥形角度为 **15°**。
  * `超级自动瞄准` (`Super Auto Aim`)：基础锥形角度为 **25°**。
* **自瞄倍率 (`Multiplier`)**：
  * 可在 `0.5x`、`1.0x`（默认）、`1.5x`、`2.0x` 之间循环调节。
* **实际有效吸附角度计算**：
  $$\text{EffectiveAngle} = \text{BaseAngle} \times \text{Multiplier}$$
  * 例如：在 `超级自动瞄准` (25°) 下配合 `2.0x` 倍率，最大可吸附与鼠标方向偏差在 **50°** 以内的敌人。
* **界面描述与角度提示 (Angle Prompt)**：
  * 当在控制面板中切换自瞄模式时，执行结果描述 (`result.keyboard_aim_assist.mode.*`) 会显式带有基础角度提示（如 `普通自动瞄准（基础角度 15°）` / `超级自动瞄准（基础角度 25°）`），使玩家能够清晰感知当前的锁定倾角范围。

---

## 3. 命名缘由 (Naming Rationale)

`普通自动瞄准` (`Auto Aim`) 与 `超级自动瞄准` (`Super Auto Aim`) 的命名直接沿用了《挺进地牢》(Enter the Gungeon) 游戏内原版手柄辅助瞄准 (Controller Aim Assist) 设置选项的官方中文与英文术语，旨在保持与原生游戏设置面板的词汇集与玩家认知完全一致。

---

## 相关代码路径

* `src/EtgGameplayDashboard/Commands/ControllerAimLockService.cs`
* `src/EtgGameplayDashboard/Runtime/ControllerAimLockHooks.cs`
* `src/EtgGameplayDashboard/Commands/KeyboardAimAssistService.cs`
* `src/EtgGameplayDashboard/Commands/KeyboardAimAssistTargetSelector.cs`
* `src/EtgGameplayDashboard/Commands/KeyboardAimAssistUiDefinition.cs`
* `src/EtgGameplayDashboard/Runtime/KeyboardAimAssistHooks.cs`
* `src/EtgGameplayDashboard.Core/Input/KeyboardAimAssistSettings.cs`
* `src/EtgGameplayDashboard/Commands/InGameCommandController.CommandPage.Combat.cs`
