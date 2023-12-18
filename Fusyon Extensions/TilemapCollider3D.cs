using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Tilemaps.Tile;
using static UnityEngine.Tilemaps.Tilemap;

namespace Fusyon.Extensions
{
    /// <summary>
    /// 3D colliders for tilemaps.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(MeshCollider))]
    public class TilemapCollider3D : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private Vector3 _Offset;
        [SerializeField] private Vector3 _Size = Vector3.one;
        #endregion

        /// <summary>
        /// The job for baking a mesh.
        /// </summary>
        private struct BakeMeshJob : IJob
        {
            private int MeshID { get; }

            public BakeMeshJob(int meshID)
            {
                MeshID = meshID;
            }

            public void Execute()
            {
                Physics.BakeMesh(MeshID, false);
            }
        }

        /// <summary>
        /// The triangles of a cube.
        /// </summary>
        private static int[] CubeTriangles = new int[]
        {
            0, 1, 2, 3, 2, 1,
            0, 4, 2, 6, 2, 4,
            1, 5, 0, 4, 0, 5,
            2, 6, 3, 7, 3, 6,
            3, 7, 1, 5, 1, 7,
            4, 5, 6, 7, 6, 5
        };

        /// <summary>
        /// The vertices of a cube.
        /// </summary>
        private static Vector3[] CubeVertices = new Vector3[]
        {
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0),
            new Vector3(0, 0, -1), new Vector3(0, 1, -1), new Vector3(1, 0, -1), new Vector3(1, 1, -1)
        };

        /// <summary>
        /// The global offset for the collider positions.
        /// </summary>
        private Vector3 Offset { get => _Offset; }
        /// <summary>
        /// The size of each collider.
        /// </summary>
        private Vector3 Size { get => _Size; }
        /// <summary>
        /// The target tilemap.
        /// </summary>
        private Tilemap Tilemap { get; set; }
        /// <summary>
        /// The collider.
        /// </summary>
        private MeshCollider MeshCollider { get; set; }
        /// <summary>
        /// The collider mesh.
        /// </summary>
        private Mesh Mesh { get; set; }
        /// <summary>
        /// The vertices of the collider mesh.
        /// </summary>
        private List<Vector3> Vertices { get; set; }
        /// <summary>
        /// The triangles of the collider mesh.
        /// </summary>
        private List<int> Triangles { get; set; }
        /// <summary>
        /// The collider indices by their positions.
        /// </summary>
        private Dictionary<Vector3Int, int> ColliderIndexByPosition { get; set; }

        #region Unity
        private void Awake()
        {
            Tilemap = GetComponent<Tilemap>();
            MeshCollider = GetComponent<MeshCollider>();
            Mesh = new Mesh() { name = name };
            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            ColliderIndexByPosition = new Dictionary<Vector3Int, int>();
            tilemapTileChanged += OnTilemapTileChanged;
        }

        private void Start()
        {
            Refresh();
        }
        #endregion

        /// <summary>
        /// Clears all tiles from the tilemap.
        /// </summary>
        [ContextMenu("Clear")]
        public void Clear()
        {
            GetComponent<Tilemap>().ClearAllTiles();
        }

        /// <summary>
        /// Refreshes all the colliders.
        /// </summary>
        private void Refresh()
        {
            foreach (Vector3Int position in Tilemap.cellBounds.allPositionsWithin)
            {
                OnTileChanged(position);
            }

            UpdateMesh();
        }

        /// <summary>
        /// Called when a tile from the tilemap is changed.
        /// </summary>
        /// <param name="position">The position of the changed tile.</param>
        private void OnTileChanged(Vector3Int position)
        {
            bool hasCollider = Tilemap.GetColliderType(position) != ColliderType.None;

            if (hasCollider)
            {
                AddCollider(position);
            }
            else
            {
                RemoveCollider(position);
            }
        }

        /// <summary>
        /// Called when a tile from a tilemap is changed.
        /// </summary>
        /// <param name="tilemap">The tilemap that was changed.</param>
        /// <param name="syncTiles">The tile changes.</param>
        private void OnTilemapTileChanged(Tilemap tilemap, SyncTile[] syncTiles)
        {
            // Our tilemap hasn't changed.
            if (tilemap != Tilemap)
            {
                return;
            }

            foreach (SyncTile syncTile in syncTiles)
            {
                OnTileChanged(syncTile.position);
            }

            UpdateMesh();
        }

        /// <summary>
        /// Updates the collider mesh.
        /// </summary>
        private void UpdateMesh()
        {
            Mesh.Clear();

            if (Vertices.Count > 0)
            {
                Mesh.SetVertices(Vertices);
                Mesh.SetTriangles(Triangles, 0);

                BakeMeshJob bakeMeshJob = new BakeMeshJob(Mesh.GetInstanceID());
                bakeMeshJob.Schedule().Complete();

                MeshCollider.sharedMesh = Mesh;
            }
        }

        /// <summary>
        /// Adds a collider in the position.
        /// </summary>
        /// <param name="position">The position to add the collider.</param>
        private void AddCollider(Vector3Int position)
        {
            // There's already a collider in the position.
            if (ColliderIndexByPosition.ContainsKey(position))
            {
                return;
            }

            // The original vertex count.
            int vertexIndex = Vertices.Count;

            // Adds all the vertices.
            foreach (Vector3 vertex in CubeVertices)
            {
                Vertices.Add(Vector3.Scale(vertex, Size) + position + Offset);
            }

            // Adds all the triangles.
            foreach (int triangle in CubeTriangles)
            {
                Triangles.Add(vertexIndex + triangle);
            }

            // The collider index is the index for its section of vertices in memory.
            int index = vertexIndex / CubeVertices.Length;

            ColliderIndexByPosition.Add(position, index);
        }

        /// <summary>
        /// Removes a collider in the position.
        /// </summary>
        /// <param name="position">The position to remove the collider.</param>
        private void RemoveCollider(Vector3Int position)
        {
            // There's no collider in the position.
            if (!ColliderIndexByPosition.TryGetValue(position, out int index))
            {
                return;
            }

            // The original vertex count.
            int vertexCount = Vertices.Count;

            // Swaps up with the last collider.
            Vector3Int lastColliderPosition = Vector3Int.FloorToInt(Vertices[vertexCount - CubeVertices.Length] - Offset);
            ColliderIndexByPosition[lastColliderPosition] = index;

            // Removes the vertices backwards and by swapping with the last vertices to decrease removal time.
            for (int i = CubeVertices.Length - 1; i >= 0; i--)
            {
                int vertexIndex = index * CubeVertices.Length + i;
                int lastVertexIndex = vertexCount - CubeVertices.Length + i;
                Vertices[vertexIndex] = Vertices[lastVertexIndex];
                Vertices.RemoveAt(lastVertexIndex);
            }

            // The original triangle count.
            int triangleCount = Triangles.Count;

            // Removes the triangles backwards to decrease removal time.
            for (int i = CubeTriangles.Length - 1; i >= 0; i--)
            {
                int lastTriangleIndex = triangleCount - CubeTriangles.Length + i;
                Triangles.RemoveAt(lastTriangleIndex);
            }

            // Removes the collider.
            ColliderIndexByPosition.Remove(position);
        }
    }
}
