# 老虎机关停爆光白块修复

用户反馈：老虎机中奖钻石右侧出现白色方块。后台定位到每列停止时由 SlotMachineManager.OnStop 启动的 fxList，三列均使用“飞入爆炸特效”Prefab。

该 Prefab 是 Hit04_pink 的变体，Glow 与根爆光的 Renderer 被覆盖为 DefaultParticle.mat。材质文件及 Shader 均存在，但主贴图 _MainTex 未绑定，粒子的 Texture Sheet Animation 也未开启。UI/Additive Shader 的默认采样是纯白，因而绘制整张方形粒子面片。

## 修改

- 新增 Toon Collection/Materials/SlotStopGlow.mat，保留原 UI/Additive 材质参数，绑定工程已有 glow1.png 的柔光透明贴图。
- 在“飞入爆炸特效”变体中，仅替换 Glow 与根爆光两条材质引用。三列继承同一修复。
- 保留节点、组件、粒子数量、发射参数、时长、颜色动画、脚本、UI 与奖励逻辑。全局 DefaultParticle.mat 不改。
- 该效果源另被项目中两个路径各自的 StarEndFx / VFX_ItemMatch 复用；它们继承的是相同爆光层的贴图修复，不修改其参数。

## 验证

fix-report.json 记录备份与修改后的哈希、两项字段差异和材质/Shader/贴图链。透明贴图为既有 512×512 PNG，alpha 范围 0–255，四角全透明，中心不透明；没有生成或修改图片。

independent-validation.json 为独立文件与资源核对。Unity 导入状态另存 import-state.json。未控制鼠标、键盘、窗口或触发抽奖；不将资源校验称为中奖动画运行验收。

Before/ 保存本轮修改前的源 Prefab。
