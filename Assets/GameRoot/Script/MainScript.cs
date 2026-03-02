using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScript : MonoBehaviour
{
    void Start()
    {
        // Initialize main menu logic if needed
    }

    void Update()
    {
        // Main menu per-frame updates if needed
    }

    public void StartGameBtn()
    {
        // Load the game scene
        SceneManager.LoadScene("tmpScene");
    }
}