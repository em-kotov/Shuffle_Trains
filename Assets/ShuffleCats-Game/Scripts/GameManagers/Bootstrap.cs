using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene("PersistentManagers", LoadSceneMode.Additive);
        SceneManager.LoadScene("Level-1", LoadSceneMode.Additive);
    }
}
