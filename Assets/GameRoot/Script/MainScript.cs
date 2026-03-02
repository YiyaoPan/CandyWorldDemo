// ====================================================
// MainScript.cs
// ====================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScript : MonoBehaviour
{
    void Start()
    {
        // Initialization code (if needed)
    }

    void Update()
    {
        // Per-frame update (if needed)
    }

    // Called when the Start Game button is pressed
    public void StartGameBtn()
    {
        // Load the game scene (tmpScene)
        SceneManager.LoadScene("tmpScene");
    }
}