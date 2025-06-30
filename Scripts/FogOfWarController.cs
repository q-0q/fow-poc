using UnityEngine;
using UnityEngine.Serialization;

public class FogOfWarController : MonoBehaviour
{
    [Header("Occlusion texture world bounds")]
    public Vector3 worldMin = new Vector3(0, 0, 0);
    public Vector3 worldMax = new Vector3(10, 0, 10);
    public RenderTexture occlusionTexture;
    
    public ComputeShader computeVolumeShader;
    public Material computeVolumeMaterial;
    public RenderTexture outputVolumeTexture;
    
    private FogOfWarVolumeComputeShaderInterface _fogVolumeCompute;
    
    

    void Update()
    {
        ComputeShader();
    }
    
    void Start()
    {
        _fogVolumeCompute = new FogOfWarVolumeComputeShaderInterface();
        _fogVolumeCompute.Start(computeVolumeShader, computeVolumeMaterial, occlusionTexture, outputVolumeTexture);

    }
    

    void ComputeShader()
    {
        HandleUnitVisibility();
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
        _fogVolumeCompute.UpdateUnits(UnitController.Singleton.GetAlignedUnits());
    }

}