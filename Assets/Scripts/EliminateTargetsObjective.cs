// GDD section 13 — EliminateTargets objective type (MVP: one per arena).
public class EliminateTargetsObjective : MissionObjective
{
    public int totalTargets;

    public override void CheckObjective()
    {
        if (totalTargets > 0 && EnemyAI.Alive.Count == 0) Complete();
    }

    public override string ProgressText()
    {
        int down = totalTargets - EnemyAI.Alive.Count;
        return objectiveDescription + "  (" + down + "/" + totalTargets + ")";
    }
}
