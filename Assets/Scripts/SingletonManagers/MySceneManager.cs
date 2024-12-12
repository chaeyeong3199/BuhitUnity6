using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class MySceneManager : MonoBehaviour
{
    private static MySceneManager instance = null;

    public static MySceneManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null)
        {
            DestroyImmediate(this.gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;

        doorPos = Screen.width / 2;

        SceneManager.sceneLoaded += OnSceneLoaded; // �̺�Ʈ�� �߰�

        //Image_LeftDoor.GetComponent<RectTransform>().anchoredPosition = new Vector2(-doorPos, 0);
        //Image_RightDoor.GetComponent<RectTransform>().anchoredPosition = new Vector2(doorPos, 0);
    }

    public void Start()
    {
    }

    public CanvasGroup Panel_Loading;
    public float duration = 2f;

    public Image Image_LeftDoor;
    public Image Image_RightDoor;

    private float doorPos;

    /*
    public Scene GetActiveScene()
    {
        return SceneManager.GetActiveScene();
    }*/

    public bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void ChangeScene(string sceneName)
    { /// �ܺο��� ��ȯ�� �� �̸� �ޱ� ///
        Debug.Log("ChangeScene");
        Panel_Loading.alpha = 1;
        Panel_Loading.blocksRaycasts = true;

        Image_LeftDoor.transform.DOLocalMoveX(0, 1f);
        Image_RightDoor.transform.DOLocalMoveX(0, 1f).OnComplete(() => {
            StartCoroutine("LoadScene", sceneName); /// �� �ε� �ڷ�ƾ ���� ///
        });
    }

    public void ChangeSceneWithoutDoorOpen(string sceneName)
    { /// �ܺο��� ��ȯ�� �� �̸� �ޱ� ///
        //Debug.Log("ChangeSceneWithoutDoorOpen");
        StartCoroutine("LoadScene", sceneName);
    }

    IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false; //�ۼ�Ʈ �����̿�

        float past_time = 0;
        float percentage = 0;

        while (!(async.isDone))
        {
            yield return null;

            past_time += Time.deltaTime;

            if (percentage >= 90)
            {
                percentage = Mathf.Lerp(percentage, 100, past_time);

                if (percentage == 100)
                {
                    async.allowSceneActivation = true; //�� ��ȯ �غ� �Ϸ�
                }
            }
            else
            {
                percentage = Mathf.Lerp(percentage, async.progress * 100f, past_time);
                if (percentage >= 90) past_time = 0;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(SceneManager.GetActiveScene().name == "Login") {
            //Debug.Log("Login - OnSceneLoaded : invoke");

            return; 
        }
        //Debug.Log("OnSceneLoaded" + scene.name);

        Image_LeftDoor.transform.DOLocalMoveX(-doorPos, 1f);
        Image_RightDoor.transform.DOLocalMoveX(doorPos, 1f).OnComplete(() => {
            Panel_Loading.alpha = 0;
            Panel_Loading.blocksRaycasts = false;/// �� �ε� �ڷ�ƾ ���� ///
        });
    }

    public void DoorOpen()
    {
        Debug.Log("DoorOpen");
        Image_LeftDoor.transform.DOLocalMoveX(-doorPos, 1f);
        Image_RightDoor.transform.DOLocalMoveX(doorPos, 1f).OnComplete(() => {
            Panel_Loading.alpha = 0;
            Panel_Loading.blocksRaycasts = false;/// �� �ε� �ڷ�ƾ ���� ///
        });
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // �̺�Ʈ���� ����*
    }

    public void ReloadScene()
    {
        ChangeScene(SceneManager.GetActiveScene().name);
    }
}
