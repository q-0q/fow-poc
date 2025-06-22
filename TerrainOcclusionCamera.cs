using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainOcclusionCamera : MonoBehaviour
{
    
    public Shader replacementShader;
    
    // Start is called before the first frame update
    void Start()
    {
        if (replacementShader == null)
        {
            Debug.LogError("Replacement shader not assigned!");
            return;
        }

        Camera cam = GetComponent<Camera>();
        cam.RenderWithShader(replacementShader, "");
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
