using Firebase.Auth;
using Firebase;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseAuthManager : MonoBehaviour
{
    private static FirebaseAuthManager instance = null;

    public static FirebaseAuthManager Instance
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


    private FirebaseAuth auth;
    private FirebaseUser user;
    public bool signedIn = false;
    private bool tryLogin = true;

    public Action<bool> LoginState;
    public Action<int> StateAction;

    public enum ENUM_STATE
    {
        DEFAULT = 0,
        SIGN_UP,
        ERROR_EMAIL_BLANK,
        ERROR_EMAIL_INVALID,
        ERROR_EMAIL_NOTFOUND,
        ERROR_EMAIL_ALREADY_IN_USE,
        ERROR_PASSWORD_BLANK,
        ERROR_PASSWORD_INVALID,
        ERROR_PASSWORD_WEAK,
    }

    public ENUM_STATE CurrentState { get; private set; } = ENUM_STATE.DEFAULT;

    public async Task InitializeFirebaseAsync()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
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

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (!tryLogin) return;

        signedIn = user != auth.CurrentUser && auth.CurrentUser != null && auth.CurrentUser.IsValid();
        //Debug.Log("signedIn = " + signedIn);

        if (auth.CurrentUser != user)
        {
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }

        LoginState?.Invoke(signedIn);
    }

    public async void SignInWithEmail(string email, string password)
    {
        Debug.Log(email + password);
        tryLogin = true;
        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            Debug.LogFormat("User signed in successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);
            CurrentState = ENUM_STATE.DEFAULT;
        }
        catch (FirebaseException e)
        {
            HandleFirebaseException(e);
            tryLogin = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"�� �� ���� ���� �߻�: {e.Message}");
            tryLogin = false;
        }
        StateAction?.Invoke((int)CurrentState);
    }

    public void SignOut()
    {
        Debug.Log("SignOut");
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
            Debug.LogFormat("ȸ������ ����: {0} ({1})", result.User.DisplayName, result.User.UserId);
            // ȸ������ ���� �� �߰� ó��
            // FirebaseRDBManager.Instance.OnSignUp(result.User.UserId);
            CurrentState = ENUM_STATE.SIGN_UP;
        }
        catch (FirebaseException e)
        {
            HandleFirebaseException(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"�� �� ���� ���� �߻�: {e.Message}");
        }
        StateAction?.Invoke((int)CurrentState);
        SignOut();
    }

    private void HandleFirebaseException(FirebaseException e)
    {
        switch (e.ErrorCode)
        {
            case (int)AuthError.MissingEmail:
                Debug.LogError("�̸����� �Է����ּ���.");
                CurrentState = ENUM_STATE.ERROR_EMAIL_BLANK;
                break;
            case (int)AuthError.InvalidEmail:
                Debug.LogError("��ȿ���� ���� �̸��� �����Դϴ�.");
                CurrentState = ENUM_STATE.ERROR_EMAIL_INVALID;
                break;
            case (int)AuthError.UserNotFound:
                Debug.LogError("�������� �ʴ� �����Դϴ�.");
                CurrentState = ENUM_STATE.ERROR_EMAIL_NOTFOUND;
                break;
            case (int)AuthError.EmailAlreadyInUse:
                Debug.LogError("�̹� ��� ���� �̸����Դϴ�.");
                CurrentState = ENUM_STATE.ERROR_EMAIL_ALREADY_IN_USE;
                break;
            case (int)AuthError.MissingPassword:
                Debug.LogError("��й�ȣ�� �Է����ּ���.");
                CurrentState = ENUM_STATE.ERROR_PASSWORD_BLANK;
                break;
            case (int)AuthError.WrongPassword:
                Debug.LogError("��й�ȣ�� �ùٸ��� �ʽ��ϴ�.");
                CurrentState = ENUM_STATE.ERROR_PASSWORD_INVALID;
                break;
            case (int)AuthError.WeakPassword:
                Debug.LogError("��й�ȣ�� �ʹ� ���մϴ�. 6�ڸ� �̻� �Է����ּ���.");
                CurrentState = ENUM_STATE.ERROR_PASSWORD_WEAK;
                break;
            default:
                Debug.LogError($"���� �߻�: {e.Message}");
                break;
        }
    }

    public string GetCurrentUserId()
    {
        return auth.CurrentUser.UserId;
    }

    public bool IsSignedIn() => signedIn;

    public void ResetState() => CurrentState = ENUM_STATE.DEFAULT;
}