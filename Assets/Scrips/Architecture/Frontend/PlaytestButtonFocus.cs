using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(Outline))]
    public sealed class PlaytestButtonFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private void OnEnable() => GetComponent<Outline>().enabled = false;
        public void OnSelect(BaseEventData data) => GetComponent<Outline>().enabled = true;
        public void OnDeselect(BaseEventData data) => GetComponent<Outline>().enabled = false;
    }
}
