using UnityEngine;
using System.IO;

public static class LoggerScrittore
{
    private static readonly string logPath = @"C:\Users\migli\.gemini\antigravity\brain\72da6231-54bb-4e04-994c-8d278da44f9d\scratch\unity_session_log.txt";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InizializzaLogger()
    {
        try
        {
            string dir = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(logPath, $"=== NUOVA SESSIONE DI GIOCO - {System.DateTime.Now:dd/MM/yyyy HH:mm:ss} ===\n");
            Application.logMessageReceived += GestisciLog;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoggerScrittore] Impossibile creare il file di log: {e.Message}");
        }
    }

    private static void GestisciLog(string logString, string stackTrace, LogType type)
    {
        try
        {
            string prefix = $"[{type}] [{System.DateTime.Now:HH:mm:ss}] ";
            string logLine = prefix + logString + "\n";
            
            // Includi lo stack trace per errori ed eccezioni per facilitare il debug
            if (type == LogType.Error || type == LogType.Exception)
            {
                logLine += stackTrace + "\n";
            }
            
            File.AppendAllText(logPath, logLine);
        }
        catch
        {
            // Ignora eventuali errori di scrittura concorrente a runtime
        }
    }
}
