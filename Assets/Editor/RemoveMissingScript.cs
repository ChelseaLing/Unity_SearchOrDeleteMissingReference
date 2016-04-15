using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

public class RemoveMissingScript : ScriptableObject
{
    [MenuItem("Assets/Chelsea/Remove Missing Scripts")]
    public static void missingScript()
    {
        Rect wr = new Rect(300, 400, 600, 50);
        MissingScriptEditor window = (MissingScriptEditor)EditorWindow.GetWindowWithRect(typeof(MissingScriptEditor), wr, true, "Search selectePrefab Missing scripts");
        window.Show();
    }
}

public class MissingScriptEditor : EditorWindow
{
    private ArrayList selects = new ArrayList();
    private ArrayList searchPrefabs = new ArrayList();
    private Regex regex = new Regex("(Assets){1}");
    private bool isStartSearch = false;
    private string outputText;
    private int currentHandleIndex = 0;
    private int searchPrefabsCount = 0;

    public void OnInspectorUpdate()
    {
        this.Repaint();
    }

    public void OnGUI()
    {
        GUILayout.Label(outputText, EditorStyles.boldLabel);
    }

    public void Awake()
    {
        isStartSearch = false;
        currentHandleIndex = 0;
        searchPrefabsCount = 0;
        searchPrefabs.Clear();
        selects.Clear();
        addGameObjectToSelects(Selection.objects);
        addGameObjectToSelects(Selection.activeGameObject);
        if (!getFilesBySelect())
        {
            outputText = "Error: please select file or folder at first!";
            return;
        }
        searchPrefabsCount = searchPrefabs.Count;
        if (searchPrefabsCount <= 0)
        {
            outputText = "Error: there is no '.prefab' file in your selected files or folders!";
            return;
        }
        isStartSearch = true;
    }

    private void addGameObjectToSelects(Object go)
    {
        if (go == null)
            return;
        if (selects.IndexOf(go) >= 0)
            return;
        selects.Add(go);
    }

    private void addGameObjectToSelects(Object[] gos)
    {
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i] == null)
                continue;
            addGameObjectToSelects(gos[i]);
        }
    }

    private bool getFilesBySelect()
    {
        if (selects.Count <= 0)
            return false;
        int length = selects.Count;
        for (int i = 0; i < length; i++)
        {
            Object tempObj = selects[i] as Object;
            string tempPath = (AssetDatabase.GetAssetPath(tempObj));
            if (tempPath.IndexOf(".meta") >= 0)
                continue;
            tempPath = Application.dataPath + regex.Replace(tempPath, "", 1, 0);
            if (Directory.Exists(tempPath))
            {
                DirectoryInfo tempInfo = Directory.CreateDirectory(tempPath);
                getFilesByDir(tempInfo);
                tempInfo = null;
                tempObj = null;
                continue;
            }
            tempObj = null;
            addFileToArray(tempPath);
        }
        return true;
    }

    private void getFilesByDir(DirectoryInfo dir)
    {
        FileInfo[] allFile = dir.GetFiles();
        DirectoryInfo[] allDir = dir.GetDirectories();
        int fileCount = allFile.Length;
        int DirCount = allDir.Length;

        for (int i = 0; i < fileCount; i++)
        {
            FileInfo fi = allFile[i];
            addFileToArray(fi.DirectoryName + "/" + fi.Name);
            fi = null;
        }

        for (int j = 0; j < DirCount; j++)
        {
            getFilesByDir(allDir[j]);
        }
    }

    private void addFileToArray(string path)
    {
        if (path.IndexOf(".meta") >= 0)
            return;
        if (path.IndexOf(".prefab") >= 0)
        {
            path = path.Replace('\\', '/');
            string savePath = "Assets" + path.Replace(Application.dataPath, "");
            if (searchPrefabs.IndexOf(savePath) == -1)
            {
                searchPrefabs.Add(savePath);
            }
        }
    }

    private void searchMissing(int index)
    {
        outputText = "Removing: " + searchPrefabs[index].ToString();
        GameObject prefab = AssetDatabase.LoadAssetAtPath(searchPrefabs[index].ToString(), typeof(GameObject)) as GameObject;
        if (prefab == null)
            return;
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        GameObject[] sceneObj = GameObject.FindObjectsOfType<GameObject>();
        int sceneObjCount = sceneObj.Length;
        for (int i = 0; i < sceneObjCount; i++)
        {
            Component[] components = sceneObj[i].GetComponents<Component>();
            SerializedObject serializedObject = new SerializedObject(sceneObj[i]);
            var prop = serializedObject.FindProperty("m_Component");
            int removeCount = 0;
            for (int j = 0; j < components.Length; j++)
            {
                if (components[j] == null)
                {
                    prop.DeleteArrayElementAtIndex(j - removeCount);
                    removeCount++;
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(instance);
        }
        PrefabUtility.ReplacePrefab(instance, prefab);
        DestroyImmediate(instance);
        AssetDatabase.Refresh();
    }

    void Update()
    {
        if (isStartSearch)
        {
            searchMissing(currentHandleIndex);
            currentHandleIndex++;
            if (currentHandleIndex >= searchPrefabsCount)
            {
                isStartSearch = false;
                AssetDatabase.Refresh();
                outputText = "Congratulations: it over ";
            }
        }
    }
}