# Loadout 长列表性能优化

本文只记录 Start Items / Loadout 编辑器长列表的虚拟化优化。Items 页面打开、筛选和图标绘制优化单独记录在 [Items 页面性能优化](./performance-items.md)。

## 优化范围

本轮处理以下列表：

- Loadout Presets 双列卡片
- Loadout Rules
- Random Pool
- Preset Pickups

## 优化内容

这些列表仍保留完整内容高度用于滚动条和焦点导航，但只绘制当前滚动视口附近的行或卡片，并保留少量上下缓冲项。

不可见项不再创建对应的 GUI 按钮、文本、背景和图标绘制调用，从而避免列表数量增长时每帧绘制全部内容。

Preset 卡片使用可变高度，因此其布局仍会计算每行卡片高度；该计算不再伴随不可见卡片的实际 GUI 绘制。

## 行为约束

- Preset 双列布局保持不变。
- Loadout Rule 的启用、编辑和删除按钮保持不变。
- Random Pool 和 Preset Pickups 的编辑、计数和删除操作保持不变。
- 手柄焦点导航仍可滚动到当前焦点项。

## 后续边界

本文不包含 Items 页面目录加载、筛选缓存或图标绘制优化，也不包含 Loadout 数据刷新本身的耗时优化。
