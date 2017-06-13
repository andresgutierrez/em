using GB;
using UnityEngine;
using UnityEngine.UI;

public class SaveState : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Save);
    }

    private void Save()
    {
        Loader.instance.core.memory.SaveState();
    }
}
