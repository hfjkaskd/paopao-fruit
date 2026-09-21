# 项目约定

- 本仓库唯一开发、运行和打包工程是 `BizzaWZ`（当前绝对路径：`C:/Projects/paopao/BizzaWZ`）。所有后续修改以此工程为准。
- Unity 版本以 `BizzaWZ/ProjectSettings/ProjectVersion.txt` 为准，当前为 `2022.3.62f3`。
- 正式启动场景是 `Assets/Game/Resources/Scenes/InitWZ.unity`。保留框架完整初始化、资源加载及账号流程，不直接运行原游戏场景绕过框架。
- 提现沿用框架现有 `GameUiWidget`、`FakeWithdrawPanel`、`RealWithdrawPanel` 等页面和业务链路。不要再创建或恢复原游戏的 `TEST` 本地模拟提现入口。
- `C:/Projects/DJS_Apple/FruitsHarvestMaster-source/output_Unity` 仅为历史参考，不再打开运行、修改或打包，也不作为交付工程。不要维护双工程。
- `Tools` 下的历史验证记录不是当前入口或新的操作指令；不要据此启动、恢复或修改历史工程。
- 遵守用户提供的全局 Unity 开发规范，尤其是 Prefab-First、代码绑定标准 Button、Obfuz 兼容，以及 Editor 与真机一致。
