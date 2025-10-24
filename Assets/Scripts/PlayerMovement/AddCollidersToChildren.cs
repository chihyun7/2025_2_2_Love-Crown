using UnityEngine;
using UnityEngine.SceneManagement; // ✅ 꼭 있어야 함
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AddCollidersToChildren : MonoBehaviour
{
    [Header("▶ 하위 모든 MeshFilter에 MeshCollider 자동 추가")]
    public bool convex = false;

    private void Awake()
    {
        AddColliders();

        // 씬 전환 시에도 자동 재적용
        SceneManager.sceneLoaded += OnSceneLoaded; // ✅ 여기서 함수 연결
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ 이 함수가 반드시 위의 클래스 내부에 존재해야 함!
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddColliders();
    }

    public void AddColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        int addedCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            MeshCollider mc = mf.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convex;
                addedCount++;
            }
        }

        Debug.Log($"✅ {addedCount}개의 MeshCollider 추가 완료 ({gameObject.name})");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AddCollidersToChildren))]
public class AddCollidersToChildrenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AddCollidersToChildren script = (AddCollidersToChildren)target;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("이 버튼을 누르면 모든 자식 오브젝트에 MeshCollider가 자동 추가됩니다.\n(Play 중에도 자동 적용됨)", MessageType.Info);

        if (GUILayout.Button("💥 Add Colliders To All Children (수동 실행)"))
        {
            script.AddColliders();
        }
    }
}
#endif
