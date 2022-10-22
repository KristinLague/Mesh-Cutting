using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DrawCut : MonoBehaviour
{
   // public Transform boxVis;
    Vector3 pointA;
    Vector3 pointB;


    Camera cam;
    public GameObject obj;
    public Material cutMat;

    void Start() {
        cam = FindObjectOfType<Camera>();
    }

    void Update()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z;

        if (Input.GetMouseButtonDown(0))
        {
            pointA = cam.ScreenToWorldPoint(mouse);
        }

        if (Input.GetMouseButtonUp(0)) {
            pointB = cam.ScreenToWorldPoint(mouse);
            CreateSlicePlane();
        }
    }

    void CreateSlicePlane() 
    {
        Vector3 pointInPlane = (pointA + pointB) / 2;
        
        Vector3 cutPlaneNormal = Vector3.Cross((pointA-pointB),(pointA-cam.transform.position)).normalized;
        Quaternion orientation = Quaternion.FromToRotation(Vector3.up, cutPlaneNormal);
        //boxVis.rotation = orientation;
       // boxVis.localScale = new Vector3(10, 0.25f, 10);
       // boxVis.position = pointInPlane;

        
        var all = Physics.OverlapBox(pointInPlane, new Vector3(100, 0.01f, 100), orientation);
        
        //Ray ray = new Ray(pointA, (pointB - pointA).normalized);
        //var all = Physics.RaycastAll(ray);
        {
            foreach (var hit in all)
            {
                MeshFilter filter = hit.gameObject.GetComponentInChildren<MeshFilter>();
                if(filter != null)
                    Cutter.Cut(hit.gameObject, pointInPlane, cutPlaneNormal,cutMat,true,true);
            }
        }
        
    }
}
