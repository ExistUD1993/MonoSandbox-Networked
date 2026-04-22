using UnityEngine;

public static class ManagerUtils
{
    public static readonly Color ValidColor = new Color(0.392f, 0.722f, 0.820f, 0.4509804f);
    public static readonly Color InvalidColor = new Color(0.8314f, 0.2471f, 0.1569f, 0.4509804f);

    public static GameObject CreateSphereCursor(float scale = 0.2f)
    {
        GameObject cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursor.transform.localScale = Vector3.one * scale;
        cursor.GetComponent<Renderer>().material = new Material(MonoSandbox.RefCache.Selection);
        Object.Destroy(cursor.GetComponent<SphereCollider>());
        return cursor;
    }

    public static void DestroyCursor(ref GameObject cursor)
    {
        if (cursor == null)
        {
            return;
        }

        Object.Destroy(cursor);
        cursor = null;
    }

    public static bool IsMonoObject(Transform target)
    {
        return target != null && target.gameObject.name.Contains("MonoObject");
    }

    public static bool TryGetMonoRigidbody(RaycastHit hit, out Rigidbody body)
    {
        body = hit.collider?.attachedRigidbody;
        return IsMonoObject(hit.transform) && body != null;
    }

    public static void UpdateCursor(GameObject cursor, RaycastHit hit, bool isValid)
    {
        cursor.transform.position = hit.point;
        cursor.GetComponent<Renderer>().material.color = isValid ? ValidColor : InvalidColor;
    }
}
