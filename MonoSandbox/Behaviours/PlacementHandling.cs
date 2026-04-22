using UnityEngine;

namespace MonoSandbox.Behaviours
{
    public class PlacementHandling : MonoBehaviour
    {
        private static readonly Color CursorValidColor = new Color(0.392f, 0.722f, 0.820f, 0.4509804f);

        public float Offset = 4f;
        public bool IsEditing, IsActivated, Placed;
        public GameObject Cursor, SandboxContainer;

        public virtual GameObject CursorRef => null;

        public virtual void Activated(RaycastHit hitInfo)
        {
            HapticManager.Haptic(HapticManager.HapticType.Create);
        }

        public virtual void DrawCursor(RaycastHit hitInfo)
        {
        }

        private void Start()
        {
            SandboxContainer = RefCache.SandboxContainer;
        }

        private void Update()
        {
            EnsureSandboxContainer();
            UpdateCursorState();

            if (!IsEditing || Cursor == null)
            {
                return;
            }

            UpdateCursorVisuals(RefCache.Hit);
            UpdatePlacementInput(RefCache.Hit);
        }

        private void EnsureSandboxContainer()
        {
            if (!SandboxContainer)
            {
                SandboxContainer = RefCache.SandboxContainer;
            }
        }

        private void UpdateCursorState()
        {
            if (IsEditing && !Cursor)
            {
                Cursor = CursorRef;
                Cursor.GetComponent<Renderer>().material = new Material(RefCache.Selection);
                return;
            }

            if (!IsEditing && Cursor)
            {
                Destroy(Cursor);
            }
        }

        private void UpdateCursorVisuals(RaycastHit hitInfo)
        {
            if (Cursor.activeSelf != RefCache.HitExists)
            {
                Cursor.SetActive(RefCache.HitExists);
            }

            Cursor.GetComponent<Renderer>().material.color = CursorValidColor;

            if (RefCache.HitExists)
            {
                DrawCursor(hitInfo);
            }
        }

        private void UpdatePlacementInput(RaycastHit hitInfo)
        {
            IsActivated = InputHandling.RightPrimary;

            if (IsActivated && !Placed)
            {
                Placed = true;
                Activated(hitInfo);
            }
            else if (!IsActivated && Placed)
            {
                Placed = false;
            }
        }
    }
}
