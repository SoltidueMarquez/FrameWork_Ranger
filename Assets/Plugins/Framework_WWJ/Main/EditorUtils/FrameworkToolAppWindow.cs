using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plugins.Framework_WWJ.Main.Base;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Plugins.Framework_WWJ.Main.EditorUtils
{
    public class FrameworkToolAppWindow : EditorWindow
    {
        private sealed class ToolAppDefinition
        {
            public string Id;
            public string Name;
            public string Category;
            public string[] Keywords;
            public Action DrawContent;
        }

        private const float AppButtonSize = 110f;
        private const float AppButtonGap = 10f;

        private readonly List<ToolAppDefinition> m_apps = new List<ToolAppDefinition>();
        private readonly Dictionary<string, bool> m_categoryFoldout = new Dictionary<string, bool>();

        private string m_appSearch = string.Empty;
        private string m_currentAppId;
        private Vector2 m_homeScroll;
        private Vector2 m_toolScroll;

        // SO creator state
        private readonly List<Type> m_allSoTypes = new List<Type>();
        private string m_soTypeSearch = string.Empty;
        private Vector2 m_soTypeScroll;
        private Type m_selectedSoType;
        private ScriptableObject m_previewObject;
        private Editor m_previewEditor;
        private string m_targetFolder = "Assets";
        private string m_assetName = string.Empty;

        // Pool Manager state
        private List<ObjectPoolCfg> m_allPoolConfigs = new List<ObjectPoolCfg>();
        private string m_poolConfigSearch = string.Empty;
        private Vector2 m_poolConfigScroll;
        private ObjectPoolCfg m_selectedPoolConfig;
        private Editor m_poolConfigEditor;
        private string m_newPoolConfigName = "ObjectPoolCfg";

        [MenuItem("Framework/Tool App", priority = -1000)]
        public static void OpenWindow()
        {
            var window = GetWindow<FrameworkToolAppWindow>();
            window.titleContent = new GUIContent("Framework Tool App");
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            BuildAppRegistry();
            RefreshSOTypes();
            RefreshPoolConfigs();
            m_targetFolder = FrameworkEditorUtility.GetCurrentDirectory();
        }

        private void OnDisable()
        {
            CleanupPreview();
            CleanupPoolManager();
        }

        private void OnGUI()
        {
            DrawTopSearchBar();

            if (string.IsNullOrEmpty(m_currentAppId))
            {
                DrawAppCenter();
                return;
            }

            DrawCurrentApp();
        }

        private void DrawTopSearchBar()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(48f));
            m_appSearch = EditorGUILayout.TextField(m_appSearch);
            if (GUILayout.Button("Clear", GUILayout.Width(60f)))
            {
                m_appSearch = string.Empty;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            GUILayout.EndVertical();
        }

        private void DrawAppCenter()
        {
            m_homeScroll = GUILayout.BeginScrollView(m_homeScroll);

            var filtered = m_apps
                .Where(MatchAppSearch)
                .GroupBy(a => a.Category)
                .OrderBy(g => g.Key);

            foreach (var group in filtered)
            {
                if (!m_categoryFoldout.ContainsKey(group.Key))
                {
                    m_categoryFoldout[group.Key] = true;
                }

                m_categoryFoldout[group.Key] = EditorGUILayout.Foldout(m_categoryFoldout[group.Key], group.Key, true);
                if (!m_categoryFoldout[group.Key])
                {
                    continue;
                }

                DrawAppGrid(group.ToList());
                GUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawAppGrid(List<ToolAppDefinition> apps)
        {
            if (apps.Count == 0)
            {
                return;
            }

            float available = Mathf.Max(1f, position.width - 30f);
            int columns = Mathf.Max(1, Mathf.FloorToInt((available + AppButtonGap) / (AppButtonSize + AppButtonGap)));
            int rows = Mathf.CeilToInt(apps.Count / (float)columns);

            var style = new GUIStyle(GUI.skin.button)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = AppButtonSize,
                fixedHeight = AppButtonSize
            };

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                GUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    if (index < apps.Count)
                    {
                        var app = apps[index++];
                        if (GUILayout.Button(app.Name, style))
                        {
                            m_currentAppId = app.Id;
                            GUI.FocusControl(null);
                        }
                    }
                    else
                    {
                        GUILayout.Space(AppButtonSize + AppButtonGap);
                    }

                    if (c < columns - 1)
                    {
                        GUILayout.Space(AppButtonGap);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(AppButtonGap);
            }
        }

        private void DrawCurrentApp()
        {
            var app = m_apps.FirstOrDefault(a => a.Id == m_currentAppId);

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Back", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                m_currentAppId = null;
                GUI.FocusControl(null);
            }

            GUILayout.Label(app?.Name ?? "Unknown Tool", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            m_toolScroll = GUILayout.BeginScrollView(m_toolScroll);
            if (app == null)
            {
                EditorGUILayout.HelpBox("Tool is not available.", MessageType.Warning);
            }
            else
            {
                app.DrawContent?.Invoke();
            }
            GUILayout.EndScrollView();
        }

        private void BuildAppRegistry()
        {
            m_apps.Clear();
            m_categoryFoldout.Clear();

            m_apps.Add(new ToolAppDefinition
            {
                Id = "so-creator",
                Name = "SO Creator",
                Category = "Asset Tools",
                Keywords = new[] { "so", "scriptableobject", "asset", "creator", "generalso" },
                DrawContent = DrawSOCreatorTool
            });

            m_apps.Add(new ToolAppDefinition
            {
                Id = "pool-manager",
                Name = "Pool Manager",
                Category = "Module Tools",
                Keywords = new[] { "pool", "objectpool", "manager", "config" },
                DrawContent = DrawObjectPoolManagerTool
            });
        }

        private bool MatchAppSearch(ToolAppDefinition app)
        {
            if (string.IsNullOrWhiteSpace(m_appSearch))
            {
                return true;
            }

            string query = m_appSearch.Trim();
            if (app.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return app.Keywords != null && app.Keywords.Any(k => k.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void DrawSOCreatorTool()
        {
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type Search", GUILayout.Width(78f));
            m_soTypeSearch = EditorGUILayout.TextField(m_soTypeSearch);
            if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
            {
                RefreshSOTypes();
            }
            GUILayout.EndHorizontal();

            var filteredTypes = m_allSoTypes
                .Where(t => string.IsNullOrWhiteSpace(m_soTypeSearch) ||
                            t.Name.IndexOf(m_soTypeSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (t.FullName?.IndexOf(m_soTypeSearch.Trim(), StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                .ToList();

            EditorGUILayout.LabelField($"Creatable SO Types: {filteredTypes.Count}", EditorStyles.miniBoldLabel);

            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(180f));
            m_soTypeScroll = GUILayout.BeginScrollView(m_soTypeScroll);
            foreach (var type in filteredTypes)
            {
                bool selected = m_selectedSoType == type;
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(0.65f, 0.85f, 1f) : oldColor;
                if (GUILayout.Button(type.FullName ?? type.Name, GUILayout.Height(24f)))
                {
                    SelectSOType(type);
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            DrawCreatePanel();
        }

        private void DrawCreatePanel()
        {
            if (m_selectedSoType == null)
            {
                EditorGUILayout.HelpBox("Select a GeneralSO type from the list above.", MessageType.Info);
                return;
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Create Settings", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Folder", GUILayout.Width(100f));
            m_targetFolder = EditorGUILayout.TextField(m_targetFolder);
            if (GUILayout.Button("Select", GUILayout.Width(60f)))
            {
                var newFolder = EditorUtility.OpenFolderPanel("Select Target Folder", m_targetFolder, string.Empty);
                if (!string.IsNullOrEmpty(newFolder))
                {
                    if (newFolder.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                    {
                        m_targetFolder = "Assets" + newFolder.Substring(Application.dataPath.Length).Replace("\\", "/");
                    }
                    else
                    {
                        Debug.LogWarning("Please select a folder inside this project Assets directory.");
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset Name", GUILayout.Width(100f));
            m_assetName = EditorGUILayout.TextField(m_assetName);
            GUILayout.Label(".asset", GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            if (m_previewEditor != null && m_previewObject != null)
            {
                m_previewEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Unable to create preview for current type.", MessageType.Warning);
            }

            GUILayout.Space(8f);
            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);
            if (GUILayout.Button("Create Asset", GUILayout.Height(34f)))
            {
                CreateSelectedSOAsset();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
        }

        private void RefreshSOTypes()
        {
            m_allSoTypes.Clear();

            var types = AssemblyUtilities.GetTypes(AssemblyCategory.All)
                .Where(t => !t.IsAbstract &&
                            !t.ContainsGenericParameters &&
                            typeof(GeneralSO).IsAssignableFrom(t) &&
                            !Attribute.IsDefined(t, typeof(HideInFrameworkSOCreatorAttribute), inherit: false))
                .OrderBy(t => t.FullName)
                .ToList();

            m_allSoTypes.AddRange(types);

            if (m_selectedSoType != null && !m_allSoTypes.Contains(m_selectedSoType))
            {
                SelectSOType(null);
            }
        }

        private void SelectSOType(Type type)
        {
            if (m_selectedSoType == type && m_previewObject != null)
            {
                return;
            }

            m_selectedSoType = type;
            RebuildPreview();
        }

        private void RebuildPreview()
        {
            CleanupPreview();

            if (m_selectedSoType == null)
            {
                return;
            }

            m_previewObject = CreateInstance(m_selectedSoType) as ScriptableObject;
            if (m_previewObject == null)
            {
                return;
            }

            m_previewEditor = Editor.CreateEditor(m_previewObject);
            m_assetName = m_selectedSoType.Name;
            m_targetFolder = FrameworkEditorUtility.GetCurrentDirectory();
        }

        private void CleanupPreview()
        {
            if (m_previewEditor != null)
            {
                DestroyImmediate(m_previewEditor);
                m_previewEditor = null;
            }

            if (m_previewObject != null && !AssetDatabase.Contains(m_previewObject))
            {
                DestroyImmediate(m_previewObject);
                m_previewObject = null;
            }
        }

        private void CreateSelectedSOAsset()
        {
            if (m_selectedSoType == null || m_previewObject == null)
            {
                Debug.LogError("Please select a valid SO type first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(m_targetFolder) || !AssetDatabase.IsValidFolder(m_targetFolder))
            {
                Debug.LogError($"Invalid target folder: {m_targetFolder}");
                return;
            }

            string assetFileName = string.IsNullOrWhiteSpace(m_assetName) ? m_selectedSoType.Name : m_assetName.Trim();
            assetFileName = SanitizeFileName(assetFileName);

            if (string.IsNullOrWhiteSpace(assetFileName))
            {
                Debug.LogError("Asset name is empty or invalid.");
                return;
            }

            string dest = AssetDatabase.GenerateUniqueAssetPath($"{m_targetFolder}/{assetFileName}.asset");

            AssetDatabase.CreateAsset(m_previewObject, dest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var createdAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dest);
            Selection.activeObject = createdAsset;
            Debug.Log($"Asset Created: {dest}");

            RebuildPreview();
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), string.Empty);
            }

            return fileName;
        }

        #region Pool Manager Tool

        private void RefreshPoolConfigs()
        {
            m_allPoolConfigs.Clear();
            var guids = AssetDatabase.FindAssets("t:ObjectPoolCfg");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<ObjectPoolCfg>(path);
                if (cfg != null) m_allPoolConfigs.Add(cfg);
            }
        }

        private void SelectPoolConfig(ObjectPoolCfg cfg)
        {
            if (m_poolConfigEditor != null)
            {
                DestroyImmediate(m_poolConfigEditor);
                m_poolConfigEditor = null;
            }

            m_selectedPoolConfig = cfg;
            if (m_selectedPoolConfig != null)
            {
                m_poolConfigEditor = Editor.CreateEditor(m_selectedPoolConfig);
            }
        }

        private void CleanupPoolManager()
        {
            if (m_poolConfigEditor != null)
            {
                DestroyImmediate(m_poolConfigEditor);
                m_poolConfigEditor = null;
            }
        }

        private void DrawObjectPoolManagerTool()
        {
            GUILayout.Space(4f);
            
            // 1. Create New Config Section
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Create New ObjectPoolCfg", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            m_newPoolConfigName = EditorGUILayout.TextField("Config Name", m_newPoolConfigName);
            if (GUILayout.Button("Create", GUILayout.Width(70f)))
            {
                CreateNewPoolConfig();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            // 2. Select Existing Config Section
            GUILayout.BeginHorizontal();
            GUILayout.Label("Config Search", GUILayout.Width(100f));
            string oldSearch = m_poolConfigSearch;
            m_poolConfigSearch = EditorGUILayout.TextField(m_poolConfigSearch);
            if (GUILayout.Button("Refresh", GUILayout.Width(70f)))
            {
                RefreshPoolConfigs();
            }
            GUILayout.EndHorizontal();

            var filteredConfigs = m_allPoolConfigs
                .Where(c => string.IsNullOrWhiteSpace(m_poolConfigSearch) || 
                            c.name.IndexOf(m_poolConfigSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            EditorGUILayout.LabelField($"Available Configs: {filteredConfigs.Count}", EditorStyles.miniBoldLabel);

            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(150f));
            m_poolConfigScroll = GUILayout.BeginScrollView(m_poolConfigScroll);
            foreach (var cfg in filteredConfigs)
            {
                bool selected = m_selectedPoolConfig == cfg;
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = selected ? new Color(0.65f, 0.85f, 1f) : oldColor;
                if (GUILayout.Button(cfg.name, GUILayout.Height(24f)))
                {
                    SelectPoolConfig(cfg);
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10f);

            // 3. Editor Section
            if (m_selectedPoolConfig != null && m_poolConfigEditor != null)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label($"Editing: {m_selectedPoolConfig.name}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                {
                    EditorGUIUtility.PingObject(m_selectedPoolConfig);
                }
                GUILayout.EndHorizontal();

                m_poolConfigEditor.OnInspectorGUI();
                GUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a config to edit.", MessageType.Info);
            }
        }

        private void CreateNewPoolConfig()
        {
            string folder = FrameworkEditorUtility.GetCurrentDirectory();
            string name = string.IsNullOrWhiteSpace(m_newPoolConfigName) ? "ObjectPoolCfg" : m_newPoolConfigName.Trim();
            name = SanitizeFileName(name);
            
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
            
            var asset = CreateInstance<ObjectPoolCfg>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            RefreshPoolConfigs();
            SelectPoolConfig(asset);
            
            Debug.Log($"[PoolManager] Created new config at {path}");
        }

        #endregion
    }
}
