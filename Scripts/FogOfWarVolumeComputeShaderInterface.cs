
using System.Collections.Generic;
using UnityEngine;

public class FogOfWarVolumeComputeShaderInterface
{
    public Material fogMaterial;
    public ComputeShader fogComputeShader;
    private ComputeBuffer observerBuffer;
    private int kernelID;
    
    public void Start(ComputeShader _fogComputeShader, Material _fogMaterial)
    {

        fogComputeShader = _fogComputeShader;
        fogMaterial = _fogMaterial;
        kernelID = fogComputeShader.FindKernel("CSMain");
        
        Camera.main.depthTextureMode |= DepthTextureMode.Depth;

        observerBuffer = null;


    }

    public void Update(FogVolume fogVolume)
    {
        if (fogComputeShader is null) return;
        if (observerBuffer is null) return;

        var bounds = fogVolume.GetWorldBounds();
        var worldMin = bounds.min;
        var worldMax = bounds.max;
        
        Debug.Log("min: " + worldMin + ", max:" + worldMax);
        
        // Set compute shader parameters
        fogComputeShader.SetTexture(kernelID, "ResultVolume", fogVolume.OutputVolumeTexture);
        fogComputeShader.SetTexture(kernelID, "OcclusionTex", fogVolume.OcclusionTexture);
        fogComputeShader.SetBuffer(kernelID, "Observers", observerBuffer);
        fogComputeShader.SetVector("WorldMin", worldMin);
        fogComputeShader.SetVector("WorldMax", worldMax);
        fogComputeShader.SetInts("VolumeSize", fogVolume.OutputVolumeTexture.width, fogVolume.OutputVolumeTexture.height, fogVolume.OutputVolumeTexture.volumeDepth);
        

        Vector3 scale = worldMax - worldMin;
        Vector3 offset = worldMin;
        Matrix4x4 volumeToWorld = Matrix4x4.TRS(offset, Quaternion.identity, scale);
        Matrix4x4 worldToVolume = volumeToWorld.inverse;
        
        
        fogComputeShader.SetMatrix("VolumeToWorldMatrix", volumeToWorld);
        fogComputeShader.SetMatrix("WorldToVolumeMatrix", worldToVolume);
        

        int groupsX = Mathf.CeilToInt(fogVolume.OutputVolumeTexture.width / 8.0f);
        int groupsY = Mathf.CeilToInt(fogVolume.OutputVolumeTexture.height / 8.0f);
        int groupsZ = fogVolume.OutputVolumeTexture.volumeDepth; // since thread group size z is 1
        fogComputeShader.Dispatch(kernelID, groupsX, groupsY, groupsZ);
        
        var props = new MaterialPropertyBlock();
        props.SetTexture("_MainTex", fogVolume.OutputVolumeTexture);
        props.SetVector("_WorldMin", bounds.min);
        props.SetVector("_WorldMax", bounds.max);
        props.SetMatrix("_VolumeToWorldMatrix", volumeToWorld); // <-- this is critical

        fogVolume.SetMaterialProps(props);
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
}
