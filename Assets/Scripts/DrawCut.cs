using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCut : MonoBehaviour
{
    Vector3 pointA;
    Vector3 pointB;
    
    Camera cam;
    public GameObject obj;

    void Start() {
        cam = FindObjectOfType<Camera>();
    }

    void Update()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;

        if (Input.GetMouseButtonDown(0)) {
            pointA = cam.ScreenToWorldPoint(mouse);
            
        }
        if (Input.GetMouseButtonUp(0)) {
            pointB = cam.ScreenToWorldPoint(mouse);
            CreateSlicePlane();
        }
    }

    void CreateSlicePlane() 
    {
        Vector3 cutDir = Vector3.Cross((pointA-pointB),(pointA-cam.transform.position)).normalized;
        
        Ray ray = new Ray(pointA, (pointB - pointA).normalized);
        var all = Physics.RaycastAll(ray);
        {
            foreach (var hit in all)
            {
                Debug.Log(hit.collider.gameObject.name);
                Cutter.Cut(hit.collider.gameObject, hit.point, cutDir,null,true,true);
            }
        }
        
    }
}
