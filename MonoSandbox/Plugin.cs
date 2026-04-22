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
    }
}
