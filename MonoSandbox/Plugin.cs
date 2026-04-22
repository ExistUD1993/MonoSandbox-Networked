using System.Reflection;
using BepInEx;
using GorillaLocomotion;
using HarmonyLib;
using MonoSandbox.Behaviours;
using MonoSandbox.Behaviours.UI;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace MonoSandbox
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private const float MenuActivationThreshold = 0.6f;
        private const int MaxRaycastDistance = 2000;

        public static Plugin Instance { get; private set; }
        public static bool InRoom;

        private bool _gameInitialized , _initialized, _lastInRoom;
        private LayerMask _layerMask;
        private AssetBundle _bundle;
        private SandboxMenu _listManager;

        public GameObject _list, _itemsContainer;
        public AudioClip _pageOpen, _itemOpen;

        private BoxManager boxManager;
        private GravityManager gravityManager;
        private SphereManager sphereManager;
        private BeanManager beanManager;
        private CrateManager crateManager;
        private BathManager bathManager;
        private CouchManager couchManager;
        private RagdollManager ragdollManager;
        private AirStrikeManager airstrikeManager;
        private SpringManager springManager;
        private WeldManager weldManager;
        private FreezeManager freezeManager;
        private PhysGunManager physGunManager;
        private ThrusterManager thrusterManager;
        private C4Manager C4Control;
        private BalloonManager balloonManager;
        private WeaponManager weaponManager;
        private HammerManager hammerManager;
        private GrenadeManager grenadeManager;

        public void Awake()
        {
            Instance = this;
        }

        public Plugin()
        {
            new Harmony(PluginInfo.GUID).PatchAll(typeof(Plugin).Assembly);
        }

        public void OnEnable()
        {
            if (_initialized)
            {
                ResetEditModes();
            }
        }

        public void OnDisable()
        {
            if (_initialized)
            {
                ResetEditModes();
            }

            _list?.SetActive(enabled);
        }

        public void Update()
        {
            if (GTPlayer.Instance != null)
            {
                EnsureGameInitialized();
                SyncRoomState();
            }

            UpdateHitCache();
            UpdateMenuState();
        }

        public void OnJoin()
        {
            InRoom = true;

            foreach (Transform child in _itemsContainer.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        public void OnLeave()
        {
            InRoom = false;

            foreach (Transform child in _itemsContainer.transform)
            {
                child.gameObject.SetActive(false);
            }

            _list.SetActive(false);
        }

        public void OnGameInitialized()
        {
            gameObject.AddComponent<InputHandling>();
            gameObject.AddComponent<SandboxNetwork>();

            _layerMask = GTPlayer.Instance.locomotionEnabledLayers;
            _layerMask |= 1 << 8;

            CreateItemsContainer();
            LoadBundle();
            LoadSharedAssets();
            CreateManagers();
            CreateMenu();

            _initialized = true;
            ResetEditModes();
        }

        private void EnsureGameInitialized()
        {
            if (_gameInitialized)
            {
                return;
            }

            _gameInitialized = true;
            OnGameInitialized();
        }

        private void SyncRoomState()
        {
            if (PhotonNetwork.InRoom && !_lastInRoom)
            {
                OnJoin();
            }

            if (!PhotonNetwork.InRoom && _lastInRoom)
            {
                OnLeave();
            }

            _lastInRoom = PhotonNetwork.InRoom;
        }

        private void UpdateHitCache()
        {
            if (GTPlayer.Instance == null)
            {
                return;
            }

            Transform controllerTransform = GTPlayer.Instance.RightHand.controllerTransform;
            RefCache.HitExists = Physics.Raycast(
                controllerTransform.position,
                controllerTransform.forward,
                out RefCache.Hit,
                MaxRaycastDistance,
                _layerMask);
        }

        private void UpdateMenuState()
        {
            if (!InRoom || !enabled || !_initialized)
            {
                HideMenuAndResetEditModes();
                return;
            }

            bool shouldShowMenu = InputHandling.LeftGrip > MenuActivationThreshold;

            if (_list.activeInHierarchy)
            {
                ApplyMenuSelections();
                HandleUtilityActions();
            }

            if (_list.activeSelf != shouldShowMenu)
            {
                _list.SetActive(shouldShowMenu);
            }
        }

        private void HideMenuAndResetEditModes()
        {
            if (_list != null && _list.activeSelf)
            {
                _list.SetActive(false);
            }

            if (_initialized)
            {
                ResetEditModes();
            }
        }

        private void CreateItemsContainer()
        {
            _itemsContainer = Instantiate(new GameObject());
            _itemsContainer.name = "ItemFolderMono";
            _itemsContainer.transform.position = Vector3.zero;
            RefCache.SandboxContainer = _itemsContainer;
        }

        private void LoadBundle()
        {
            _bundle = AssetBundle.LoadFromStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("MonoSandbox.Assets.sandboxbundle"));
        }

        private void LoadSharedAssets()
        {
            RefCache.Default = _bundle.LoadAsset<Material>("Default");
            RefCache.Selection = _bundle.LoadAsset<Material>("Selection");
            RefCache.PageSelection = _bundle.LoadAsset<AudioClip>("Step1");
            RefCache.ItemSelection = _bundle.LoadAsset<AudioClip>("Step2");
        }

        private void CreateManagers()
        {
            C4Control = _itemsContainer.AddComponent<C4Manager>();
            C4Control.C4Model = _bundle.LoadAsset<GameObject>("C4_Weapon");
            C4Control.Mine = _bundle.LoadAsset<GameObject>("Mine_02");
            C4Control.ExplodeModel = _bundle.LoadAsset<GameObject>("Explosion");

            boxManager = _itemsContainer.AddComponent<BoxManager>();

            sphereManager = _itemsContainer.AddComponent<SphereManager>();
            sphereManager.Softbody = _bundle.LoadAsset<GameObject>("BoneSphere");
            sphereManager.Entity = _bundle.LoadAsset<GameObject>("Demon");

            beanManager = _itemsContainer.AddComponent<BeanManager>();
            beanManager.Explosion = _bundle.LoadAsset<GameObject>("Explosion");
            beanManager.Barrel = _bundle.LoadAsset<GameObject>("Barrel");

            gravityManager = _itemsContainer.AddComponent<GravityManager>();

            couchManager = _itemsContainer.AddComponent<CouchManager>();
            couchManager.Couch = _bundle.LoadAsset<GameObject>("Couch");

            crateManager = _itemsContainer.AddComponent<CrateManager>();
            crateManager.Crate = _bundle.LoadAsset<GameObject>("Crate");

            bathManager = _itemsContainer.AddComponent<BathManager>();
            bathManager.Bath = _bundle.LoadAsset<GameObject>("Bath");

            springManager = _itemsContainer.AddComponent<SpringManager>();
            ragdollManager = _itemsContainer.AddComponent<RagdollManager>();

            airstrikeManager = _itemsContainer.AddComponent<AirStrikeManager>();
            airstrikeManager.ExplodeModel = _bundle.LoadAsset<GameObject>("Explosion");
            airstrikeManager.CursorModel = _bundle.LoadAsset<GameObject>("Cursor");
            airstrikeManager.AirStrikeModel = _bundle.LoadAsset<GameObject>("Missile");

            thrusterManager = _itemsContainer.AddComponent<ThrusterManager>();
            thrusterManager.ThrusterModel = _bundle.LoadAsset<GameObject>("Thruster 1");
            thrusterManager.ThrustParticles = _bundle.LoadAsset<GameObject>("Thruster 2");

            weaponManager = _itemsContainer.AddComponent<WeaponManager>();
            weaponManager.ShotgunModel = _bundle.LoadAsset<GameObject>("Shotgun");
            weaponManager.ToolGunModel = _bundle.LoadAsset<GameObject>("ToolGun");
            weaponManager.RevolverModel = _bundle.LoadAsset<GameObject>("Pistol");
            weaponManager.SniperModel = _bundle.LoadAsset<GameObject>("SniperRifle");
            weaponManager.BananaGunModel = _bundle.LoadAsset<GameObject>("Banan");
            weaponManager.LaserGunModel = _bundle.LoadAsset<GameObject>("LaserGun");
            weaponManager.MelonCannonModel = _bundle.LoadAsset<GameObject>("Cannon");
            weaponManager.MelonModel = _bundle.LoadAsset<GameObject>("Melon");
            weaponManager.MelonExplodeModel = _bundle.LoadAsset<GameObject>("MelonExplode");
            weaponManager.HitPointParticle = _bundle.LoadAsset<GameObject>("HitPoint");
            weaponManager.AssultRiffle = _bundle.LoadAsset<GameObject>("AssaultRifle");
            weaponManager.LaserExplode = _bundle.LoadAsset<GameObject>("Explosion 2");

            weldManager = _itemsContainer.AddComponent<WeldManager>();
            freezeManager = _itemsContainer.AddComponent<FreezeManager>();

            balloonManager = _itemsContainer.AddComponent<BalloonManager>();
            balloonManager.Balloon = _bundle.LoadAsset<GameObject>("Balloon");

            ragdollManager.Body = _bundle.LoadAsset<GameObject>("Body");
            ragdollManager.Gorilla = _bundle.LoadAsset<GameObject>("GorillaBody");

            physGunManager = _itemsContainer.AddComponent<PhysGunManager>();

            hammerManager = _itemsContainer.AddComponent<HammerManager>();
            hammerManager.asset = _bundle.LoadAsset<GameObject>("Hammer_Weapon");

            grenadeManager = _itemsContainer.AddComponent<GrenadeManager>();
            grenadeManager.Grenade = _bundle.LoadAsset<GameObject>("Grenade");
            grenadeManager.Explode = _bundle.LoadAsset<GameObject>("Explosion");
        }

        private void CreateMenu()
        {
            _list = Instantiate(_bundle.LoadAsset<GameObject>("List"));
            _list.name = "List";
            _list.SetActive(false);
            _list.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().text = PluginInfo.Version;

            _listManager = _list.AddComponent<SandboxMenu>();
            _listManager._text = _bundle.LoadAsset<GameObject>("Temp");
        }

        private void ApplyMenuSelections()
        {
            boxManager.IsEditing = _listManager.objectButtons[0] || _listManager.objectButtons[7];
            boxManager.IsPlane = _listManager.objectButtons[7];

            sphereManager.IsEditing = _listManager.objectButtons[1] || _listManager.objectButtons[11] || _listManager.funButtons[0];
            sphereManager.IsSoftbody = _listManager.objectButtons[11];
            sphereManager.IsEnemy = _listManager.funButtons[0];

            beanManager.IsEditing = _listManager.objectButtons[2] || _listManager.objectButtons[4] || _listManager.objectButtons[5];
            beanManager.IsBarrel = _listManager.objectButtons[4];
            beanManager.IsWheel = _listManager.objectButtons[5];

            ragdollManager.IsEditing = _listManager.objectButtons[8] || _listManager.objectButtons[9];
            ragdollManager.UseGorilla = _listManager.objectButtons[9];

            crateManager.IsEditing = _listManager.objectButtons[3];
            couchManager.IsEditing = _listManager.objectButtons[6];
            bathManager.IsEditing = _listManager.objectButtons[10];

            weaponManager.editMode =
                _listManager.weaponButtons[0] ||
                _listManager.weaponButtons[1] ||
                _listManager.weaponButtons[2] ||
                _listManager.weaponButtons[3] ||
                _listManager.weaponButtons[4] ||
                _listManager.weaponButtons[7] ||
                _listManager.weaponButtons[8] ||
                _listManager.toolButtons[4];

            C4Control.editMode = _listManager.weaponButtons[5] || _listManager.weaponButtons[9];
            C4Control.IsMine = _listManager.weaponButtons[9];
            airstrikeManager.editMode = _listManager.weaponButtons[6];
            hammerManager.editMode = _listManager.weaponButtons[11];
            grenadeManager.editMode = _listManager.weaponButtons[10];

            weaponManager.currentWeapon = GetSelectedWeaponIndex();

            weldManager.editMode = _listManager.toolButtons[0];
            thrusterManager.editMode = _listManager.toolButtons[1];
            springManager.editMode = _listManager.toolButtons[2];
            physGunManager.editMode = _listManager.toolButtons[3];
            freezeManager.editMode = _listManager.toolButtons[5];
            gravityManager.editMode = _listManager.toolButtons[6];
            balloonManager.editMode = _listManager.toolButtons[7];
        }

        private int GetSelectedWeaponIndex()
        {
            if (_listManager.weaponButtons[0]) return 0;
            if (_listManager.weaponButtons[1]) return 1;
            if (_listManager.weaponButtons[4]) return 2;
            if (_listManager.weaponButtons[3]) return 3;
            if (_listManager.weaponButtons[7]) return 4;
            if (_listManager.weaponButtons[8]) return 5;
            if (_listManager.toolButtons[4]) return 6;
            if (_listManager.weaponButtons[2]) return 7;

            return weaponManager.currentWeapon;
        }

        private void HandleUtilityActions()
        {
            if (_listManager.utilButtons[0])
            {
                SandboxNetwork.BroadcastClearAll();
                DestroyChildren(_itemsContainer.transform);
            }

            if (_listManager.utilButtons[1])
            {
                DestroyTrackedObjects(thrusterManager.objectList);
            }

            if (_listManager.utilButtons[2])
            {
                DestroyTrackedObjects(springManager.objectList);
            }

            if (_listManager.utilButtons[3])
            {
                DestroyTrackedObjects(balloonManager.objectList);
            }
        }

        private static void DestroyChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
        }

        private static void DestroyTrackedObjects(System.Collections.Generic.List<GameObject> objects)
        {
            if (objects.Count == 0)
            {
                return;
            }

            foreach (GameObject trackedObject in objects)
            {
                if (trackedObject != null)
                {
                    Destroy(trackedObject);
                }
            }

            objects.Clear();
        }

        private void ResetEditModes()
        {
            ragdollManager.IsEditing = false;
            springManager.editMode = false;
            weaponManager.editMode = false;
            thrusterManager.editMode = false;
            C4Control.editMode = false;
            boxManager.IsEditing = false;
            sphereManager.IsEditing = false;
            beanManager.IsEditing = false;
            crateManager.IsEditing = false;
            weldManager.editMode = false;
            bathManager.IsEditing = false;
            balloonManager.editMode = false;
            freezeManager.editMode = false;
            physGunManager.editMode = false;
            gravityManager.editMode = false;
            airstrikeManager.editMode = false;
            couchManager.IsEditing = false;
            hammerManager.editMode = false;
            grenadeManager.editMode = false;
        }

        internal GameObject CreateNetworkedSpawn(SandboxSpawnKind kind, Vector3 point, Vector3 normal, int networkId)
        {
            GameObject spawnedObject = kind switch
            {
                SandboxSpawnKind.Box => CreateBox(point, normal, false),
                SandboxSpawnKind.Plane => CreateBox(point, normal, true),
                SandboxSpawnKind.Sphere => CreateSphere(point, normal),
                SandboxSpawnKind.SoftSphere => CreateSoftSphere(point, normal),
                SandboxSpawnKind.Enemy => CreateEnemy(point),
                SandboxSpawnKind.Bean => CreateBean(point, normal),
                SandboxSpawnKind.Barrel => CreateBarrel(point, normal),
                SandboxSpawnKind.Wheel => CreateWheel(point, normal),
                SandboxSpawnKind.Crate => CreateCrate(point, normal),
                SandboxSpawnKind.Couch => CreateCouch(point),
                SandboxSpawnKind.Bath => CreateBath(point, normal),
                SandboxSpawnKind.Ragdoll => CreateRagdoll(point, false),
                SandboxSpawnKind.GorillaRagdoll => CreateRagdoll(point, true),
                SandboxSpawnKind.C4 => CreateC4(point, normal),
                SandboxSpawnKind.Mine => CreateMine(point, normal),
                SandboxSpawnKind.Airstrike => CreateAirstrike(point),
                _ => null
            };

            if (spawnedObject != null)
            {
                SandboxNetwork.AttachIdentity(spawnedObject, networkId);
            }

            return spawnedObject;
        }

        internal GameObject CreateNetworkedAttachment(
            SandboxAttachmentKind kind,
            int primaryId,
            int secondaryId,
            Vector3 point,
            Vector3 normal,
            float value,
            int networkId)
        {
            if (!SandboxNetwork.TryGetObject(primaryId, out GameObject primaryTarget))
            {
                return null;
            }

            return kind switch
            {
                SandboxAttachmentKind.Thruster => CreateThrusterAttachment(primaryTarget, point, normal, value, networkId),
                SandboxAttachmentKind.Balloon => CreateBalloonAttachment(primaryTarget, point, value, networkId),
                SandboxAttachmentKind.Spring => SandboxNetwork.TryGetObject(secondaryId, out GameObject springTarget)
                    ? CreateSpringAttachment(primaryTarget, springTarget, networkId)
                    : null,
                SandboxAttachmentKind.Weld => SandboxNetwork.TryGetObject(secondaryId, out GameObject weldTarget)
                    ? CreateWeldAttachment(primaryTarget, weldTarget)
                    : null,
                _ => null
            };
        }

        internal void ApplyNetworkedState(int targetId, SandboxStateKind stateKind, Color color)
        {
            if (!SandboxNetwork.TryGetObject(targetId, out GameObject target))
            {
                return;
            }

            switch (stateKind)
            {
                case SandboxStateKind.Freeze:
                    if (target.TryGetComponent(out Rigidbody freezeBody))
                    {
                        freezeBody.constraints = freezeBody.constraints == RigidbodyConstraints.None
                            ? RigidbodyConstraints.FreezeAll
                            : RigidbodyConstraints.None;
                    }
                    break;
                case SandboxStateKind.Gravity:
                    if (target.TryGetComponent(out Rigidbody gravityBody))
                    {
                        gravityBody.useGravity = !gravityBody.useGravity;
                    }
                    break;
                case SandboxStateKind.Colorize:
                    Renderer renderer = target.GetComponent<Renderer>() ?? target.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = color;
                    }
                    break;
            }
        }

        internal void ClearNetworkedObjects()
        {
            DestroyChildren(_itemsContainer.transform);
        }

        private GameObject CreateBox(Vector3 point, Vector3 normal, bool isPlane)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<BoxCollider>();

            box.layer = 8;
            box.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(box.transform, false);
            box.transform.SetParent(_itemsContainer.transform, false);

            Rigidbody rigidbody = box.AddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.mass = 2.5f;

            box.transform.forward = normal;
            box.transform.position = point + box.transform.forward / 4f;
            box.GetComponent<Renderer>().material = RefCache.Default;
            box.transform.localScale = isPlane ? new Vector3(0.6f, 0.6f, 0.1f) : new Vector3(0.4f, 0.4f, 0.4f);
            return box;
        }

        private GameObject CreateSphere(Vector3 point, Vector3 normal)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Rigidbody rigidbody = ball.AddComponent<Rigidbody>();
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<SphereCollider>();

            ball.layer = 8;
            ball.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(ball.transform, false);
            ball.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 3.5f;

            ball.transform.forward = normal;
            ball.transform.position = point + ball.transform.forward / 4f;
            ball.GetComponent<Renderer>().material = RefCache.Default;
            ball.transform.localScale = Vector3.one * 0.4f;
            return ball;
        }

        private GameObject CreateSoftSphere(Vector3 point, Vector3 normal)
        {
            GameObject ball = Instantiate(sphereManager.Softbody);
            ball.layer = 8;
            ball.name += "MonoObject";
            ball.transform.SetParent(_itemsContainer.transform, false);

            foreach (Transform child in ball.transform.GetChild(0).GetComponentInChildren<Transform>())
            {
                child.name += "MonoObject";
            }

            ball.transform.forward = normal;
            ball.transform.position = point + ball.transform.forward / 4f;
            ball.transform.GetChild(1).GetComponent<Renderer>().material = RefCache.Default;
            ball.transform.localScale = Vector3.one * 0.3f;
            ball.AddComponent<BoneSphere>();
            return ball;
        }

        private GameObject CreateEnemy(Vector3 point)
        {
            GameObject enemyObject = Instantiate(sphereManager.Entity);
            enemyObject.name = "MonoObject";
            enemyObject.AddComponent<SphereCollider>();

            Enemy enemy = enemyObject.AddComponent<Enemy>();
            enemy.Health = 40f;
            enemy.Defence = 1.75f;

            enemyObject.transform.SetParent(_itemsContainer.transform, false);
            enemyObject.transform.position = point + Vector3.up / 2f;
            return enemyObject;
        }

        private GameObject CreateBean(Vector3 point, Vector3 normal)
        {
            GameObject bean = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Rigidbody rigidbody = bean.AddComponent<Rigidbody>();
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<CapsuleCollider>().height = 2;

            bean.layer = 8;
            bean.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(bean.transform, false);
            bean.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 3.5f;

            bean.transform.up = normal;
            bean.transform.position = point + bean.transform.up / 2.5f;
            bean.GetComponent<Renderer>().material = RefCache.Default;
            bean.transform.localScale = Vector3.one * 0.4f;
            return bean;
        }

        private GameObject CreateBarrel(Vector3 point, Vector3 normal)
        {
            GameObject barrel = Instantiate(beanManager.Barrel);
            barrel.AddComponent<BoxCollider>().size = new Vector3(0.025f, 0.025f, 0.025f);
            Rigidbody rigidbody = barrel.AddComponent<Rigidbody>();
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<BoxCollider>().size = new Vector3(0.025f, 0.025f, 0.025f);

            Explode explosion = barrel.AddComponent<Explode>();
            explosion.Multiplier = 4f;
            explosion.Explosion = beanManager.Explosion;

            barrel.layer = 8;
            barrel.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(barrel.transform, false);
            barrel.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 3.5f;

            barrel.transform.up = normal;
            barrel.transform.position = point + barrel.transform.up / 2.5f;
            barrel.transform.localScale = Vector3.one * 15f;
            return barrel;
        }

        private GameObject CreateWheel(Vector3 point, Vector3 normal)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            Rigidbody rigidbody = wheel.AddComponent<Rigidbody>();
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<BoxCollider>();

            wheel.layer = 8;
            wheel.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(wheel.transform, false);
            wheel.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 3.5f;

            wheel.transform.up = normal;
            wheel.transform.position = point + wheel.transform.up / 2.5f;
            wheel.GetComponent<Renderer>().material = RefCache.Default;
            return wheel;
        }

        private GameObject CreateCrate(Vector3 point, Vector3 normal)
        {
            GameObject crate = Instantiate(crateManager.Crate);
            Rigidbody rigidbody = crate.AddComponent<Rigidbody>();
            GameObject userCollision = new GameObject();
            userCollision.AddComponent<BoxCollider>();

            crate.layer = 8;
            crate.name += "MonoObject";
            userCollision.layer = 0;
            userCollision.transform.SetParent(crate.transform, false);
            crate.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 2.5f;
            crate.transform.forward = normal;
            crate.transform.position = point + crate.transform.forward / 4f;
            return crate;
        }

        private GameObject CreateCouch(Vector3 point)
        {
            GameObject couch = Instantiate(couchManager.Couch);
            Rigidbody rigidbody = couch.AddComponent<Rigidbody>();
            couch.layer = 8;
            couch.AddComponent<BoxCollider>();
            couch.name += "MonoObject";
            couch.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 8f;
            couch.transform.position = point + Vector3.up * 0.4f;
            couch.transform.localScale = Vector3.one * 100f;
            return couch;
        }

        private GameObject CreateBath(Vector3 point, Vector3 normal)
        {
            GameObject bath = Instantiate(bathManager.Bath);
            Rigidbody rigidbody = bath.AddComponent<Rigidbody>();
            bath.layer = 8;
            bath.name += "MonoObject";
            bath.transform.SetParent(_itemsContainer.transform, false);

            rigidbody.useGravity = true;
            rigidbody.mass = 8f;

            bath.transform.forward = normal;
            bath.transform.position = point + bath.transform.forward / 4f;
            bath.transform.localScale = Vector3.one * 20f;
            return bath;
        }

        private GameObject CreateRagdoll(Vector3 point, bool useGorilla)
        {
            GameObject ragdoll = Instantiate(useGorilla ? ragdollManager.Gorilla : ragdollManager.Body);
            ragdoll.name += "MonoObject_Ragdoll";
            ragdoll.transform.SetParent(_itemsContainer.transform, false);

            if (useGorilla)
            {
                foreach (Transform child in ragdoll.transform.GetChild(1).GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = 8;
                    child.name += "MonoObject";
                }

                ragdoll.transform.position = point + new Vector3(0f, 0.45f, 0f);
                GorillaTag.GTColor.HSVRanges ranges = new GorillaTag.GTColor.HSVRanges(0f, 1f, 0.8f, 0.6f, 1f, 1f);
                Material material = new Material(ragdoll.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material)
                {
                    color = GorillaTag.GTColor.RandomHSV(ranges)
                };
                ragdoll.GetComponentInChildren<SkinnedMeshRenderer>().material = material;
            }
            else
            {
                foreach (Transform child in ragdoll.transform.GetChild(0).GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = 8;
                    child.name += "MonoObject";
                }

                ragdoll.transform.position = point + new Vector3(0f, 0.6f, 0f);
                ragdoll.transform.localScale = new Vector3(0.4f, 0.4f, 0.5f);
                ragdoll.transform.GetChild(1).GetComponent<Renderer>().material.color = Color.grey;
                Destroy(ragdoll.GetComponent<MeshCollider>());
            }

            return ragdoll;
        }

        private GameObject CreateC4(Vector3 point, Vector3 normal)
        {
            GameObject c4 = Instantiate(C4Control.C4Model);
            Destroy(c4.GetComponent<MeshCollider>());
            c4.AddComponent<BoxCollider>();
            c4.transform.SetParent(_itemsContainer.transform, false);
            c4.transform.position = point;
            c4.transform.forward = normal;
            c4.transform.localScale = Vector3.one * 1.4f;

            BombDetonate bombDetonate = c4.AddComponent<BombDetonate>();
            bombDetonate.ExplosionOBJ = C4Control.ExplodeModel;
            bombDetonate.multiplier = C4Control.multiplier;
            return c4;
        }

        private GameObject CreateMine(Vector3 point, Vector3 normal)
        {
            GameObject mine = Instantiate(C4Control.Mine);
            mine.transform.SetParent(_itemsContainer.transform, false);
            mine.transform.position = point;
            mine.transform.up = normal;
            mine.transform.localScale = Vector3.one;

            MineDetonate mineDetonate = mine.AddComponent<MineDetonate>();
            mineDetonate.Explosion = C4Control.ExplodeModel;
            mineDetonate.Multiplier = C4Control.multiplier;
            return mine;
        }

        private GameObject CreateAirstrike(Vector3 point)
        {
            GameObject missile = Instantiate(airstrikeManager.AirStrikeModel);
            missile.transform.SetParent(_itemsContainer.transform, false);
            missile.transform.position = point + new Vector3(0f, 80f, 0f);
            missile.transform.localScale = Vector3.one * 50f;

            Airstrike airstrike = missile.AddComponent<Airstrike>();
            airstrike.StrikeLocation = point;
            airstrike.ExplosionOBJ = airstrikeManager.ExplodeModel;
            return missile;
        }

        private GameObject CreateThrusterAttachment(GameObject target, Vector3 point, Vector3 normal, float multiplier, int networkId)
        {
            Rigidbody targetBody = target.GetComponent<Rigidbody>();
            if (targetBody == null)
            {
                return null;
            }

            GameObject thruster = Instantiate(thrusterManager.ThrusterModel);
            thruster.transform.localScale = Vector3.one * 10f;
            thruster.transform.SetParent(target.transform, true);
            thruster.transform.position = point;
            thruster.transform.forward = normal;
            thruster.name = "Thruster MonoObject";
            thruster.GetComponent<Renderer>().material.color = Color.black;

            ThrusterControls control = thruster.AddComponent<ThrusterControls>();
            control.rb = targetBody;
            control.multiplier = multiplier;
            control.particle = Instantiate(thrusterManager.ThrustParticles);

            thrusterManager.objectList.Add(thruster);
            SandboxNetwork.AttachIdentity(thruster, networkId);
            return thruster;
        }

        private GameObject CreateBalloonAttachment(GameObject target, Vector3 point, float power, int networkId)
        {
            Rigidbody connectedBody = target.GetComponent<Rigidbody>();
            if (connectedBody == null)
            {
                return null;
            }

            GameObject balloonObject = Instantiate(balloonManager.Balloon);
            balloonObject.layer = 8;
            balloonObject.name = "Balloon MonoObject";
            balloonObject.transform.SetParent(_itemsContainer.transform, false);
            balloonObject.transform.localScale = Vector3.one * 0.3f;
            balloonObject.transform.position = point + Vector3.up * 0.3f;
            balloonObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

            Balloon balloonScript = balloonObject.AddComponent<Balloon>();
            balloonScript.power = power;

            SpringLine line = balloonObject.AddComponent<SpringLine>();
            line.lineRenderer = SpringLine.CreateRenderer(balloonObject);
            line.pointone = balloonObject.transform.GetChild(0).gameObject;
            line.pointtwo = target;

            SpringJoint joint = balloonObject.AddComponent<SpringJoint>();
            joint.maxDistance = 0f;
            joint.spring = 20f;
            joint.damper = 10f;
            joint.connectedBody = connectedBody;

            balloonManager.objectList.Add(balloonObject);
            SandboxNetwork.AttachIdentity(balloonObject, networkId);
            return balloonObject;
        }

        private GameObject CreateSpringAttachment(GameObject primaryTarget, GameObject secondaryTarget, int networkId)
        {
            Rigidbody primaryBody = primaryTarget.GetComponent<Rigidbody>();
            Rigidbody secondaryBody = secondaryTarget.GetComponent<Rigidbody>();
            if (primaryBody == null || secondaryBody == null)
            {
                return null;
            }

            GameObject jointObject = new GameObject
            {
                name = "MSJoint MonoObject"
            };

            jointObject.transform.SetParent(primaryTarget.transform, false);
            springManager.objectList.Add(jointObject);

            FixedJoint fixedJoint = jointObject.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = primaryBody;

            SpringJoint joint = jointObject.AddComponent<SpringJoint>();
            joint.minDistance = Vector3.Distance(primaryTarget.transform.position, secondaryTarget.transform.position) - 1f;
            joint.damper = 30f;
            joint.spring = 10f;
            joint.massScale = 12f;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = secondaryBody;

            SpringLine line = jointObject.AddComponent<SpringLine>();
            line.lineRenderer = SpringLine.CreateRenderer(jointObject);
            line.pointone = jointObject;
            line.pointtwo = secondaryTarget;

            SandboxNetwork.AttachIdentity(jointObject, networkId);
            return jointObject;
        }

        private GameObject CreateWeldAttachment(GameObject primaryTarget, GameObject secondaryTarget)
        {
            Rigidbody primaryBody = primaryTarget.GetComponent<Rigidbody>();
            if (primaryBody == null)
            {
                return null;
            }

            FixedJoint joint = secondaryTarget.AddComponent<FixedJoint>();
            joint.connectedBody = primaryBody;
            joint.autoConfigureConnectedAnchor = true;
            return secondaryTarget;
        }
    }
}
