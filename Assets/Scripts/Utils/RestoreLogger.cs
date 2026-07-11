using UnityEngine;

/// <summary>
/// Classe di utilità statica per gestire il logging condizionale all'interno dell'Editor di Unity.
/// I log di informazione ordinari vengono rimossi a compile-time nelle build di produzione.
/// </summary>
public static class RestoreLogger
{
    /// <summary>
    /// Invia un messaggio di log informativo in Console, abilitato solo nell'Editor.
    /// </summary>
    /// <param name="message">Il messaggio o l'oggetto da loggare.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    /// <summary>
    /// Invia un messaggio di log informativo in Console associato ad un contesto specifico, abilitato solo nell'Editor.
    /// </summary>
    /// <param name="message">Il messaggio o l'oggetto da loggare.</param>
    /// <param name="context">Il GameObject o l'oggetto Unity che genera il log per la tracciabilità.</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message, Object context)
    {
        Debug.Log(message, context);
    }

    /// <summary>
    /// Invia un messaggio di Warning (avviso) in Console. Sempre abilitato anche in build.
    /// </summary>
    /// <param name="message">Il messaggio di avvertimento.</param>
    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    /// <summary>
    /// Invia un messaggio di Warning (avviso) in Console associato ad un contesto. Sempre abilitato anche in build.
    /// </summary>
    /// <param name="message">Il messaggio di avvertimento.</param>
    /// <param name="context">Il contesto che ha generato il log.</param>
    public static void LogWarning(object message, Object context)
    {
        Debug.LogWarning(message, context);
    }

    /// <summary>
    /// Invia un messaggio di Error (errore critico) in Console. Sempre abilitato anche in build.
    /// </summary>
    /// <param name="message">Il messaggio di errore.</param>
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// Invia un messaggio di Error (errore critico) in Console associato ad un contesto. Sempre abilitato anche in build.
    /// </summary>
    /// <param name="message">Il messaggio di errore.</param>
    /// <param name="context">Il contesto che ha generato l'errore.</param>
    public static void LogError(object message, Object context)
    {
        Debug.LogError(message, context);
    }
}
