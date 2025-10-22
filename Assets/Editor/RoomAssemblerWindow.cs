// Assets/Editor/RoomAssemblerWindow.cs
// Map-only assembler with global GRID offset defaults: right +7, down -2.
// Paints tiles from tileSpawns(grid). No Spawners/Exits.
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

    [System.Serializable] public class TileSpawnDef
    {
        public string id;   // always "tileset_1_1"
        public Grid2 grid;  // original grid coords (pre-offset)
    }

    [System.Serializable] public class RoomData
    {
        public string roomId = "A2";
        public Vec2 cellSize = new Vec2 { x = 1.28f, y = 1.28f };
        public Vec2 gridOrigin = new Vec2 { x = 0f, y = 0f };
        public bool addCompositeCollider = false;
        public TileSpawnDef[] tileSpawns;

        // optional per-file override; if null, tool UI defaults are used
        public Grid2 globalOffsetGrid = null; // e.g., {x:7, y:-2}
    }

    // -------- UI Fields --------
    TextAsset roomJson;
    Tile tileSample; // assign a Tile asset using your tileset_1_1 sprite

    // Default baseline offset: right +7, down -2
    int uiOffsetX;
    int uiOffsetY;

    [MenuItem("Tools/Room Assembler (Map-Only, Offset)")]
    static void Open() => GetWindow<RoomAssemblerWindow>("Room Assembler (Map-Only, Offset)");

    void OnGUI()
    {
        roomJson = (TextAsset)EditorGUILayout.ObjectField("RoomData JSON", roomJson, typeof(TextAsset), false);
        tileSample = (Tile)EditorGUILayout.ObjectField("Tile (tileset_1_1)", tileSample, typeof(Tile), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Global Grid Offset (baseline: {uiOffsetX} right, {uiOffsetY} down)", EditorStyles.boldLabel);
        uiOffsetX = EditorGUILayout.IntField("Offset X (+right)", uiOffsetX);
        uiOffsetY = EditorGUILayout.IntField("Offset Y (+up)", uiOffsetY);

        using (new EditorGUI.DisabledScope(roomJson == null || tileSample == null))
        {
            if (GUILayout.Button("Assemble Room Prefab"))
                Assemble();
        }

        EditorGUILayout.HelpBox("tileSpawns.grid are shifted by global offset before painting. No spawners/exits created.", MessageType.Info);
    }

    void Assemble()
    {
        var data = JsonUtility.FromJson<RoomData>(roomJson.text);
        if (data == null) { EditorUtility.DisplayDialog("Room Assembler", "Invalid JSON.", "OK"); return; }
        if (data.tileSpawns == null || data.tileSpawns.Length == 0) { EditorUtility.DisplayDialog("Room Assembler", "No tileSpawns in JSON.", "OK"); return; }

        // resolve offset
        int offX = uiOffsetX;
        int offY = uiOffsetY;
        // if (data.globalOffsetGrid != null)
        // {
        //     offX = data.globalOffsetGrid.x;
        //     offY = data.globalOffsetGrid.y;
        // }

        // Root
        var root = new GameObject($"Room_{data.roomId}");

        // Grid + Tilemap
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(data.cellSize.x, data.cellSize.y, 0);

        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(gridGO.transform, false);
        var tilemap = groundGO.AddComponent<Tilemap>();
        var renderer = groundGO.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 0;

        // Paint tiles from grid coords with global offset
        foreach (var t in data.tileSpawns)
        {
            if (t == null) continue;
            int gx = t.grid.x + offX;
            int gy = t.grid.y + offY;
            var cellPos = new Vector3Int(gx, gy, 0);
            tilemap.SetTile(cellPos, tileSample);
        }

        // Save prefab
        var folder = $"Assets/Rooms/{data.roomId}";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        var path = $"{folder}/Room_{data.roomId}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Room Assembler", $"Saved: {path}\nOffset Applied: ({offX}, {offY})", "OK");
    }
}
#endif
