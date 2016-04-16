using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

public class MissingReference : ScriptableObject
{
    [MenuItem("Assets/Chelsea/Search selectePrefab Missing reference")]
    public static void missingReference()
    {
        Rect wr = new Rect(300, 400, 200, 50);
        MissingReferenceEditor window = (MissingReferenceEditor)EditorWindow.GetWindowWithRect(typeof(MissingReferenceEditor), wr, true, "Search selectePrefab Missing reference");
        window.findPrefabInYouSelect();
    }
}

public class MissingReferenceEditor : EditorWindow
{
    private ArrayList selects = new ArrayList();
    private ArrayList searchPrefabs = new ArrayList();
    private Regex regex = new Regex("(Assets){1}");
    private bool isStartSearch = false;
    private int currentHandleIndex = 0;
    private int searchPrefabsCount = 0;
    private int interval = 10;

    void OnGUI()
    {
        GUILayout.Label(" in Searching: wait please!");
    }

    public void findPrefabInYouSelect()
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
            Debug.LogError("Error: please select file or folder at first!");
            return;
        }
        searchPrefabsCount = searchPrefabs.Count;
        if (searchPrefabsCount <= 0)
        {
            Debug.LogError("Error: there is no '.prefab' file in your selected files or folders!");
            return;
        }
        isStartSearch = true;
        Debug.Log("Searching,please waitting !");
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

    private void showError(string objectName, string propertyName)
    {
        Debug.LogError("Missing reference found in: " + objectName + ", Property : " + propertyName);
    }

    private string fullObjectPath(GameObject go)
    {
        return go.transform.parent == null ? go.name : fullObjectPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    private void searchMissing(int index)
    {
        if (index >= searchPrefabs.Count)
            return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath(searchPrefabs[index].ToString(), typeof(GameObject)) as GameObject;
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        GameObject[] sceneObj = GameObject.FindObjectsOfType<GameObject>();
        int sceneObjLength = sceneObj.Length;
        for (int i = 0; i < sceneObjLength; i++)
        {
            var objects = sceneObj[i].GetComponents<Component>();
            foreach (var c in objects)
            {
                if (c == null)
                {
                    Debug.LogError("Missing script found on: " + fullObjectPath(sceneObj[i]));
                    continue;
                }
                SerializedObject serializedObject = new SerializedObject(c);
                SerializedProperty serializedProperty = serializedObject.GetIterator();
                while (serializedProperty.NextVisible(true))
                {
                    if (serializedProperty.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    if (serializedProperty.objectReferenceValue == null && serializedProperty.objectReferenceInstanceIDValue != 0)
                        showError(fullObjectPath(sceneObj[i]), serializedProperty.name);
                }
                serializedProperty = null;
                serializedObject = null;
            }
        }
        DestroyImmediate(instance);
        AssetDatabase.Refresh();
        sceneObj = null;
        instance = null;
        prefab = null;
    }


    void Update()
    {
        if (!isStartSearch)
            return;
        if (interval >= 10)
        {
            searchMissing(currentHandleIndex);
            currentHandleIndex++;
            if (currentHandleIndex >= searchPrefabsCount)
            {
                isStartSearch = false;
                AssetDatabase.Refresh();
                this.Close();
                Debug.Log("Congratulations: searching is over! ");
            }
        }
        else
            interval++;
    }
}