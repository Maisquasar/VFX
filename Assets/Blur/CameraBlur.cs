using System;
using UnityEngine;
using UnityEngine.VFX.Utility;

[ExecuteAlways]
public class CameraBlur : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] MeshRenderer meshRenderer;
    
    private ExposedProperty materialBaseMap = "_BaseMap";
    private RenderTexture blurredTexture;

    private void OnEnable()
    {
        blurredTexture = new RenderTexture(renderTexture.width, renderTexture.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };
        blurredTexture.Create();
    }

    private void OnDisable()
    {
        blurredTexture.Release();
    }

    private void LateUpdate()
    {
        Debug.Assert(renderTexture != null);
        Debug.Assert(meshRenderer != null);
        Debug.Assert(computeShader != null);

        int numThreadsX = renderTexture.width / 4;
        int numThreadsY = renderTexture.height / 4;
            
        computeShader.SetTexture(0, "_Source", renderTexture);
        computeShader.SetTexture(0, "_Destination", blurredTexture);
        
        computeShader.SetInt("_TextureWidth", renderTexture.width);
        computeShader.SetInt("_TextureHeight", renderTexture.height);
        
        computeShader.Dispatch(0, numThreadsX, numThreadsY, 1);
        
        meshRenderer.sharedMaterial.SetTexture(materialBaseMap, blurredTexture);
    }
}
