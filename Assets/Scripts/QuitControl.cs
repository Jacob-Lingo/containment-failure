using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitControls : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Dev_MainMenu";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Application.Quit();
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameState.ForceUnfreeze();
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}