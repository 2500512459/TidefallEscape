using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    public float velocity = 15;
    [SerializeField] Material materialRange;
    [SerializeField] Material materialTrajectory;
    Transform range;
    Transform trajectory;
    Vector3 targetPosition = new Vector3(10, 0, 0);
    float currentVelocity = 0;
    // Start is called before the first frame update
    void Start()
    {
        range = CreateDynamicMeshObject("Range", CreateCircleEdgeMesh(0.98f, 1f, 60), materialRange);
        trajectory = CreateDynamicMeshObject("Trajectory", CreatePlaneMesh(50, 2), materialTrajectory);
    }
    // Update is called once per frame
    void Update()
    {
        UpdateTargetPosition();
        targetPosition.y = transform.position.y;
        trajectory.LookAt(targetPosition);
        float G = 9.8f;
        float maxZCoordinate = velocity * velocity / G;
        range.localScale = Vector3.one * maxZCoordinate;
        float distance = (targetPosition - transform.position).magnitude;
        if (distance > maxZCoordinate)
        {
            trajectory.localScale = new Vector3(0.2f, 1, maxZCoordinate);
            currentVelocity = velocity;
            Shader.SetGlobalFloat("_LaunchVelocity", currentVelocity);
        }
        else
        {
            trajectory.localScale = new Vector3(0.2f, 1, distance);
            currentVelocity = velocity * Mathf.Sqrt(distance / maxZCoordinate);
            Shader.SetGlobalFloat("_LaunchVelocity", currentVelocity);
        }
    }
    Mesh CreateCircleEdgeMesh(float innerRadius, float outerRadius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        float anglePerSegment = 2 * Mathf.PI / segments;
        List<int> innerCircleVertices = new List<int>();
        List<int> outerCircleVertices = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float angle = anglePerSegment * i;
            Vector3 innerVertex = new Vector3(innerRadius * Mathf.Cos(angle), 0, innerRadius * Mathf.Sin(angle));
            Vector3 outerVertex = new Vector3(outerRadius * Mathf.Cos(angle), 0, outerRadius * Mathf.Sin(angle));
            int innerIndex = vertices.Count;
            int outerIndex = innerIndex + 1;
            vertices.Add(innerVertex);
            vertices.Add(outerVertex);
            uvs.Add(new Vector2((float)i / segments, 1));
            uvs.Add(new Vector2((float)i / segments, 0));
            innerCircleVertices.Add(innerIndex);
            outerCircleVertices.Add(outerIndex);
        }
        for (int i = 0; i < segments; i++)
        {
            int innerCurrent = innerCircleVertices[i];
            int innerNext = innerCircleVertices[(i + 1) % segments];
            int outerCurrent = outerCircleVertices[i];
            int outerNext = outerCircleVertices[(i + 1) % segments];
            triangles.Add(innerCurrent);
            triangles.Add(innerNext);
            triangles.Add(outerCurrent);
            triangles.Add(outerCurrent);
            triangles.Add(innerNext);
            triangles.Add(outerNext);
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        return mesh;
    }
    Mesh CreatePlaneMesh(int widthSegments, int heightSegments)
    {
        Mesh mesh = new Mesh();
        float width = 1f / widthSegments;
        float height = 1f / heightSegments;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int y = 0; y < heightSegments + 1; y++)
        {
            for (int x = 0; x < widthSegments + 1; x++)
            {
                Vector3 vertex = new Vector3(-0.5f + y * height, 0, x * width);
                vertices.Add(vertex);
            }
        }
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int topLeft = y * (widthSegments + 1) + x;
                int topRight = topLeft + 1;
                int bottomLeft = (y + 1) * (widthSegments + 1) + x;
                int bottomRight = bottomLeft + 1;
                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }
    Transform CreateDynamicMeshObject(string name, Mesh mesh, Material mat)
    {
        GameObject dynamicMeshObject = new GameObject(name);
        MeshFilter meshFilter = dynamicMeshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshRenderer meshRenderer = dynamicMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;
        dynamicMeshObject.transform.SetParent(transform);
        dynamicMeshObject.transform.localPosition = Vector3.zero;
        dynamicMeshObject.transform.localEulerAngles = Vector3.zero;
        dynamicMeshObject.transform.localScale = Vector3.one;
        return dynamicMeshObject.transform;
    }
    void UpdateTargetPosition()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                targetPosition = hit.point;
            }
        }
    }
    public float GetCurrentVelocity()
    {
        return currentVelocity;
    }
    public Vector3 GetShootDirection()
    {
        Quaternion rotation = Quaternion.Euler(-45, 0, 0);
        return trajectory.rotation * rotation * Vector3.forward;
    }
}


