using UnityEngine;
using WarChess.Core;

namespace WarChess.Units
{
    /// <summary>
    /// Unity MonoBehaviour representing a unit on the grid. Reads from UnitInstance
    /// to update visual position, health bar, and animations.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _healthBarFill;
        [SerializeField] private SpriteRenderer _healthBarRenderer;

        [Header("Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private Color _healthGreen = new Color(0.27f, 0.67f, 0.27f);
        [SerializeField] private Color _healthYellow = new Color(0.8f, 0.67f, 0.13f);
        [SerializeField] private Color _healthRed = new Color(0.8f, 0.13f, 0.13f);

        private UnitInstance _unitInstance;
        private GridView _gridView;
        private Vector3 _targetPosition;
        private bool _isMoving;

        /// <summary>The logical unit this view represents.</summary>
        public UnitInstance UnitInstance => _unitInstance;

        /// <summary>
        /// Initializes this view with a unit instance and grid view for coordinate conversion.
        /// </summary>
        public void Initialize(UnitInstance unitInstance, GridView gridView)
        {
            if (unitInstance == null || gridView == null) return;

            _unitInstance = unitInstance;
            _gridView = gridView;
            _targetPosition = _gridView.GridToWorld(unitInstance.Position);
            transform.position = _targetPosition;
            UpdateHealthBar();
        }

        /// <summary>
        /// Smoothly moves the unit to a new grid position.
        /// </summary>
        public void MoveTo(GridCoord coord)
        {
            _targetPosition = _gridView.GridToWorld(coord);
            _isMoving = true;
        }

        /// <summary>
        /// Instantly sets position (for initial placement).
        /// </summary>
        public void SetPosition(GridCoord coord)
        {
            _targetPosition = _gridView.GridToWorld(coord);
            transform.position = _targetPosition;
            _isMoving = false;
        }

        /// <summary>
        /// Updates the health bar to reflect current HP.
        /// </summary>
        public void UpdateHealthBar()
        {
            if (_unitInstance == null || _healthBarFill == null) return;

            float hpRatio = (float)_unitInstance.CurrentHp / _unitInstance.MaxHp;
            _healthBarFill.localScale = new Vector3(hpRatio, 1f, 1f);

            if (_healthBarRenderer != null)
            {
                if (hpRatio > 0.5f)
                    _healthBarRenderer.color = _healthGreen;
                else if (hpRatio > 0.25f)
                    _healthBarRenderer.color = _healthYellow;
                else
                    _healthBarRenderer.color = _healthRed;
            }
        }

        /// <summary>
        /// Triggers a visual hit flash effect.
        /// </summary>
        public void PlayHitFlash()
        {
            if (_spriteRenderer != null)
            {
                // Simple flash: briefly set to white, then restore
                StartCoroutine(HitFlashCoroutine());
            }
        }

        /// <summary>
        /// Triggers the death animation and destroys the GameObject.
        /// </summary>
        public void PlayDeath()
        {
            // Fade out and destroy
            StartCoroutine(DeathCoroutine());
        }

        private void Update()
        {
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, _targetPosition, _moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetPosition) < 0.01f)
                {
                    transform.position = _targetPosition;
                    _isMoving = false;
                }
            }
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            var originalColor = _spriteRenderer.color;
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            _spriteRenderer.color = originalColor;
        }

        private System.Collections.IEnumerator DeathCoroutine()
        {
            if (_spriteRenderer != null)
            {
                var color = _spriteRenderer.color;
                float elapsed = 0f;
                float duration = 0.5f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    color.a = 1f - (elapsed / duration);
                    _spriteRenderer.color = color;
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}
