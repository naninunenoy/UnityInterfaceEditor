using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;

// ref https://github.com/5argon/Minefield/blob/master/Editor/AssignIconTool.cs

public static class ScriptEditedCallback {

    static readonly MethodInfo SetIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo CopyMonoScriptIconToImporters = typeof(MonoImporter).GetMethod("CopyMonoScriptIconToImporters", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly Type T_Annotation = Type.GetType("UnityEditor.Annotation, UnityEditor");
    static readonly FieldInfo AnnotationClassId = T_Annotation.GetField("classID");
    static readonly FieldInfo AnnotationScriptClass = T_Annotation.GetField("scriptClass");
    static readonly Type AnnotationUtility = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
    static readonly MethodInfo GetAnnotations = AnnotationUtility.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
    static readonly MethodInfo SetIconEnabled = AnnotationUtility.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

    [DidReloadScripts(0)]
    private static void OnDidReloadScripts() {
        // overwrite icon of IXXXXX.cs
        var icon = EditorGUIUtility.Load("interface-icon.png") as Texture2D;
        // MonoScript (expept class)
        var notClassScripts = MonoImporter
            .GetAllRuntimeMonoScripts()
            .Where(x => { return x.GetClass() == null;  });
        // pick up IXXXXX.cs
        var interfaceNameList = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith("Assets/", StringComparison.CurrentCulture))
            .Where(x => x.EndsWith(".cs", StringComparison.CurrentCulture))
            .Where(x => x != "Assets/Editor/ScriptEditedCallback.cs") // expept me
            .Where(x => IsInterfaceFile(File.ReadAllText(x)))
            .Select(x => new FileInfo(x).Name.Split('.')[0]);
        // compare MonoScript[] and interface file name list
        var editedScripts = new List<MonoScript>();
        foreach (string interfaceName in interfaceNameList) {
            var s = notClassScripts.FirstOrDefault(x => x.name == interfaceName);
            if (s != null) {
                // if match interface, overwrite icon
                SetIconForObject.Invoke(null, new object[] { s, icon });
                CopyMonoScriptIconToImporters.Invoke(null, new object[] { s });
                editedScripts.Add(s);
            }
        }
        //Disable the icon in gizmos annotation.
        Array annotations = (Array)GetAnnotations.Invoke(null, null);
        foreach (var bc in editedScripts) {
            foreach (var a in annotations) {
                string scriptClass = (string)AnnotationScriptClass.GetValue(a);
                if (scriptClass == bc.name) {
                    int classId = (int)AnnotationClassId.GetValue(a);
                    SetIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });
                }
            }
        }
    }

    private static bool IsInterfaceFile(string fullText) {
        return fullText.Contains("interface I");
    }
}
#endif
