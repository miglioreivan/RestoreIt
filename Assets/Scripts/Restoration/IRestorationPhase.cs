using System;

public interface IRestorationPhase
{
    event Action<bool> OnPhaseCompleted;
    float Progression { get; }
}
