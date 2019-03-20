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
        statusSubText.gameObject.SetActive(false);
        finishedImage.SetActive(false);
    }

    public void OnArduinoFinished() {
        statusText.text = "Arduino logging has finished.";
        statusSubText.gameObject.SetActive(true);
        statusSubText.text = "A CSV file has been written to the folder below.";
        finishedImage.SetActive(true);
    }

    public void OnArduinoInterrupted() {
        statusSubText.text = "When you start the Arduino exercise, a CSV file is logged to the folder below.";
        statusText.text = "Arduino is ready to be logged.";
        finishedImage.SetActive(false);
    }
}
