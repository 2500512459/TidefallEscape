using UnityEngine;
using UnityEngine.UIElements;

public class MenuPanel : MonoBehaviour
{
    private VisualElement rootElement;
    private Button newGameButton, quitGameButton;

    //public ObjectEventSO newGameEvent;
    private void OnEnable()
    {
        rootElement = GetComponent<UIDocument>().rootVisualElement;
        newGameButton = rootElement.Q<Button>("NewGameButton");
        quitGameButton = rootElement.Q<Button>("QuitGameButton");

        newGameButton.clicked += OnNewGameButtonClicked;
        quitGameButton.clicked += OnQuitGameButtonClicked;
    }

    private void OnQuitGameButtonClicked()
    {
        Application.Quit();
    }

    private void OnNewGameButtonClicked()
    {
        if (LoadManager.Instance != null)
        {
            LoadManager.Instance.LoadScene("HomeScene");
        }
        else
        {
            Debug.LogError("LoadManager实例不存在，无法加载场景！");
        }
    }
}