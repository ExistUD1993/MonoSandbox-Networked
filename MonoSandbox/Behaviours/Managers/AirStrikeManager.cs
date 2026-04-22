using MonoSandbox;
using MonoSandbox.Behaviours;
using UnityEngine;

public class AirStrikeManager : MonoBehaviour
{
    private bool _canPlace = true;
    public bool editMode;
    public GameObject Cursor, AirStrikeModel,CursorModel,ExplodeModel;

    public void Update()
    {
        if (!editMode)
        {
            ManagerUtils.DestroyCursor(ref Cursor);
            return;
        }

        Cursor ??= CreateCursor();

        RaycastHit hitInfo = RefCache.Hit;
        Cursor.transform.position = hitInfo.point;
        Cursor.transform.forward = hitInfo.normal;

        if (InputHandling.RightPrimary)
        {
            if (_canPlace)
            {
                if (SandboxNetwork.TrySpawn(SandboxSpawnKind.Airstrike, hitInfo.point, hitInfo.normal))
                {
                    HapticManager.Haptic(HapticManager.HapticType.Create);
                    _canPlace = false;
                    return;
                }

                CreateAirstrike(hitInfo.point);
                HapticManager.Haptic(HapticManager.HapticType.Create);
                _canPlace = false;
            }

            return;
        }

        _canPlace = true;
    }

    private GameObject CreateCursor()
    {
        GameObject cursor = Instantiate(CursorModel);
        cursor.transform.localScale = Vector3.one * 20f;
        return cursor;
    }

    private void CreateAirstrike(Vector3 hitPoint)
    {
        GameObject missile = Instantiate(AirStrikeModel);
        missile.transform.SetParent(transform, false);
        missile.transform.position = hitPoint + new Vector3(0f, 80f, 0f);
        missile.transform.localScale = Vector3.one * 50f;

        Airstrike airstrikeControl = missile.AddComponent<Airstrike>();
        airstrikeControl.StrikeLocation = hitPoint;
        airstrikeControl.ExplosionOBJ = ExplodeModel;
    }
}

public class Airstrike : MonoBehaviour
{
    public static float speed = 20f;

    public Vector3 StrikeLocation;
    public GameObject ExplosionOBJ;

    private bool _canExplode = true;

    public void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, StrikeLocation, speed * Time.deltaTime);

        if (_canExplode && Vector3.Distance(transform.position, StrikeLocation) < 0.5f)
        {
            _canExplode = false;
            Explode();
        }
    }

    public void Explode()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.minDistance = 15f;
        audioSource.Play();
        GetComponent<Renderer>().enabled = false;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GameObject explosionObject = Instantiate(ExplosionOBJ);
        explosionObject.transform.SetParent(transform);
        explosionObject.transform.localPosition = Vector3.zero;
        explosionObject.transform.localScale = Vector3.one * 0.3f;

        foreach (Collider nearby in Physics.OverlapSphere(transform.position, 10f))
        {
            nearby.GetComponent<BombDetonate>()?.Explode();
            nearby.GetComponent<MineDetonate>()?.Explode();
            nearby.GetComponent<Explode>()?.ExplodeObject();

            Rigidbody body = nearby.GetComponent<Rigidbody>();
            if (body != null && body.useGravity)
            {
                body.AddExplosionForce(14400f, transform.position, 80f, 0.5f, ForceMode.Force);
            }
        }

        Rigidbody playerBody = GorillaLocomotion.GTPlayer.Instance.GetComponent<Rigidbody>();
        playerBody.AddExplosionForce(14400f * Mathf.Sqrt(playerBody.mass), transform.position, 80f, 0.5f, ForceMode.Force);
        Invoke(nameof(Finish), 2f);
    }

    private void Finish()
    {
        Destroy(gameObject);
    }
}
