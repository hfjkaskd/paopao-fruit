# MAX SDK compile-blocker fix

Unity 2022.3.62f3 blocked Play mode with CS0246 in `MaxSdkAndroid.cs:71` and `MaxSdkUnityEditor.cs:107`: `MaxUserData` could not be found.

The real `MaxUserData.cs` already existed in `Assets/MaxSdk/Scripts`. Without an assembly reference it belonged to `Assembly-CSharp`, while the SDK call sites were compiled into the package assembly `MaxSdk.Scripts`. Its internal `ToDictionary()` method also requires the same assembly as those call sites.

Added `Assets/MaxSdk/Scripts/MaxSdk.Scripts.asmref` and its meta file. The reference points to the existing package assembly definition GUID `a4cfc1a18fa3a469b96d885db522f42e`. No package-cache source, SDK version, ad logic, game logic, scene or Prefab was changed for this fix. This compilation configuration applies to the SDK's existing supported platforms.

Verification:

- Unity refreshed and completed compilation with zero errors in the BizzaWZ project's `Design/OrchardUI/state.json`.
- `Library/Bee/artifacts/1300b0aE.dag/MaxSdk.Scripts.rsp` includes `Assets/MaxSdk/Scripts/MaxUserData.cs` alongside `MaxSdkAndroid.cs`.
- `Library/ScriptAssemblies/MaxSdk.Scripts.dll` and `Assembly-CSharp.dll` were rebuilt on 2026-09-20 at approximately 22:44 local time.
- No desktop input, Play-mode command, account reset or ad invocation was used for validation. The compilation gate is verified; no new claim is made about a complete runtime session.

The formal entry scene remains `Assets/Game/Resources/Scenes/InitWZ.unity`.
