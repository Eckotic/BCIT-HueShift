using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] 
    private string gameScene;
    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(gameScene);
    }
}