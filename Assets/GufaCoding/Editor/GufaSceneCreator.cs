using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GufaCoding.EditorTools
{
    public static class GufaSceneCreator
    {
        const string ScenePath = "Assets/GufaCoding/Scenes/GufaMain.unity";

        [MenuItem("古法编程/创建游戏场景")]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1E, 0x1E, 0x1E, 0xFF);
            cam.orthographic = true;

            new GameObject("GufaGame").AddComponent<GufaGame>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[古法编程] 场景已创建：" + ScenePath + "，已设为构建第一个场景。点 Play 开玩。");
        }

        [MenuItem("古法编程/清空存档")]
        static void ResetSave()
        {
            SaveData.ResetAll(LevelDatabase.Levels.Length);
            Debug.Log("[古法编程] 存档已清空（工资、关卡、道具）。");
        }
    }
}
