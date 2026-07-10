public interface IRestorationPhaseManager
{
    /// <summary>
    /// Chiamata quando la transizione della telecamera verso la postazione di restauro è completata.
    /// Consente alla fase di sbloccare l'interazione o avviare il proprio minigioco.
    /// </summary>
    void CameraTransitionCompleted();
}
