# Pickup Gameplay Glossary

这份文档只负责：

- 固定句式翻译
- 游戏系统术语翻译
- 枪械类别 / 属性术语翻译
- 当前阶段的翻译偏好

如果只需要术语，不需要读取 `README.md`。

## 基础规则

### 外部作品 / 人名 / 专有名词

第一次出现外部作品、电影、角色、人名或其他非游戏内专有名词时，使用：

- `中文译名（English Original）`

示例：

- `《终结者2：审判日》（Terminator 2: Judgement Day）`

如果同一条里后面再次出现，同一专有名词可以只写中文，不必每次重复英文原文。

### 英文引号内容

如果英文源文里出现了带英文双引号的原文：

- `"..."` 或句中被双引号包住的英文短语

中文翻译时必须保留这段英文原文，不要只保留中文意译。

推荐写法：

- `中文说明（"English Quote"）`
- 或者直接在中文句子里保留 `"English Quote"`

示例：

- `“Better Than A Box Of Roses” 这句引用出自电影《终结者2：审判日》（Terminator 2: Judgement Day）中的一幕。`
- `人们也会把它称为“Tommy Gun”“Trench Broom”以及“Chicago Typewriter”。`

注意：

- 这里要求保留的是英文原文内容本身
- 不要求必须保留英文句子上下文
- 如果英文没有引号，就不适用这条规则

### 游戏内引用短句

像 `“Better Than A Box Of Roses”` 这种内容，可以理解为：

- 游戏内出现的引用语句
- 或者游戏借来致敬影视、作品、角色、流行文化的短句

当前阶段不要求和游戏反编译文本逐字对齐，但要求：

- 中文里要说明它是在引用什么
- 英文引号原文要保留下来

### 游戏内物品名称

如果文本里提到的是游戏内可拾取物品、枪械、被动、主动、协同相关道具名：

- 优先使用 `defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json` 里已有的 `chineseDisplayName`
- 不要自己另起译名
- 一般不需要额外保留英文原名
- 如果只是协同标题、梗名、缩写说明中的英文，不要机械替换

示例：

- `Buzzkill` -> `锯枪`
- `Scope` 作为物品名时 -> `瞄准镜`
- `Triple Crossbow` -> `三重十字弩`

### Synergies

这里的 `Synergies` 指的是：

- 游戏内物品、枪械、被动、主动之间的组合效果
- 玩家同时拥有特定组合时，会触发的额外效果

当前阶段的翻译目标是：

- 先把 `Synergies` 的效果描述翻译清楚
- 协同标题名如果没有游戏内正式中文文本，可以先保留英文
- 不要求现在就把所有 `Synergies` 名称翻成和游戏完全一致

示例：

- `Not So Sawed-Off`
- `Future Gangster`
- `360 Yes Scope`

这些都属于协同效果名，当前阶段可以先保留英文标题，再把后面的效果描述翻清楚。

## Gameplay Prose Glossary

后续 agent 翻译 `pickupId 0-823` 的正文时，优先按这份术语表执行。没有特别理由时，不要每条临时换一种说法。

### 固定句式

- `If the player has ...` -> `如果玩家拥有……`
- `If the player also has ...` -> `如果玩家同时拥有……`
- `benefits from the ... synergy` -> `会受到……协同效果加成`
- `does not benefit from the ... synergy` -> `不会受到……协同效果加成`
- `is in the ... gun class` -> `属于……枪械类别`
- `is not in the ... gun class` -> `不属于……枪械类别`
- `is a reference to ...` -> `致敬……` 或 `是在影射……`
- `is an acronym for ...` -> `是……的缩写`
- `wielded by ...` -> `由……使用` 或 `……会使用这把枪`
- `has a chance to ...` -> `有几率……`
- `Upon reloading ...` -> `装填时……`
- `Reloading will ...` -> `装填时会……`
- `while held` -> `持有时`
- `Once acquired ...` -> `获得后……`
- `can be used to ...` -> `可以用来……`

### 游戏系统术语

- `gun class` -> `枪械类别`
- `synergy` -> `协同效果`
- `synergies` -> `协同效果`
- `gungeoneer` -> `枪牢者`
- `Ammonomicon` -> `Ammonomicon`
- `pickup line` -> `拾取台词`
- `equip animation` -> `装备动画`
- `max ammo` -> `最大弹药量`
- `magazine size` -> `弹匣容量`
- `rate of fire` / `fire rate` -> `射速`
- `reload time` -> `装填时间`
- `shot speed` -> `弹速`
- `range` -> `射程`
- `spread` -> `散布`
- `force` -> `击退`

### 枪械类别 / 属性术语

这些术语优先和 `defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json` 顶层 `valueMappings` 保持一致。

- `SHOTGUN` -> `霰弹枪`
- `RIFLE` -> `步枪`
- `FULLAUTO` -> `全自动`
- `SILLY` -> `特殊`
- `FIRE` -> `火焰`
- `ICE` -> `冰冻`
- `Automatic` -> `全自动`
- `Semiautomatic` -> `半自动`
- `Passive` -> `被动`
- `Active` -> `主动`

### 常见协同正文表达

- `Shotgun Affinity synergy` -> `Shotgun Affinity 协同效果`
- `If the player also has Scope` -> `如果玩家同时拥有瞄准镜`
- `If the player also has Buzzkill` -> `如果玩家同时拥有锯枪`
- `If the player also has Triple Crossbow` -> `如果玩家同时拥有三重十字弩`

### 当前条目自身名称

当前条目自己的英文名是否显式保留，由 agent 按句子语义决定。

推荐：

- 普通说明句里，可以直接用中文名
- 如果句子本身在解释英文名、缩写、命名来源，优先写成 `English Name（中文名）`

示例：

- `你可以在 A.W.P.（狙击枪）两次开火之间切换别的枪补输出。`
- `A.W.P.（狙击枪）是 Arctic Warfare Police 的缩写。`

### 翻译偏好

- 协同标题名当前阶段可以先保留英文，不强求和游戏内正式文本一致。
- 正文里的游戏内物品名，优先替换为仓库中已有的 `chineseDisplayName`。
- 外部作品、角色、人名第一次出现时，优先写成 `中文译名（English Original）`。
- 英文里带双引号的引用短句，中文里必须保留英文原文。
