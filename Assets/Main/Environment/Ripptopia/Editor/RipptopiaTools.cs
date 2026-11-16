using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Use this if the Ripptopia FBX files were already imported in your project before this package.
// Select the FBX files (or a folder with them) in the Project window, then run
// Tools > Ripptopia > Link URP Materials To Selected FBX.
// Each FBX material slot is linked by name to the matching material in Ripptopia/Materials.
public static class RipptopiaTools
{
    [MenuItem("Tools/Ripptopia/Link URP Materials To Selected FBX")]
    static void LinkMaterials()
    {
        var mats = new Dictionary<string, Material>();
        foreach (var guid in AssetDatabase.FindAssets("t:Material M_"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.Contains("/Ripptopia/Materials/")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) mats[m.name] = m;
        }

        var paths = new List<string>();
        foreach (var obj in Selection.objects)
        {
            var p = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(p))
                foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { p })) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            else if (p.ToLower().EndsWith(".fbx")) paths.Add(p);
        }

        int count = 0;
        foreach (var p in paths)
        {
            var mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (mi == null) continue;
            foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            mi.SaveAndReimport();
            count++;
        }
        Debug.Log("Ripptopia: linked " + mats.Count + " materials on " + count + " FBX files.");
    }
}
