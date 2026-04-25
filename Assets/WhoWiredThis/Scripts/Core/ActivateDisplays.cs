using UnityEngine;

public class ActivateDisplays : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Display.displays.Length: " + Display.displays.Length);
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            Debug.Log("Display " + i + " activated");
        }
    }
}
