using GorillaExtensions;
using UnityEngine;

namespace MonoSandbox.Behaviours.UI
{
    public abstract class AnimatedMenuButton : MonoBehaviour
    {
        private static readonly Color ActiveColor = new Color32(71, 121, 196, 255);
        private static readonly Color InactiveColor = new Color32(215, 225, 239, 255);

        protected GameObject TextObject;
        protected float LastTime;

        private Vector3 _buttonScale, _textScale;
        private bool _active, _flipping, _initialized;
        private float _scale = 1f , _time;

        protected abstract bool IsActive { get; }
        protected abstract GameObject AssignedTextObject { get; }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            TextObject = AssignedTextObject;
            if (TextObject == null)
            {
                return;
            }

            _active = IsActive;
            GetComponent<Renderer>().material.color = _active ? ActiveColor : InactiveColor;
            _buttonScale = transform.localScale;
            _textScale = TextObject.transform.localScale;
            _initialized = true;
        }

        public void Update()
        {
            EnsureInitialized();
            if (!_initialized)
            {
                return;
            }

            bool isActive = IsActive;

            if (_active != isActive)
            {
                _active = isActive;
                _flipping = true;
                _time = Mathf.PI * -0.5f;
            }

            if (_flipping)
            {
                _time += Time.deltaTime * 18f;
                _scale = Mathf.Abs(Mathf.Sin(_time));

                if (_time > 0f)
                {
                    GetComponent<Renderer>().material.color = isActive ? ActiveColor : InactiveColor;
                }

                if (_time >= Mathf.PI * 0.5f)
                {
                    _scale = 1f;
                    _flipping = false;
                }
            }

            transform.localScale = _buttonScale.WithY(_buttonScale.y * _scale);
            TextObject.transform.localScale = _textScale.WithY(_textScale.y * _scale);
        }
    }

    public class Button : AnimatedMenuButton
    {
        public SandboxMenu _list;
        public GameObject _text;
        public int _buttonIndex;

        protected override GameObject AssignedTextObject => _text;

        protected override bool IsActive =>
            (_list._currentPage == 0 && _list.objectButtons[_buttonIndex]) ||
            (_list._currentPage == 1 && _list.weaponButtons[_buttonIndex]) ||
            (_list._currentPage == 2 && _list.toolButtons[_buttonIndex]) ||
            (_list._currentPage == 3 && _list.utilButtons[_buttonIndex]) ||
            (_list._currentPage == 4 && _list.funButtons[_buttonIndex]);

        public void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out GorillaTriggerColliderHandIndicator component) &&
                !component.isLeftHand &&
                Time.time > LastTime + 0.25f)
            {
                LastTime = Time.time;
                GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                bool[] array = _list.GetArray();
                _list.Clear();
                _list.PlayAudio(true);

                if (array[_buttonIndex])
                {
                    return;
                }

                array = _list.GetArray();
                array[_buttonIndex] = true;
                _list.SetArray(array);
            }
        }
    }
}
