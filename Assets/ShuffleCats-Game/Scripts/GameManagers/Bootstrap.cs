using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private int _firstLevelIndex = 1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        SceneManager.LoadSceneAsync(_firstLevelIndex, LoadSceneMode.Single);
    }
}
