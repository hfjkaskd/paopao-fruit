using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TripleHarvest.EditorSupport
{
    internal sealed class AudioMixerParameterPostprocessor : AssetPostprocessor
    {
        private static readonly Type EffectControllerType;
        private static readonly MethodInfo PreallocateGuidsMethod;

        static AudioMixerParameterPostprocessor()
        {
            Assembly editorAssembly = typeof(AssetPostprocessor).Assembly;
            EffectControllerType = editorAssembly.GetType(
                "UnityEditor.Audio.AudioMixerEffectController", true);
            PreallocateGuidsMethod = EffectControllerType.GetMethod(
                "PreallocateGUIDs", BindingFlags.Public | BindingFlags.Instance);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (PreallocateGuidsMethod == null)
                return;

            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".mixer", StringComparison.Ordinal))
                    continue;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset.GetType() != EffectControllerType)
                        continue;
                    PreallocateGuidsMethod.Invoke(asset, Array.Empty<object>());
                    EditorUtility.SetDirty(asset);
                }
            }
        }
    }
}
