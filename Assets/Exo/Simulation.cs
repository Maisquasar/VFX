using System;
using UnityEngine;

public class Simulation : MonoBehaviour
{
    public enum Resolution
    {
        _32 = 32,
        _64 = 64,
        _128 = 128
    }
    
    public Transform PlayerTransform;
    public float StepSize = 0.5f;
    public Resolution SimulationResolution = Resolution._32;
    public float PreviewDistance = 1f;

    public Transform PreviewQuadTransform;
    public MeshRenderer PreviewQuadMeshRenderer;
    
    public ComputeShader SimulationComputeShader;
    
    Vector3 previousPosition;
    
    RenderTexture buffer;
    RenderTexture buffer2;

    private bool flip;

    private void OnEnable()
    {
        previousPosition = SnapPosition(PlayerTransform.position);
        
        buffer = CreateBuffer("Simulation Buffer");
        buffer2 = CreateBuffer("Simulation Buffer 2");
    }
    
    private void OnDisable()
    {
        buffer.Release();
    }

    RenderTexture CreateBuffer(string name)
    {
        var buffer =  new RenderTexture((int)SimulationResolution, (int)SimulationResolution, 0, 
                RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        
        buffer.enableRandomWrite = true;
        buffer.name = name;
        buffer.Create();
        
        return buffer;
    }

    Vector2 WorldToSim(Vector3 pos, Vector3 simulationCenter, float simulationSize)
    {
        pos = (pos - simulationCenter) / simulationSize;
        return new Vector2(pos.x, pos.z);
    }

    private void LateUpdate()
    {
        float scale = (int)SimulationResolution * StepSize;
        var snappedPosition = SnapPosition(PlayerTransform.position);
        PreviewQuadTransform.position = snappedPosition + Vector3.up * PreviewDistance;
        PreviewQuadTransform.localScale = new Vector3(scale, scale, scale);

        Vector2 texelOffset = new Vector2Int(
            (int)((snappedPosition.x - previousPosition.x) / StepSize),
            (int)((snappedPosition.z - previousPosition.z) / StepSize)
        );
        
        int numX = (int)SimulationResolution / 8;
        SimulationComputeShader.SetInt("resolution", (int)SimulationResolution);
        Vector2 playerPosition = WorldToSim(PlayerTransform.position, snappedPosition, scale);
        SimulationComputeShader.SetFloats("playerPosition", playerPosition.x, playerPosition.y);
        
        SimulationComputeShader.SetTexture(0, "Previous", buffer);
        SimulationComputeShader.SetTexture(0, "Result", buffer2);
        SimulationComputeShader.Dispatch(0, numX, numX, 1);
        
        SimulationComputeShader.SetFloats("offset", texelOffset.x, texelOffset.y);
        SimulationComputeShader.SetTexture(1, "Previous", buffer2);
        SimulationComputeShader.SetTexture(1, "Result", buffer);
        SimulationComputeShader.Dispatch(1, numX, numX, 1);
        
        PreviewQuadMeshRenderer.sharedMaterial.SetTexture("_BaseMap", buffer);
        
        previousPosition = snappedPosition;
        
        // flip = !flip;
    }

    private Vector3 SnapPosition(Vector3 pos)
    {
        return new Vector3(pos.x - (pos.x % StepSize), pos.y, pos.z - (pos.z % StepSize));
    }
}