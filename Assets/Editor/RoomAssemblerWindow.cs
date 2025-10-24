// Assets/Editor/RoomAssemblerWindow.cs
// Map-only assembler with Platform/Trap/Laser tile layers + global grid offset.
// - Ground:      Tilemap "Ground"     (solid terrain)
// - Platform:    Tilemap "Platforms"  (one-way/bridge style; collider/effector는 프로젝트에서 추가)
// - Trap:        Tilemap "Traps"      (wall/floor traps visual)
// - Laser:       Tilemap "Lasers"     (vertical laser base/marker visual)
// JSON: ground[], platforms[], traps[], lasers[] — all use grid coords {x,y}. id is kept for future use.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

public class RoomAssemblerWindow : EditorWindow
{
    // -------- Data Types --------
    [System.Serializable] public class Vec2 { public float x; public float y; }
    [System.Serializable] public class Grid2 { public int x; public int y; }

    [System.Serializable] public class TileSpawn
    {
        public string id = "tileset_1_1"; // kept for compatibility (ignored by painter)
        public Grid2 grid;
    }

    [System.Serializable] public class RoomData
    {
        public string roomId = "A2";
        public Vec2 cellSize = new Vec2 { x = 1.28f, y = 1.28f };
        public Vec2 gridOrigin = new Vec2 { x = 0f, y = 0f };
        public bool addCompositeCollider = false;

        public TileSpawn[] ground;     // solid terrain
        public TileSpawn[] platforms;  // platform tiles
        public TileSpawn[] traps;      // floor/wall traps
        public TileSpawn[] lasers;     // vertical laser bases
    }

    // -------- UI Fields --------
    TextAsset roomJson;
    Tile groundTile;    // tileset_1_1 (solid)
    Tile platformTile;  // visual for platform
    Tile trapTile;      // visual for spikes etc.
    Tile laserTile;     // visual for laser base/marker

    // Default baseline offset: +X=7 (right), +Y=-2 (down)
    int uiOffsetX = 7;
    int uiOffsetY = -2;

    [MenuItem("Tools/Room Assembler (Map+Platform+Trap+Laser)")]
    static void Open() => GetWindow<RoomAssemblerWindow>("Room Assembler+");

    void OnGUI()
    {
        roomJson    = (TextAsset)EditorGUILayout.ObjectField("RoomData JSON", roomJson, typeof(TextAsset), false);
        groundTile  = (Tile)EditorGUILayout.ObjectField("Ground Tile (tileset_1_1)", groundTile, typeof(Tile), false);
        platformTile= (Tile)EditorGUILayout.ObjectField("Platform Tile", platformTile, typeof(Tile), false);
        trapTile    = (Tile)EditorGUILayout.ObjectField("Trap Tile", trapTile, typeof(Tile), false);
        laserTile   = (Tile)EditorGUILayout.ObjectField("Laser Tile", laserTile, typeof(Tile), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Global Grid Offset (baseline: +7 right, -2 down)", EditorStyles.boldLabel);
        uiOffsetX = EditorGUILayout.IntField("Offset X (+right)", uiOffsetX);
        uiOffsetY = EditorGUILayout.IntField("Offset Y (+up / 아래는 음수)", uiOffsetY);

        using (new EditorGUI.DisabledScope(roomJson == null || groundTile == null))
        {
            if (GUILayout.Button("Assemble Room Prefab"))
                Assemble();
        }

        EditorGUILayout.HelpBox("Reads grid coords and paints multiple tile layers. Add your own colliders/effectors at runtime as needed.", MessageType.Info);
    }

    void Assemble()
    {
        var data = JsonUtility.FromJson<RoomData>(roomJson.text);
        if (data == null) { EditorUtility.DisplayDialog("Room Assembler", "Invalid JSON.", "OK"); return; }

        var root = new GameObject($"Room_{data.roomId}");

        // Grid
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(data.cellSize.x, data.cellSize.y, 0);

        // Create tilemap layer helper
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

        // painter
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
