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

    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void OnSaving()
    {
        statusText.text = "Saving Logs...";
    }

      public void OnSending()
    {
        statusText.text = "Sending Logs to the database...";
    }

    public void OnArduinoStarted() {
        statusText.text = "Arduino is sending data..";
    }

    public void OnArduinoFinished() {
        statusText.text = "Arduino logging has finished.";
    }

    public void OnArduinoInterrupted() {
        statusText.text = "Arduino is ready to be logged.";
    }
}
