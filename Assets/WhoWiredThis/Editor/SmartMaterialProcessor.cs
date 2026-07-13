using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Smart Material Processor for Unity URP.
/// Automatically creates materials from textures exported from Substance Painter / Tripo (URP preset),
/// matches them to FBX materials by name, remaps via ModelImporter, and creates a prefab.
/// </summary>
public class SmartMaterialProcessor : EditorWindow
{
    // ── UI State ──────────────────────────────────────────────
    private DefaultAsset texturesFolder;
    private GameObject fbxAsset;
    private string materialOutputPath = "Assets/Materials/";
    private string prefabOutputPath   = "Assets/Prefabs/";
    private bool   createPrefab       = true;
    private bool   debugMode          = true;
    private Vector2 scrollPos;

    // Batch embedded-texture extraction (FBX folder)
    private DefaultAsset fbxFolder;
    private bool batchRecursive = true;

    // ── Known URP map suffixes ────────────────────────────────
    private static readonly string[] MapSuffixes = new[]
    {
        "AlbedoTransparency",
        "BaseMap",
        "BaseColor",
        "MetallicSmoothness",
        "MetallicGlossMap",
        "Normal",
        "Emission",
        "Emissive",
        "Height",
        "Occlusion",
        "AO"
    };

    private static readonly Regex SuffixRegex = new Regex(
        $@"^(.+?)_({string.Join("|", MapSuffixes)})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── Generic material names to ignore ──────────────────────
    private static readonly HashSet<string> GenericMaterialNames = new HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "Lit", "Default-Material", "Default", "None",
        "Standard", "URP/Lit", "Universal Render Pipeline/Lit"
    };

    // ── Menu ──────────────────────────────────────────────────
    [MenuItem("Tools/Smart Material Processor")]
    public static void ShowWindow()
    {
        var win = GetWindow<SmartMaterialProcessor>("Smart Material Processor");
        win.minSize = new Vector2(420, 380);
    }

    // ── GUI ───────────────────────────────────────────────────
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(4);
        GUILayout.Label("Smart Material Processor (URP)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) Point to a Textures folder and an FBX.\n" +
            "2) Click Process — the tool will:\n" +
            "   • Create URP Lit materials from the textures\n" +
            "   • Match & remap them onto the FBX via the ModelImporter\n" +
            "   • Optionally create a prefab.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        texturesFolder     = (DefaultAsset)EditorGUILayout.ObjectField("Textures Folder", texturesFolder, typeof(DefaultAsset), false);
        fbxAsset           = (GameObject)EditorGUILayout.ObjectField("FBX Asset", fbxAsset, typeof(GameObject), false);
        materialOutputPath = EditorGUILayout.TextField("Material Output Folder", materialOutputPath);
        prefabOutputPath   = EditorGUILayout.TextField("Prefab Output Folder", prefabOutputPath);
        createPrefab       = EditorGUILayout.Toggle("Create Prefab", createPrefab);
        debugMode          = EditorGUILayout.Toggle("Debug Logging", debugMode);

        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(texturesFolder == null || fbxAsset == null);
        if (GUILayout.Button("▶  Process", GUILayout.Height(32)))
        {
            Process();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(4);

        EditorGUI.BeginDisabledGroup(texturesFolder == null);
        if (GUILayout.Button("Generate Materials Only"))
        {
            string folder = AssetDatabase.GetAssetPath(texturesFolder);
            var mats = GenerateMaterials(folder);
            Debug.Log($"✅ Generated / updated {mats.Count} material(s).");
        }
        EditorGUI.EndDisabledGroup();

        // Debug helper
        EditorGUI.BeginDisabledGroup(fbxAsset == null);
        if (GUILayout.Button("🔍 Show FBX Material Names (Debug)"))
        {
            string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
            var names = GetFBXMaterialNames(fbxPath);
            Debug.Log($"📋 FBX contains {names.Count} material(s): {string.Join(", ", names)}");
        }
        EditorGUI.EndDisabledGroup();

        // Batch: pull EMBEDDED textures + materials OUT of a whole folder of FBX
        // (e.g. Tripo exports). This is the inverse of the pipeline above, which
        // builds materials from loose texture files.
        EditorGUILayout.Space(12);
        GUILayout.Label("Batch Extract Embedded Textures (FBX folder)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "For FBX that carry their textures embedded inside them (e.g. Tripo).\n" +
            "Point to a folder of FBX; each model's embedded textures + materials are\n" +
            "extracted to sibling Name_Textures / Name_Materials folders so it renders\n" +
            "in URP. Safe to re-run — already-extracted FBX do nothing.",
            MessageType.Info);

        fbxFolder      = (DefaultAsset)EditorGUILayout.ObjectField("FBX Folder", fbxFolder, typeof(DefaultAsset), false);
        batchRecursive = EditorGUILayout.Toggle("Include Subfolders", batchRecursive);

        EditorGUI.BeginDisabledGroup(fbxFolder == null);
        if (GUILayout.Button("▶  Batch Extract Embedded Textures + Materials", GUILayout.Height(32)))
        {
            BatchExtractFromFolder(AssetDatabase.GetAssetPath(fbxFolder));
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    // ── Main pipeline ─────────────────────────────────────────
    private void Process()
    {
        string texFolder = AssetDatabase.GetAssetPath(texturesFolder);
        string fbxPath   = AssetDatabase.GetAssetPath(fbxAsset);

        // Step 1 — Generate materials
        var generatedMaterials = GenerateMaterials(texFolder);
        if (generatedMaterials.Count == 0)
        {
            Debug.LogWarning("⚠ No materials were generated. Check your textures folder.");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"📦 Generated {generatedMaterials.Count} material(s):");
            foreach (var kvp in generatedMaterials)
                Debug.Log($"   • BaseName key: \"{kvp.Key}\" → Material: \"{kvp.Value.name}\"");
        }

        // Step 2 — Remap
        int remapped = RemapMaterialsOnImporter(fbxPath, generatedMaterials);

        // Step 3 — Prefab
        if (createPrefab)
        {
            CreatePrefabFromFBX(fbxPath);
        }

        Debug.Log($"✅ Done! Generated {generatedMaterials.Count} material(s), remapped {remapped} slot(s).");
    }

    // ══════════════════════════════════════════════════════════
    //  STEP 1: Generate URP Lit Materials
    // ══════════════════════════════════════════════════════════

    private Dictionary<string, Material> GenerateMaterials(string folderPath)
    {
        var texturePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".tga") || p.EndsWith(".tif") || p.EndsWith(".psd"))
            .Select(p => p.Replace("\\", "/"))
            .ToArray();

        var groups = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var path in texturePaths)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            var match = SuffixRegex.Match(filename);
            if (!match.Success)
            {
                if (debugMode) Debug.Log($"   ⏭ Skipped (no suffix match): {filename}");
                continue;
            }

            string baseName = match.Groups[1].Value;
            string mapType  = NormalizeMapType(match.Groups[2].Value);

            if (debugMode) Debug.Log($"   📄 Texture: {filename} → base=\"{baseName}\", map=\"{mapType}\"");

            if (!groups.ContainsKey(baseName))
                groups[baseName] = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            groups[baseName][mapType] = path;
        }

        if (!Directory.Exists(materialOutputPath))
        {
            Directory.CreateDirectory(materialOutputPath);
            AssetDatabase.Refresh();
        }

        var result = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in groups)
        {
            string baseName = kvp.Key;
            var maps = kvp.Value;

            string matPath = $"{materialOutputPath}{baseName}.mat".Replace("\\", "/");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }

            if (maps.TryGetValue("albedo", out var albedoPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
            }

            if (maps.TryGetValue("normal", out var normalPath))
            {
                EnsureNormalMapImportType(normalPath);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                mat.SetTexture("_BumpMap", tex);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (maps.TryGetValue("metallic", out var metallicPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                mat.SetTexture("_MetallicGlossMap", tex);
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_SmoothnessTextureChannel", 0);
            }

            if (maps.TryGetValue("emission", out var emissionPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
                mat.SetTexture("_EmissionMap", tex);
                mat.SetColor("_EmissionColor", Color.white);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            if (maps.TryGetValue("height", out var heightPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
                mat.SetTexture("_ParallaxMap", tex);
                mat.SetFloat("_Parallax", 0.005f);
            }

            if (maps.TryGetValue("occlusion", out var occlusionPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(occlusionPath);
                mat.SetTexture("_OcclusionMap", tex);
                mat.SetFloat("_OcclusionStrength", 1f);
            }

            EditorUtility.SetDirty(mat);
            result[baseName] = mat;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return result;
    }

    private string NormalizeMapType(string raw)
    {
        switch (raw.ToLower())
        {
            case "albedotransparency":
            case "basemap":
            case "basecolor":
                return "albedo";
            case "metallicsmoothness":
            case "metallicglossmap":
                return "metallic";
            case "normal":
                return "normal";
            case "emission":
            case "emissive":
                return "emission";
            case "height":
                return "height";
            case "occlusion":
            case "ao":
                return "occlusion";
            default:
                return raw.ToLower();
        }
    }

    private void EnsureNormalMapImportType(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Get FBX material names (3 methods, reordered)
    // ══════════════════════════════════════════════════════════

    private List<string> GetFBXMaterialNames(string fbxPath)
    {
        var names = new List<string>();

        if (debugMode) Debug.Log($"🔍 Scanning FBX for materials: {fbxPath}");

        // ── Method 1 (PRIMARY): SerializedObject on ModelImporter ──
        // This reads the actual material slot names from the FBX file,
        // which is what Unity shows in the Inspector's "Remapped Materials".
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            var so = new SerializedObject(importer);

            // Try m_ExternalObjects first (contains remap entries)
            var extObj = so.FindProperty("m_ExternalObjects");
            if (extObj != null && extObj.arraySize > 0)
            {
                for (int i = 0; i < extObj.arraySize; i++)
                {
                    var element = extObj.GetArrayElementAtIndex(i);
                    var first = element.FindPropertyRelative("first");
                    string typeName = first?.FindPropertyRelative("type")?.stringValue ?? "";
                    string name = first?.FindPropertyRelative("name")?.stringValue ?? "";
                    if (typeName.Contains("Material") && !string.IsNullOrEmpty(name)
                        && !GenericMaterialNames.Contains(name))
                    {
                        names.Add(name);
                    }
                }
                if (names.Count > 0 && debugMode)
                    Debug.Log($"   ✓ Found {names.Count} material(s) via m_ExternalObjects");
            }

            // Try m_Materials (source material descriptions from the FBX)
            if (names.Count == 0)
            {
                var mats = so.FindProperty("m_Materials");
                if (mats != null && mats.arraySize > 0)
                {
                    for (int i = 0; i < mats.arraySize; i++)
                    {
                        var element = mats.GetArrayElementAtIndex(i);
                        var matName = element.FindPropertyRelative("name");
                        if (matName != null && !string.IsNullOrEmpty(matName.stringValue)
                            && !GenericMaterialNames.Contains(matName.stringValue))
                        {
                            names.Add(matName.stringValue);
                        }
                    }
                    if (names.Count > 0 && debugMode)
                        Debug.Log($"   ✓ Found {names.Count} material(s) via m_Materials");
                }
            }
        }

        if (names.Count > 0)
            return names.Distinct().ToList();

        // ── Method 2: LoadAllAssetsAtPath — finds embedded materials ──
        if (debugMode) Debug.Log("   ⚠ Importer method found nothing, trying sub-assets...");

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (debugMode) Debug.Log($"   Found {allAssets.Length} total sub-assets in FBX");

        foreach (var asset in allAssets)
        {
            if (asset is Material mat && !GenericMaterialNames.Contains(mat.name))
            {
                names.Add(mat.name);
                if (debugMode) Debug.Log($"   Sub-asset Material: \"{mat.name}\"");
            }
        }

        if (names.Count > 0)
        {
            if (debugMode) Debug.Log($"   ✓ Found {names.Count} material(s) via sub-assets");
            return names.Distinct().ToList();
        }

        // ── Method 3: Renderers on loaded prefab (last resort) ──
        if (debugMode) Debug.Log("   ⚠ No materials via sub-assets, trying renderer scan...");
        var fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxObj != null)
        {
            foreach (var renderer in fbxObj.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in renderer.sharedMaterials)
                {
                    if (m != null && !names.Contains(m.name) && !GenericMaterialNames.Contains(m.name))
                        names.Add(m.name);
                }
            }
        }

        if (names.Count > 0)
        {
            if (debugMode) Debug.Log($"   ✓ Found {names.Count} material(s) via renderers");
        }
        else
        {
            if (debugMode) Debug.Log("   ✗ No material names found by any method");
        }

        return names.Distinct().ToList();
    }

    // ══════════════════════════════════════════════════════════
    //  STEP 2: Remap via ModelImporter.AddRemap
    // ══════════════════════════════════════════════════════════

    private int RemapMaterialsOnImporter(string fbxPath, Dictionary<string, Material> generatedMaterials)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"❌ Could not get ModelImporter for {fbxPath}");
            return 0;
        }

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        var fbxMaterialNames = GetFBXMaterialNames(fbxPath);

        if (fbxMaterialNames.Count == 0)
        {
            Debug.LogWarning($"⚠ No materials found in FBX: {fbxPath}");
            return 0;
        }

        Debug.Log($"🔍 FBX materials: [{string.Join("], [", fbxMaterialNames)}]");
        Debug.Log($"🔍 Generated material keys: [{string.Join("], [", generatedMaterials.Keys)}]");

        int remappedCount = 0;
        var notFound = new List<string>();

        foreach (var fbxMatName in fbxMaterialNames)
        {
            Material matchedMaterial = FindBestMatch(fbxMatName, generatedMaterials);

            if (matchedMaterial != null)
            {
                var sourceId = new AssetImporter.SourceAssetIdentifier(typeof(Material), fbxMatName);
                importer.AddRemap(sourceId, matchedMaterial);
                remappedCount++;
                Debug.Log($"✔ Remapped: \"{fbxMatName}\" → \"{matchedMaterial.name}\"");
            }
            else
            {
                notFound.Add(fbxMatName);
                Debug.LogWarning($"✖ No match for FBX material: \"{fbxMatName}\"");
            }
        }

        if (remappedCount > 0)
        {
            importer.SaveAndReimport();
        }

        if (notFound.Count > 0)
            Debug.LogWarning($"⚠ Unmatched: {string.Join(", ", notFound)}");

        return remappedCount;
    }

    private Material FindBestMatch(string fbxMatName, Dictionary<string, Material> generatedMaterials)
    {
        string fbxLower = fbxMatName.ToLower().Trim();

        if (debugMode) Debug.Log($"   🔎 Matching \"{fbxMatName}\"...");

        // 1. Exact match on full key
        foreach (var kvp in generatedMaterials)
        {
            if (kvp.Key.Equals(fbxMatName, System.StringComparison.OrdinalIgnoreCase))
            {
                if (debugMode) Debug.Log($"      ✓ Exact match: \"{kvp.Key}\"");
                return kvp.Value;
            }
        }

        // 2. Generated key ends with the FBX material name
        //    e.g. key="sorcery room_Big_Book_Cover" ends with "Big_Book_Cover"
        var endsWith = generatedMaterials
            .Where(kvp => kvp.Key.ToLower().EndsWith(fbxLower) ||
                          kvp.Key.ToLower().EndsWith("_" + fbxLower))
            .OrderBy(kvp => kvp.Key.Length)
            .FirstOrDefault();
        if (endsWith.Value != null)
        {
            if (debugMode) Debug.Log($"      ✓ EndsWith match: \"{endsWith.Key}\"");
            return endsWith.Value;
        }

        // 3. FBX name ends with generated key's suffix
        //    e.g. fbx="Big_Book_Cover" and key contains it
        foreach (var kvp in generatedMaterials)
        {
            string keyLower = kvp.Key.ToLower();
            // Strip common prefixes from key to get the core name
            string keySuffix = StripCommonPrefix(keyLower);
            if (!string.IsNullOrEmpty(keySuffix) &&
                keySuffix.Equals(fbxLower, System.StringComparison.OrdinalIgnoreCase))
            {
                if (debugMode) Debug.Log($"      ✓ Prefix-stripped match: \"{kvp.Key}\" (suffix=\"{keySuffix}\")");
                return kvp.Value;
            }
        }

        // 4. BaseName contains fbxMatName
        var contains = generatedMaterials
            .Where(kvp => kvp.Key.ToLower().Contains(fbxLower))
            .OrderBy(kvp => kvp.Key.Length)
            .FirstOrDefault();
        if (contains.Value != null)
        {
            if (debugMode) Debug.Log($"      ✓ Contains match: \"{contains.Key}\"");
            return contains.Value;
        }

        // 5. fbxMatName contains baseName
        var reverse = generatedMaterials
            .Where(kvp => fbxLower.Contains(kvp.Key.ToLower()))
            .OrderByDescending(kvp => kvp.Key.Length)
            .FirstOrDefault();
        if (reverse.Value != null)
        {
            if (debugMode) Debug.Log($"      ✓ Reverse-contains match: \"{reverse.Key}\"");
            return reverse.Value;
        }

        // 6. Fuzzy: strip prefixes/suffixes and compare cleaned names
        string fbxCleaned = CleanMaterialName(fbxLower);
        if (debugMode) Debug.Log($"      Fuzzy: fbxCleaned = \"{fbxCleaned}\"");
        foreach (var kvp in generatedMaterials)
        {
            string genCleaned = CleanMaterialName(kvp.Key.ToLower());
            if (debugMode) Debug.Log($"      Fuzzy compare: \"{genCleaned}\" vs \"{fbxCleaned}\"");
            if (!string.IsNullOrEmpty(fbxCleaned) && !string.IsNullOrEmpty(genCleaned) &&
                (fbxCleaned == genCleaned ||
                 fbxCleaned.Contains(genCleaned) ||
                 genCleaned.Contains(fbxCleaned)))
            {
                if (debugMode) Debug.Log($"      ✓ Fuzzy match: \"{kvp.Key}\"");
                return kvp.Value;
            }
        }

        if (debugMode) Debug.Log($"      ✗ No match found");
        return null;
    }

    /// <summary>
    /// Strips a common prefix pattern from a generated material key.
    /// e.g. "sorcery room_Big_Book_Cover" → "Big_Book_Cover"
    /// Handles patterns like "project name_MaterialName" where the first
    /// underscore-separated segment contains spaces (folder/project name).
    /// </summary>
    private string StripCommonPrefix(string name)
    {
        // Pattern: "word word_RestOfName" — strip everything up to and including
        // the first underscore that follows a space-containing prefix
        int firstUnderscore = name.IndexOf('_');
        if (firstUnderscore > 0 && firstUnderscore < name.Length - 1)
        {
            string prefix = name.Substring(0, firstUnderscore);
            // If the prefix contains a space, it's likely a project/folder name
            if (prefix.Contains(" "))
            {
                return name.Substring(firstUnderscore + 1);
            }
        }
        return name;
    }

    private string CleanMaterialName(string name)
    {
        name = Regex.Replace(name, @"^(tripo_convert_[a-f0-9\-]+_)", "");
        name = Regex.Replace(name, @"(_mat|_material|_blinn|_aimtl|_urpmat)$", "");
        name = Regex.Replace(name, @"[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}", "");
        name = name.Trim('_', '-');
        return name;
    }

    // ══════════════════════════════════════════════════════════
    //  STEP 3: Create Prefab
    // ══════════════════════════════════════════════════════════

    private void CreatePrefabFromFBX(string fbxPath)
    {
        var fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxObj == null) return;

        if (!Directory.Exists(prefabOutputPath))
            Directory.CreateDirectory(prefabOutputPath);

        string name = Path.GetFileNameWithoutExtension(fbxPath);
        string savePath = $"{prefabOutputPath}{name}.prefab".Replace("\\", "/");

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
        if (existingPrefab != null)
        {
            Debug.Log($"✅ Prefab already exists and was updated: {savePath}");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(fbxObj) as GameObject;
        PrefabUtility.SaveAsPrefabAsset(instance, savePath);
        Object.DestroyImmediate(instance);
        Debug.Log($"✅ Prefab created: {savePath}");
    }

    // ──────────────────────────────────────────────────────────
    //  BATCH: extract embedded textures + materials from FBX.
    //  Mirrors PlayerCharacterSlot.ExtractFBXAssets (the proven
    //  player-character path), applied across a whole folder so a
    //  large set of Tripo FBX render in URP in one click.
    // ──────────────────────────────────────────────────────────

    private void BatchExtractFromFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"❌ Not a valid project folder: \"{folderPath}\"");
            return;
        }

        var option = batchRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var fbxPaths = Directory.GetFiles(folderPath, "*.fbx", option)
            .Where(p => p.ToLower().EndsWith(".fbx"))
            .Select(p => p.Replace("\\", "/"))
            .ToArray();

        if (fbxPaths.Length == 0)
        {
            Debug.LogWarning($"⚠ No .fbx found in \"{folderPath}\"" + (batchRecursive ? " (incl. subfolders)." : "."));
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Batch Extract Embedded Textures",
                $"Found {fbxPaths.Length} FBX in:\n{folderPath}\n\n" +
                "Each will be reimported — this can take a few minutes for large models.\n\nContinue?",
                "Extract", "Cancel"))
        {
            return;
        }

        int processed = 0, withMaterials = 0, failed = 0;
        try
        {
            for (int i = 0; i < fbxPaths.Length; i++)
            {
                var fbxPath = fbxPaths[i];
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Extracting embedded textures",
                    $"({i + 1}/{fbxPaths.Length}) {Path.GetFileName(fbxPath)}",
                    (float)i / fbxPaths.Length);
                if (cancel) { Debug.LogWarning($"⚠ Cancelled after {processed} FBX."); break; }

                try
                {
                    if (ExtractEmbeddedAssets(fbxPath)) withMaterials++;
                    processed++;
                }
                catch (System.Exception e)
                {
                    failed++;
                    Debug.LogError($"❌ Extraction failed for \"{fbxPath}\": {e.Message}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"✅ Batch extract done. {processed}/{fbxPaths.Length} FBX processed " +
                  $"({withMaterials} textured, {failed} failed).");
    }

    /// <summary>
    /// Extracts embedded textures + materials from one FBX into sibling
    /// "Name_Textures" / "Name_Materials" folders so it renders in URP.
    /// Idempotent: once extracted a material is no longer an FBX sub-asset, so
    /// re-runs find nothing to do. Mirrors PlayerCharacterSlot.ExtractFBXAssets.
    /// Returns true if any embedded materials were extracted on this run.
    /// </summary>
    private bool ExtractEmbeddedAssets(string assetPath)
    {
        var fbxName = Path.GetFileNameWithoutExtension(assetPath);
        var fbxDir  = Path.GetDirectoryName(assetPath).Replace('\\', '/');

        var materialsFolderName = $"{fbxName}_Materials";
        var texturesFolderName  = $"{fbxName}_Textures";
        var materialsDir = $"{fbxDir}/{materialsFolderName}";
        var texturesDir  = $"{fbxDir}/{texturesFolderName}";

        if (!AssetDatabase.IsValidFolder(materialsDir))
            AssetDatabase.CreateFolder(fbxDir, materialsFolderName);
        if (!AssetDatabase.IsValidFolder(texturesDir))
            AssetDatabase.CreateFolder(fbxDir, texturesFolderName);

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            if (debugMode) Debug.Log($"   ⏭ Not a ModelImporter, skipped: {assetPath}");
            return false;
        }

        // Extract embedded textures (no-op when textures are already external).
        try
        {
            bool extracted = importer.ExtractTextures(texturesDir);
            if (debugMode) Debug.Log($"   Textures extracted for \"{fbxName}\" → {extracted}");
        }
        catch (System.Exception e)
        {
            if (debugMode) Debug.Log($"   ⚠ ExtractTextures threw for \"{fbxName}\": {e.Message}");
        }

        AssetDatabase.Refresh();

        // Extract embedded materials. After extraction a material is no longer
        // an FBX sub-asset, so subsequent calls find nothing to do.
        int extractedCount = 0;
        foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
        {
            if (!(sub is Material mat)) continue;
            var newPath = AssetDatabase.GenerateUniqueAssetPath($"{materialsDir}/{mat.name}.mat");
            var error = AssetDatabase.ExtractAsset(mat, newPath);
            if (string.IsNullOrEmpty(error))
            {
                extractedCount++;
                if (debugMode) Debug.Log($"   ✔ Extracted material '{mat.name}' → '{newPath}'");
            }
            else if (debugMode)
            {
                Debug.Log($"   ✖ ExtractAsset for '{mat.name}' failed: {error}");
            }
        }

        if (extractedCount > 0)
        {
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // Final step: Tripo embeds textures as loose images but does NOT wire
        // them into the FBX material, so the extracted material(s) come out with
        // empty slots. Assign the extracted textures to each material's URP/Lit
        // slots by name (Color->_BaseMap, Normal->_BumpMap, Metallic+Roughness ->
        // packed _MetallicGlossMap, etc.). Runs whether or not materials were
        // extracted on THIS pass, so it also repairs previously-extracted empties.
        int texturedMats = 0;
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var matGuid in AssetDatabase.FindAssets("t:Material", new[] { materialsDir }))
        {
            var matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;
            if (urpLit != null && mat.shader != urpLit) mat.shader = urpLit;
            int n = AssignTripoTextures(mat, texturesDir);
            if (n > 0)
            {
                texturedMats++;
                if (debugMode) Debug.Log($"   🎨 Assigned {n} map(s) to material '{mat.name}'");
            }
            else if (debugMode)
            {
                Debug.Log($"   ⚠ No recognizable textures for material '{mat.name}' in {texturesDir}");
            }
        }
        if (texturedMats > 0) AssetDatabase.SaveAssets();

        return texturedMats > 0;
    }

    // ──────────────────────────────────────────────────────────
    //  Assign the loose textures in a Tripo "Name_Textures" folder
    //  to a material's URP/Lit slots, matching by map name. Returns
    //  the number of slots assigned.
    // ──────────────────────────────────────────────────────────
    private int AssignTripoTextures(Material mat, string texturesDir)
    {
        var texPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesDir })
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .ToArray();
        if (texPaths.Length == 0) return 0;

        string albedo = null, normal = null, metallic = null, roughness = null,
               smoothness = null, metalSmooth = null, emission = null, occlusion = null;

        foreach (var p in texPaths)
        {
            var n = Path.GetFileNameWithoutExtension(p).ToLower();
            if      (MapNameMatches(n, "color", "basecolor", "base_color", "albedo", "diffuse", "basemap")) albedo      = albedo      ?? p;
            else if (MapNameMatches(n, "normal", "normalmap", "normalgl", "nrm", "bump"))                   normal      = normal      ?? p;
            else if (MapNameMatches(n, "metallicsmoothness", "metallicgloss", "metalsmooth"))               metalSmooth = metalSmooth ?? p;
            else if (MapNameMatches(n, "metallic", "metalness", "metal"))                                   metallic    = metallic    ?? p;
            else if (MapNameMatches(n, "roughness", "rough"))                                               roughness   = roughness   ?? p;
            else if (MapNameMatches(n, "smoothness", "gloss", "glossiness"))                                smoothness  = smoothness  ?? p;
            else if (MapNameMatches(n, "emissive", "emission", "emit"))                                     emission    = emission    ?? p;
            else if (MapNameMatches(n, "ao", "occlusion", "ambientocclusion", "ambient_occlusion"))         occlusion   = occlusion   ?? p;
        }

        int assigned = 0;

        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
            mat.SetColor("_BaseColor", Color.white);
            assigned++;
        }

        if (normal != null)
        {
            EnsureNormalMapImportType(normal);
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            mat.EnableKeyword("_NORMALMAP");
            assigned++;
        }

        // URP/Lit wants metallic (R) + smoothness (A) in ONE map. Tripo gives
        // them separate, so pack them (smoothness = 1 - roughness).
        if (metalSmooth == null && (metallic != null || roughness != null || smoothness != null))
            metalSmooth = BuildMetallicSmoothnessMap(texturesDir, metallic, roughness, smoothness);

        if (metalSmooth != null)
        {
            EnsureLinearImport(metalSmooth);
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metalSmooth));
            mat.EnableKeyword("_METALLICGLOSSMAP");
            mat.SetFloat("_Metallic", 1f);
            mat.SetFloat("_Smoothness", 1f);
            mat.SetFloat("_SmoothnessTextureChannel", 0);
            assigned++;
        }

        if (emission != null)
        {
            mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emission));
            mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            assigned++;
        }

        if (occlusion != null)
        {
            mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(occlusion));
            mat.SetFloat("_OcclusionStrength", 1f);
            assigned++;
        }

        if (assigned > 0) EditorUtility.SetDirty(mat);
        return assigned;
    }

    // Exact match, or "base_key" — avoids false positives like "ao" inside "halo".
    private bool MapNameMatches(string name, params string[] keys)
    {
        foreach (var k in keys)
            if (name == k || name.EndsWith("_" + k)) return true;
        return false;
    }

    // ──────────────────────────────────────────────────────────
    //  Pack separate Metallic / Roughness maps into a single URP
    //  MetallicSmoothness texture (R = metallic, A = 1 - roughness),
    //  written next to the source textures. Returns its asset path.
    // ──────────────────────────────────────────────────────────
    private string BuildMetallicSmoothnessMap(string texturesDir, string metallicPath, string roughnessPath, string smoothnessPath)
    {
        Color32[] metalPix = null, roughPix = null, smoothPix = null;
        int w = 0, h = 0;

        if (metallicPath != null && LoadRawPixels(metallicPath, out var mp, out var mw, out var mh))
        { metalPix = mp; w = mw; h = mh; }

        if (roughnessPath != null && LoadRawPixels(roughnessPath, out var rp, out var rw, out var rh))
        {
            if (w == 0) { roughPix = rp; w = rw; h = rh; }
            else if (rw == w && rh == h) roughPix = rp;
        }

        if (smoothnessPath != null && LoadRawPixels(smoothnessPath, out var sp, out var sw, out var sh))
        {
            if (w == 0) { smoothPix = sp; w = sw; h = sh; }
            else if (sw == w && sh == h) smoothPix = sp;
        }

        if (w == 0 || h == 0) return null;

        var outPix = new Color32[w * h];
        for (int i = 0; i < outPix.Length; i++)
        {
            byte metal  = metalPix  != null ? metalPix[i].r : (byte)0;
            byte smooth = smoothPix != null ? smoothPix[i].r
                        : roughPix  != null ? (byte)(255 - roughPix[i].r)
                        : (byte)128;
            outPix[i] = new Color32(metal, 0, 0, smooth);
        }

        var packed = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        packed.SetPixels32(outPix);
        packed.Apply();
        var png = packed.EncodeToPNG();
        Object.DestroyImmediate(packed);

        var outPath = $"{texturesDir}/MetallicSmoothness.png";
        File.WriteAllBytes(outPath, png);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
        return outPath;
    }

    // Loads a texture's pixels straight from disk (bypassing the importer, so
    // the raw stored values are read regardless of sRGB/compression settings).
    private bool LoadRawPixels(string assetPath, out Color32[] pixels, out int width, out int height)
    {
        pixels = null; width = 0; height = 0;
        try
        {
            var bytes = File.ReadAllBytes(assetPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(tex, bytes)) { Object.DestroyImmediate(tex); return false; }
            width = tex.width; height = tex.height;
            pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            return true;
        }
        catch (System.Exception e)
        {
            if (debugMode) Debug.Log($"   ⚠ Could not read pixels from {assetPath}: {e.Message}");
            return false;
        }
    }

    // Metallic-smoothness / data maps must be sampled linearly, not as sRGB.
    private void EnsureLinearImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
    }
}
