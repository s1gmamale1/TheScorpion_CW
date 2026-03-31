using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public class NavMeshBaker
{
    [MenuItem("Tools/Scorpion/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
            Debug.Log($"NavMesh baked on: {surface.gameObject.name}");
        }
        Debug.Log($"NavMesh bake complete. {surfaces.Length} surface(s) processed.");
    }
}
