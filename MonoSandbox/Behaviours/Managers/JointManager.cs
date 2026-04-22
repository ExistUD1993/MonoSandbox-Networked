using System;
using System.Collections.Generic;
using MonoSandbox;
using MonoSandbox.Behaviours;
using UnityEngine;

public class SpringManager : MonoBehaviour
{
    public List<GameObject> objectList = new List<GameObject>();
    public bool editMode, BasePlaced;
    public GameObject Cursor;

    private bool _canPlace = true;
    private RaycastHit _baseHit;

    public void Update()
    {
        if (!editMode)
        {
            BasePlaced = false;
            ManagerUtils.DestroyCursor(ref Cursor);
            return;
        }

        Cursor ??= ManagerUtils.CreateSphereCursor();

        RaycastHit hitInfo = RefCache.Hit;
        bool isAllowed = RefCache.HitExists && ManagerUtils.TryGetMonoRigidbody(hitInfo, out _);
        ManagerUtils.UpdateCursor(Cursor, hitInfo, isAllowed);

        if (InputHandling.RightPrimary)
        {
            if (_canPlace && isAllowed)
            {
                PlaceJoint(hitInfo);
                _canPlace = false;
            }

            return;
        }

        _canPlace = true;
    }

    private void PlaceJoint(RaycastHit hit)
    {
        if (!BasePlaced)
        {
            _baseHit = hit;
            BasePlaced = true;
            HapticManager.Haptic(HapticManager.HapticType.Create);
            return;
        }

        if (HasManagedSpring(_baseHit.transform))
        {
            BasePlaced = false;
            return;
        }

        if (SandboxNetwork.TryCreateAttachment(
                SandboxAttachmentKind.Spring,
                _baseHit.transform.gameObject,
                hit.transform.gameObject,
                Vector3.zero,
                Vector3.zero))
        {
            BasePlaced = false;
            HapticManager.Haptic(HapticManager.HapticType.Create);
            return;
        }

        GameObject jointObject = new GameObject
        {
            name = "MSJoint MonoObject"
        };

        jointObject.transform.SetParent(_baseHit.transform, false);
        objectList.Add(jointObject);

        FixedJoint fixedJoint = jointObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = _baseHit.rigidbody;

        SpringJoint joint = jointObject.AddComponent<SpringJoint>();
        joint.minDistance = Vector3.Distance(_baseHit.transform.position, hit.transform.position) - 1f;
        joint.damper = 30f;
        joint.spring = 10f;
        joint.massScale = 12f;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = hit.rigidbody;

        SpringLine line = jointObject.AddComponent<SpringLine>();
        line.lineRenderer = SpringLine.CreateRenderer(jointObject);
        line.pointone = jointObject;
        line.pointtwo = hit.transform.gameObject;

        HapticManager.Haptic(HapticManager.HapticType.Create);
        BasePlaced = false;
    }

    private static bool HasManagedSpring(Transform target)
    {
        foreach (Transform child in target)
        {
            if (child.name.Contains("MSJoint"))
            {
                return true;
            }
        }

        return false;
    }
}

public class SpringLine : MonoBehaviour
{
    public GameObject pointone, pointtwo;
    public LineRenderer lineRenderer;

    public static LineRenderer CreateRenderer(GameObject owner)
    {
        LineRenderer renderer = owner.AddComponent<LineRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.startColor = Color.white;
        renderer.endColor = Color.white;
        renderer.startWidth = 0.012f;
        renderer.endWidth = 0.012f;
        renderer.positionCount = 2;
        return renderer;
    }

    private void Update()
    {
        if (lineRenderer == null || pointone == null || pointtwo == null)
        {
            return;
        }

        lineRenderer.SetPosition(0, pointone.transform.position);
        lineRenderer.SetPosition(1, pointtwo.transform.position);
    }
}

public class WeldManager : MonoBehaviour
{
    public bool editMode, BasePlaced;
    public GameObject Cursor;

    private bool _canPlace = true;
    private GameObject _baseHit;

    private void Update()
    {
        if (!editMode)
        {
            BasePlaced = false;
            ManagerUtils.DestroyCursor(ref Cursor);
            return;
        }

        Cursor ??= ManagerUtils.CreateSphereCursor();

        RaycastHit hitInfo = RefCache.Hit;
        bool isAllowed = RefCache.HitExists && ManagerUtils.TryGetMonoRigidbody(hitInfo, out _);
        ManagerUtils.UpdateCursor(Cursor, hitInfo, isAllowed);

        if (InputHandling.RightPrimary)
        {
            if (_canPlace && isAllowed)
            {
                PlaceJoint(hitInfo);
                _canPlace = false;
            }

            return;
        }

        _canPlace = true;
    }

    private void PlaceJoint(RaycastHit hit)
    {
        if (!BasePlaced)
        {
            _baseHit = hit.transform.gameObject;
            BasePlaced = true;
            HapticManager.Haptic(HapticManager.HapticType.Create);
            return;
        }

        if (_baseHit.GetInstanceID() != hit.transform.gameObject.GetInstanceID())
        {
            if (SandboxNetwork.TryCreateAttachment(
                    SandboxAttachmentKind.Weld,
                    _baseHit,
                    hit.transform.gameObject,
                    Vector3.zero,
                    Vector3.zero))
            {
                HapticManager.Haptic(HapticManager.HapticType.Create);
                BasePlaced = false;
                return;
            }

            FixedJoint joint = hit.transform.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = _baseHit.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = true;
            HapticManager.Haptic(HapticManager.HapticType.Create);
        }

        BasePlaced = false;
    }
}

public class BalloonManager : MonoBehaviour
{
    public List<GameObject> objectList = new List<GameObject>();
    public float balloonPower = 2f;
    public bool editMode;
    public GameObject Balloon, Cursor;

    private bool _canPlace = true;

    public void Update()
    {
        if (!editMode)
        {
            ManagerUtils.DestroyCursor(ref Cursor);
            return;
        }

        Cursor ??= ManagerUtils.CreateSphereCursor();

        RaycastHit hitInfo = RefCache.Hit;
        Rigidbody connectedBody = null;
        bool isAllowed = RefCache.HitExists && ManagerUtils.TryGetMonoRigidbody(hitInfo, out connectedBody);
        ManagerUtils.UpdateCursor(Cursor, hitInfo, isAllowed);

        if (InputHandling.RightPrimary)
        {
            if (_canPlace && isAllowed && !HasBalloon(hitInfo.transform))
            {
                if (SandboxNetwork.TryCreateAttachment(
                        SandboxAttachmentKind.Balloon,
                        hitInfo.transform.gameObject,
                        null,
                        hitInfo.point,
                        Cursor.transform.forward,
                        balloonPower))
                {
                    HapticManager.Haptic(HapticManager.HapticType.Create);
                    _canPlace = false;
                    return;
                }

                CreateBalloon(hitInfo, connectedBody);
                HapticManager.Haptic(HapticManager.HapticType.Create);
                _canPlace = false;
            }

            return;
        }

        _canPlace = true;
    }

    private static bool HasBalloon(Transform target)
    {
        foreach (Transform child in target)
        {
            if (child.name.Contains("Balloon MonoObject"))
            {
                return true;
            }
        }

        return false;
    }

    private void CreateBalloon(RaycastHit hitInfo, Rigidbody connectedBody)
    {
        GameObject balloonObject = Instantiate(Balloon);
        balloonObject.layer = 8;
        balloonObject.name = "Balloon MonoObject";
        balloonObject.transform.SetParent(transform, false);
        balloonObject.transform.localScale = Vector3.one * 0.3f;
        balloonObject.transform.position = hitInfo.point + (Vector3.up * 0.3f) + Cursor.transform.forward / 3f;
        balloonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        Balloon balloonScript = balloonObject.AddComponent<Balloon>();
        balloonScript.power = balloonPower;

        SpringLine line = balloonObject.AddComponent<SpringLine>();
        line.lineRenderer = SpringLine.CreateRenderer(balloonObject);
        line.pointone = balloonObject.transform.GetChild(0).gameObject;
        line.pointtwo = hitInfo.transform.gameObject;

        SpringJoint joint = balloonObject.AddComponent<SpringJoint>();
        joint.maxDistance = 0f;
        joint.spring = 20f;
        joint.damper = 10f;
        joint.connectedBody = connectedBody;

        objectList.Add(balloonObject);
    }
}

public class Balloon : MonoBehaviour
{
    public float power;
    private float _maxSpeed;
    private Rigidbody _body;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
        _maxSpeed = Mathf.Clamp(0.2f + power * 0.4f, -5f, 5f);
    }

    private void FixedUpdate()
    {
        _body.linearVelocity = Vector3.ClampMagnitude(_body.linearVelocity, _maxSpeed);
        _body.AddForce(0f, -Physics.gravity.y + (float)Math.Pow(3, 4) + power, 0f);
    }
}

public class BoneSphere : MonoBehaviour
{
    [Header("Bones")]
    public GameObject root;
    public GameObject x;
    public GameObject x2;
    public GameObject y;
    public GameObject y2;
    public GameObject z;
    public GameObject z2;

    [Header("Spring Joint Settings")]
    [Tooltip("Strength of spring")]
    public float Spring = 800f;

    [Tooltip("Higher the value the faster the spring oscillation stops")]
    public float Damper = 0.2f;

    [Header("Other Settings")]
    public Softbody.ColliderShape Shape = Softbody.ColliderShape.Sphere;
    public float ColliderSize = 0.002f;
    public float RigidbodyMass = 0.5f;

    private void Start()
    {
        root = transform.GetChild(0).GetChild(0).gameObject;
        x = transform.GetChild(0).GetChild(1).gameObject;
        x2 = transform.GetChild(0).GetChild(2).gameObject;
        y = transform.GetChild(0).GetChild(3).gameObject;
        y2 = transform.GetChild(0).GetChild(4).gameObject;
        z = transform.GetChild(0).GetChild(5).gameObject;
        z2 = transform.GetChild(0).GetChild(6).gameObject;

        Softbody.Init(Shape, ColliderSize, RigidbodyMass, Spring, Damper, RigidbodyConstraints.FreezeRotation);

        Softbody.AddCollider(ref root, Softbody.ColliderShape.Sphere, 0.005f, 10f);
        Softbody.AddCollider(ref x);
        Softbody.AddCollider(ref x2);
        Softbody.AddCollider(ref y);
        Softbody.AddCollider(ref y2);
        Softbody.AddCollider(ref z);
        Softbody.AddCollider(ref z2);

        Softbody.AddSpring(ref x, ref root);
        Softbody.AddSpring(ref x2, ref root);
        Softbody.AddSpring(ref y, ref root);
        Softbody.AddSpring(ref y2, ref root);
        Softbody.AddSpring(ref z, ref root);
        Softbody.AddSpring(ref z2, ref root);
    }
}

public static class Softbody
{
    public enum ColliderShape
    {
        Box,
        Sphere
    }

    public static ColliderShape Shape;
    public static float ColliderSize, RigidbodyMass, Spring, Damper;
    public static RigidbodyConstraints Constraints;

    public static void Init(ColliderShape shape, float colliderSize, float rigidbodyMass, float spring, float damper, RigidbodyConstraints constraints)
    {
        Shape = shape;
        ColliderSize = colliderSize;
        RigidbodyMass = rigidbodyMass;
        Spring = spring;
        Damper = damper;
        Constraints = constraints;
    }

    public static Rigidbody AddCollider(ref GameObject gameObject)
    {
        return AddCollider(ref gameObject, Shape, ColliderSize, RigidbodyMass);
    }

    public static SpringJoint AddSpring(ref GameObject first, ref GameObject second)
    {
        return AddSpring(ref first, ref second, Spring, Damper);
    }

    public static Rigidbody AddCollider(ref GameObject gameObject, ColliderShape shape, float size, float mass)
    {
        switch (shape)
        {
            case ColliderShape.Box:
                gameObject.AddComponent<BoxCollider>().size = Vector3.one * size;
                break;
            case ColliderShape.Sphere:
                gameObject.AddComponent<SphereCollider>().radius = size;
                break;
        }

        Rigidbody body = gameObject.AddComponent<Rigidbody>();
        body.mass = mass;
        body.linearDamping = 0f;
        body.angularDamping = 10f;
        body.constraints = Constraints;
        return body;
    }

    public static SpringJoint AddSpring(ref GameObject first, ref GameObject second, float spring, float damper)
    {
        SpringJoint joint = first.AddComponent<SpringJoint>();
        joint.connectedBody = second.GetComponent<Rigidbody>();
        joint.spring = spring;
        joint.damper = damper;
        return joint;
    }
}
