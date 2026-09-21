using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TripleHarvest.EditorSupport
{
    internal sealed class SerializedShaderSaveGuard : AssetModificationProcessor
    {
        private static readonly List<string> PathsToSave = new List<string>();

        private static string[] OnWillSaveAssets(string[] paths)
        {
            PathsToSave.Clear();
            foreach (string path in paths)
            {
                if (path.EndsWith(".asset", StringComparison.Ordinal) &&
                    AssetDatabase.LoadMainAssetAtPath(path) is Shader)
                {
                    Debug.Log($"Protected serialized Shader snapshot from editor overwrite: {path}");
                    continue;
                }
                PathsToSave.Add(path);
            }
            return PathsToSave.ToArray();
        }
    }
}
