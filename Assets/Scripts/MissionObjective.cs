using System;
using UnityEngine;

// GDD section 13 — reusable objective framework. Objectives raise an event on
// completion; the GameManager listens and advances the chapter.
public abstract class MissionObjective : MonoBehaviour
{
    public string objectiveDescription;
    public bool isCompleted;

    public event Action onCompleted;

    public abstract void CheckObjective();
    public abstract string ProgressText();

    void Update()
    {
        if (!isCompleted) CheckObjective();
    }

    protected void Complete()
    {
        if (isCompleted) return;
        isCompleted = true;
        if (onCompleted != null) onCompleted();
    }
}
