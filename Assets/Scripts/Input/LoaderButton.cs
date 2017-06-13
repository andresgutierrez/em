using UnityEngine;
using UnityEngine.EventSystems;

namespace GB.Input
{
    public class LoaderButton : MonoBehaviour, IPointerUpHandler
    {
        [SerializeField]
        private string name;

        public void OnPointerUp(PointerEventData eventData)
        {
            Loader.instance.Load(name);
        }
    }
}