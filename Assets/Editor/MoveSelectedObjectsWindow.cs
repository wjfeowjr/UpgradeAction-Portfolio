// Assets/Editor/MoveSelectedObjectsWindow.cs
// Hierarchy에서 선택한 오브젝트들을 X/Y축으로 일괄 이동시키는 에디터 툴
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MoveSelectedObjectsWindow : EditorWindow
{
    private float moveX = 0f;
    private float moveY = 0f;

    [MenuItem("Tools/Move Selected Objects")]
    private static void Open() => GetWindow<MoveSelectedObjectsWindow>("Move Selected");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("선택한 오브젝트 일괄 이동", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        moveX = EditorGUILayout.FloatField("X 이동 거리", moveX);
        moveY = EditorGUILayout.FloatField("Y 이동 거리", moveY);

        EditorGUILayout.Space();

        var targets = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
        EditorGUILayout.LabelField($"선택된 오브젝트: {targets.Length}개");

        using (new EditorGUI.DisabledScope(targets.Length == 0 || (moveX == 0f && moveY == 0f)))
        {
            if (GUILayout.Button($"이동 ({moveX:+0.##;-0.##;0}, {moveY:+0.##;-0.##;0})"))
                Move(targets, new Vector3(moveX, moveY, 0f));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("방향 버튼 (위 거리의 절댓값만큼 이동)", EditorStyles.miniBoldLabel);

        using (new EditorGUI.DisabledScope(targets.Length == 0))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ -X")) Move(targets, new Vector3(-Mathf.Abs(moveX), 0f, 0f));
                if (GUILayout.Button("+X ▶")) Move(targets, new Vector3(Mathf.Abs(moveX), 0f, 0f));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▼ -Y")) Move(targets, new Vector3(0f, -Mathf.Abs(moveY), 0f));
                if (GUILayout.Button("+Y ▲")) Move(targets, new Vector3(0f, Mathf.Abs(moveY), 0f));
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("부모와 자식을 함께 선택하면 최상위 부모만 이동합니다 (중복 이동 방지).\nCtrl+Z로 되돌릴 수 있습니다.", MessageType.Info);
    }

    private void Move(Transform[] targets, Vector3 delta)
    {
        if (delta == Vector3.zero)
            return;

        foreach (var target in targets)
        {
            Undo.RecordObject(target, "Move Selected Objects");
            target.position += delta;
            EditorUtility.SetDirty(target);
        }

        // 프리팹 편집 모드든 씬이든 현재 스테이지를 더티 표시
        EditorSceneManager.MarkSceneDirty(targets[0].gameObject.scene);
        Debug.Log($"[Move Selected] {targets.Length}개 오브젝트를 ({delta.x}, {delta.y})만큼 이동");
    }

    // 선택이 바뀔 때 창의 오브젝트 개수 표시를 갱신
    private void OnSelectionChange() => Repaint();
}
#endif
