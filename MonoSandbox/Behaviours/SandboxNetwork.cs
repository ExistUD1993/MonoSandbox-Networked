using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MonoSandbox
{
    public enum SandboxSpawnKind : byte
    {
        Box,
        Plane,
        Sphere,
        SoftSphere,
        Enemy,
        Bean,
        Barrel,
        Wheel,
        Crate,
        Couch,
        Bath,
        Ragdoll,
        GorillaRagdoll,
        C4,
        Mine,
        Airstrike
    }

    public enum SandboxAttachmentKind : byte
    {
        Thruster,
        Balloon,
        Spring,
        Weld
    }

    public enum SandboxStateKind : byte
    {
        Freeze,
        Gravity,
        Colorize
    }

    public class SandboxObjectIdentity : MonoBehaviour
    {
        public int NetworkId, OwnerActorNumber;

        public bool IsOwnedLocally => PhotonNetwork.LocalPlayer != null && OwnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

        private void OnDestroy()
        {
            SandboxNetwork.Unregister(NetworkId, gameObject);
        }
    }

    public class SandboxRigidbodySync : MonoBehaviour
    {
        private const float SendInterval = 0.05f;

        private float _lastSendTime;
        private Rigidbody _body;
        private SandboxObjectIdentity _identity;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _identity = GetComponent<SandboxObjectIdentity>();
        }

        private void FixedUpdate()
        {
            if (_body == null || _identity == null || !_identity.IsOwnedLocally || !SandboxNetwork.ShouldSync)
            {
                return;
            }

            if (Time.time < _lastSendTime + SendInterval)
            {
                return;
            }

            _lastSendTime = Time.time;
            SandboxNetwork.BroadcastTransform(
                _identity.NetworkId,
                transform.position,
                transform.rotation,
                _body.linearVelocity,
                _body.angularVelocity);
        }
    }

    public class SandboxNetwork : MonoBehaviour, IOnEventCallback
    {
        private const byte SpawnEventCode = 61;
        private const byte ClearAllEventCode = 62;
        private const byte TransformEventCode = 63;
        private const byte StateEventCode = 64;
        private const byte AttachmentEventCode = 65;
        private const byte ExplosionEventCode = 66;

        private static readonly Dictionary<int, GameObject> NetworkedObjects = new Dictionary<int, GameObject>();

        public static SandboxNetwork Instance { get; private set; }
        public static bool ShouldSync => Instance != null && PhotonNetwork.InRoom && Plugin.InRoom;
        public static bool IsApplyingRemoteExplosion { get; private set; }

        private int _nextNetworkId = 1;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static bool TrySpawn(SandboxSpawnKind kind, Vector3 point, Vector3 normal)
        {
            if (!ShouldSync)
            {
                return false;
            }

            int networkId = Instance.NextNetworkId();
            Plugin.Instance.CreateNetworkedSpawn(kind, point, normal, networkId);

            object[] eventData =
            {
                (byte)kind,
                networkId,
                point.x, point.y, point.z,
                normal.x, normal.y, normal.z
            };

            RaiseCachedEvent(SpawnEventCode, eventData);
            return true;
        }

        public static bool TryCreateAttachment(SandboxAttachmentKind kind, GameObject primaryTarget, GameObject secondaryTarget, Vector3 point, Vector3 normal, float value = 0f)
        {
            if (!ShouldSync || !TryGetNetworkId(primaryTarget, out int primaryId))
            {
                return false;
            }

            int secondaryId = 0;
            if (secondaryTarget != null && !TryGetNetworkId(secondaryTarget, out secondaryId))
            {
                return false;
            }

            int networkId = Instance.NextNetworkId();
            Plugin.Instance.CreateNetworkedAttachment(kind, primaryId, secondaryId, point, normal, value, networkId);

            object[] eventData =
            {
                (byte)kind,
                networkId,
                primaryId,
                secondaryId,
                point.x, point.y, point.z,
                normal.x, normal.y, normal.z,
                value
            };

            RaiseCachedEvent(AttachmentEventCode, eventData);
            return true;
        }

        public static bool TryApplyState(GameObject target, SandboxStateKind stateKind, Color color)
        {
            if (!ShouldSync || !TryGetNetworkId(target, out int targetId))
            {
                return false;
            }

            Plugin.Instance.ApplyNetworkedState(targetId, stateKind, color);

            object[] eventData =
            {
                (byte)stateKind,
                targetId,
                color.r, color.g, color.b, color.a
            };

            PhotonNetwork.RaiseEvent(
                StateEventCode,
                eventData,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);

            return true;
        }

        public static void BroadcastExplosion(Vector3 point, float force, float radius, float upModifier = 0f)
        {
            if (!ShouldSync)
            {
                return;
            }

            object[] eventData = { point.x, point.y, point.z, force, radius, upModifier };
            PhotonNetwork.RaiseEvent(
                ExplosionEventCode,
                eventData,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);
        }

        public static void BroadcastClearAll()
        {
            if (!ShouldSync)
            {
                return;
            }

            RaiseCachedEvent(ClearAllEventCode, null);
        }

        public static void BroadcastTransform(int networkId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            object[] eventData =
            {
                networkId,
                position.x, position.y, position.z,
                rotation.x, rotation.y, rotation.z, rotation.w,
                velocity.x, velocity.y, velocity.z,
                angularVelocity.x, angularVelocity.y, angularVelocity.z
            };

            PhotonNetwork.RaiseEvent(
                TransformEventCode,
                eventData,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendUnreliable);
        }

        public static void AttachIdentity(GameObject gameObject, int networkId, int ownerActorNumber = 0)
        {
            SandboxObjectIdentity identity = gameObject.GetComponent<SandboxObjectIdentity>();
            if (identity == null)
            {
                identity = gameObject.AddComponent<SandboxObjectIdentity>();
            }

            identity.NetworkId = networkId;
            identity.OwnerActorNumber = ownerActorNumber == 0 && PhotonNetwork.LocalPlayer != null
                ? PhotonNetwork.LocalPlayer.ActorNumber
                : ownerActorNumber;

            if (gameObject.GetComponent<Rigidbody>() != null && gameObject.GetComponent<SandboxRigidbodySync>() == null)
            {
                gameObject.AddComponent<SandboxRigidbodySync>();
            }

            NetworkedObjects[networkId] = gameObject;
        }

        public static bool TryGetNetworkId(GameObject target, out int networkId)
        {
            networkId = 0;
            if (target == null)
            {
                return false;
            }

            SandboxObjectIdentity identity = target.GetComponentInParent<SandboxObjectIdentity>();
            if (identity == null)
            {
                return false;
            }

            networkId = identity.NetworkId;
            return true;
        }

        public static bool TryGetObject(int networkId, out GameObject gameObject)
        {
            return NetworkedObjects.TryGetValue(networkId, out gameObject) && gameObject != null;
        }

        public static void Unregister(int networkId, GameObject gameObject)
        {
            if (networkId == 0)
            {
                return;
            }

            if (NetworkedObjects.TryGetValue(networkId, out GameObject current) && current == gameObject)
            {
                NetworkedObjects.Remove(networkId);
            }
        }

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case SpawnEventCode:
                    HandleSpawnEvent((object[])photonEvent.CustomData, photonEvent.Sender);
                    break;
                case ClearAllEventCode:
                    Plugin.Instance.ClearNetworkedObjects();
                    break;
                case TransformEventCode:
                    HandleTransformEvent((object[])photonEvent.CustomData, photonEvent.Sender);
                    break;
                case StateEventCode:
                    HandleStateEvent((object[])photonEvent.CustomData);
                    break;
                case AttachmentEventCode:
                    HandleAttachmentEvent((object[])photonEvent.CustomData, photonEvent.Sender);
                    break;
                case ExplosionEventCode:
                    HandleExplosionEvent((object[])photonEvent.CustomData);
                    break;
            }
        }

        private int NextNetworkId()
        {
            return _nextNetworkId++;
        }

        private static void RaiseCachedEvent(byte eventCode, object customData)
        {
            PhotonNetwork.RaiseEvent(
                eventCode,
                customData,
                new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others,
                    CachingOption = EventCaching.AddToRoomCache
                },
                SendOptions.SendReliable);
        }

        private void HandleSpawnEvent(object[] eventData, int senderActorNumber)
        {
            SandboxSpawnKind kind = (SandboxSpawnKind)(byte)eventData[0];
            int networkId = (int)eventData[1];
            Vector3 point = new Vector3((float)eventData[2], (float)eventData[3], (float)eventData[4]);
            Vector3 normal = new Vector3((float)eventData[5], (float)eventData[6], (float)eventData[7]);

            GameObject created = Plugin.Instance.CreateNetworkedSpawn(kind, point, normal, networkId);
            if (created != null)
            {
                AttachIdentity(created, networkId, senderActorNumber);
            }
        }

        private static void HandleTransformEvent(object[] eventData, int senderActorNumber)
        {
            int networkId = (int)eventData[0];
            if (!TryGetObject(networkId, out GameObject target))
            {
                return;
            }

            SandboxObjectIdentity identity = target.GetComponent<SandboxObjectIdentity>();
            if (identity != null && identity.OwnerActorNumber == PhotonNetwork.LocalPlayer?.ActorNumber)
            {
                return;
            }

            Vector3 position = new Vector3((float)eventData[1], (float)eventData[2], (float)eventData[3]);
            Quaternion rotation = new Quaternion((float)eventData[4], (float)eventData[5], (float)eventData[6], (float)eventData[7]);
            Vector3 velocity = new Vector3((float)eventData[8], (float)eventData[9], (float)eventData[10]);
            Vector3 angularVelocity = new Vector3((float)eventData[11], (float)eventData[12], (float)eventData[13]);

            target.transform.SetPositionAndRotation(position, rotation);
            Rigidbody body = target.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = velocity;
                body.angularVelocity = angularVelocity;
            }

            if (identity != null)
            {
                identity.OwnerActorNumber = senderActorNumber;
            }
        }

        private static void HandleStateEvent(object[] eventData)
        {
            SandboxStateKind stateKind = (SandboxStateKind)(byte)eventData[0];
            int targetId = (int)eventData[1];
            Color color = new Color((float)eventData[2], (float)eventData[3], (float)eventData[4], (float)eventData[5]);
            Plugin.Instance.ApplyNetworkedState(targetId, stateKind, color);
        }

        private void HandleAttachmentEvent(object[] eventData, int senderActorNumber)
        {
            SandboxAttachmentKind kind = (SandboxAttachmentKind)(byte)eventData[0];
            int networkId = (int)eventData[1];
            int primaryId = (int)eventData[2];
            int secondaryId = (int)eventData[3];
            Vector3 point = new Vector3((float)eventData[4], (float)eventData[5], (float)eventData[6]);
            Vector3 normal = new Vector3((float)eventData[7], (float)eventData[8], (float)eventData[9]);
            float value = (float)eventData[10];

            GameObject created = Plugin.Instance.CreateNetworkedAttachment(kind, primaryId, secondaryId, point, normal, value, networkId);
            if (created != null && kind != SandboxAttachmentKind.Weld)
            {
                AttachIdentity(created, networkId, senderActorNumber);
            }
        }

        private static void HandleExplosionEvent(object[] eventData)
        {
            Vector3 point = new Vector3((float)eventData[0], (float)eventData[1], (float)eventData[2]);
            float force = (float)eventData[3];
            float radius = (float)eventData[4];
            float upModifier = (float)eventData[5];

            IsApplyingRemoteExplosion = true;
            try
            {
                foreach (Collider nearby in Physics.OverlapSphere(point, radius))
                {
                    Rigidbody body = nearby.GetComponent<Rigidbody>();
                    body?.AddExplosionForce(force, point, radius, upModifier);
                    nearby.GetComponent<BombDetonate>()?.Explode();
                    nearby.GetComponent<MineDetonate>()?.Explode();
                    nearby.GetComponent<Explode>()?.ExplodeObject();
                }
            }
            finally
            {
                IsApplyingRemoteExplosion = false;
            }
        }
    }
}
