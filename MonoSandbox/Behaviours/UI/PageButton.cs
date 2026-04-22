using GorillaExtensions;
using UnityEngine;

namespace MonoSandbox.Behaviours.UI
{
    public class PageButton : AnimatedMenuButton
    {
        public SandboxMenu _list;
        public GameObject _text;
        public int _pageIndex;

        protected override GameObject AssignedTextObject => _text;
        protected override bool IsActive => _list._currentPage == _pageIndex;

        public void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out GorillaTriggerColliderHandIndicator component) &&
                !component.isLeftHand &&
                Time.time > LastTime + 0.4f)
            {
                LastTime = Time.time;
                GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                if (_list._currentPage != _pageIndex)
                {
                    _list._currentPage = _pageIndex;
                    _list.Clear();
                    _list.PlayAudio(false);
                }
            }
        }
    }
}
