# Android 发布包

唯一工程：`C:/Projects/paopao/BizzaWZ`；Unity 版本以其 ProjectVersion.txt 为准。

导出 Android Gradle 工程后，使用该 Unity 自带的 Gradle 7.5.1 打包，保持与 Android Gradle Plugin 7.4.2 配套。Ad Review 构建插件固定为 5.12.12，由 `OrchardAndroidBuildCompatibility` 在 MAX 导出回调之后应用。

**APK 与 AAB 必须分两次 Gradle 调用构建。** 已在真机复现两种打包任务合在一次调用时，SafeDK 将 AAB 的 DEX 输入字段配置用于 APK，APK 仍使用旧 DEX，造成 `SafeDKApplication` 未入包而启动闪退；同次构建的 AAB 含有该类。切换 Gradle 或降低 Ad Review 版本均未解决。脚本会分两次构建并逐个检查实际 DEX 类定义。

不要使用 `+` 自动升级构建插件；升级时须核对包内类定义并完成真机启动检查。

```powershell
python Tools/Build/package_android_export.py <导出的Gradle工程> `
  --output <交付目录> `
  --signing-directory 'C:/Users/pc/Desktop/SignatureFiles/签名文件/com.wiwitsugeh' `
  --unity-editor 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor' `
  --version '1.0.2' --version-code 102
```

签名密码和别名仅从指定目录读取、通过进程环境传入 Gradle；不保存到源码或命令行。构建完成后恢复导出工程的签名配置并删除临时签名文件。

Unity 批处理入口 `OrchardSignedAndroidBuild.BuildReleaseBatch` 通过 `ORCHARD_RELEASE_VERSION` 和 `ORCHARD_RELEASE_VERSION_CODE` 校对 ProjectSettings 中的版本，不会静默覆盖版本设置。

脚本会核对 APK/AAB 的 Application、Activity、Service、Receiver、Provider 是否存在于实际 DEX 类定义中；缺失即停止交付。版本、证书、Addressables 资源校验和真机启动检查仍需完成。

单独核对现有包：

```powershell
python Tools/Build/validate_android_startup.py <APK文件> --aab <AAB文件> `
  --android-player 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/PlaybackEngines/AndroidPlayer' `
  --report <校验JSON文件>
```

Google 官方配套表：[Android Gradle Plugin 7.4](https://developer.android.com/build/releases/agp-7-4-0-release-notes)。
