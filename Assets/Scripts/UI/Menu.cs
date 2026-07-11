using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestisce le opzioni di flusso base del Menu principale di gioco (caricamento partita, chiusura applicazione).
/// </summary>
public class Menu : MonoBehaviour
{
    /// <summary>
    /// Forza la chiusura del gioco (funzionante in build).
    /// </summary>
    public void Quit()
    {
        Application.Quit();
    }

    /// <summary>
    /// Carica in modo sincrono la scena principale del museo (build index 1).
    /// </summary>
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}
