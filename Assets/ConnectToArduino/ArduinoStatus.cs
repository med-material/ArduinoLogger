using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ArduinoStatus : MonoBehaviour
{

    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text statusSubText;

    [SerializeField]
    private Button disconnectButton;

    [SerializeField]
    private GameObject finishedImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnArduinoStarted() {
        statusText.text = "Arduino is sending data..";
    }

    public void OnArduinoFinished() {
        statusText.text = "Arduino logging has finished.";
    }

    public void OnArduinoInterrupted() {
        statusText.text = "Arduino is ready to be logged.";
        finishedImage.SetActive(false);
    }
}
