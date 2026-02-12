using UnityEngine;

public class Influencer : MonoBehaviour
{
    [HideInInspector] public Vector3 prevPosition;

    void Start()
    {
        var simulation = FindFirstObjectByType<Simulation>();
        simulation.AddInfluencer(this);
    }
    
    void OnDestroy()
    {
        var simulation = FindFirstObjectByType<Simulation>();
        if (!simulation)
            return;
        simulation.RemoveInfluencer(this);
    }
}
