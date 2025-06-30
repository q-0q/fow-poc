
using System.Collections.Generic;
using UnityEngine;

public class FogOfWarVolumeComputeShaderInterface
{
    public Material fogMaterial;
    public ComputeShader fogComputeShader;
    public RenderTexture occlusionTexture;
    
    private RenderTexture outputTexture;
    private ComputeBuffer observerBuffer;
    private int kernelID;
    
    public void Start(ComputeShader _fogComputeShader, Material _fogMaterial, RenderTexture _occlusionTexture, RenderTexture _outputTexture)
    {

        fogComputeShader = _fogComputeShader;
        fogMaterial = _fogMaterial;
        occlusionTexture = _occlusionTexture;
        
        outputTexture = _outputTexture;
        outputTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        outputTexture.volumeDepth = 256;
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();;
        kernelID = fogComputeShader.FindKernel("CSMain");
        
        Camera.main.depthTextureMode |= DepthTextureMode.Depth;

        observerBuffer = null;


    }

    public void Update(Vector3 worldMin, Vector3 worldMax)
    {
        if (fogComputeShader is null) return;
        if (observerBuffer is null) return;

        // Set compute shader parameters
        fogComputeShader.SetTexture(kernelID, "ResultVolume", outputTexture);
        fogComputeShader.SetTexture(kernelID, "OcclusionTex", occlusionTexture);
        fogComputeShader.SetBuffer(kernelID, "Observers", observerBuffer);
        fogComputeShader.SetVector("WorldMin", worldMin);
        fogComputeShader.SetVector("WorldMax", worldMax);
        fogComputeShader.SetInts("VolumeSize", outputTexture.width, outputTexture.height, outputTexture.volumeDepth);

        int groupsX = Mathf.CeilToInt(outputTexture.width / 8.0f);
        int groupsY = Mathf.CeilToInt(outputTexture.height / 8.0f);
        int groupsZ = outputTexture.volumeDepth; // since thread group size z is 1
        fogComputeShader.Dispatch(kernelID, groupsX, groupsY, groupsZ);
        
        fogMaterial.SetVector("_BoxMin", worldMin);
        fogMaterial.SetVector("_BoxMax", worldMax);

        
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
            Vector3 pos = units[i].transform.position;
            data[i] = new Vector4(pos.x, pos.y, pos.z, units[i].viewRange); // x, z, y, radius
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
