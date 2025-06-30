using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

public class FogVolume : MonoBehaviour
{
    
    public RenderTexture OutputVolumeTexture;
    public RenderTexture OcclusionTexture;
    
    // Start is called before the first frame update
    void Start()
    {
        OutputVolumeTexture = CreateVolumeTexture(256);
        OcclusionTexture = CreateOcclusionTexture(256);
        GetComponentInChildren<Camera>().targetTexture = OcclusionTexture;
    }
    
    RenderTexture CreateVolumeTexture(int size)
    {
        var rt = new RenderTexture(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = size,
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        rt.Create();
        return rt;
    }
    
    RenderTexture CreateOcclusionTexture(int size)
    {
        var rt = new RenderTexture(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            
        };
        rt.Create();
        return rt;
    }

    public Bounds GetWorldBounds()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Bounds worldBounds = meshRenderer.bounds;
        return worldBounds;
    }

    public void SetMaterialProps(MaterialPropertyBlock props)
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.SetPropertyBlock(props);
    }
}
