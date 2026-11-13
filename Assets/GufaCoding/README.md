# 古法编程模拟器 · Unity MVP

第一章「C 语言时代」10 关，纯代码生成 UI（uGUI），不依赖任何预制体或第三方包。

## 跑起来（3 步）

1. 把整个 `GufaCoding` 文件夹拷进 Unity 项目的 `Assets/` 下（Unity 2020.3+，2022 LTS / Unity 6 / 团结引擎都行）。
2. 菜单栏 **古法编程 → 创建游戏场景**，会生成 `Assets/GufaCoding/Scenes/GufaMain.unity` 并设为构建第一个场景。
3. Game 视图分辨率调成 **1080x1920** 竖屏，点 Play。

想重玩：菜单 **古法编程 → 清空存档**。

## 文件说明

| 文件 | 作用 |
|---|---|
| `Scripts/GufaGame.cs` | 主流程：标题、选关、需求、找 Bug/补代码、编译、事故、结算、小卖部、终章 |
| `Scripts/LevelDatabase.cs` | 10 关题库，加关卡直接往数组里加 |
| `Scripts/SaveData.cs` | PlayerPrefs 存档 + 广告每日次数 |
| `Scripts/Ads/Ads.cs` | 广告统一入口、编辑器模拟广告、微信广告实现 |
| `Scripts/UIKit.cs` | 代码搭 UI 的小工具：运行时生成圆角九宫格、投影、按压回弹、布局 |
| `Scripts/UITween.cs` | 弹出 / 淡入上浮 / 抖动 / 脉冲 / 闪烁动效（不依赖 DOTween） |
| `Scripts/CodeHighlighter.cs` | C 语法高亮（VS Code Dark+ 配色，输出富文本） |
| `Resources/GufaLogo.png` | 印章 Logo，标题页和结算日报卡用；删掉会自动退回纯色印章 |
| `Editor/GufaSceneCreator.cs` | 一键建场景 / 清存档菜单 |

## UI 风格

整体做成 VS Code 暗色主题：

- 圆角面板，按钮带投影和按下回弹
- 代码区是带红黄绿三个点、`main.c` 标签页和行号栏的编辑器窗口，代码有语法高亮
- 底部有一条蓝色状态栏，显示当前关卡和工资
- 弹窗弹出、页面淡入；点错时那一行会抖动，Deadline 剩 5 秒会变红并跳动
- 自动适配刘海屏安全区

所有颜色都在 `GufaGame.cs` 顶部，换主题只改那里。

## 玩法

- **找 Bug**：点出有问题的那一行，点错掉 20 根头发。
- **补代码**：从 4 个选项里挑出空行该填的代码。
- 每个任务有 Deadline，超时可以掉 30 根头发硬扛，或者看广告延期。
- 头发掉光就「秃了」，可以看广告植发复活。
- 编译过程有吐槽日志；上线有 20% 概率出线上事故。
- 工资可以在小卖部买咖啡（+10 秒）和橡皮鸭（每关免费提示一次）。
- 打通第 10 关进入终章：AI 三秒写完了你熬十个通宵的代码。

## 广告位一览

| 位置 | 类型 | placement | 每日上限 |
|---|---|---|---|
| 请教大佬（高亮答案） | 激励视频 | `hint` | 20 |
| 秃了 → 植发复活 | 激励视频 | `revive` | 5 |
| Deadline 到 → 找老板延期 | 激励视频 | `delay` | 10 |
| 结算 → 加班费 x2 | 激励视频 | `double` | 10 |
| 线上事故 → 紧急回滚 | 激励视频 | `rollback` | 5 |
| 小卖部 → 摸鱼领工资 | 激励视频 | `slack` | 3 |
| 编译中 | 插屏 | `compile` | 第 4 关起，每 2 关最多 1 次 |

上限和频率都在 `GufaGame.cs` 顶部的 `AdCaps`、`InterstitialFromLevel`、`InterstitialEvery` 里改。所有激励视频都是可选捷径，不看广告也能通关。

## 发布到微信小游戏

1. **中文字体（必做）**：WebGL 里没有系统字体，中文会显示成空白。找一个可商用的中文 TTF（比如思源黑体），改名为 `GufaFont.ttf`，放到 `Assets/GufaCoding/Resources/`。想让代码用等宽字体，再放一个 `GufaMono.ttf`（需要包含中文，否则代码注释会缺字）。字体包体积大的话，可以用字体裁剪工具只保留用到的字。
2. 导入微信官方 Unity 转换插件（minigame-unity-webgl-transform）。
3. 在 `Ads.cs` 的 `AdConfig` 里填流量主后台的激励视频 / 插屏广告位 ID。
4. **Player Settings → Scripting Define Symbols** 加上 `GUFA_WECHAT`，广告就从模拟广告切到微信广告。
5. `WeChatAdProvider` 是按插件常见 API 写的。如果插件版本不同导致签名对不上，对照插件里的 `WX.cs` 调整 `Show` / `Load` / `OnClose` 的参数即可，逻辑不用变。

## 后续可以加的

- 结算页「日报卡」接 `wx.shareAppMessage`，生成分享图
- 第二章「面向对象时代」、第三章「复制粘贴时代」
- 拖拽拼代码、合并冲突等新玩法
