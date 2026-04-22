using GorillaTag;
using MonoSandbox;
using MonoSandbox.Behaviours;
using UnityEngine;

public class RagdollManager : PlacementHandling
{
    public bool UseGorilla;
    public GameObject Gorilla, Body;

    public void Start()
    {
        Offset = 4.5f;
    }

    public override GameObject CursorRef
    {
        get
        {
            GameObject cursor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cursor.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
            Destroy(cursor.GetComponent<Collider>());
            return cursor;
        }
    }

    public override void DrawCursor(RaycastHit hitInfo)
    {
        base.DrawCursor(hitInfo);
        Cursor.transform.position = hitInfo.point + Vector3.up * 0.15f;
    }

    public override void Activated(RaycastHit hitInfo)
    {
        base.Activated(hitInfo);

        if (SandboxNetwork.TrySpawn(UseGorilla ? SandboxSpawnKind.GorillaRagdoll : SandboxSpawnKind.Ragdoll, hitInfo.point, hitInfo.normal))
        {
            return;
        }

        if (UseGorilla)
        {
            GameObject ragdoll = Instantiate(Gorilla);
            ragdoll.name += "MonoObject_Ragdoll";
            ragdoll.transform.SetParent(SandboxContainer.transform, false);

            foreach (Transform child in ragdoll.transform.GetChild(1).GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = 8;
                child.name += "MonoObject";
            }

            ragdoll.transform.position = hitInfo.point + new Vector3(0f, 0.45f, 0f);

            GTColor.HSVRanges ranges = new GTColor.HSVRanges(0f, 1f, 0.8f, 0.6f, 1f, 1f);
            Material material = new Material(ragdoll.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material)
            {
                color = GTColor.RandomHSV(ranges)
            };
            ragdoll.GetComponentInChildren<SkinnedMeshRenderer>().material = material;
            return;
        }

        GameObject body = Instantiate(Body);
        body.name += "MonoObject_Ragdoll";
        body.transform.SetParent(SandboxContainer.transform, false);

        foreach (Transform child in body.transform.GetChild(0).GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = 8;
            child.name += "MonoObject";
        }

        body.transform.position = hitInfo.point + new Vector3(0f, 0.6f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.4f, 0.5f);
        body.transform.GetChild(1).GetComponent<Renderer>().material.color = Color.grey;
        Destroy(body.GetComponent<MeshCollider>());
    }
}
