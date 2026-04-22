using UnityEngine;
using UnityEngine.UI;

namespace MonoSandbox.Behaviours.UI
{
    public class SandboxMenu : MonoBehaviour
    {
        private const int ButtonsPerLine = 4;
        private static readonly int[] PageLabelIndices = { 0, 2, 4, 6, 8 };

        public GameObject _menu, _text, _objParent, _toolParent, _utilsParent, _weaponsParent, _funParent, _sideBtnParent, _sender;
        public int _currentPage;

        public bool[] objectButtons = new bool[12], weaponButtons = new bool[12], toolButtons = new bool[8], utilButtons = new bool[4], funButtons = new bool[1];
        public string[] objectNames = { "Box", "Sphere", "Bean", "Crate", "Barrel", "Wheel", "Couch", "Plane", "Body", "Gorilla", "Bathtub", "Soft Sphere" };
        public string[] weaponNames = { "Revolver", "Shotgun", "Rifle", "Sniper", "Melon Cannon", "C4", "Airstrike", "Laser Gun", "Banana Gun", "Mine", "Grenade", "Hammer" };
        public string[] toolNames = { "Weld", "Thruster", "Spring", "Gravity Gun", "Colourize", "Freeze", "Toggle Gravity", "Balloon" };
        public string[] utilNames = { "Remove All", "Remove Thrusters", "Remove Springs", "Remove Balloons" };
        public string[] funNames = { "Entity" };

        private Canvas _canvas;
        private AudioSource _audioSource;
        private GameObject[] _pageParents;

        public void Start()
        {
            _menu = transform.GetChild(1).gameObject;
            _canvas = _menu.transform.GetChild(0).GetComponent<Canvas>();
            _objParent = new GameObject();
            _weaponsParent = new GameObject();
            _toolParent = new GameObject();
            _utilsParent = new GameObject();
            _funParent = new GameObject();
            _sideBtnParent = new GameObject
            {
                name = "SideButtons"
            };
            _sideBtnParent.transform.SetParent(_menu.transform, false);

            _pageParents = new[] { _objParent, _weaponsParent, _toolParent, _utilsParent, _funParent };

            AddPage(objectNames, "Objects", _objParent, 0);
            AddPage(weaponNames, "Weapons", _weaponsParent, 1);
            AddPage(toolNames, "Tools", _toolParent, 2);
            AddPage(utilNames, "Utils", _utilsParent, 3);
            AddPage(funNames, "Fun", _funParent, 4);

            transform.SetParent(RefCache.LHand.transform, false);
            transform.localPosition = new Vector3(0f, 0.14f, 0.075f);
            transform.localScale = Vector3.one * 0.5f;
            transform.localEulerAngles = new Vector3(0f, 90f, -5f);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0.4f;
            _audioSource.spatialBlend = 1f;
            _audioSource.clip = RefCache.PageSelection;
            _audioSource.Play();
        }

        public void Update()
        {
            if (_pageParents == null)
            {
                return;
            }

            for (int i = 0; i < _pageParents.Length; i++)
            {
                bool isActive = _currentPage == i;
                GameObject pageParent = _pageParents[i];
                GameObject pageLabel = _canvas.transform.GetChild(PageLabelIndices[i]).gameObject;

                if (pageParent.activeSelf != isActive)
                {
                    pageParent.SetActive(isActive);
                }

                if (pageLabel.activeSelf != isActive)
                {
                    pageLabel.SetActive(isActive);
                }
            }
        }

        public void AddPage(string[] buttonNames, string pageName, GameObject buttonParent, int pIndex)
        {
            int currentSpot = 0;
            buttonParent.transform.SetParent(_menu.transform, false);
            buttonParent.name = pageName;

            GameObject textParent = new GameObject
            {
                name = pageName
            };
            textParent.transform.parent = _canvas.transform;

            GameObject sideButton = CreateButtonRoot(_sideBtnParent.transform, new Vector3(0.02f, 0.07f, 0.225f), new Vector3(0f, 0.085f - pIndex * 0.1f, 0.745f));
            GameObject pageLabel = CreateLabel(pageName, sideButton.transform.position, _canvas.transform);

            PageButton pageButton = sideButton.AddComponent<PageButton>();
            pageButton._pageIndex = pIndex;
            pageButton._text = pageLabel;
            pageButton._list = this;

            for (int i = 0; i < buttonNames.Length; i++)
            {
                GameObject button = CreateButtonRoot(buttonParent.transform, new Vector3(0.025f, 0.145f, 0.145f), GetButtonPosition(i, ref currentSpot));
                GameObject itemLabel = CreateLabel(buttonNames[i], button.transform.position, textParent.transform);

                Button buttonScript = button.AddComponent<Button>();
                buttonScript._buttonIndex = i;
                buttonScript._text = itemLabel;
                buttonScript._list = this;
            }
        }

        private static GameObject CreateButtonRoot(Transform parent, Vector3 scale, Vector3 localPosition)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.layer = 18;
            button.GetComponent<BoxCollider>().isTrigger = true;
            button.transform.SetParent(parent, false);
            button.transform.localScale = scale;
            button.transform.localPosition = localPosition;
            button.GetComponent<Renderer>().material.shader = Shader.Find("GorillaTag/UberShader");
            return button;
        }

        private GameObject CreateLabel(string text, Vector3 worldPosition, Transform parent)
        {
            GameObject label = Instantiate(_text);
            label.transform.SetParent(parent, false);
            label.GetComponent<RectTransform>().eulerAngles = new Vector3(0f, 90f, 0f);
            label.transform.position = worldPosition + new Vector3(-0.015f, 0f, 0f);
            label.GetComponent<Text>().text = text.ToUpper();
            label.GetComponent<Text>().color = Color.black;
            label.name = text;
            return label;
        }

        private static Vector3 GetButtonPosition(int index, ref int currentSpot)
        {
            int currentLine = index / ButtonsPerLine;
            Vector3 position = new Vector3(
                0.02f,
                -currentLine * (0.145f + 0.03f),
                (ButtonsPerLine - 1 - currentSpot) * (0.145f + 0.02f));

            currentSpot++;
            if (currentSpot == ButtonsPerLine)
            {
                currentSpot = 0;
            }

            return position;
        }

        public void Clear()
        {
            objectButtons = new bool[12];
            weaponButtons = new bool[12];
            toolButtons = new bool[8];
            utilButtons = new bool[4];
            funButtons = new bool[1];
        }

        public void PlayAudio(bool item)
        {
            _audioSource.PlayOneShot(item ? RefCache.ItemSelection : RefCache.PageSelection);
        }

        public bool[] GetArray() => _currentPage switch
        {
            0 => objectButtons,
            1 => weaponButtons,
            2 => toolButtons,
            3 => utilButtons,
            4 => funButtons,
            _ => throw new System.IndexOutOfRangeException()
        };

        public void SetArray(bool[] array)
        {
            switch (_currentPage)
            {
                case 0:
                    objectButtons = array;
                    break;
                case 1:
                    weaponButtons = array;
                    break;
                case 2:
                    toolButtons = array;
                    break;
                case 3:
                    utilButtons = array;
                    break;
                case 4:
                    funButtons = array;
                    break;
            }
        }
    }
}
