using UnityEditor;
using UnityEngine;

public class ForceRefresh
{
    [MenuItem("Tools/Force Refresh AssetDatabase")]
    public static void Refresh()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[ForceRefresh] AssetDatabase refreshed!");
    }
}
