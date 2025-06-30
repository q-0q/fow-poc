using UnityEngine;
using UnityEngine.Serialization;

public class FogOfWarController : MonoBehaviour
{
    public Material fogMaterial; // assign your FogOfWarRaymarch material here

    public Transform player;     // player transform (world position used)
    public float visionRadius = 5f;

    [Header("Occlusion texture world bounds")]
    public Vector3 worldMin = new Vector3(0, 0, 0);
    public Vector3 worldMax = new Vector3(10, 0, 10);
    public int textureResolution = 256;
    public ComputeShader computeShader;
    public RenderTexture occlusionTexture;
    public RenderTexture outputTexture;
    
    public ComputeShader computeVolumeShader;
    public Material computeVolumeMaterial;
    public RenderTexture outputVolumeTexture;

    private FogOfWarComputeShaderInterface _fogCompute;
    private FogOfWarVolumeComputeShaderInterface _fogVolumeCompute;
    
    

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
        // _fogCompute = new FogOfWarComputeShaderInterface();
        // _fogCompute.Start(computeShader, occlusionTexture, outputTexture);
        _fogVolumeCompute = new FogOfWarVolumeComputeShaderInterface();
        _fogVolumeCompute.Start(computeVolumeShader, computeVolumeMaterial, occlusionTexture, outputVolumeTexture);

    }
    

    void ComputeShader()
    {
        HandleUnitVisibility();
        // _fogCompute.Update(worldMin, worldMax);
        _fogVolumeCompute.Update(worldMin, worldMax);
    }

    void HandleUnitVisibility()
    {
        Unit.SetRevealedThisFrameFalse?.Invoke();
        
        foreach (var (alignment, units) in UnitController.Singleton.alignmentUnitMap)
        {
            if (alignment == UnitController.Singleton.selectedUnit.alignment)
            {
                foreach (var unit in units)
                {
                    unit.SetRevealedThisFrame(true);
                }
            }

            else
            {
                foreach (var unalignedUnit in units)
                {
                    foreach (var alignedUnit in UnitController.Singleton.GetAlignedUnits())
                    {
                        if (Vector3.Distance(unalignedUnit.transform.position, alignedUnit.transform.position) >
                            alignedUnit.viewRange)
                        {
                            continue;
                        }
                        
                        var direction = unalignedUnit.transform.position - alignedUnit.transform.position;
                        var unitDistance = Vector3.Distance(unalignedUnit.transform.position,
                            alignedUnit.transform.position);
                        var maxRaycastDistance = Mathf.Min(unitDistance, alignedUnit.viewRange);
                        
                        if (Physics.Raycast(alignedUnit.transform.position, direction, out RaycastHit hit, maxRaycastDistance,
                                LayerMask.GetMask("Terrain")))
                        {
                            Debug.DrawRay(alignedUnit.transform.position, direction, Color.red);
                            Debug.Log(hit.transform.name);
                        }
                        else
                        {
                            unalignedUnit.SetRevealedThisFrame(true);
                            break;
                        }
                    }
                }
            }
        }
        
        Unit.SetMeshRendererVisibility?.Invoke();
    }
    
    
    void LateUpdate()
    {
        // _fogCompute.UpdateUnits(UnitController.Singleton.GetAlignedUnits());
        _fogVolumeCompute.UpdateUnits(UnitController.Singleton.GetAlignedUnits());
    }

}