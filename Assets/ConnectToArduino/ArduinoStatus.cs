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
        statusText.text = "Sending Logs To The Database...";
    }

    public void OnArduinoStarted() {
        statusText.text = "Arduino Is Sending Data..";
    }

    public void OnArduinoFinished() {
        statusText.text = "Arduino Logging Has Finished.";
    }

    public void OnArduinoInterrupted() {
        statusText.text = "Arduino Is Ready To Be Logged.";
    }
}
