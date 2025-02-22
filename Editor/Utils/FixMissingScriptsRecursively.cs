/*
 * Ref from https://gist.github.com/vildninja/fefddf7390646a113ba7ee2a5da0525e
 */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArcherStudio.Utils.Editor
{
    public static class FixMissingScriptsRecursively
    {
        [MenuItem("ArcherStudio/Editor/Utils/Remove Missing Scripts Recursively")]
        private static void FindAndRemoveMissingInSelected()
        {
            // EditorUtility.CollectDeepHierarchy does not include inactive children
            var deeperSelection = Selection.gameObjects.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject);
            var prefabs = new HashSet<Object>();
            int compCount = 0;
            int goCount = 0;
            foreach (var go in deeperSelection)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(go))
                    {
                        RecursivePrefabSource(go, prefabs, ref compCount, ref goCount);
                        count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                        if (count == 0)
                            continue;
                    }

                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    compCount += count;
                    goCount++;
                }
            }

            Debug.Log($"Found and removed {compCount} missing scripts from {goCount} GameObjects");
        }

        private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs, ref int compCount,
            ref int goCount)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (source == null || !prefabs.Add(source))
                return;

            RecursivePrefabSource(source, prefabs, ref compCount, ref goCount);

            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(source);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source);
                compCount += count;
                goCount++;
            }
        }
    }
}