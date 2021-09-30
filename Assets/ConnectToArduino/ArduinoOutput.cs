using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ArduinoOutput : MonoBehaviour
{
    private bool enableoutput = false;

    [SerializeField]
    private Text arduinoOutputField;

    [SerializeField]
    private ScrollRect scrollRect;

    private List<string> arduinoSerial;
    private int outputFieldLimit = 0;
    string newOutputContent = "";
    // Start is called before the first frame update
    
    void Start()
    {
        arduinoSerial = new List<string>();
        Arduino.NewRawSerialEvent += NewData;
    }

    void NewData(Arduino arduino)
    {
        try
        {
            arduinoOutputField.text = (arduinoOutputField.text + "\n" + arduino.rawSerialEvent).Substring(Math.Max((arduinoOutputField.text + "\n" + arduino.rawSerialEvent).Length - 1000, 0), Math.Min((arduinoOutputField.text + "\n" + arduino.rawSerialEvent).Length, 1000));
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }

    }

    public void ClearOutput()
    {
        arduinoOutputField.text = "";
    }
    

    // Update is called once per frame
    void Update()
    {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0;
    }
}
