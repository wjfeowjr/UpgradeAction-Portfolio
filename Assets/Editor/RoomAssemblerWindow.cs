// Assets/Editor/RoomAssemblerWindow.cs
// Map assembler supporting Ground/Platforms/Traps/Lasers + Transform markers (monsters/traps/props).
// - Reads: ground[], platforms[], traps[], lasers[]  (Tile layers)
// - Reads: transforms[] -> { "name": "<prefabOrMarkerName>", "grid": {"x":int,"y":int} }
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;

public class RoomAssemblerWindow : EditorWindow
{
    // -------- Data Types --------
    [System.Serializable] public class Vec2 { public float x; public float y; }
    [System.Serializable] public class Grid2 { public int x; public int y; }

    [System.Serializable] public class TileSpawn
    {
        public string id = "tileset_1_1"; // kept for compatibility
        public Grid2 grid;
    }

    [System.Serializable] public class TransformSpawn
    {
        public string name;
        public Grid2 grid;
    }

    [System.Serializable] public class RoomData
    {
        public string roomId = "A3";
        public Vec2 cellSize = new Vec2 { x = 1.28f, y = 1.28f };
        public Vec2 gridOrigin = new Vec2 { x = 0f, y = 0f };

        public TileSpawn[] ground;
        public TileSpawn[] platforms;
        public TileSpawn[] traps;
        public TileSpawn[] lasers;

        public TransformSpawn[] transforms; // monsters/traps/props as transform markers
    }

    // -------- UI Fields --------
    TextAsset roomJson;
    Tile groundTile;
    Tile platformTile;
    Tile trapTile;
    Tile laserTile;

    int uiOffsetX = 7;  // +right
    int uiOffsetY = -2; // +up (아래로는 음수)

    [MenuItem("Tools/Room Assembler (Tiles + Transforms)")]
    static void Open() => GetWindow<RoomAssemblerWindow>("Room Assembler+");

    void OnGUI()
    {
        roomJson    = (TextAsset)EditorGUILayout.ObjectField("RoomData JSON", roomJson, typeof(TextAsset), false);
        groundTile  = (Tile)EditorGUILayout.ObjectField("Ground Tile", groundTile, typeof(Tile), false);
        platformTile= (Tile)EditorGUILayout.ObjectField("Platform Tile", platformTile, typeof(Tile), false);
        trapTile    = (Tile)EditorGUILayout.ObjectField("Trap Tile", trapTile, typeof(Tile), false);
        laserTile   = (Tile)EditorGUILayout.ObjectField("Laser Tile", laserTile, typeof(Tile), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Global Grid Offset (baseline: +7, -2)", EditorStyles.boldLabel);
        uiOffsetX = EditorGUILayout.IntField("Offset X (+right)", uiOffsetX);
        uiOffsetY = EditorGUILayout.IntField("Offset Y (+up)", uiOffsetY);

        using (new EditorGUI.DisabledScope(roomJson == null || groundTile == null))
        {
            if (GUILayout.Button("Assemble Room Prefab")) Assemble();
        }

        EditorGUILayout.HelpBox("Also creates empty GameObjects for `transforms` at grid positions.\n" +
            "Use these as spawn markers (name is kept as GameObject name).", MessageType.Info);
    }

    void Assemble()
    {
        var data = JsonUtility.FromJson<RoomData>(roomJson.text);
        if (data == null) { EditorUtility.DisplayDialog("Room Assembler", "Invalid JSON.", "OK"); return; }

        var root = new GameObject($"Room_{data.roomId}");

        // Grid parent
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(data.cellSize.x, data.cellSize.y, 0f);

        // Make tilemap layers
        Tilemap MakeLayer(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(gridGO.transform, false);
            var tm = go.AddComponent<Tilemap>();
            var rd = go.AddComponent<TilemapRenderer>();
            rd.sortingOrder = sortingOrder;
            return tm;
        }
        var tmGround   = MakeLayer("Ground",    0);
        var tmPlatform = MakeLayer("Platforms", 1);
        var tmTrap     = MakeLayer("Traps",     2);
        var tmLaser    = MakeLayer("Lasers",    3);

        // Painter
        void Paint(Tilemap tm, Tile tile, TileSpawn[] arr)
        {
            if (arr == null || tile == null) return;
            foreach (var t in arr)
            {
                var cell = new Vector3Int(t.grid.x + uiOffsetX, t.grid.y + uiOffsetY, 0);
                tm.SetTile(cell, tile);
            }
        }
        Paint(tmGround,   groundTile,   data.ground);
        Paint(tmPlatform, platformTile, data.platforms);
        Paint(tmTrap,     trapTile,     data.traps);
        Paint(tmLaser,    laserTile,    data.lasers);

        // Transforms (spawn markers)
        if (data.transforms != null && data.transforms.Length > 0)
        {
            var tRoot = new GameObject("Transforms");
            tRoot.transform.SetParent(root.transform, false);

            foreach (var t in data.transforms)
            {
                var go = new GameObject(string.IsNullOrEmpty(t.name) ? "TransformMarker" : t.name);
                go.transform.SetParent(tRoot.transform, false);
                var pos = new Vector3((t.grid.x + uiOffsetX) * data.cellSize.x, (t.grid.y + uiOffsetY) * data.cellSize.y, 0f);
                go.transform.localPosition = pos;

                // Optional: small icon gizmo size hint via SpriteRenderer (not required)
                // var sr = go.AddComponent<SpriteRenderer>(); sr.color = new Color(1,1,1,0); // invisible
            }
        }

        // Save prefab
        var folder = $"Assets/Rooms/{data.roomId}";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        var path = $"{folder}/Room_{data.roomId}.prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Room Assembler", $"Saved: {path}\nOffset Applied: ({uiOffsetX}, {uiOffsetY})", "OK");
    }
}
#endif
