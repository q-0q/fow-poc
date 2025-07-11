using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UnitController : MonoBehaviour
{

    public static UnitController Singleton;

    public Unit selectedUnit;
    private Camera _camera;

    public Dictionary<int, List<Unit>> alignmentUnitMap;

    private Vector2 _cameraRotationAxes;

    private void Awake()
    {
        Singleton = this;
    }

    void Start()
    {
        _camera = Camera.main;
        _cameraRotationAxes = new Vector2();
        alignmentUnitMap = new Dictionary<int, List<Unit>>();
        alignmentUnitMap[0] = new List<Unit>();
        alignmentUnitMap[1] = new List<Unit>();
        
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            alignmentUnitMap[unit.alignment].Add(unit);
        }
    }

    void Update()
    {
        HandleSelectNewUnit();
        UpdateCameraHolderPosition();
    }
    
    // Camera + util functions
    
    void HandleSelectNewUnit()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        
        // todo put units on a layer and layermask for better performance
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        var unit = hit.transform.GetComponent<Unit>();
        if (unit is null) return;
        selectedUnit.center = selectedUnit.transform.position;
        selectedUnit = unit;
        
    }

    void UpdateCameraHolderPosition()
    {
        var selectedUnitTransform = selectedUnit.transform;
        var cameraHolder = _camera.transform.parent;
        Debug.DrawRay(selectedUnitTransform.position, Vector3.up * 15f, Color.green);
        cameraHolder.position =
            Vector3.Lerp(cameraHolder.position, selectedUnitTransform.position, Time.deltaTime * 8f);
        // cameraHolder.rotation = Quaternion.Euler(Input.mousePosition.y, Input.mousePosition.x , 0);
    }

    public List<Unit> GetAlignedUnits()
    {
        return alignmentUnitMap[selectedUnit.alignment];
    }
}
