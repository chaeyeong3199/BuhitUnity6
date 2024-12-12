using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.CompilerServices;

using DBModels;
using System.Collections.Generic;
using Unity.VisualScripting;

public class FirebaseRDBManager : MonoBehaviour
{
    private static FirebaseRDBManager instance;
    public static FirebaseRDBManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
            return instance;
        }
    }

    private DatabaseReference databaseReference;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FirebaseAuthManager.Instance.authInitialized += myStart;
    }

    private async void myStart()
    {
        await InitializeFirebaseRealtimeDBAsync();
    }

    private async Task InitializeFirebaseRealtimeDBAsync()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase database initialized successfully.");
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Firebase �ʱ�ȭ ����: {ex.Message}");
        }
    }

    public async void OnSignUp(string userId)
    {
        try
        {
            string initJson;
            string filePath = Path.Combine(Application.streamingAssetsPath, "init.json");

            // �÷����� ���� �б� ó��
            if (Application.platform == RuntimePlatform.Android)
            {
                using (UnityWebRequest www = UnityWebRequest.Get(filePath))
                {
                    await www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"���� �б� ����: {www.error}");
                        return;
                    }
                    initJson = www.downloadHandler.text;
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor ||
                     Application.platform == RuntimePlatform.WindowsPlayer ||
                     Application.platform == RuntimePlatform.OSXEditor ||
                     Application.platform == RuntimePlatform.OSXPlayer ||
                     Application.platform == RuntimePlatform.LinuxEditor ||
                     Application.platform == RuntimePlatform.LinuxPlayer)
            {
                // PC �÷��� (Windows, Mac, Linux)
                Debug.Log("PC �÷���");
                initJson = File.ReadAllText(filePath);
            }
            else
            {
                // iOS �� ��Ÿ �÷���
                using (UnityWebRequest www = UnityWebRequest.Get(filePath))
                {
                    await www.SendWebRequest();
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"���� �б� ����: {www.error}");
                        return;
                    }
                    initJson = www.downloadHandler.text;
                }
            }

            // Firebase�� ����
            DatabaseReference userRef = databaseReference.Child("users").Child(userId);
            await userRef.SetRawJsonValueAsync(initJson).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("������ ���� ����: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("������ ���� ����");
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveUserData ����: {e.Message}");
        }
    }

    public async Task<HomeSceneData> GetUserDataHomeScene()
    {
        DatabaseReference userRef = databaseReference.Child("users").Child(FirebaseAuthManager.Instance.GetCurrentUserId());

        try
        {
            var snapshot = await userRef.GetValueAsync();
            if (snapshot.Exists)
            {
                HomeSceneData initData = new HomeSceneData();

                // �����̼� ������ ��������
                var stations = snapshot.Child("Stations").Children;
                foreach (var station in stations)
                {
                    Station tmp = new Station();
                    tmp.stationName = station.Key;
                    tmp.isUnlocked = int.Parse(station.Child("isUnlocked").Value.ToString());
                    tmp.unlockCamellia = int.Parse(station.Child("unlockCamellia").Value.ToString());
                    // �����̼� ������ ó��

                    initData.stations.stationList.Add(tmp);
                }

                // ���� ������ ��������
                var userData = snapshot.Child("userData");
                initData.userData.nickname = userData.Child("nickName").Value.ToString();
                initData.userData.camellia = int.Parse(userData.Child("camellia").Value.ToString());

                // ������ ������ Ȱ��
                return initData;
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"������ �ε� ����: {e.Message}");
            return null;
        }
    }
    public async Task<IngameObjectList> GetMapData(String stationName, String mapName)
    {
        DatabaseReference mapRef = databaseReference.Child("users").Child(FirebaseAuthManager.Instance.GetCurrentUserId()).Child("Stations").Child(stationName).Child("Maps").Child(mapName).Child("objectCheckList");

        try
        {
            var snapshot = await mapRef.GetValueAsync();
            if (snapshot.Exists)
            {
                Debug.Log("����� ��");
                IngameObjectList ingameObjectList = new IngameObjectList();

                // �����̼� ������ ��������
                foreach (var ingameObject in snapshot.Children)
                {
                    IngameObject tmpIO = new IngameObject();

                    tmpIO.id = ingameObject.Key;

                    string discription = ingameObject.Child("Description").Value?.ToString();
                    int isCleared = int.Parse(ingameObject.Child("isCleared").Value?.ToString());

                    tmpIO.objectInfo.Description = discription;
                    tmpIO.objectInfo.isChecked = isCleared;

                    ingameObjectList.ingameObjectList.Add(tmpIO);

                    Debug.Log(discription + isCleared);
                }

                // ������ ������ Ȱ��
                return ingameObjectList;
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"������ �ε� ����: {e.Message}");
            return null;
        }
    }


    // ������ ������Ʈ-----------------------------------------
    public void UpdateUnlockData(int camellia, String targetStationName)
    {
        Debug.Log(targetStationName + camellia);

        String targetUserId = FirebaseAuthManager.Instance.GetCurrentUserId();
        // ������Ʈ�� ������ �غ�
        Dictionary<string, object> updates = new Dictionary<string, object>();

        updates["/users/" + targetUserId + "/userData/camellia"] = camellia;
        updates["/users/" + targetUserId + "/Stations/" + targetStationName + "/isUnlocked"] = 1;

        // ��Ƽ�н� ������Ʈ ����
        databaseReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("�����Ͱ� ���������� ������Ʈ�Ǿ����ϴ�.");
            }
            else
            {
                Debug.LogError("������ ������Ʈ �� ���� �߻�: " + task.Exception);
            }
        });
    }

    // ������ ������Ʈ-----------------------------------------
    public void InitData(string stationName, string mapName, int n)
    {
        String targetUserId = FirebaseAuthManager.Instance.GetCurrentUserId();
        // ������Ʈ�� ������ �غ�
        Dictionary<string, object> updates = new Dictionary<string, object>();

        for(int i = 1; i <= n; i++)
        {
            updates["/users/" + targetUserId + "/Stations/" + stationName + "/Maps/" + mapName + "/objectCheckList/" + i + "/isCleared"] = 0;
        }

        // ��Ƽ�н� ������Ʈ ����
        databaseReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("�����Ͱ� ���������� ������Ʈ�Ǿ����ϴ�.");
            }
            else
            {
                Debug.LogError("������ ������Ʈ �� ���� �߻�: " + task.Exception);
            }
        });
    }


    public void UpdateObject(String stationName, String mapName, int objectId)
    {
        String targetUserId = FirebaseAuthManager.Instance.GetCurrentUserId();

        string path = "/users/" + targetUserId + "/Stations/" + stationName + "/Maps/" + mapName + "/objectCheckList/" + objectId + "/isCleared";

        // ������Ʈ�� ������ �غ�
        Dictionary<string, object> updates = new Dictionary<string, object>();

        updates[path] = 1;

        // ��Ƽ�н� ������Ʈ ����
        databaseReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("������Ʈ�����Ͱ� ���������� ������Ʈ�Ǿ����ϴ�.");
            }
            else
            {
                Debug.LogError("������ ������Ʈ �� ���� �߻�: " + task.Exception);
            }
        });
    }

    //���� �ٽ� �ؾߵ� �ӽ÷� �س�������
    public void OnClearMap(String stationName, String mapName)
    {
        Debug.Log("���� ���� ��");
        String targetUserId = FirebaseAuthManager.Instance.GetCurrentUserId();

        DatabaseReference mapRef = FirebaseDatabase.DefaultInstance.GetReference("users/" + targetUserId + "/Stations/" + stationName + "/clearedMapCount");

        bool isCleared = false;

        mapRef.RunTransaction(mutableData =>
        {
            int value = 0;
            if (mutableData.Value != null)
            {
                value = int.Parse(mutableData.Value.ToString());
            }
            
            if(value != 0)
            {
                isCleared = true;
            }
            else
            {
                mutableData.Value = value + 1;
            }

            Debug.Log(isCleared);

            if (!isCleared)
            {
                Debug.Log(isCleared + "2");

                UpdateCamellia(3);
            }

            return TransactionResult.Success(mutableData);
        });
    }

    public void UpdateCamellia(int camellia)
    {
        String targetUserId = FirebaseAuthManager.Instance.GetCurrentUserId();

        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.GetReference("users/" + targetUserId + "/userData/camellia");

        Debug.Log("���� ����");
        // camellia �� ����
        userRef.RunTransaction(mutableData =>
        {
            int value = 0;
            if (mutableData.Value != null)
            {
                value = int.Parse(mutableData.Value.ToString());
            }
            mutableData.Value = value + 3;
            return TransactionResult.Success(mutableData);
        });
    }
}




public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += obj => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }
}
