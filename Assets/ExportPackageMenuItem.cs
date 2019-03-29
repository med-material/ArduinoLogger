#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExportPackageMenuItem : MonoBehaviour
{
    [MenuItem("Tools/Export package")]
    public static void ExportPackage()
    {
        string[] projectContent = new string[] {"Assets/ConnectToArduino"};
        AssetDatabase.ExportPackage(projectContent, "ConnectToArduino.unitypackage",ExportPackageOptions.Interactive | ExportPackageOptions.Recurse |ExportPackageOptions.IncludeDependencies);
        Debug.Log("Project Exported");
    }
}
#endif