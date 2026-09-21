# 顶部按钮修复（2026-09-21）

修改工程：C:/Projects/paopao/BizzaWZ。

根因：正式 InitWZ 运行时，RealGamePanel/BG/Image (2) 的颜色透明度为 0，但 Raycast Target 仍为 true。它位于 HUD 之上的 Canvas 层级，实际 EventSystem 首个命中是该装饰图，且没有点击处理器，导致顶部按钮收不到点击。修改前的完整运行报告为 hud-runtime-interception-before.json。

- 在 RealGamePanel.prefab 关闭该装饰图的 Raycast Target；换皮脚本 ApplyGamePanel 同时确保 BG 装饰不接收射线。
- 将 CoinBox/ButtonView 和 DollarBox/ButtonView 放入各自现有的 RealBtn/FakeBtn 下，删除独立透明点击图形；Button targetGraphic 指向实际可见的绿色按钮。
- 保留 Button 组件 fileID、CurrencyBar 业务引用和 Teach_01 的 RealBtn 路径；同步换皮脚本路径。
- 保留 RealWithdrawPanel / FakeWithdrawPanel / PausePanel 路由，为异步打开补充 Forget()，使异常可被报告。

验证：Unity 2022.3.62f3 编译通过。正式 InitWZ 初始化完成后，实际 EventSystem 射线均命中对应按钮，通过标准 Button 指针事件依次成功打开并关闭 RealWithdrawPanel、FakeWithdrawPanel、PausePanel。最新完整报告 hud-runtime-validation.json 的 passed=true，三个按钮的 correctHandler/pageOpened/pageClosed 全部为 true。验证只打开提现页面，没有执行提现提交。

隔离 Prefab 检查的三个按钮共 15 个采样点也全部命中正确的标准 Button；修改前后可见区域采样坐标一致。隔离检查不含 RealGamePanel 外层，因此不能单独发现本次装饰遮挡问题，修复结论以完整运行验证为准。

运行验证方式：先启动本工程并等待 Editor 导入、编译完成，再向 Design/OrchardUI/hud-runtime.command 写入 validate。验证器从 InitWZ 正式启动，等待真实框架、账户及教程状态允许交互后执行页面导航验证，完成后退出测试 Editor。启动即执行验证的旧测试曾停在预加载；延迟到 Editor 就绪后已正常完成，无需修改核心初始化或加入 Editor 专用兜底。
