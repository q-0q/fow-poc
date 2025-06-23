
using System.Collections.Generic;
using UnityEngine;

public class FogOfWarComputeShaderInterface
{
    public ComputeShader fogComputeShader;
    public RenderTexture occlusionTexture;
    
    private RenderTexture outputTexture;
    private ComputeBuffer observerBuffer;
    private int kernelID;
    
    public void Start(ComputeShader _fogComputeShader, RenderTexture _occlusionTexture, RenderTexture _outputTexture)
    {

        fogComputeShader = _fogComputeShader;
        occlusionTexture = _occlusionTexture;
        
        outputTexture = _outputTexture;
        outputTexture.enableRandomWrite = true;
        outputTexture.filterMode = FilterMode.Point;
        outputTexture.Create();
        kernelID = fogComputeShader.FindKernel("CSMain");

        observerBuffer = null;


    }

    public void Update(Vector2 worldMin, Vector2 worldMax)
    {
        if (fogComputeShader is null) return;
        if (observerBuffer is null) return;

        // Set compute shader parameters
        fogComputeShader.SetTexture(kernelID, "Result", outputTexture);
        fogComputeShader.SetTexture(kernelID, "OcclusionTex", occlusionTexture);
        fogComputeShader.SetBuffer(kernelID, "Observers", observerBuffer);
        fogComputeShader.SetVector("WorldMin", worldMin);
        fogComputeShader.SetVector("WorldMax", worldMax);
        fogComputeShader.SetInts("TextureSize", outputTexture.width, outputTexture.height);

        int threadGroupsX = Mathf.CeilToInt(outputTexture.width / 8f);
        int threadGroupsY = Mathf.CeilToInt(outputTexture.height / 8f);

        fogComputeShader.Dispatch(kernelID, threadGroupsX, threadGroupsY, 1);
    }
    
    public void UpdateUnits(List<Unit> units)
    {
        if (observerBuffer != null)
            observerBuffer.Release();

        fogComputeShader.SetInt("ObserverCount", units.Count);

        observerBuffer = new ComputeBuffer(units.Count, sizeof(float) * 4);
        Vector4[] data = new Vector4[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            float x = units[i].transform.position.x;
            float y = units[i].transform.position.z;
            float radius = units[i].viewRange;
            data[i] = new Vector4(x, y, radius, 0);
        }

        observerBuffer.SetData(data);
    }

    public void OnDestroy()
    {
        if (observerBuffer != null)
            observerBuffer.Release();
        if (outputTexture != null)
            outputTexture.Release();
    }
}
