using MonoSandbox;
using MonoSandbox.Behaviours;
using UnityEngine;

public abstract class ToggleRigidbodyStateManager : MonoBehaviour
{
    private bool _canToggle = true;

    protected GameObject Cursor;
    public bool editMode;

    protected abstract SandboxStateKind StateKind { get; }
    protected abstract void Toggle(Rigidbody body);

    private void Update()
    {
        if (!editMode)
        {
            ManagerUtils.DestroyCursor(ref Cursor);
            return;
        }

        Cursor ??= ManagerUtils.CreateSphereCursor();

        RaycastHit hitInfo = RefCache.Hit;
        bool isAllowed = ManagerUtils.TryGetMonoRigidbody(hitInfo, out Rigidbody targetBody);
        ManagerUtils.UpdateCursor(Cursor, hitInfo, isAllowed);

        if (InputHandling.RightPrimary)
        {
            if (_canToggle && isAllowed)
            {
                if (SandboxNetwork.TryApplyState(targetBody.gameObject, StateKind, Color.clear))
                {
                    HapticManager.Haptic(HapticManager.HapticType.Create);
                    _canToggle = false;
                    return;
                }

                Toggle(targetBody);
                HapticManager.Haptic(HapticManager.HapticType.Create);
                _canToggle = false;
            }

            return;
        }

        _canToggle = true;
    }
}

public class FreezeManager : ToggleRigidbodyStateManager
{
    protected override SandboxStateKind StateKind => SandboxStateKind.Freeze;

    protected override void Toggle(Rigidbody body)
    {
        body.constraints = body.constraints == RigidbodyConstraints.None
            ? RigidbodyConstraints.FreezeAll
            : RigidbodyConstraints.None;
    }
}

public class GravityManager : ToggleRigidbodyStateManager
{
    protected override SandboxStateKind StateKind => SandboxStateKind.Gravity;

    protected override void Toggle(Rigidbody body)
    {
        body.useGravity = !body.useGravity;
    }
}
