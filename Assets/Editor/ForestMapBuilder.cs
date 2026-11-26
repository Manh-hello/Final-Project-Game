using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the forest from the existing artwork, with separate reusable sprite/prefab assets.</summary>
public static class ForestMapBuilder
{
    const string Atlas = "Assets/Sprites/Environment/ForestAtlas.png";
    const string Prefabs = "Assets/Prefabs/Environment/Forest";
    const string RootName = "ForestMap_Designed";
    static readonly string[] Names = ("Tree_Ancient Tree_Round Canopy_Hanging Tree_Tall Tree_Young Vine_Loop Vine_Short Vine_Long Vine_Arch Leaves_Cluster Leaves_Broad Fern_Large Bush_Large Shrub_Large Flower_RedTall Flower_OrangeTall Flower_Purple Flower_Blue Bush_RockMoss Flower_Pink Flower_Yellow Bush_Round Grass_Tall Bush_Moss Bush_Small Bush_Wide Grass_Tuft Rock_Small Grass_Sprout Grass_Low Rock_Tower Rock_Large Rock_Pointed Log_Fallen Rock_Tall Stump_Old Rock_RoundA Rock_RoundB Rock_RoundC Mushroom_BlueLarge Mushroom_PurpleTall Lotus_Large Mushroom_PurpleCluster Flower_MagicBush Crystal_CyanA Flower_MagicTall Mushroom_Orange Mushroom_BlueSmall Crystal_CyanB Mushroom_PurpleSmall Crystal_Pink Plant_Moss Mushroom_OrangeSmall Flower_MagicSmall Waterfall_Rocks Waterfall_Stream Pond_Forest Sparkle_CyanA Sparkle_CyanB Sparkle_PinkA Sparkle_BlueA Water_Splash Sparkle_TinyCyan Sparkle_PinkB Water_Ripple Sparkle_BlueB Sparkle_CyanSmall Sparkle_CyanTiny Sparkle_BlueSmall Sparkle_PinkSmall Sparkle_Purple Sparkle_PinkC Sparkle_BlueTiny Ground_Pillar Ground_SlopeDown Ground_SlopeUp Ground_Long Ground_Short").Split(' ');
    sealed class Piece { public int label; public RectInt rect; public List<int> pixels = new List<int>(); public string name; public GUID guid; }
    static Dictionary<string, GameObject> prefabs;
    static Material unlit;
    static System.Random rng;
    static int instances;

    [MenuItem("Tools/Forest Map/Build in GamePlayMap1")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GamePlayMap1.unity" || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open GamePlayMap1 in Edit mode first.");
        if (scene.GetRootGameObjects().Any(g => g.name == RootName))
            throw new InvalidOperationException("The designed map already exists. Move or rename it before building another.");
        Folder("Assets/Sprites/Environment"); Folder(Prefabs); Folder("Assets/Materials/Environment");
        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas) && !AssetDatabase.CopyAsset("Assets/Sprites/Items/map1_resources.png", Atlas))
            throw new InvalidOperationException("Could not copy the forest atlas.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
        int[] labels;
        var pieces = Scan(texture, out labels);
        if (pieces.Count != Names.Length) throw new InvalidOperationException("Expected " + Names.Length + " pieces, found " + pieces.Count);
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i]; p.name = Names[i]; p.guid = GUID.Generate();
            rects.Add(new SpriteRect { name = p.name, rect = new Rect(p.rect.x, p.rect.y, p.rect.width, p.rect.height), pivot = new Vector2(0.5f, 0), alignment = SpriteAlignment.BottomCenter, spriteID = p.guid });
        }
        provider.SetSpriteRects(rects.ToArray());
        var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider.SetNameFileIdPairs(pieces.Select(p => new SpriteNameFileIdPair(p.name, p.guid)));
        var outlineProvider = provider.GetDataProvider<ISpriteOutlineDataProvider>();
        foreach (var piece in pieces)
            outlineProvider.SetOutlines(piece.guid, Outlines(piece, labels, texture.width, texture.height));
        provider.Apply(); importer.SaveAndReimport();
        importer.isReadable = false; importer.SaveAndReimport();
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Forest_Unlit.mat");
        if (!unlit) { unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")); AssetDatabase.CreateAsset(unlit, "Assets/Materials/Environment/Forest_Unlit.mat"); }
        prefabs = new Dictionary<string, GameObject>();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>()) MakePrefab(sprite);
        rng = new System.Random(1427); instances = 0;
        var root = new GameObject(RootName);
        var distant = Group(root.transform, "01_DistantForest_NoCollision");
        var middle = Group(root.transform, "02_Midground_NoCollision");
        var ground = Group(root.transform, "03_Ground_WithColliders");
        var details = Group(root.transform, "04_Decorations_NoCollision");
        var magic = Group(root.transform, "05_WaterAndFireflies");
        var markers = Group(root.transform, "06_GameplayMarkers");
        for (int i = 0; i < 34; i++)
        {
            float x = -13 + i * 3.8f;
            var t = Place(i % 3 == 0 ? "Tree_Ancient" : "Tree_Tall", distant, x, -1.1f + Rand(0, 0.6f), Rand(2.2f, 3.6f), -35);
            t.GetComponent<SpriteRenderer>().color = new Color(0.13f, 0.31f, 0.34f, 0.48f);
        }
        for (int i = 0; i < 25; i++)
        {
            var t = Place(i % 2 == 0 ? "Tree_Round" : "Tree_Young", middle, -8 + i * 4.8f, -0.6f, Rand(1.7f, 2.8f), -25);
            t.GetComponent<SpriteRenderer>().color = new Color(0.33f, 0.55f, 0.52f, 0.64f);
        }
        // Four connected stretches, with short jump gaps and stepping stones at the stream.
        Stretch(ground, -5, 25, 0); Stretch(ground, 27, 52, 0);
        Stretch(ground, 54, 83, 0); Stretch(ground, 85, 107, 0);
        Platform(ground, "Ground_Short", 25.7f, 0.45f, 0.7f);
        Platform(ground, "Ground_Pillar", 52.8f, 0.65f, 0.85f);
        Platform(ground, "Ground_Short", 83.9f, 0.3f, 0.65f);
        float[] px = { 11, 16.7f, 22, 33, 38.5f, 44, 59, 64.8f, 70.5f, 76, 91, 97 };
        float[] py = { 1.8f, 3.1f, 1.9f, 1.8f, 3.3f, 2.1f, 1.7f, 3, 4.4f, 2.2f, 1.9f, 3.4f };
        for (int i = 0; i < px.Length; i++)
        {
            Platform(ground, i % 3 == 1 ? "Ground_SlopeUp" : "Ground_Short", px[i], py[i], i % 3 == 1 ? 0.8f : 1.3f);
            Place(i % 2 == 0 ? "Flower_Blue" : "Mushroom_PurpleSmall", details, px[i] - 0.45f, py[i] + 0.02f, 0.65f, 5);
            if (i > 5 && i < 10) Place("Crystal_CyanA", details, px[i] + 0.7f, py[i], 0.65f, 4);
        }
        // Landmark trees frame clearings without blocking the visible walking surface.
        float[] trees = { -2.5f, 5, 19, 29, 46.5f, 57, 79.5f, 88, 103 };
        for (int i = 0; i < trees.Length; i++)
        {
            Place(i % 3 == 0 ? "Tree_Ancient" : "Tree_Round", details, trees[i], 0.04f, i % 3 == 0 ? 1.75f : 1.6f, -4, i % 2 == 0);
            Place("Bush_Large", details, trees[i] + 1.1f, 0, 0.8f, -2);
        }
        string[] plants = { "Grass_Tuft", "Grass_Sprout", "Grass_Low", "Bush_Small", "Flower_Yellow", "Flower_Pink", "Flower_Blue", "Fern_Large", "Mushroom_OrangeSmall", "Rock_RoundA" };
        for (int i = 0; i < 120; i++)
        {
            float x = Rand(-4, 105); if (InGap(x)) continue;
            Place(plants[i % plants.Length], details, x, Rand(-0.07f, 0.02f), Rand(0.55f, 1.05f), i % 3 == 0 ? 7 : -1, i % 2 == 0);
        }
        Place("Log_Fallen", details, 8.4f, 0.03f, 0.9f, -2);
        Place("Stump_Old", details, 21.9f, 0.01f, 1, -2);
        Place("Rock_Tower", details, 36.5f, 0, 1.65f, -6);
        Place("Waterfall_Rocks", magic, 36.6f, 0.1f, 2.3f, -5);
        Place("Waterfall_Stream", magic, 39.4f, 0.02f, 2, -4);
        Place("Pond_Forest", magic, 40, 0.04f, 1.65f, 2);
        Place("Water_Splash", magic, 39.4f, 0.22f, 0.75f, 3);
        Place("Water_Ripple", magic, 41.3f, 0.2f, 1, 4);
        for (int i = 0; i < 15; i++)
        {
            float x = 56 + i * 1.55f;
            Place(i % 3 == 0 ? "Crystal_Pink" : i % 3 == 1 ? "Crystal_CyanA" : "Crystal_CyanB", details, x, 0.02f, Rand(0.65f, 1.3f), -2);
            Place(i % 2 == 0 ? "Mushroom_BlueLarge" : "Mushroom_PurpleCluster", details, x + 0.55f, 0.02f, Rand(0.5f, 0.9f), 4);
        }
        Place("Lotus_Large", details, 98, 0.03f, 1.6f, 3);
        Place("Crystal_CyanA", details, 101, 0.02f, 2, -2);
        Place("Crystal_Pink", details, 102.5f, 0.01f, 1.4f, -1);
        for (int i = 0; i < 52; i++)
        {
            Place(i % 4 == 0 ? "Sparkle_PinkA" : i % 4 == 1 ? "Sparkle_CyanA" : i % 4 == 2 ? "Sparkle_BlueA" : "Sparkle_CyanSmall", magic, Rand(-1, 104), Rand(0.6f, 5.8f), Rand(0.3f, 0.75f), 12);
        }
        for (int i = 0; i < 14; i++)
        {
            var canopy = Place("Canopy_Hanging", details, -4 + i * 8.3f, 6.1f + Rand(0, 1.5f), Rand(1.6f, 2.1f), 15, i % 2 == 0);
            canopy.GetComponent<SpriteRenderer>().color = new Color(0.53f, 0.72f, 0.60f, 0.94f);
            if (i % 2 == 0) Place("Vine_Arch", details, -3 + i * 8.3f, 5.4f, 1.2f, 14);
        }
        // Use the remaining pieces in small botanical clusters so every prefab has an example.
        var used = new HashSet<string>(root.GetComponentsInChildren<SpriteRenderer>().Select(r => r.sprite.name));
        int botanical = 0;
        foreach (var name in Names.Where(n => !used.Contains(n) && !n.StartsWith("Ground_")))
        {
            float x = 2 + (botanical * 7.13f) % 99;
            if (InGap(x)) x += 2;
            bool hanging = name.StartsWith("Vine_");
            bool sparkle = name.StartsWith("Sparkle_");
            Place(name, sparkle ? magic : details, x, hanging ? 5.8f : sparkle ? 2.8f : 0.02f, sparkle ? 0.65f : 0.85f, sparkle ? 12 : -1);
            botanical++;
        }
        Platform(ground, "Ground_SlopeDown", 49, 1.6f, 0.8f);
        Marker(markers, "PlayerSpawn", new Vector3(1, 0.7f, 0));
        Marker(markers, "Checkpoint_Stream", new Vector3(42, 0.7f, 0));
        Marker(markers, "Checkpoint_Crystals", new Vector3(72, 0.7f, 0));
        Marker(markers, "LevelExit", new Vector3(102, 0.7f, 0));
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Environment");
        if (old) { Undo.RecordObject(old, "Preserve earlier layout"); old.SetActive(false); }
        var camera = scene.GetRootGameObjects().Select(g => g.GetComponent<Camera>()).FirstOrDefault(c => c);
        if (!camera) throw new InvalidOperationException("Main camera missing.");
        Undo.RecordObject(camera.transform, "Frame forest entrance"); Undo.RecordObject(camera, "Frame forest entrance");
        camera.orthographic = true; camera.orthographicSize = 5.1f;
        camera.transform.position = new Vector3(5.5f, 2.65f, -10);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.10f, 0.23f, 0.28f, 1);
        var follow = camera.GetComponent<ForestCameraFollow>(); if (!follow) follow = Undo.AddComponent<ForestCameraFollow>(camera.gameObject);
        follow.target = null;
        EditorUtility.SetDirty(camera); EditorUtility.SetDirty(follow);
        Undo.RegisterCreatedObjectUndo(root, "Create designed forest map");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (SceneView.lastActiveSceneView) { SceneView.lastActiveSceneView.in2DMode = true; SceneView.lastActiveSceneView.LookAt(new Vector3(49, 2, 0), Quaternion.identity, 58); }
        Selection.activeGameObject = root;
        Debug.Log("Forest map created: " + pieces.Count + " sprites, " + prefabs.Count + " prefabs, " + instances + " instances. Existing Environment preserved inactive.");
    }

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); Folder(path.Substring(0, slash)); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static List<Piece> Scan(Texture2D texture, out int[] labels)
    {
        int w = texture.width, h = texture.height;
        var colors = texture.GetPixels32(); labels = new int[colors.Length];
        var result = new List<Piece>(); var queue = new Queue<int>(); int label = 0;
        // Scan top-to-bottom to match the artist's rows.
        for (int y = h - 1; y >= 0; y--) for (int x = 0; x < w; x++)
        {
            int seed = y * w + x; if (labels[seed] != 0 || colors[seed].a < 35) continue;
            var p = new Piece { label = ++label }; queue.Enqueue(seed); labels[seed] = label;
            int minx = w, miny = h, maxx = 0, maxy = 0;
            while (queue.Count != 0)
            {
                int index = queue.Dequeue(), cx = index % w, cy = index / w; p.pixels.Add(index);
                minx = Math.Min(minx, cx); miny = Math.Min(miny, cy); maxx = Math.Max(maxx, cx); maxy = Math.Max(maxy, cy);
                foreach (int d in new[] { -1, 1, -w, w })
                {
                    int next = index + d;
                    if (next < 0 || next >= colors.Length || Math.Abs(next % w - cx) > 1 || labels[next] != 0 || colors[next].a < 35) continue;
                    labels[next] = label; queue.Enqueue(next);
                }
            }
            if (p.pixels.Count < 45 || maxx - minx < 5 || maxy - miny < 5) continue;
            int left = Math.Max(0, minx - 2), bottom = Math.Max(0, miny - 2);
            p.rect = new RectInt(left, bottom, Math.Min(w, maxx + 3) - left, Math.Min(h, maxy + 3) - bottom); result.Add(p);
        }
        return result;
    }

    // Follow each component's silhouette, excluding neighboring artwork even where bounding boxes overlap.
    static List<Vector2[]> Outlines(Piece piece, int[] labels, int w, int h)
    {
        var edges = new Dictionary<int, List<int>>(); int stride = w + 1;
        Action<int, int> edge = (a, b) => { if (!edges.ContainsKey(a)) edges[a] = new List<int>(); edges[a].Add(b); };
        foreach (int index in piece.pixels)
        {
            int x = index % w, y = index / w, a = y * stride + x;
            if (y == 0 || labels[index - w] != piece.label) edge(a, a + 1);
            if (x == w - 1 || labels[index + 1] != piece.label) edge(a + 1, a + stride + 1);
            if (y == h - 1 || labels[index + w] != piece.label) edge(a + stride + 1, a + stride);
            if (x == 0 || labels[index - 1] != piece.label) edge(a + stride, a);
        }
        var loops = new List<List<Vector2>>();
        while (edges.Count > 0)
        {
            int start = edges.Keys.First(), current = start; var loop = new List<Vector2>();
            do
            {
                loop.Add(new Vector2(current % stride, current / stride));
                List<int> outgoing; if (!edges.TryGetValue(current, out outgoing)) break;
                int next = outgoing[outgoing.Count - 1]; outgoing.RemoveAt(outgoing.Count - 1); if (outgoing.Count == 0) edges.Remove(current); current = next;
            } while (current != start && loop.Count <= piece.pixels.Count * 4);
            if (loop.Count > 3) loops.Add(loop);
        }
        if (loops.Count == 0) return new List<Vector2[]>();
        var outline = loops.OrderByDescending(l => l.Count).First();
        // Remove grid-collinear points, then simplify the silhouette to keep import/mesh costs bounded.
        var corners = new List<Vector2>();
        for (int i = 0; i < outline.Count; i++)
        {
            var a = outline[(i + outline.Count - 1) % outline.Count]; var b = outline[i]; var c = outline[(i + 1) % outline.Count];
            if (Vector2.Dot((b - a).normalized, (c - b).normalized) < 0.999f) corners.Add(b);
        }
        var simple = new List<Vector2>();
        if (corners.Count > 3)
        {
            corners.Add(corners[0]); Simplify(corners, 0, corners.Count - 1, 1.1f, simple);
        }
        if (simple.Count < 3) simple = outline;
        Vector2 center = new Vector2(piece.rect.x + piece.rect.width * 0.5f, piece.rect.y + piece.rect.height * 0.5f);
        return new List<Vector2[]> { simple.Select(v => v - center).ToArray() };
    }

    static void Simplify(List<Vector2> points, int first, int last, float tolerance, List<Vector2> output)
    {
        float max = 0; int split = -1; var a = points[first]; var b = points[last]; var line = b - a;
        for (int i = first + 1; i < last; i++)
        {
            float t = line.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(points[i] - a, line) / line.sqrMagnitude) : 0;
            float distance = Vector2.Distance(points[i], a + t * line); if (distance > max) { max = distance; split = i; }
        }
        if (split >= 0 && max > tolerance) { Simplify(points, first, split, tolerance, output); Simplify(points, split, last, tolerance, output); }
        else output.Add(a);
    }

    static void MakePrefab(Sprite sprite)
    {
        string category = sprite.name.StartsWith("Ground_") ? "Ground" : sprite.name.StartsWith("Water") || sprite.name.StartsWith("Pond") || sprite.name.StartsWith("Sparkle") ? "Effects" : "Decorations";
        Folder(Prefabs + "/" + category);
        var go = new GameObject(sprite.name); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = unlit;
        if (category == "Ground")
        {
            float w = sprite.rect.width / 100f, h = sprite.rect.height / 100f;
            if (sprite.name.Contains("Slope"))
            {
                var collider = go.AddComponent<PolygonCollider2D>(); float left = sprite.name.EndsWith("Up") ? h * 0.42f : h * 0.85f, right = sprite.name.EndsWith("Up") ? h * 0.9f : h * 0.4f;
                collider.points = new[] { new Vector2(-w * 0.45f, h * 0.18f), new Vector2(w * 0.45f, h * 0.18f), new Vector2(w * 0.45f, right), new Vector2(-w * 0.45f, left) };
            }
            else
            {
                var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(w * 0.95f, h * 0.48f); collider.offset = new Vector2(0, h * 0.61f);
            }
        }
        else if (sprite.name.StartsWith("Sparkle_") || sprite.name.StartsWith("Water_"))
        {
            var ambient = go.AddComponent<ForestAmbient>(); ambient.floatVertically = sprite.name.StartsWith("Sparkle_");
            ambient.phase = Array.IndexOf(Names, sprite.name) * 1.7f;
        }
        string path = Prefabs + "/" + category + "/" + sprite.name + ".prefab";
        prefabs[sprite.name] = PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go);
    }

    static Transform Group(Transform parent, string name) { var g = new GameObject(name); g.transform.SetParent(parent, false); return g.transform; }
    static float Rand(float min, float max) { return min + (float)rng.NextDouble() * (max - min); }
    static bool InGap(float x) { return (x > 25 && x < 27) || (x > 52 && x < 54) || (x > 83 && x < 85); }
    static GameObject Place(string name, Transform parent, float x, float y, float scale, int order, bool flip = false)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[name], parent); go.transform.localPosition = new Vector3(x, y, 0); go.transform.localScale = new Vector3(scale, scale, 1);
        var sr = go.GetComponent<SpriteRenderer>(); sr.sortingOrder = order; sr.flipX = flip; instances++; return go;
    }
    static void Platform(Transform parent, string name, float x, float top, float scale)
    {
        float height = prefabs[name].GetComponent<SpriteRenderer>().sprite.rect.height / 100f;
        Place(name, parent, x, top - height * scale * 0.85f, scale, 0);
    }
    static void Stretch(Transform parent, float start, float end, float top)
    {
        float nominal = prefabs["Ground_Long"].GetComponent<SpriteRenderer>().sprite.rect.width / 100f * 1.5f * 0.95f;
        int count = Mathf.CeilToInt((end - start) / nominal); float width = (end - start) / count;
        for (int i = 0; i < count; i++)
        {
            float sourceWidth = prefabs["Ground_Long"].GetComponent<SpriteRenderer>().sprite.rect.width / 100f;
            var go = Place("Ground_Long", parent, start + width * (i + 0.5f), top - prefabs["Ground_Long"].GetComponent<SpriteRenderer>().sprite.rect.height / 100f * 1.5f * 0.85f, 1.5f, 0);
            go.transform.localScale = new Vector3((width + 0.04f) / (sourceWidth * 0.95f), 1.5f, 1);
        }
    }
    static void Marker(Transform parent, string name, Vector3 position) { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = position; }
}
