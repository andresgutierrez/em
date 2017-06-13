
using GB.Graphics;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace GB
{
    public class Loader : MonoBehaviour
    {
        public static Loader instance;

        [SerializeField]
        public Drawer drawer;

        public Core core;

        private KeyCode[] keys = {
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.X, KeyCode.Z, KeyCode.Return, KeyCode.RightShift
        };

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }

        private void Start()
        {
            Load("ST");
        }

        public void Load(string path)
        {
            if (core != null)
                core.initialized = false;
            core = new Core(Resources.Load(path) as TextAsset, drawer);
            core.Start();
        }

        private void Update()
        {
            if (core.initialized)
            {
                core.Run();

#if UNITY_EDITOR
                for (int i = 0; i < keys.Length; i++)
                {
                    if (UnityInput.GetKeyDown(keys[i]))
                        core.keyboard.JoyPadEvent(keys[i], true);

                    if (UnityInput.GetKeyUp(keys[i]))
                        core.keyboard.JoyPadEvent(keys[i], false);
                }
#endif
            }
        }
    }
}