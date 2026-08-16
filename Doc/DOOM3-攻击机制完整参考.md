# DOOM3 BFG 攻击机制完整参考：武器、投射物、AI 攻击、特殊玩法

这份文档补上路线图和 Phase 0.1 精读指南里没覆盖的部分——DOOM3 具体怎么实现"子弹、近战、范围爆炸、特殊武器、怪物特殊攻击"这些东西。是完整参考，不是简化教程，代码引用尽量精确到文件和行号，方便你自己回去对照源码验证。

**范围说明**：这次开源的是**引擎+游戏逻辑代码**（`neo/d3xp/`），不包含具体的 `.def`/`.script` 游戏内容文件（跟 `baseq3/pak0.pk3` 一样，是版权内容，没有一起开源）。所以这份文档讲的是"这套机制怎么设计的"（C++ 侧提供了哪些积木、怎么暴露给数据/脚本用），看不到"暴风兵这只怪物具体攻击参数是多少"这种真实调好的数值——这些数值本来就该是你自己设计的部分，不是抄来的。

---

## 1. 武器状态机——`Weapon.h`/`Weapon.cpp`

### 1.1 状态本身很薄，真正的状态机在脚本里

`weaponStatus_t`（`Weapon.h:44-51`）只有几个状态：`WP_READY`、`WP_OUTOFAMMO`、`WP_RELOAD`、`WP_HOLSTERED`、`WP_RISING`、`WP_LOWERING`。**C++ 这边只是个薄壳**——真正的状态转换逻辑跑在每把武器自己的 `.script` 里，`idWeapon::UpdateScript()` 驱动一个 `idThread` 去执行这份脚本，脚本再通过 `Event_WeaponReady`/`Event_WeaponOutOfAmmo`/`Event_WeaponReloading`/`Event_WeaponHolstered`/`Event_WeaponRising`/`Event_WeaponLowering`（`Weapon.cpp:3035-3103`）回调回 C++ 设置 `status`。

玩家输入这一侧（`BeginAttack`/`EndAttack`、`Reload`、`Raise`/`PutAway`）只是翻转几个 `idScriptBool` 标志位（`WEAPON_ATTACK`、`WEAPON_RELOAD`、`WEAPON_RAISEWEAPON`、`WEAPON_LOWERWEAPON`，`Weapon.cpp:1643-1671,1792-1831`），脚本自己去轮询这些标志决定该干嘛。

**给你的启发**：C++ 只管"武器有哪些状态、状态之间谁能转到谁"这套骨架和跟外部（输入、动画、库存）的接口，具体"这把武器按下扳机之后过多久能再开一枪、换弹动画放多久"这类调参逻辑，交给数据/脚本层，不要硬编码进 C++ 状态机里——不然每加一把手感不同的武器就要改一遍 C++。

### 1.2 子弹 vs 投射物：不是两套代码，是一个数据开关

开火不是一个统一的分发函数决定"这是子弹还是导弹"，而是武器 `.script` 自己选调用哪个：
- `Event_LaunchProjectiles`（`Weapon.cpp:3520`）——正常开火
- `Event_CreateProjectile`（`3478`）——生成一个投射物但先不发射（用于手雷这种"先掏出来、再扔出去"的武器）
- `Event_Melee`（`3967`）——近战，见 1.3

**关键点**：`projectileDict`（`Weapon.h:265`）是武器 `.def` 里 `def_projectile` 字段指向的那个投射物定义，缓存在武器实体本地（这样发射时不用跨 DLL 边界查一遍）。**"这发是打即命中的子弹，还是有飞行时间的实体"是投射物 `.def` 里的一个数据字段** `net_instanthit`（`Weapon.cpp:3606,3647,3679` 处被检查），**不是两条独立的代码路径**——同一套发射逻辑，靠这一个布尔值决定行为。

**给你的启发**：这是数据驱动设计的核心思想之一——"霰弹枪"和"火箭筒"对你的开火系统来说应该是同一段代码 + 不同的投射物配置，不是两个 `Fire_Shotgun()`/`Fire_Rocket()` 函数。

### 1.3 近战是真正独立的一条路径

近战武器（拳头、电锯）走 `def_melee` 定义的 `meleeDef`/`meleeDefName`/`meleeDistance`（`Weapon.h:264-267`，从 `Weapon.cpp:1131-1135` 读取）。`Event_Melee`（`3967-4067`）**自己做一次 `gameLocal.clip.TracePoint`**，沿玩家视角方向、距离 `meleeDistance`，不生成任何投射物实体——这跟子弹/导弹那条路径完全不共用代码，是刻意设计成两套机制的（近战不需要飞行时间、不需要物理模拟，直接一次 trace 判定就够）。

### 1.4 手电筒：不是"武器模式切换"，是"第二把武器同时装备"

BFG 版新增的手电筒实现方式可能跟你想的不一样——**它是一把完全独立、单独实例化的 `idWeapon`**，不是当前武器的一个附加模式。`idPlayer::flashlight`（`Player.h:322`）是独立的 `idEntityPtr<idWeapon>`，`idPlayer::Spawn` 里单独生成（`Player.cpp:1522-1523`）。`idPlayer::UpdateFlashlight()`（`5096-5190`）每帧独立调用它自己的 `PresentWeapon(true)`，跟主武器的 `PresentWeapon` 调用（`5088`）**并行发生**——两把"武器"同时在渲染、同时在更新，只是手电筒绑在玩家的 `"Chest"` 骨骼上、主武器绑在手上。

**给你的启发**：如果你的游戏也想做"能同时用手电筒 + 武器"这种玩法，不要往你的武器状态机里加一个"手电筒模式"分支，直接让玩家能同时持有两个独立的武器实例，这个设计现成的例子已经帮你验证过可行了。

---

## 2. 投射物系统——`Projectile.h`/`Projectile.cpp`

### 2.1 基础飞行：物理模拟，碰撞后可以反弹也可以直接炸

基类 `idProjectile` 用 `idPhysics_RigidBody` 做弹道飞行。`Collide()`（`554-724`）决定"擦弹反弹"还是"命中引爆"：靠两个标志位 `projectileFlags.detonate_on_actor`/`detonate_on_world`（`Projectile.h:109-110`）——命中的表面类型如果不匹配这两个标志要求的类型，播放一个跳弹音效，`return false`（物理继续模拟，子弹弹开），命中匹配就往下走伤害+爆炸逻辑。

### 2.2 直接命中和范围伤害是解耦的两步，不是二选一

这是最值得学的一点：`Collide()` 里，如果直接命中的实体 `ent->fl.takedamage`，先对**这一个目标**调用 `ent->Damage(...)`（`687/690`，"火箭直接命中"的那份伤害）。然后**无条件**调用 `Explode(collision, ignore)`（`709`），`Explode` 内部如果 `!projectileFlags.noSplashDamage`（`1085`），再读 `def_splash_damage` 定义、调用 `gameLocal.RadiusDamage(...)` 对爆炸半径内的所有实体（除了已经直接伤害过的那个，用 `ignore` 参数排除，避免重复伤害）造成范围伤害。

**给你的启发**：一发火箭 = 直接命中伤害（可选） + 范围伤害（可选），两个开关独立控制，不是"要么是子弹要么是范围武器"这种非此即彼的设计。一把武器想同时有"直接命中加成伤害 + 溅射伤害"，这套结构直接支持，不用额外设计。

### 2.3 特殊投射物子类

- **`idGuidedProjectile`**——追踪导弹。`Think()`（`1651-1721`）每帧朝 `GetSeekPos()`（追踪目标的眼睛位置，`1632-1644`）转向，转向速率被 `turn_max` 限制，还有 `burstMode`/`burstDist`/`burstVelocity`——快到目标时切换成一段"不再转向、直线冲刺"的末端弹道，防止追踪导弹在近距离疯狂画圈追不上。
- **`idSoulCubeMissile : idGuidedProjectile`**——灵魂方块（DOOM3 的必杀武器），在追踪基础上加了 `ReturnToOwner()`/`KillTarget()`，有"追踪→命中即秒杀→飞回玩家手里"三个阶段。
- **`idBFGProjectile`**——BFG 那把武器的实现，是**光束类攻击**，不是普通抛射体：维护一个 `idList<beamTarget_t> beamTargets` 列表，周期性调用 `ApplyDamage()` 对多个目标同时造成持续伤害（不是一次性爆炸），自己重写了 `Explode()` 并把 `noSplashDamage` 设成 `true`，用自己的伤害逻辑代替默认的范围伤害。
- **`idHomingProjectile`**——另一套追踪实现，跟 `idGuidedProjectile` 平行存在，各自维护 `seekPos`/`SetEnemy`。
- **`idDebris`**——不造成伤害的纯视觉碎片（弹开、爆炸后的残骸），复用同一套飞行/碰撞框架但跳过伤害逻辑。

### 2.4 "粘性"投射物不需要专门的子类

`Explode()` 里会检查 `spawnArgs.GetBool("bindOnImpact")`，为真就调用 `Bind(...)`（`1080-1082`）把投射物粘在命中的表面/实体上——**又是一个纯数据开关，不需要专门写一个 `idStickyProjectile` 类**。这跟前面子弹/导弹的设计思路一致：能用数据字段解决的行为分支，就不要拆成子类。

---

## 3. AI 攻击机制——`ai/AI.cpp`、`ai/AI_events.cpp`

### 3.1 除了 `LaunchProjectile`/`DirectDamage`，还有这些

- `TestMelee()`（`AI.cpp:4399-4442`）——只做范围+视线检测，**不造成伤害**，用来问"我现在打得到目标吗"
- `AttackMelee(meleeDefName)`（`4456` 起）——真正的近战攻击，内部重新验证一遍范围/视线（不信任调用者已经检查过），甚至对玩家目标有个"必杀一击自动打不死"的保底判定（`CalcDamagePoints` 里处理，防止一拳秒杀玩家体验太差）
- `BeginAttack`/`EndAttack`（`4139-4151`）——纯粹设置/清空一个 `attack` 字符串和 `lastAttackTime` 时间戳，标记"现在在攻击状态"，具体这个状态怎么用交给脚本/动画
- `CalculateAttackOffsets()`（`4026-4067`）——预计算攻击关节（比如手爪、枪口）在特定动画帧的世界坐标，用于预判瞄准

### 3.2 攻击时机可以是动画帧驱动的，不一定靠脚本计时

这点很有意思，比"脚本里 `wait 0.5` 再开火"更精细：`anim/Anim.h` 定义了一批"帧命令"类型——`FC_MELEE`、`FC_DIRECTDAMAGE`、`FC_LAUNCHMISSILE`、`FC_BEGINATTACK`、`FC_ENDATTACK`、`FC_MUZZLEFLASH`、`FC_CREATEMISSILE`、`FC_TRIGGER_FX`，这些是直接写在 `.md5anim` 动画文件的帧命令字符串里解析出来的。`idAnim::CallFrameCommands`（`anim/Anim_Blend.cpp:895-934`）在动画播放到对应帧时**自动触发**：`FC_MELEE` 触发 `AI_AttackMelee` 事件（`896`）、`FC_DIRECTDAMAGE` 触发 `AI_DirectDamage`（`900`）、`FC_LAUNCHMISSILE` 触发 `AI_AttackMissile`（`920`）。

也就是说：**"挥拳动画播到第 12 帧的时候才真正造成伤害"这种精确到帧的时机控制，是直接嵌在动画资源里的，不是脚本里猜时间**。`CalculateAttackOffsets` 甚至会提前算好 `FC_LAUNCHMISSILE` 那一帧攻击关节会在哪个世界坐标，用来提前预判瞄准方向。当然脚本也可以不依赖动画帧、自己直接调用这些攻击事件，两条路径都存在、互不冲突。

**给你的启发**：这是"打击感"精细度的关键——攻击判定时机跟动画牢牢绑在一起，而不是"动画播个大概时长、逻辑上定时器算一算差不多命中了"，后者手感容易对不上。

### 3.3 冲锋攻击、动画可行性预判

- `Event_ChargeAttack`/`Event_TestChargeAttack`（`AI_events.cpp:1743-1802`）——冲锋攻击：AI 直接朝敌人当前位置猛冲（`BeginAttack` + `DirectMoveToPosition` + `TurnToward`），冲锋前会先用 `idAI::PredictPath` 对着寻路网格（AAS）预判这条冲锋路线走不走得通，避免冲到墙里卡死
- `Event_TestAnimAttack`（`1908` 起）——预判"如果播放这个攻击动画，动画自带的位移量（`animator.TotalMovementDelta`）会不会撞墙"，用来决定要不要放一个"扑击"这类带位移的攻击动画
- 范围/锥形检测：`Event_EntityInAttackCone`、`Event_EnemyInCombatCone`，还有 `LaunchProjectile` 自己的 `attack_accuracy`/`attack_cone` 数据字段（`AI.cpp:4227-4228`），控制"发射有没有散布、目标偏出多少角度还算能打中"

### 3.4 `DirectDamage` 完全不做范围检测——这是刻意的

`DirectDamage`（`AI.cpp:4356-4392`）拿到 `meleeDefName` 之后，只检查目标 `fl.takedamage`，就直接调用 `ent->Damage(...)`，**没有任何距离/视线判定**。这是故意设计成"无条件信任调用者"的原语——它就是打算配合 3.2 说的动画帧命令（`FC_DIRECTDAMAGE`）用的：既然动画已经播到了"爪子挥到目标身上"那一帧，逻辑上就该信任这一刻命中已经成立，不需要再算一次。这跟 `AttackMelee` 那种"不信任调用者、自己重新验证"的设计形成对比——**同一个引擎里，两种检测严格度的攻击原语都保留了，看场景选用哪个**。

---

## 4. `AI_Vagary`——唯一需要真正写 C++ 子类的怪物

DOOM3 几乎所有怪物类型都直接用通用的 `idAI`，靠 `.def`+`.script` 配置差异化，**只有 Vagary 这一个怪物真正继承出了 C++ 子类**（`idAI_Vagary : public idAI`，`AI_Vagary.cpp:42`），只加了两个事件：`Event_ChooseObjectToThrow`、`Event_ThrowObjectAtEnemy`。

为什么这个必须写 C++：这只怪物的技能是"抓起场景里的物理道具扔向玩家"，需要做两件脚本语言够不着的事：
1. **空间查询**：调用 `gameLocal.clip.EntitiesTouchingBounds` 在物理世界里搜索附近能扔的 `idMoveable` 物体（`64-83`），还要做能见度/距离/是不是在身后这些几何过滤
2. **弹道反解**：调用 `PredictTrajectory(...)` 针对重力和碰撞体做迭代弹道解算，算出一个初速度，让这个物体扔出去真的能命中一个还在移动的目标（`113-114`、`138-139`）

**给你的启发**：这划出了"数据/脚本够用"和"必须写 C++"的清晰边界——凡是需要直接查询物理世界、做数值解算（弹道预测这种），脚本语言没有暴露这类底层 API，就得下沉到引擎层。反过来说，绝大多数"这只怪物攻击方式不一样"的需求，根本不需要走到这一步。

---

## 5. Grabber 抓取枪——特殊武器机制的例子

`idGrabber`（`Grabber.h:40`）**不是** `idWeapon` 的子类，是作为一个普通成员变量 `idGrabber grabber;` 挂在 `idWeapon` 内部（`Weapon.h:416`），武器脚本通过 `Event_Grabber`/`Event_GrabberHasTarget`/`Event_GrabberSetGrabDistance` 这几个事件去驱动它。

**抓取流程**：`Update()`（`Grabber.cpp:422` 起）朝玩家视角方向做一次带体积的 trace（`TraceBounds`，`475`），过滤出符合条件（类型是 `idMoveable`/`idMoveableItem`/`idProjectile`/`idAFEntity_Gibbable`，大小/速度在限制内）的目标，`StartDrag()`（`218-300`）把它的重力清零，挂一个 `idForce_Grab` 物理约束力，把它往玩家前方某个点拉，再配一个 `idBeam` 做视觉连线。

**抓子弹/导弹的特殊处理**：抓到的如果是投射物，会单独调用 `idProjectile::CatchProjectile(player, "_catch")`（`Grabber.cpp:250` → `Projectile.cpp:1237-1256`）——把这发投射物的 `owner` 改成玩家、如果是追踪弹还会把它的追踪目标重新指回原来的发射者（"接住敌人的导弹再打回去"），并且尝试把伤害定义换成一个 `..._catch` 后缀的变体（如果配置了的话）。松手/扔出去是直接给物理对象加速度，不走 `idWeapon::Event_LaunchProjectiles` 那套发射逻辑。

**给你的启发**：一个"特殊武器机制"不一定要塞进你的核心武器状态机里，作为一个独立的、被武器持有和驱动的辅助对象来实现，往往更干净——`idGrabber` 就是个很好的范例：它有自己完整的抓取/拖拽/释放逻辑，`idWeapon` 只是在恰当的时机调用它。

---

## 6. 特效系统不只是好看——`Fx.cpp`

`idEntityFx`（`Fx.h:54`）播放一份 `idDeclFX` 资源里定义的一串动作（灯光、音效、贴花、粒子/模型、屏幕震动、发射物、冲击波），通过战斗代码调 `idEntityFx::StartFx(...)` 触发（枪口火花、命中特效、爆炸特效都走这条路）。

**这套系统不是纯视觉的**：`FX_SHAKE`/`FX_SHOCKWAVE` 这两种动作类型会真的调用 `gameLocal.RadiusPush(...)`（`Fx.cpp:487`）对周围物体施加物理冲量——爆炸的"震退"效果就是靠这个，不是单纯的相机抖动。更极端的是 `FX_LAUNCH`（`519-541`），它会**真的生成并发射一个 `idProjectile`**——意味着一条特效声明本身可以是"二级爆炸/连锁反应"的实现机制，纯视觉表现层和实际游戏逻辑在这里是模糊的，不是完全分离的。

**给你的启发**：如果你的特效系统只做渲染，没有"能触发物理冲量、能生成新的攻击实体"这类钩子，像"手雷爆炸把周围箱子震飞""连锁爆炸"这类效果就得在别处单独写一遍逻辑，不如从设计上就让特效系统本身具备触发游戏逻辑的能力。

---

## 7. 玩家特殊机制——`Player.cpp`

**武器切换**：`NextWeapon()`/`PrevWeapon()`/`NextBestWeapon()`（`Player.cpp:4441-4523` 附近）遍历武器槽位，检查 `inventory.weapons` 位掩码、`def_weapon%d`/`weapon%d_cycle`/`weapon%d_best` 这些数据字段和弹药库存，设置 `idealWeapon` + 一个 `weaponSwitchTime` 延迟（对应"举枪/收枪"过渡动画的时长）。

**增益道具（powerup）**：`Player.h:100-106` 定义了 `BERSERK`（狂暴）、`INVISIBILITY`（隐身）、`MEGAHEALTH`、`ADRENALINE`、`HELLTIME`（子弹时间）、`ENVIROSUIT`（环境防护服）。近战伤害/攻速这些数值通过 `owner->PowerUpModifier(MELEE_DAMAGE/SPEED/PROJECTILE_DAMAGE)`（在 `Weapon.cpp:4004,4031` 和 `Projectile.cpp:672` 里被调用）统一做倍率修正——**增益效果是一个集中的修正器接口，而不是散落在每个攻击函数里各自判断"玩家是不是在狂暴状态"**。

**BFG 版特有内容**：前面提到的独立手电筒（第 1.4 节），以及 `weapon_bloodstone`/`weapon_bloodstone_active1-3` 这几个槽位（`Player.h:337-340`）——"失落使命"资料片里的血石能力武器，`SelectWeapon`（`Player.cpp:3444-3447`）里有专门的开关逻辑处理它的多阶段切换。

---

## 8. 近战伤害判定：两种严格程度，刻意并存

| | 触发方 | 判定方式 |
|---|---|---|
| 玩家近战 | `idWeapon::Event_Melee`（`Weapon.cpp:3967-4067`） | 每次调用自己做一次 `TracePoint`，从玩家视角沿视线方向打到 `meleeDistance`，打中什么就伤害什么 |
| AI `AttackMelee` | `AI.cpp:4456` 起 | 不信任调用者，重新验证一遍范围+视线（跟 `TestMelee` 同一套判定），哪怕脚本"盲调"这个函数，目标已经跑开了也会判定不命中 |
| AI `DirectDamage` | `AI.cpp:4356-4392` | **完全不检测**，无条件伤害传入的目标，设计上要求调用者（通常是动画帧命令）已经自行确认过命中时机 |

这不是设计疏漏，是刻意提供了两档严格程度的攻击原语：**常规攻击用"便宜的自校验"版本，需要精确匹配动画表现的攻击用"不校验、信任调用时机"的版本**，各自适配不同场景。

---

## 9. 总结：给你自己引擎的设计原则清单

1. **武器差异 = 数据配置的差异，不是代码分支的差异**——同一套开火逻辑，靠"这次打的是不是即时命中"这类布尔字段决定手枪还是火箭筒的行为
2. **近战值得单独一条代码路径**，不要硬塞进"投射物飞行时间极短"这种取巧实现里，判定方式和武器不一样
3. **直接命中伤害和范围伤害是两个独立开关**，可以都开、可以只开一个，别设计成互斥的
4. **攻击判定时机最好能挂在动画帧上**，而不是纯靠脚本/逻辑层估算时间，打击感差异很明显
5. **给"信任调用者的无检测攻击"和"自校验攻击"都留一个接口**，动画驱动的精确攻击用前者，脚本粗粒度控制的攻击用后者
6. **绝大多数"这个敌人攻击方式不一样"用数据/脚本解决就够**，只有真正需要空间查询/数值解算（弹道预测这类）的特殊技能才值得写专门的类
7. **特殊武器机制（比如抓取枪）适合做成独立的辅助对象**，被武器持有和驱动，不用塞进核心武器状态机里
8. **特效系统如果只做渲染，会限制你能做的效果**——让它有能力触发物理冲量/生成新的攻击实体，连锁反应这类效果会好实现很多
9. **增益/减益效果最好走一个集中的修正器接口**，不要在每个伤害计算的地方各自判断玩家当前状态
