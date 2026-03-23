using UnityEngine;

public class RestartPersistentMusicOnSceneStart : MonoBehaviour
{
    [SerializeField] private bool restartOnStart = true;

    private void Start()
    {
        if (!restartOnStart)
            return;

        PersistentGeneralMusicController.RestartPersistentMusicFromBeginning();
    }
}
