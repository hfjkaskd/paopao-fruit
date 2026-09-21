# 水果游戏

唯一开发、运行和打包工程：`C:/Projects/paopao/BizzaWZ`。

使用 Unity **2022.3.62f3**（以 `BizzaWZ/ProjectSettings/ProjectVersion.txt` 为准）。在仓库根目录运行：

```powershell
.\Open-Game.ps1
```

脚本根据工程版本寻找 Unity Hub 安装的编辑器，并打开 `BizzaWZ`。它不会自动进入 Play、关闭其他工程或执行打包。如果 Unity 安装在自定义目录，可传入对应版本的编辑器路径：

```powershell
.\Open-Game.ps1 -UnityEditor 'D:\Unity\2022.3.62f3\Editor\Unity.exe'
```

脚本会核对工程实例记录、进程可执行文件及 `-projectPath` 参数。工程已运行时只提示切换到已有窗口，不重复启动。遇到遗留 `UnityLockfile`，只有完整读取 Unity 进程列表、确认本工程未运行，且能独占只读打开锁文件时才允许 Unity 正常启动；无法确认或锁文件占用时拒绝启动。脚本不会结束进程、删除锁文件或自动操作窗口。

进入 Unity 后，从 `Assets/Game/Resources/Scenes/InitWZ.unity` 启动游戏。正式打包也使用该工程及现有 Build Settings，保留框架初始化流程。

提现使用框架现有页面：玩法页的提现入口进入框架余额、金额档位、进度、提现及历史记录流程。不要恢复原游戏左下角的 `TEST` 本地模拟入口。

`C:/Projects/DJS_Apple/FruitsHarvestMaster-source/output_Unity` 仅为历史参考，不再打开运行、修改或打包。`Tools` 中的旧验证脚本和截图只供追溯，不代表当前启动方式。
