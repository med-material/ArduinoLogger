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

    private List<string> arduinoSerial;

    private int outputFieldLimit = 70;
    string newOutputContent = "";
    // Start is called before the first frame update
    void Start()
    {
        arduinoSerial = new List<string>();
        Arduino.NewRawSerialEvent += NewData;
    }

    void NewData(Arduino arduino) {
        arduinoSerial.Add(arduino.inputBuffer);
        newOutputContent = "";
        if (arduinoSerial.Count <= outputFieldLimit) {
            newOutputContent = string.Join("\n", arduinoSerial);
        } else {
            newOutputContent = string.Join("\n", arduinoSerial.GetRange(arduinoSerial.Count-outputFieldLimit, outputFieldLimit));
        }
        Debug.Log("serialamount: " + arduinoSerial.Count + "fieldlimit: " + outputFieldLimit);
        arduinoOutputField.text = newOutputContent;
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
