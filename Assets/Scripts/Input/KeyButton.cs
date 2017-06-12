using UnityEngine;
using UnityEngine.EventSystems;

namespace GB.Input
{
    public class KeyButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        [SerializeField]
        private KeyCode key;       		

		public void OnPointerUp(PointerEventData eventData)
		{
            Loader.instance.core.keyboard.JoyPadEvent(key, false);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
            Loader.instance.core.keyboard.JoyPadEvent(key, true);
		}       	
    }
}