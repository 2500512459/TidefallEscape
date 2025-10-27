using UnityEngine;
using System.Collections.Generic;


public class Buoyancy : MonoBehaviour
{
    [SerializeField] private int slicesPerAxis = 2;
    [SerializeField] private int voxelsLimit = 16;
    [SerializeField] private float density = 500;

    private const float DAMPFER = 0.1f;
    private const float WATER_DENSITY = 1000;

    private float VoxelHalfHeight { get; set; }
    private Vector3 LocalArchimedesForce { get; set; }
    private List<Vector3> Voxels { get; set; }
    private List<Vector3[]> Forces { get; set; } // For drawing force gizmos

    private Rigidbody Rb { get; set; }
    private Collider Col { get; set; }

    private void Start()
    {
        InitializeComponents();
        CalculateVoxelHalfHeight();
        SetupRigidbody();
        CreateVoxels();
        CalculateArchimedesForce();
    }

    private void InitializeComponents()
    {
        Forces = new List<Vector3[]>();
        Col = GetComponent<Collider>() ?? gameObject.AddComponent<MeshCollider>();
        Rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
    }

    private void CalculateVoxelHalfHeight()
    {
        var bounds = Col.bounds;
        VoxelHalfHeight = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) / (2 * slicesPerAxis);
    }

    private void SetupRigidbody()
    {
        var bounds = Col.bounds;
        Rb.centerOfMass =
            new Vector3(0, -bounds.extents.y * 0f, 0) +
            transform.InverseTransformPoint(bounds.center);
    }

    private void CreateVoxels()
    {
        //Voxels = SliceIntoVoxels(IsMeshCollider);
        Voxels = SliceConvex();
        WeldPoints(Voxels, voxelsLimit);
    }

    private void CalculateArchimedesForce()
    {
        float volume = Rb.mass / density;
        float archimedesForceMagnitude = WATER_DENSITY * Mathf.Abs(Physics.gravity.y) * volume;
        LocalArchimedesForce = new Vector3(0, archimedesForceMagnitude, 0) / Voxels.Count;
    }

    //private List<Vector3> SliceIntoVoxels(bool mesh)
    //{
    //    return mesh ? SliceMesh() : SliceConvex();
    //}

    //private List<Vector3> SliceMesh()
    //{
    //    var points = new List<Vector3>();
    //    var meshCol = Col as MeshCollider;
    //    var convexValue = meshCol.convex;
    //    meshCol.convex = true;

    //    var bounds = Col.bounds;
    //    for (int ix = 0; ix < slicesPerAxis; ix++)
    //    {
    //        for (int iy = 0; iy < slicesPerAxis; iy++)
    //        {
    //            for (int iz = 0; iz < slicesPerAxis; iz++)
    //            {
    //                Vector3 p = CalculateVoxelPoint(bounds, ix, iy, iz);
    //                if (PointIsInsideMeshCollider(meshCol, p))
    //                {
    //                    points.Add(p);
    //                }
    //            }
    //        }
    //    }

    //    meshCol.convex = convexValue;
    //    return points.Count > 0 ? points : new List<Vector3> { bounds.center };
    //}

    private List<Vector3> SliceConvex()
    {
        var points = new List<Vector3>();
        var bounds = Col.bounds;
        for (int ix = 0; ix < slicesPerAxis; ix++)
        {
            for (int iy = 0; iy < slicesPerAxis; iy++)
            {
                for (int iz = 0; iz < slicesPerAxis; iz++)
                {
                    points.Add(CalculateVoxelPoint(bounds, ix, iy, iz));
                }
            }
        }
        return points;
    }

    private Vector3 CalculateVoxelPoint(Bounds bounds, int ix, int iy, int iz)
    {
        float x = bounds.min.x + bounds.size.x / slicesPerAxis * (0.5f + ix);
        float y = bounds.min.y + bounds.size.y / slicesPerAxis * (0.5f + iy);
        float z = bounds.min.z + bounds.size.z / slicesPerAxis * (0.5f + iz);
        return transform.InverseTransformPoint(new Vector3(x, y, z));
    }

    private static bool PointIsInsideMeshCollider(Collider c, Vector3 p)
    {
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        foreach (var ray in directions)
        {
            if (!c.Raycast(new Ray(p - ray * 1000, ray), out _, 1000f))
            {
                return false;
            }
        }

        return true;
    }

    private static void WeldPoints(IList<Vector3> list, int targetCount)
    {
        if (list.Count <= 2 || targetCount < 2)
        {
            return;
        }

        while (list.Count > targetCount)
        {
            int first, second;
            FindClosestPoints(list, out first, out second);

            var mixed = (list[first] + list[second]) * 0.5f;
            list.RemoveAt(second);
            list.RemoveAt(first);
            list.Add(mixed);
        }
    }

    private static void FindClosestPoints(IList<Vector3> list, out int firstIndex, out int secondIndex)
    {
        float minDistance = float.MaxValue;
        firstIndex = 0;
        secondIndex = 1;

        for (int i = 0; i < list.Count - 1; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                float distance = Vector3.Distance(list[i], list[j]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    firstIndex = i;
                    secondIndex = j;
                }
            }
        }
    }

    private float GetWaterLevel(float x, float z)
    {
        if (Water.Instance != null)
            return Water.Instance.GetWaterHeight(new Vector3(x, 0, z));

        return 0;
    }

    private void FixedUpdate()
    {
        Forces.Clear();
        foreach (var point in Voxels)
        {
            ApplyBuoyancyForce(point);
        }
    }

    private void ApplyBuoyancyForce(Vector3 point)
    {
        var worldPoint = transform.TransformPoint(point);
        float waterLevel = GetWaterLevel(worldPoint.x, worldPoint.z);

        if (worldPoint.y - VoxelHalfHeight < waterLevel)
        {
            float k = Mathf.Clamp01((waterLevel - worldPoint.y) / (2 * VoxelHalfHeight) + 0.5f);
            var velocity = Rb.GetPointVelocity(worldPoint);
            var localDampingForce = -velocity * DAMPFER * Rb.mass;
            //var force = localDampingForce + Mathf.Sqrt(k) * LocalArchimedesForce;
            var force = localDampingForce + k * LocalArchimedesForce;
            Rb.AddForceAtPosition(force, worldPoint);

            Forces.Add(new[] { worldPoint, force });
        }
    }

    private void OnDrawGizmos()
    {
        if (Voxels == null || Forces == null)
        {
            return;
        }

        const float gizmoSize = 0.05f;
        Gizmos.color = Color.yellow;

        foreach (var p in Voxels)
        {
            Gizmos.DrawCube(transform.TransformPoint(p), new Vector3(gizmoSize, gizmoSize, gizmoSize));
        }

        Gizmos.color = Color.cyan;

        foreach (var force in Forces)
        {
            Gizmos.DrawCube(force[0], new Vector3(gizmoSize, gizmoSize, gizmoSize));
            Gizmos.DrawLine(force[0], force[0] + force[1] / Rb.mass);
        }
    }
}
