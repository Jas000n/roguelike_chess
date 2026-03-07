using System.Collections.Generic;
using UnityEngine;

// Minimal scaffolding for future split of RoguelikeFramework responsibilities.
// This is a light wrapper to start moving logic out of RoguelikeFramework.cs.
public interface IFlowStep {
    void ExecuteStep();
}

public class FlowController : MonoBehaviour
{
    // Expose a couple of light-weight hooks used by existing systems.
    public List<IFlowStep> Steps = new List<IFlowStep>();

    void Start()
    {
        // no heavy init; this is a placeholder for incremental refactor.
        Debug.Log("FlowController initialized - placeholder for incremental split of core logic.");
    }

    public void RunAll()
    {
        foreach (var s in Steps)
        {
            s?.ExecuteStep();
        }
        Debug.Log("FlowController ran all steps (placeholder).");
    }
}
