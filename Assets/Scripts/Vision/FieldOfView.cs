using System.Collections.Generic;
using UnityEngine;

namespace ArcherCraft.Vision
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FieldOfView : MonoBehaviour
    {
        [Header("FOV Settings")]
        [SerializeField] private float viewRadius = 16f;
        [Range(0, 360)]
        [SerializeField] private float viewAngle = 100f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float meshResolution = 1.0f; // 도당 레이캐스트 개수
        [SerializeField] private int edgeResolveIterations = 4;
        [SerializeField] private float edgeDstThreshold = 0.5f;

        private Mesh viewMesh;
        private MeshFilter viewMeshFilter;

        public float ViewRadius => viewRadius;
        public float ViewAngle => viewAngle;

        private void Start()
        {
            viewMesh = new Mesh { name = "FOV_Mesh" };
            viewMeshFilter = GetComponent<MeshFilter>();
            viewMeshFilter.mesh = viewMesh;
        }

        private void LateUpdate()
        {
            DrawFieldOfView();
        }

        private void DrawFieldOfView()
        {
            int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
            float stepAngleSize = viewAngle / stepCount;
            List<Vector3> viewPoints = new List<Vector3>();
            ViewCastInfo oldViewCast = new ViewCastInfo();

            for (int i = 0; i <= stepCount; i++)
            {
                float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
                ViewCastInfo newViewCast = ViewCast(angle);

                if (i > 0)
                {
                    bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                    if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                    {
                        EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                        if (edge.pointA != Vector3.zero) viewPoints.Add(edge.pointA);
                        if (edge.pointB != Vector3.zero) viewPoints.Add(edge.pointB);
                    }
                }

                viewPoints.Add(newViewCast.point);
                oldViewCast = newViewCast;
            }

            int vertexCount = viewPoints.Count + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[(vertexCount - 2) * 3];

            vertices[0] = Vector3.zero; // 로컬 중심
            for (int i = 0; i < vertexCount - 1; i++)
            {
                vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

                if (i < vertexCount - 2)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            viewMesh.Clear();
            viewMesh.vertices = vertices;
            viewMesh.triangles = triangles;
            viewMesh.RecalculateNormals();
        }

        private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
        {
            float minAngle = minViewCast.angle;
            float maxAngle = maxViewCast.angle;
            Vector3 minPoint = Vector3.zero;
            Vector3 maxPoint = Vector3.zero;

            for (int i = 0; i < edgeResolveIterations; i++)
            {
                float angle = (minAngle + maxAngle) / 2;
                ViewCastInfo newViewCast = ViewCast(angle);

                bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
                {
                    minAngle = angle;
                    minPoint = newViewCast.point;
                }
                else
                {
                    maxAngle = angle;
                    maxPoint = newViewCast.point;
                }
            }

            return new EdgeInfo(minPoint, maxPoint);
        }

        private ViewCastInfo ViewCast(float globalAngle)
        {
            Vector3 dir = DirFromAngle(globalAngle, true);
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, viewRadius, obstacleLayer.value != 0 ? obstacleLayer : ~0))
            {
                return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
            }
            else
            {
                return new ViewCastInfo(false, origin + dir * viewRadius, viewRadius, globalAngle);
            }
        }

        public Vector3 DirFromAngle(float angleInDegrees, bool isGlobal)
        {
            if (!isGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        public struct ViewCastInfo
        {
            public bool hit;
            public Vector3 point;
            public float dst;
            public float angle;

            public ViewCastInfo(bool hit, Vector3 point, float dst, float angle)
            {
                this.hit = hit;
                this.point = point;
                this.dst = dst;
                this.angle = angle;
            }
        }

        public struct EdgeInfo
        {
            public Vector3 pointA;
            public Vector3 pointB;

            public EdgeInfo(Vector3 pointA, Vector3 pointB)
            {
                this.pointA = pointA;
                this.pointB = pointB;
            }
        }
    }
}
