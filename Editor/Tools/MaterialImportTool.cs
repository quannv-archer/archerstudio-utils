using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace ArcherStudio.Utils.Editor
{
    public class MaterialImportTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool autoFindMaterials = true;
        private bool caseSensitiveMatch = true;
        private string materialsFolderPath = "Assets/Materials";
        private List<ModelData> modelList = new List<ModelData>();
        private Dictionary<string, Material> availableMaterials;
        private Dictionary<string, Material> availableMaterialsCaseInsensitive;
        private bool selectAll = false;

        public enum ProcessAction
        {
            Continue,
            Skip,
            Stop
        }

        private class ModelData
        {
            public Object modelObject;
            public bool isSelected;
            public string assetPath;
            public bool isValid;
            public string errorMessage;

            public ModelData(Object obj)
            {
                modelObject = obj;
                isSelected = true;
                assetPath = AssetDatabase.GetAssetPath(obj);
                isValid = !string.IsNullOrEmpty(assetPath) && assetPath.ToLower().EndsWith(".fbx");
                errorMessage = isValid ? "" : "Not a valid FBX file";
            }
        }

        [MenuItem("ArcherStudio/Editor/Tools/Material Import Tool")]
        public static void ShowWindow()
        {
            GetWindow<MaterialImportTool>("Material Import Tool");
        }

        private void OnEnable()
        {
            UpdateModelList();
            UpdateAvailableMaterials();
        }

        private void OnSelectionChange()
        {
            UpdateModelList();
            Repaint();
        }

        private void UpdateModelList()
        {
            modelList.Clear();
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects != null && selectedObjects.Length > 0)
            {
                foreach (Object obj in selectedObjects)
                {
                    modelList.Add(new ModelData(obj));
                }
            }
        }

        private void UpdateAvailableMaterials()
        {
            availableMaterials = new Dictionary<string, Material>();
            availableMaterialsCaseInsensitive =
                new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolderPath });

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    availableMaterials[material.name] = material;
                    availableMaterialsCaseInsensitive[material.name] = material;
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Material Import Settings Tool", EditorStyles.boldLabel);

            DrawMaterialSettings();
            DrawModelList();
            DrawButtons();
        }

        private void DrawMaterialSettings()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Materials Folder");
            materialsFolderPath = EditorGUILayout.TextField(materialsFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.OpenFolderPanel("Select Materials Folder", materialsFolderPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    materialsFolderPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                    UpdateAvailableMaterials();
                }
            }

            EditorGUILayout.EndHorizontal();

            autoFindMaterials = EditorGUILayout.Toggle("Auto-Find Materials", autoFindMaterials);
            caseSensitiveMatch = EditorGUILayout.Toggle("Case Sensitive Material Names", caseSensitiveMatch);
        }

        private void DrawModelList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Models:", EditorStyles.boldLabel);

            if (modelList.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                bool newSelectAll = EditorGUILayout.Toggle("Select All", selectAll, GUILayout.Width(100));
                if (newSelectAll != selectAll)
                {
                    selectAll = newSelectAll;
                    foreach (var model in modelList)
                    {
                        model.isSelected = selectAll;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (modelList.Count > 0)
            {
                foreach (var model in modelList)
                {
                    EditorGUILayout.BeginHorizontal();

                    model.isSelected = EditorGUILayout.Toggle(model.isSelected, GUILayout.Width(20));
                    EditorGUILayout.ObjectField(model.modelObject, typeof(Object), false);

                    if (!model.isValid)
                    {
                        EditorGUILayout.LabelField(model.errorMessage, EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No objects selected. Please select FBX models in the Project window.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(modelList.Count == 0 || !modelList.Any(m => m.isSelected && m.isValid)))
            {
                if (GUILayout.Button("Process Selected Models"))
                {
                    ProcessSelectedModels();
                }
            }
        }

        private string GetMissingMaterialsList(Object[] allMaterials, List<string> foundMaterials)
        {
            var missingMaterials = allMaterials
                .Where(m => !foundMaterials.Contains(m.name))
                .Select(m => m.name);
            return string.Join("\n", missingMaterials);
        }

        private ProcessAction ShowMaterialWarningDialog(string modelName, string missingMaterials)
        {
            string message =
                $"Some materials not found for model: {modelName}\n\nMissing materials:\n{missingMaterials}\n\nWhat would you like to do?";
            int choice = EditorUtility.DisplayDialogComplex(
                "Missing Materials Warning",
                message,
                "Continue", // Apply found materials and continue
                "Skip", // Skip this model and continue with others
                "Stop" // Stop the entire process
            );

            switch (choice)
            {
                case 0: return ProcessAction.Continue;
                case 1: return ProcessAction.Skip;
                case 2: return ProcessAction.Stop;
                default: return ProcessAction.Stop;
            }
        }

        private void ProcessSelectedModels()
        {
            var selectedModels = modelList.Where(m => m.isSelected && m.isValid).ToList();
            if (selectedModels.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select at least one valid FBX model to process.", "OK");
                return;
            }

            int processedCount = 0;
            int totalCount = selectedModels.Count;

            try
            {
                foreach (var model in selectedModels)
                {
                    EditorUtility.DisplayProgressBar("Processing Models",
                        $"Processing {System.IO.Path.GetFileName(model.assetPath)} ({processedCount + 1}/{totalCount})",
                        (float)processedCount / totalCount);

                    ModelImporter modelImporter = AssetImporter.GetAtPath(model.assetPath) as ModelImporter;
                    if (modelImporter != null)
                    {
                        ProcessAction action = ProcessModelImporter(modelImporter);
                        if (action == ProcessAction.Stop)
                        {
                            Debug.Log($"Process stopped at model: {model.assetPath}");
                            break;
                        }
                        else if (action == ProcessAction.Continue)
                        {
                            processedCount++;
                        }
                    }
                }

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Process Complete",
                    $"Successfully processed {processedCount} out of {totalCount} models.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private ProcessAction ProcessModelImporter(ModelImporter modelImporter)
        {
            bool wasExternal = modelImporter.materialLocation == ModelImporterMaterialLocation.External;
            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
            
            if (wasExternal)
            {
                modelImporter.SaveAndReimport();
                // Re-get the importer after reimport to ensure we have fresh state
                modelImporter = AssetImporter.GetAtPath(modelImporter.assetPath) as ModelImporter;
            }

            if (autoFindMaterials)
            {
                Object[] materials = AssetDatabase.LoadAllAssetsAtPath(modelImporter.assetPath)
                    .Where(x => x is Material)
                    .ToArray();

                var remapList = new List<(string, Material)>();
                var foundMaterialNames = new List<string>();

                foreach (Object materialObj in materials)
                {
                    Material projectMaterial = null;
                    bool found = caseSensitiveMatch
                        ? availableMaterials.TryGetValue(materialObj.name, out projectMaterial)
                        : availableMaterialsCaseInsensitive.TryGetValue(materialObj.name, out projectMaterial);

                    if (found)
                    {
                        remapList.Add((materialObj.name, projectMaterial));
                        foundMaterialNames.Add(materialObj.name);
                    }
                }

                // Check if any materials are missing
                if (foundMaterialNames.Count < materials.Length)
                {
                    string missingMaterials = GetMissingMaterialsList(materials, foundMaterialNames);
                    ProcessAction action = ShowMaterialWarningDialog(
                        System.IO.Path.GetFileName(modelImporter.assetPath),
                        missingMaterials
                    );

                    if (action == ProcessAction.Skip)
                    {
                        return ProcessAction.Skip;
                    }
                    else if (action == ProcessAction.Stop)
                    {
                        return ProcessAction.Stop;
                    }
                }

                // Apply material remapping if we're continuing
                if (remapList.Any())
                {
                    foreach (var (originalName, newMaterial) in remapList)
                    {
                        modelImporter.RemoveRemap(
                            new AssetImporter.SourceAssetIdentifier(typeof(Material), originalName));
                        modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), originalName),
                            newMaterial);
                    }

                    modelImporter.SaveAndReimport();
                }
            }

            return ProcessAction.Continue;
        }
    }
}