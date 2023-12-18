using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Tilemaps.Tile;

namespace Fusyon.Extensions
{
    /// <summary>
    /// A Tilesetter's blob tileset.
    /// </summary>
    [CreateAssetMenu(fileName = "New Tilesetter Blob", menuName = "Fusyon/Extensions/Tilesetter Blob")]
    public class TilesetterBlob : TileBase
    {
        #region Inspector
        [SerializeField] private Texture2D _Texture;
        [SerializeField, HideInInspector] private Texture2D _LastTexture;
        [SerializeField] private Color _Color = Color.white;
        [SerializeField] private ColliderType _DefaultColliderType = ColliderType.None;
        [Space, SerializeField] private List<Tile> _Tiles;
        [SerializeField, HideInInspector] private SerializableDictionary<int, int> _TileIndexByMask;
        #endregion

        /// <summary>
        /// A tile from this tileset.
        /// </summary>
        [System.Serializable]
        private struct Tile
        {
            #region Inspector
            [SerializeField, HideInInspector] private string _Name;
            [SerializeField] private Sprite _Sprite;
            [SerializeField] private ColliderType _ColliderType;
            #endregion

            /// <summary>
            /// The name of the tile.
            /// </summary>
            public string Name { get => _Name; set => _Name = value; }
            /// <summary>
            /// The sprite of the tile.
            /// </summary>
            public Sprite Sprite { get => _Sprite; private set => _Sprite = value; }
            /// <summary>
            /// The type of collider used by the tile.
            /// </summary>
            public ColliderType ColliderType { get => _ColliderType; private set => _ColliderType = value; }

            /// <summary>
            /// Constructs a tile.
            /// </summary>
            /// <param name="sprite">The sprite of the tile.</param>
            /// <param name="colliderType">The type of collider used by the tile.</param>
            public Tile(Sprite sprite, ColliderType colliderType) : this()
            {
                Name = sprite.name;
                Sprite = sprite;
                ColliderType = colliderType;
            }
        }

        /// <summary>
        /// The neighbor of a tile.
        /// </summary>
        private struct Neighbor
        {
            /// <summary>
            /// The value of this neighbor [0, 31].
            /// </summary>
            public int Value { get; set; }
            /// <summary>
            /// The offset relative to the center tile.
            /// </summary>
            public Vector3Int Offset { get; set; }

            /// <summary>
            /// Constructs a neighbor.
            /// </summary>
            /// <param name="value">The value of this neighbor [0, 31].</param>
            /// <param name="offset">The offset relative to the center tile.</param>
            public Neighbor(int value, Vector3Int offset)
            {
                Value = value;
                Offset = offset;
            }
        }

        /// <summary>
        /// All the possible neighbors of a tile.
        /// </summary>
        private static Neighbor[] Neighbors = new Neighbor[8]
        {
            new Neighbor(1, new Vector3Int(0, 1, 0)),
            new Neighbor(3, new Vector3Int(1, 0, 0)),
            new Neighbor(5, new Vector3Int(0, -1, 0)),
            new Neighbor(7, new Vector3Int(-1, 0, 0)),
            new Neighbor(0, new Vector3Int(-1, 1, 0)),
            new Neighbor(2, new Vector3Int(1, 1, 0)),
            new Neighbor(4, new Vector3Int(1, -1, 0)),
            new Neighbor(6, new Vector3Int(-1, -1, 0))
        };

        /// <summary>
        /// The masks for each tile in the tileset.
        /// </summary>
        private static int[] TileMasks = new int[]
        {
            56, 62, 14, 8, 248, 255, 143, 136, 224, 227, 131, 128, 32, 34, 2, 0, 40, 46, 58, 10,
            42, 232, 239, 251, 139, 235, 184, 191, 254, 142, 190, 160, 163, 226, 130, 162, 168,
            175, 250, 138, 170, 187, 238, 186, 174, 234, 171
        };

        /// <summary>
        /// The default type of collider used when tiles are generated.
        /// </summary>
        public ColliderType DefaultColliderType { get => _DefaultColliderType; }

        /// <summary>
        /// The texture used by the tileset.
        /// </summary>
        private Texture2D Texture { get => _Texture; }
        /// <summary>
        /// The last texture used by the tileset.
        /// </summary>
        private Texture2D LastTexture { get => _LastTexture; set => _LastTexture = value; }
        /// <summary>
        /// The color of the tiles.
        /// </summary>
        private Color Color { get => _Color; }
        /// <summary>
        /// The tiles of the tileset.
        /// </summary>
        private List<Tile> Tiles { get => _Tiles; }
        /// <summary>
        /// The tile indices by their masks.
        /// </summary>
        private SerializableDictionary<int, int> TileIndexByMask { get => _TileIndexByMask; }

        #region Unity
        private void OnValidate()
        {
            CreateTiles();
        }
        #endregion

        #region Overrides
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            Tile tile = GetTile(position, tilemap);

            tileData.sprite = tile.Sprite;
            tileData.color = Color;
            tileData.colliderType = tile.ColliderType;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            base.RefreshTile(position, tilemap);

            foreach (Neighbor neighbor in Neighbors)
            {
                base.RefreshTile(position + neighbor.Offset, tilemap);
            }
        }
        #endregion

        /// <summary>
        /// Gets the mask of a tile.
        /// </summary>
        /// <param name="position">The position of the tile.</param>
        /// <param name="tilemap">The tilemap that contains the tile.</param>
        /// <returns>The mask of the tile.</returns>
        private int GetTileMask(Vector3Int position, ITilemap tilemap)
        {
            int mask = 0;

            foreach (Neighbor neighbor in Neighbors)
            {
                // The neighbor tileset.
                TilesetterBlob tileset = tilemap.GetTile<TilesetterBlob>(position + neighbor.Offset);

                // Only equal tilesets are tiled together.
                if (tileset == this)
                {
                    int newMask = mask | (1 << neighbor.Value);

                    // The new mask is a valid mask.
                    if (TileIndexByMask.ContainsKey(newMask))
                    {
                        mask = newMask;
                    }
                }
            }

            return mask;
        }

        /// <summary>
        /// Creates all the tiles.
        /// </summary>
        private void CreateTiles()
        {
            // The tiles are created only when changing textures.
            if (Texture == LastTexture)
            {
                return;
            }

            OnCreateTiles();
        }

        /// <summary>
        /// Called when creating tiles.
        /// </summary>
        [ContextMenu("Reload")]
        private void OnCreateTiles()
        {
            LastTexture = Texture;

            Tiles.Clear();
            TileIndexByMask.Clear();

            if (Texture == null)
            {
                return;
            }

#if UNITY_EDITOR
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(Texture));

            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    Tiles.Add(new Tile(sprite, DefaultColliderType));
                }
            }

            // The number of tiles that exist and have masks.
            int validTileCount = Mathf.Min(Tiles.Count, TileMasks.Length);

            // Correlates masks to their resulting tiles.
            for (int i = 0; i < validTileCount; i++)
            {
                int mask = TileMasks[i];
                TileIndexByMask[mask] = i;
            }
#endif
        }

        /// <summary>
        /// Gets a tile in the position.
        /// </summary>
        /// <param name="position">The position of the tile.</param>
        /// <param name="tilemap">The tilemap that contains the tile.</param>
        /// <returns>The tile in the position.</returns>
        private Tile GetTile(Vector3Int position, ITilemap tilemap)
        {
            // The mask for the tile.
            int mask = GetTileMask(position, tilemap);

            // Gets the tile assigned to the mask.
            if (TileIndexByMask.TryGetValue(mask, out int tileIndex))
            {
                return Tiles[tileIndex];
            }

            return Tiles.Count > 0 ? Tiles[0] : default;
        }
    }
}