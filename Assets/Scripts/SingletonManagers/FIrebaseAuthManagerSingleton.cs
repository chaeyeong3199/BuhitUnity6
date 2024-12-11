using Firebase.Auth;
using Firebase;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class FirebaseAuthManagerSingleton : MonoBehaviour
{
    private static FirebaseAuthManagerSingleton instance = null;

    public static FirebaseAuthManagerSingleton Instance
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

    public Firebase.FirebaseApp app;
    public event Action FirebaseInitialized;

    public bool signedIn = false;
    private FirebaseAuth auth;
    private FirebaseUser user;

    private bool tryLogin = true;

    public event Action StateAction;
    public Action<bool> SignInAction;

    enum ENUM_STATE
    {
        DEFAULT = 0, //0
        SIGN_UP,
        ERROR_EMAIL_BLANK,
        ERROR_EMAIL_INVALID,
        ERROR_EMAIL_NOTFOUND,
        ERROR_EMAIL_ALREADY_IN_USE,
        ERROR_PASSWORD_BLANK,
        ERROR_PASSWORD_INVALID,
        ERROR_PASSWORD_WEAK,
    }

    public int CurrentState = (int)ENUM_STATE.DEFAULT;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeFirebaseAsync();
    }

    private async Task InitializeFirebaseAsync()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += AuthStateChanged;
                Debug.Log("Firebase Auth initialized successfully");
            }
            else
            {
                throw new Exception($"Firebase ���Ӽ� ����: {dependencyStatus}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Firebase �ʱ�ȭ ����: {ex.Message}");
        }
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        Debug.Log("StateChange");

        if (!tryLogin) { return; }
        signedIn = user != auth.CurrentUser && auth.CurrentUser != null && auth.CurrentUser.IsValid();

        Debug.Log("signedIn = " + signedIn);

        if (!signedIn && user == null)
        {
            FirebaseInitialized?.Invoke();
        }

        if (auth.CurrentUser != user)
        {
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
                SignInAction?.Invoke(signedIn);
            }

            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                SignInAction?.Invoke(signedIn);
            }
        }

    }

    public async void SignInWithEmail(string email, string password)
    {
        Debug.Log(email + password);

        tryLogin = true;

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);

            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
        }
        catch (FirebaseException e)
        {
            switch (e.ErrorCode)
            {
                case (int)AuthError.MissingEmail:
                    Debug.LogError("�̸����� �Է����ּ���.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_BLANK;
                    break;
                case (int)AuthError.InvalidEmail:
                    Debug.LogError("��ȿ���� ���� �̸��� �����Դϴ�.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_INVALID;
                    break;
                case (int)AuthError.UserNotFound:
                    Debug.LogError("�������� �ʴ� �����Դϴ�.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_NOTFOUND;
                    break;
                case (int)AuthError.MissingPassword:
                    Debug.LogError("��й�ȣ�� �Է����ּ���.");
                    CurrentState = (int)ENUM_STATE.ERROR_PASSWORD_BLANK;
                    break;
                case (int)AuthError.WrongPassword:
                    Debug.LogError("��й�ȣ�� �ùٸ��� �ʽ��ϴ�.");
                    CurrentState = (int)ENUM_STATE.ERROR_PASSWORD_INVALID;
                    break;
                default:
                    Debug.LogError($"�α��� ����: {e.Message}");
                    break;
            }
            tryLogin = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"�� �� ���� ���� �߻�: {e.Message}");
            tryLogin = false;
        }

        StateAction?.Invoke();
    }

    public void SignOut()
    {
        Debug.Log("SignOUt");
        if (auth != null) auth.SignOut();
        if (user != null) user.DeleteAsync();
    }

    public async void CreateUserWithEmail(string email, string password)
    {
        Debug.Log("Create User With Email");
        tryLogin = false;

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            Debug.LogFormat("ȸ������ ����: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            // ȸ������ ���� �� �߰� ó��
            FirebaseRDBManager.Instance.OnSignUp(result.User.UserId);
            CurrentState = (int)ENUM_STATE.SIGN_UP;

        }
        catch (FirebaseException e)
        {
            switch (e.ErrorCode)
            {
                case (int)AuthError.MissingEmail:
                    Debug.LogError("�̸����� �Է����ּ���.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_BLANK;
                    break;
                case (int)AuthError.EmailAlreadyInUse:
                    Debug.LogError("�̹� ��� ���� �̸����Դϴ�.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_ALREADY_IN_USE;
                    break;
                case (int)AuthError.InvalidEmail:
                    Debug.LogError("��ȿ���� ���� �̸��� �����Դϴ�.");
                    CurrentState = (int)ENUM_STATE.ERROR_EMAIL_INVALID;
                    break;
                case (int)AuthError.MissingPassword:
                    Debug.LogError("��й�ȣ�� �Է����ּ���.");
                    CurrentState = (int)ENUM_STATE.ERROR_PASSWORD_BLANK;
                    break;
                case (int)AuthError.WeakPassword:
                    Debug.LogError("��й�ȣ�� �ʹ� ���մϴ�. 6�ڸ� �̻� �Է����ּ���.");
                    CurrentState = (int)ENUM_STATE.ERROR_PASSWORD_WEAK;
                    break;
                default:
                    Debug.LogError($"ȸ������ ����: {e.Message}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"�� �� ���� ���� �߻�: {e.Message}");
        }

        StateAction?.Invoke();
        SignOut();
        /*
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            // Firebase user has been created. 
            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            FirebaseRDBManager.Instance.OnSignUp(result.User.UserId);

            SignOut();
        });*/

    }

    void OnDestroy()
    {
        //auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    public bool isSignedIn()
    {
        return signedIn;
    }

    public int GetCurrentState()
    {
        return CurrentState;
    }

    public void ResetState()
    {
        CurrentState = (int)ENUM_STATE.DEFAULT;
    }
}