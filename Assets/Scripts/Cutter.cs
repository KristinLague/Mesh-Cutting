using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    public static bool currentlyCutting;
    public static Mesh originalMesh;

    public static void Cut(GameObject _originalGameObject, Vector3 _contactPoint, Vector3 _direction, Material _cutMaterial = null, bool fill = true, bool _addRigidbody = false)
    {
        if(currentlyCutting)
        {
            return;
        }

        currentlyCutting = true;

        //We are instantiating a plane through our initial object to seperate the left and right side from each other
        Plane plane = new Plane(_originalGameObject.transform.InverseTransformDirection(-_direction), _originalGameObject.transform.InverseTransformPoint(_contactPoint));
        originalMesh = _originalGameObject.GetComponent<MeshFilter>().mesh;
        List<Vector3> addedVertices = new List<Vector3>();

        //We are getting two new generated meshes for our left and right side
        GeneratedMesh leftMesh = new GeneratedMesh();
        GeneratedMesh rightMesh = new GeneratedMesh();

        //Some meshes use different submeshes to have multiple materials attached to them
        //in an early iteration I had an extra script to turn everything into one mesh to make my life a little easier
        //however the result was not great because I could only slice objects that had one single material
        int[] submeshIndices;
        int triangleIndexA, triangleIndexB, triangleIndexC;
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            submeshIndices = originalMesh.GetTriangles(i);

            //We are now going through the submesh indices as triangles to determine on what side of the mesh they are.
            for (int j = 0; j < submeshIndices.Length; j+=3)
            {
                triangleIndexA = submeshIndices[j];
                triangleIndexB = submeshIndices[j + 1];
                triangleIndexC = submeshIndices[j + 2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA,triangleIndexB,triangleIndexC,i);

                //We are now using the plane.getside function to see on which side of the cut our trianle is situated 
                //or if it might be cut through
                bool triangleALeftSide = plane.GetSide(originalMesh.vertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexC]);

                //All three vertices are on the left side of the plane, so they need to be added to the left
                //mesh
                if(triangleALeftSide && triangleBLeftSide && triangleCLeftSide)
                {
                    leftMesh.AddTriangle(currentTriangle);
                }
                //All three vertices are on the right side of the mesh.
                else if (!triangleALeftSide && !triangleBLeftSide && !triangleCLeftSide)
                {
                    rightMesh.AddTriangle(currentTriangle);
                }
                else
                {
                    
                    CutTriangle(plane,currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide,leftMesh,rightMesh,addedVertices);
                }
            }
        }

        //Filling our cut
        if(fill == true)
        {
            FillCut(addedVertices, plane, leftMesh, rightMesh);
        }

        Mesh finishedLeftMesh = leftMesh.GetGeneratedMesh();
        Mesh finishedRightMesh = rightMesh.GetGeneratedMesh();

        _originalGameObject.GetComponent<MeshFilter>().mesh = finishedLeftMesh;
        _originalGameObject.AddComponent<MeshCollider>().sharedMesh = finishedLeftMesh;
        _originalGameObject.GetComponent<MeshCollider>().convex = true;

        Material[] mats = new Material[finishedLeftMesh.subMeshCount];
        for (int i = 0; i < finishedLeftMesh.subMeshCount; i++)
		{
            mats[i] = _originalGameObject.GetComponent<MeshRenderer>().material;
        }
        _originalGameObject.GetComponent<MeshRenderer>().materials = mats;

        GameObject rightGO = new GameObject();
        rightGO.transform.position = _originalGameObject.transform.position + (Vector3.up * .05f);
        rightGO.transform.rotation = _originalGameObject.transform.rotation;
        rightGO.transform.localScale = _originalGameObject.transform.localScale;
        rightGO.AddComponent<MeshRenderer>();
        mats = new Material[finishedRightMesh.subMeshCount];
        for (int i = 0; i < finishedRightMesh.subMeshCount; i++)
		{
            mats[i] = _originalGameObject.GetComponent<MeshRenderer>().material;
        }
        rightGO.GetComponent<MeshRenderer>().materials = mats;

        rightGO.AddComponent<MeshFilter>().mesh = finishedRightMesh;

        rightGO.AddComponent<MeshCollider>().sharedMesh = finishedRightMesh;
        rightGO.GetComponent<MeshCollider>().convex = true;

        if(_addRigidbody == true)
        {
            rightGO.AddComponent<Rigidbody>();
        }

        currentlyCutting = false;

    }

    private static MeshTriangle GetTriangle(int _triangleIndexA, int _triangleIndexB, int _triangleIndexC, int _submeshIndex)
    {
        //Adding the Vertices at the triangleIndex
        Vector3[] verticesToAdd = new Vector3[]
        {
            originalMesh.vertices[_triangleIndexA],
            originalMesh.vertices[_triangleIndexB],
            originalMesh.vertices[_triangleIndexC]
        };

        //Adding the normals at the triangle index
        Vector3[] normalsToAdd = new Vector3[]
        {
            originalMesh.normals[_triangleIndexA],
            originalMesh.normals[_triangleIndexB],
            originalMesh.normals[_triangleIndexC]
        };

        //adding the uvs at the triangleIndex
        Vector2[] uvsToAdd = new Vector2[]
        {
            originalMesh.uv[_triangleIndexA],
            originalMesh.uv[_triangleIndexB],
            originalMesh.uv[_triangleIndexC]
        };

        return new MeshTriangle(verticesToAdd, normalsToAdd, uvsToAdd, _submeshIndex);
    }

    private static void CutTriangle(Plane _plane,MeshTriangle _triangle, bool _triangleALeftSide, bool _triangleBLeftSide, bool _triangleCLeftSide,
    GeneratedMesh _leftSide, GeneratedMesh _rightSide, List<Vector3> _addedVertices)
    {
        List<bool> leftSide = new List<bool>();
        leftSide.Add(_triangleALeftSide);
        leftSide.Add(_triangleBLeftSide);
        leftSide.Add(_triangleCLeftSide);

        MeshTriangle leftMeshTriangle = new MeshTriangle(new Vector3[2],new Vector3[2],new Vector2[2],_triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], _triangle.SubmeshIndex);

        bool left = false;
        bool right = false;

        for (int i = 0; i < 3; i++)
        {
            if(leftSide[i])
            {
                if (!left)
                {
                    left = true;

                    leftMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    leftMeshTriangle.Vertices[1] = leftMeshTriangle.Vertices[0];

                    leftMeshTriangle.UVs[0] = _triangle.UVs[i];
                    leftMeshTriangle.UVs[1] = leftMeshTriangle.UVs[0];

                    leftMeshTriangle.Normals[0] = _triangle.Normals[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Normals[0];
                }
                else
                {
                    leftMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = _triangle.Normals[i];
                    leftMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
            else
            {
                if(!right)
                {
                    right = true;

                    rightMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    rightMeshTriangle.Vertices[1] = rightMeshTriangle.Vertices[0];

                    rightMeshTriangle.UVs[0] = _triangle.UVs[i];
                    rightMeshTriangle.UVs[1] = rightMeshTriangle.UVs[0];

                    rightMeshTriangle.Normals[0] = _triangle.Normals[i];
                    rightMeshTriangle.Normals[1] = rightMeshTriangle.Normals[0];

                }
                else
                {
                    rightMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    rightMeshTriangle.Normals[1] = _triangle.Normals[i];
                    rightMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
        }

        float normalizedDistance;
        float distance;
        _plane.Raycast(new Ray(leftMeshTriangle.Vertices[0], (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).normalized), out distance);

        normalizedDistance = distance / (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).magnitude;
        Vector3 vertLeft = Vector3.Lerp(leftMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[0], normalizedDistance);
        _addedVertices.Add(vertLeft);

        Vector3 normalLeft = Vector3.Lerp(leftMeshTriangle.Normals[0], rightMeshTriangle.Normals[0], normalizedDistance);
        Vector2 uvLeft = Vector2.Lerp(leftMeshTriangle.UVs[0], rightMeshTriangle.UVs[0], normalizedDistance);
        
        _plane.Raycast(new Ray(leftMeshTriangle.Vertices[1], (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).normalized), out distance);

        normalizedDistance = distance / (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).magnitude;
        Vector3 vertRight = Vector3.Lerp(leftMeshTriangle.Vertices[1], rightMeshTriangle.Vertices[1], normalizedDistance);
        _addedVertices.Add(vertRight);

        Vector3 normalRight = Vector3.Lerp(leftMeshTriangle.Normals[1], rightMeshTriangle.Normals[1], normalizedDistance);
        Vector2 uvRight = Vector2.Lerp(leftMeshTriangle.UVs[1], rightMeshTriangle.UVs[1], normalizedDistance);

        //TESTING OUR FIRST TRIANGLE
        MeshTriangle currentTriangle;
        Vector3[] updatedVertices = new Vector3[] { leftMeshTriangle.Vertices[0], vertLeft, vertRight };
        Vector3[] updatedNormals = new Vector3[] { leftMeshTriangle.Normals[0], normalLeft, normalRight };
        Vector2[] updatedUVs = new Vector2[] { leftMeshTriangle.UVs[0], uvLeft, uvRight };
        
       currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);

        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            _leftSide.AddTriangle(currentTriangle);
        }

        //SECOND TRIANGLE 
        updatedVertices = new Vector3[] { leftMeshTriangle.Vertices[0], leftMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { leftMeshTriangle.Normals[0], leftMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { leftMeshTriangle.UVs[0],leftMeshTriangle.UVs[1], uvRight };


        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            _leftSide.AddTriangle(currentTriangle);
        }

        //THIRD TRIANGLE 
        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], vertLeft, vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], normalLeft, normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            _rightSide.AddTriangle(currentTriangle);
        }

        //FOURTH TRIANGLE 
        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], rightMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],rightMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            _rightSide.AddTriangle(currentTriangle);
        }
    }

    private static void FlipTriangel(MeshTriangle _triangle)
    {
        Vector3 temp = _triangle.Vertices[2];
        _triangle.Vertices[2] = _triangle.Vertices[0];
        _triangle.Vertices[0] = temp;

        temp = _triangle.Normals[2];
		_triangle.Normals[2] = _triangle.Normals[0];
		_triangle.Normals[0] = temp;

		Vector2 temp2 = _triangle.UVs[2];
		_triangle.UVs[2] = _triangle.UVs[0];
		_triangle.UVs[0] = temp2;
		
    }

    public static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> polygone = new List<Vector3>();

        for (int i = 0; i < _addedVertices.Count; i++)
        {
            if(!vertices.Contains(_addedVertices[i]))
            {
                polygone.Clear();
                polygone.Add(_addedVertices[i]);
                polygone.Add(_addedVertices[i + 1]);

                vertices.Add(_addedVertices[i]);
                vertices.Add(_addedVertices[i + 1]);

                EvaluatePairs(_addedVertices, vertices, polygone);
                Fill(polygone, _plane, _leftMesh, _rightMesh);
            }
        }
    }

    public static void EvaluatePairs(List<Vector3> _addedVertices,List<Vector3> _vertices, List<Vector3> _polygone)
    {
        bool isDone = false;
        while(!isDone)
        {
            isDone = true;
            for (int i = 0; i < _addedVertices.Count; i+=2)
            {
                if(_addedVertices[i] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i + 1]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i + 1]);
                    _vertices.Add(_addedVertices[i + 1]);
                } 
                else if (_addedVertices[i + 1] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i]);
                    _vertices.Add(_addedVertices[i]);
                }
            }
        }
    }

    public static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        //Firstly we need the center we do this by adding up all the vertices and then calculating the average
        Vector3 centerPosition = Vector3.zero;
        for (int i = 0; i < _vertices.Count; i++)
        {
            centerPosition += _vertices[i];
        }
        centerPosition = centerPosition / _vertices.Count;

        //We now need an Upward Axis we use the plane we cut the mesh with for that 
        Vector3 up = new Vector3()
        {
            x = _plane.normal.x,
            y = _plane.normal.y,
            z = _plane.normal.z
        };

        Vector3 left = Vector3.Cross(_plane.normal, up);

        Vector3 displacement = Vector3.zero;
        Vector2 uv1 = Vector2.zero;
        Vector2 uv2 = Vector2.zero;

        for (int i = 0; i < _vertices.Count; i++)
        {
            displacement = _vertices[i] - centerPosition;
            uv1 = new Vector2()
            {
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            displacement = _vertices[(i + 1) % _vertices.Count] - centerPosition;
            uv2 = new Vector2()
            { 
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            Vector3[] vertices = new Vector3[]{_vertices[i], _vertices[(i+1) % _vertices.Count], centerPosition};
			Vector3[] normals = new Vector3[]{-_plane.normal, -_plane.normal, -_plane.normal};
			Vector2[] uvs   = new Vector2[]{uv1, uv2, new Vector2(0.5f, 0.5f)};

            MeshTriangle currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _leftMesh.AddTriangle(currentTriangle);

            normals = new Vector3[] { _plane.normal, _plane.normal, _plane.normal };
            currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _rightMesh.AddTriangle(currentTriangle);
        
        } 
    }
}
    
