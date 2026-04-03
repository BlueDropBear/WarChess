using System;
using UnityEngine;
using WarChess.Units;

namespace WarChess.Core
{
    /// <summary>
    /// Unity MonoBehaviour that renders the 10x10 grid and converts between
    /// screen/world positions and grid coordinates. Reads from GridMap for state.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private Color _defaultTileColor = new Color(0.35f, 0.55f, 0.23f);
        [SerializeField] private Color _deployZoneColor = new Color(0.4f, 0.6f, 0.3f);
        [SerializeField] private Color _highlightColor = new Color(0.9f, 0.9f, 0.5f, 0.5f);

        private GridMap _gridMap;
        private GameObject[,] _tileObjects;
        private SpriteRenderer[,] _tileRenderers;
        private int _width;
        private int _height;
        private Sprite _cachedSquareSprite;

        /// <summary>Fired when a tile is clicked. Provides the grid coordinate.</summary>
        public event Action<GridCoord> OnTileClicked;

        /// <summary>
        /// Initializes the visual grid from a logical GridMap.
        /// </summary>
        public void Initialize(GridMap gridMap)
        {
            _gridMap = gridMap;
            _width = gridMap.Width;
            _height = gridMap.Height;
            _tileObjects = new GameObject[_width, _height];
            _tileRenderers = new SpriteRenderer[_width, _height];

            CreateTiles();
        }

        /// <summary>
        /// Converts a world position to a grid coordinate.
        /// Returns a coordinate that may be invalid — caller should check IsValid.
        /// </summary>
        public GridCoord WorldToGrid(Vector3 worldPos)
        {
            // Grid origin is at (0,0) for tile (1,1)
            int x = Mathf.RoundToInt(worldPos.x / _tileSize) + 1;
            int y = Mathf.RoundToInt(worldPos.y / _tileSize) + 1;
            return new GridCoord(x, y);
        }

        /// <summary>
        /// Converts a grid coordinate to a world position (center of tile).
        /// </summary>
        public Vector3 GridToWorld(GridCoord coord)
        {
            return new Vector3(
                (coord.X - 1) * _tileSize,
                (coord.Y - 1) * _tileSize,
                0f);
        }

        /// <summary>
        /// Highlights a tile with the highlight color.
        /// </summary>
        public void HighlightTile(GridCoord coord)
        {
            if (!_gridMap.IsValidCoord(coord)) return;
            _tileRenderers[coord.X - 1, coord.Y - 1].color = _highlightColor;
        }

        /// <summary>
        /// Resets a tile to its default color.
        /// </summary>
        public void ClearHighlight(GridCoord coord)
        {
            if (!_gridMap.IsValidCoord(coord)) return;
            _tileRenderers[coord.X - 1, coord.Y - 1].color = _defaultTileColor;
        }

        /// <summary>
        /// Clears all tile highlights.
        /// </summary>
        public void ClearAllHighlights()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _tileRenderers[x, y].color = _defaultTileColor;
                }
            }
        }

        private void CreateTiles()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var coord = new GridCoord(x + 1, y + 1);
                    var worldPos = GridToWorld(coord);

                    GameObject tile;
                    if (_tilePrefab != null)
                    {
                        tile = Instantiate(_tilePrefab, worldPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Fallback: create a simple sprite quad
                        tile = new GameObject($"Tile_{coord}");
                        tile.transform.SetParent(transform);
                        tile.transform.position = worldPos;
                        var sr = tile.AddComponent<SpriteRenderer>();
                        sr.sprite = GetSquareSprite();
                        sr.color = _defaultTileColor;
                    }

                    tile.name = $"Tile_{coord}";
                    _tileObjects[x, y] = tile;
                    _tileRenderers[x, y] = tile.GetComponent<SpriteRenderer>();

                    if (_tileRenderers[x, y] != null)
                        _tileRenderers[x, y].color = _defaultTileColor;
                }
            }
        }

        private Sprite GetSquareSprite()
        {
            if (_cachedSquareSprite == null)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _cachedSquareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _cachedSquareSprite;
        }

        private void Update()
        {
            // Handle mouse/touch input for tile clicking
            if (Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var coord = WorldToGrid(worldPos);
                if (_gridMap != null && _gridMap.IsValidCoord(coord))
                {
                    OnTileClicked?.Invoke(coord);
                }
            }
        }
    }
}
