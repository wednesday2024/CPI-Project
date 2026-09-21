#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordRoomTrigger : MonoBehaviour
{
    public string areaNameOnEnter = "";

    private int insideCount = 0;

    private Scene owningScene;

    private static DiscordRoomTrigger currentOwner;

    private void Awake()
    {
        owningScene = gameObject.scene;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        ForceClearIfOwner();
        insideCount = 0;
    }

    private void OnDestroy()
    {
        ForceClearIfOwner();
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene != owningScene)
        {
            ForceClearIfOwner();
            insideCount = 0;
        }
    }

    private void ApplyPresence()
    {
        if (!string.IsNullOrEmpty(areaNameOnEnter))
        {
            currentOwner = this;
            DiscordController.SetAreaNameOverrideGlobal(areaNameOnEnter);
        }
    }

    private void ForceClearIfOwner()
    {
        if (currentOwner == this)
        {
            currentOwner = null;
            DiscordController.ClearAreaNameOverrideGlobal();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        insideCount++;

        if (insideCount == 1)
            ApplyPresence();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (insideCount <= 0)
            insideCount = 1;

        ApplyPresence();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        insideCount--;
        if (insideCount < 0) insideCount = 0;

        if (insideCount == 0)
            ForceClearIfOwner();
    }
}
#endif
