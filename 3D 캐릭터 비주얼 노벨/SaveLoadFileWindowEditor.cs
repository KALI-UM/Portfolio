using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SaveLoadFileWindowEditor : EditorWindow
{
    private const string kSaveFolderName = "SaveData";
    private const string kSaveFileName = "SaveManager.json";

    [MenuItem("Window/Love Signal/Save Load File Window")]
    public static void ShowWindow()
    {
        GetWindow<SaveLoadFileWindowEditor>("Save/Load File");
    }

    //2025.08.28 KHJ : 기존 저장 파일을 편하게 삭제, 리셋, 열수 있도록 했습니다
    void OnGUI()
    {
        //var saveFolderPath = Path.Combine(Application.streamingAssetsPath, kSaveFolderName);
        var saveFolderPath = Application.streamingAssetsPath;
        var saveFullPath = Path.Combine(saveFolderPath, kSaveFileName);

        GUILayout.Label("Save File", EditorStyles.boldLabel);

        // 상태 표시
        DrawStatus(saveFullPath);

        EditorGUILayout.Space();

        // 액션 버튼들
        using (new EditorGUILayout.HorizontalScope())
        {
            // 삭제
            if (GUILayout.Button("Remove File", GUILayout.Height(24)))
            {
                RemoveFile(saveFullPath);
                Repaint();
            }

            //2025.08.27 KHJ : 기본 데이터로 리셋
            if (GUILayout.Button("Reset File", GUILayout.Height(24)))
            {
                ResetFile(saveFullPath);
                Repaint();
            }

            // 파일 열기(기본 앱으로)
            EditorGUI.BeginDisabledGroup(!File.Exists(saveFullPath));
            if (GUILayout.Button("Open File", GUILayout.Height(24)))
            {
                OpenWithDefaultApp(saveFullPath);
            }  
            EditorGUI.EndDisabledGroup();

            // 폴더 열기
            if (GUILayout.Button("Open Folder", GUILayout.Height(24)))
            {
                OpenFolder(saveFolderPath);
            }

            // 새로고침(상태 갱신)
            if (GUILayout.Button("Refresh", GUILayout.Height(24)))
            {
                Repaint();
            }
        }
    }

    private void DrawStatus(string saveFullPath)
    {
        bool exists = File.Exists(saveFullPath);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Current Save Path", saveFullPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(2);

            if (exists)
            {
                var info = new FileInfo(saveFullPath);
                EditorGUILayout.LabelField("Status", "Exists");
                EditorGUILayout.LabelField("Size", $"{info.Length:N0} bytes");
                EditorGUILayout.LabelField("Last Modified", info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                EditorGUILayout.LabelField("Status", "Not Found");
            }
        }
    }

    private void RemoveFile(string saveFullPath)
    {
        if (!File.Exists(saveFullPath))
        {
            UnityEngine.Debug.Log("No save file to remove.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Remove Save File",
            $"Delete this file?\n\n{saveFullPath}",
            "Delete", "Cancel"))
        {
            return;
        }

        try
        {
            File.Delete(saveFullPath);
            UnityEngine.Debug.Log("Save file deleted.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }

    private void ResetFile(string saveFullPath)
    {
        if (!File.Exists(saveFullPath))
        {
            UnityEngine.Debug.Log("No save file to reset.");
            return;
        }

        try
        {
            SaveManager defaultData = new SaveManager();
            string jsonToSave = JsonUtility.ToJson(defaultData);
            File.WriteAllText(saveFullPath, jsonToSave);

            UnityEngine.Debug.Log("Save file reset.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to reset save file: {e.Message}");
        }
    }

    private void OpenWithDefaultApp(string path)
    {
        try
        {
#if UNITY_EDITOR_OSX
            Process.Start("open", $"\"{path}\"");
#elif UNITY_EDITOR_LINUX
            Process.Start("xdg-open", $"\"{path}\"");
#else // Windows
            var psi = new ProcessStartInfo(path) { UseShellExecute = true };
            Process.Start(psi);
#endif
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Cannot open file: " + e.Message);
        }
    }

    private void OpenFolder(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

#if UNITY_EDITOR_OSX
            Process.Start("open", $"\"{folderPath}\"");
#elif UNITY_EDITOR_LINUX
            Process.Start("xdg-open", $"\"{folderPath}\"");
#else // Windows
            var psi = new ProcessStartInfo(folderPath) { UseShellExecute = true };
            Process.Start(psi);
#endif
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Cannot open folder: " + e.Message);
        }
    }
}
