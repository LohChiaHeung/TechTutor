using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResetPanelOverlay : MonoBehaviour
{
    public ManualFrameDrawer4Tap frameDrawer;  // drag your script here

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            frameDrawer.ResetPanel();
        });
    }

}
