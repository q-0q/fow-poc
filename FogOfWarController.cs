using UnityEngine;
using UnityEngine.Serialization;

public class FogOfWarController : MonoBehaviour
{
    public Material fogMaterial; // assign your FogOfWarRaymarch material here

    public Transform player;     // player transform (world position used)
    public float visionRadius = 5f;

    [Header("Occlusion texture world bounds")]
    public Vector2 worldMin = new Vector2(0, 0);
    public Vector2 worldMax = new Vector2(10, 10);
    public int textureResolution = 256;
    public ComputeShader computeShader;
    public RenderTexture occlusionTexture;
    public RenderTexture outputTexture;

    private FogOfWarComputeShaderInterface _fogCompute;
    
    

    void Update()
    {
        // FragmentShader();
        ComputeShader();
    }

    private void FragmentShader()
    {
        if (player is null || fogMaterial is null) return;

        Vector3 pos = player.position;

        // Pass player pos.x, pos.z + radius to shader (_PlayerPosRadius)
        fogMaterial.SetVector("_PlayerPosRadius", new Vector4(pos.x, pos.z, visionRadius, 0));

        // Pass world bounds used to map world space to UV coords in occlusion texture
        fogMaterial.SetVector("_WorldMin", new Vector4(worldMin.x, worldMin.y, 0, 0));
        fogMaterial.SetVector("_WorldMax", new Vector4(worldMax.x, worldMax.y, 0, 0));
    }
    
    void Start()
    {
        // Setup output texture
        _fogCompute = new FogOfWarComputeShaderInterface();
        _fogCompute.Start(computeShader, occlusionTexture, outputTexture, textureResolution);

    }
    

    void ComputeShader()
    {
        _fogCompute.Update(worldMin, worldMax);
    }
    
    
    void LateUpdate()
    {
        _fogCompute.UpdateUnits(UnitController.Singleton.GetAlignedUnits());
    }

}