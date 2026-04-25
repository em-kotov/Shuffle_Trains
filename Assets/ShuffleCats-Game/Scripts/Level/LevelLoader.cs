using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // public static LevelLoader Instance;

    // private void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(this);
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    // public void LoadNextLevel()
    // {
    //     int currentIndex = SceneManager.GetActiveScene().buildIndex;
    //     int nextIndex = currentIndex + 1;

    //     if (nextIndex < SceneManager.sceneCountInBuildSettings)
    //     {
    //         SceneManager.LoadScene(nextIndex);
    //     }
    //     else
    //     {
    //         Debug.Log("All levels completed!");
    //     }
    // }
}
