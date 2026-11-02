using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public GameObject iconOff; // объект с картинкой "звук выключен"
    public GameObject iconOn; // объект с картинкой "звук включен"  
    private bool isSoundOn = true;

    void Start()
    {
        UpdateIcons();
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn; // Выключаем звуки в игре
        AudioListener.pause = !isSoundOn;

        // Переключаем иконки
        UpdateIcons();
    }
    private void UpdateIcons()
    {
        iconOn.SetActive(isSoundOn);
        iconOff.SetActive(!isSoundOn);
    }
    public void PlayGame()
    {
        SceneManager.LoadScene("Game"); // или Loader.StartLoading("Game")
    }
}