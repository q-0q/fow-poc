using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    public int alignment = 0;
    
    
    public float viewRange = 5f;
    private float _speed = 8f;
    private bool _revealedThisFrame = false;


    public Vector3 center = Vector3.zero;
    private  float radius = 3f;
    
    private float offsetX;
    private float offsetZ;

    public static Action SetMeshRendererVisibility;
    public static Action SetRevealedThisFrameFalse;
    
    // Start is called before the first frame update
    void Start()
    {
        Color color = alignment == 0 ? Color.blue : Color.red;
        Material material = GetComponent<MeshRenderer>().material;
        material.color = color;
        
        offsetX = Random.Range(0f, 1000f);
        offsetZ = Random.Range(0f, 1000f);

        center = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        HandleSelectedMovement();
        HandleUnselectedMovement();
    }

    void HandleSelectedMovement()
    {
        if (UnitController.Singleton.selectedUnit != this) return;

        Vector2 vector2 = new Vector2(0, 0);
        if (Input.GetKey(KeyCode.W)) vector2 += new Vector2(0, 1);
        if (Input.GetKey(KeyCode.A)) vector2 += new Vector2(-1, 0);
        if (Input.GetKey(KeyCode.S)) vector2 += new Vector2(0, -1);
        if (Input.GetKey(KeyCode.D)) vector2 += new Vector2(1, 0);

        vector2 = vector2.normalized * (_speed * Time.deltaTime);

        transform.position += new Vector3(vector2.x, 0, vector2.y);
    }
    
    void HandleUnselectedMovement()
    {
        if (UnitController.Singleton.selectedUnit == this) return;
        
        float time = Time.time * _speed * 0.1f;

        float x = Mathf.PerlinNoise(time + offsetX, 0f) * 2f - 1f;
        float y = 0;
        float z = Mathf.PerlinNoise(time + offsetZ, 2f) * 2f - 1f;

        Vector3 noiseOffset = new Vector3(x, y, z) * radius;
        transform.position = center + noiseOffset;
    }

    public void SetRevealedThisFrame(bool val)
    {
        _revealedThisFrame = val;
    }
    
    private void SetMeshRenderer()
    {
        GetComponent<MeshRenderer>().enabled = _revealedThisFrame;
    }

    private void OnEnable()
    {
        SetMeshRendererVisibility += SetMeshRenderer;
        SetRevealedThisFrameFalse += OnSetRevealedThisFrameFalse;
    }

    private void OnSetRevealedThisFrameFalse()
    {
        SetRevealedThisFrame(false);
    }

    private void OnDestroy()
    {
        SetMeshRendererVisibility -= SetMeshRenderer;
        SetRevealedThisFrameFalse -= OnSetRevealedThisFrameFalse;
    }
}
