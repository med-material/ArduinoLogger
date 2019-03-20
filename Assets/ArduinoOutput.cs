using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArduinoOutput : MonoBehaviour
{

    [SerializeField]
    private Text arduinoOutputField;

    [SerializeField]
    private ScrollRect scrollRect;    

    // Start is called before the first frame update
    void Start()
    {
        Arduino.NewDataEvent += NewData;
    }

    void NewData(Arduino arduino) {
        arduinoOutputField.text += arduino.NewestIncomingData + '\n';
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }

    public void ClearOutput() {
        arduinoOutputField.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
