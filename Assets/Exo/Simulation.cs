using System;
using StableFluids.Marbling;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    
    public Transform PlayerTransform;
    public float StepSize = 0.5f;
    public float PreviewDistance = 1f;

    private MarblingFluidSimulator MarblingSimulator;
    public RenderTexture ForceBuffer;
    public RenderTexture VelocityBuffer;
    
    private RenderTexture tempCopy;
    
    public Shader InjectionShader;
    private Material injectionMaterial;

    public ComputeShader OffsetCompute;

    public Transform PreviewQuadTransform;
    public MeshRenderer PreviewQuadMeshRenderer;
    
    Vector3 previousSnappedPosition;
    Vector3 previousPosition;

    private void Awake()
    {
        MarblingSimulator = GetComponent<MarblingFluidSimulator>();
    }

    private void OnEnable()
    {
        previousSnappedPosition = SnapPosition(PlayerTransform.position);
        injectionMaterial = new Material(InjectionShader);
        injectionMaterial.SetFloat("_Aspect", (float)ForceBuffer.width / ForceBuffer.height);
        
        tempCopy = new RenderTexture(VelocityBuffer.width, VelocityBuffer.height, 0, VelocityBuffer.format)
        {
            enableRandomWrite = true
        };
        tempCopy.Create();
    }
    
    private void OnDisable()
    {
        injectionMaterial = null;
        
        tempCopy.Release();
    }

    Vector2 WorldToSim(Vector3 pos, Vector3 simulationCenter, float simulationSize)
    {
        pos = (pos - simulationCenter) / simulationSize;
        return new Vector2(pos.x, pos.z);
    }
    
    Vector2 VelocityWorldToSim(Vector3 pos, Vector3 prevPos, Vector3 simulationCenter, float simulationSize)
    {
        pos = WorldToSim(pos, simulationCenter, simulationSize);
        prevPos = WorldToSim(prevPos, simulationCenter, simulationSize);
        return (pos - prevPos) / Time.deltaTime;
    }

    private void LateUpdate()
    {
        int simulationResolution = ForceBuffer.width;
        float scale = (int)simulationResolution * StepSize;
        var snappedPosition = SnapPosition(PlayerTransform.position);
        PreviewQuadTransform.position = snappedPosition + Vector3.up * PreviewDistance;
        PreviewQuadTransform.localScale = new Vector3(scale, scale, scale);

        Vector2Int texelOffset = new Vector2Int(
            Mathf.RoundToInt((snappedPosition.x - previousSnappedPosition.x) / StepSize),
            Mathf.RoundToInt((snappedPosition.z - previousSnappedPosition.z) / StepSize)
        );
        int numX = (int)simulationResolution / 8;
        
        // Offset (Only velocity)
        OffsetBuffer(VelocityBuffer, numX, texelOffset);
        // OffsetBuffer(MarblingSimulator.Simulation.v1, numX, texelOffset);
        // OffsetBuffer(MarblingSimulator.Simulation.p1, numX, texelOffset);
        // OffsetBuffer(MarblingSimulator.Simulation.p2, numX, texelOffset);
        
        // Injection
        Vector2 simPos = WorldToSim(PlayerTransform.position, snappedPosition, scale);
        Vector2 simVel = VelocityWorldToSim(PlayerTransform.position, previousPosition, snappedPosition, scale);
        
        Graphics.Blit(Texture2D.blackTexture, ForceBuffer);

        if (simVel.sqrMagnitude > 0)
        {
            injectionMaterial.SetVector("_Origin", simPos);
            injectionMaterial.SetFloat("_Falloff", 200f);
            injectionMaterial.SetVector("_Force", simVel);
            Graphics.Blit(null, ForceBuffer, injectionMaterial, 1);
        }

        // Simulation
        MarblingSimulator.UpdateSimulation();
        

        PreviewQuadMeshRenderer.sharedMaterial.SetTexture("_BaseMap", VelocityBuffer);
        
        previousSnappedPosition = snappedPosition;
        previousPosition = PlayerTransform.position;
        
        // flip = !flip;
    }

    private void OffsetBuffer(RenderTexture inputBuffer, int numX, Vector2 texelOffset)
    {
        OffsetCompute.SetFloats("offset", texelOffset.x, texelOffset.y);
        OffsetCompute.SetTexture(0, "Previous", inputBuffer);
        OffsetCompute.SetTexture(0, "Result", tempCopy);
        OffsetCompute.Dispatch(0, numX, numX, 1);
        
        Graphics.Blit(tempCopy, inputBuffer);
    }

    private Vector3 SnapPosition(Vector3 pos)
    {
        return new Vector3(pos.x - (pos.x % StepSize), pos.y, pos.z - (pos.z % StepSize));
    }
}