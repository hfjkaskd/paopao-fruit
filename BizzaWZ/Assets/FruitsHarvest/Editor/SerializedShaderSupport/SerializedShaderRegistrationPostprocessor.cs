using System;
using UnityEditor;
using UnityEngine;

namespace TripleHarvest.EditorSupport
{
    internal sealed class SerializedShaderRegistrationPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.Ordinal))
                    continue;
                if (AssetDatabase.LoadMainAssetAtPath(path) is Shader shader)
                    ShaderUtil.RegisterShader(shader);
            }
        }
    }
}
