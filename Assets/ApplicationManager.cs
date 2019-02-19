using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
public class ApplicationManager : MonoBehaviour
{

    [SerializeField]
    private LogToDisk logToDisk;

    [SerializeField]
    private Color connectedColor;

    [SerializeField]
    private Color errorColor;  

    [SerializeField]
    private Color inputfieldErrorColor;    

    [SerializeField]
    ArduinoDataReceiver arduinoDataReceiver;

    [SerializeField]
    private InputField emailInputField;

    [SerializeField]
    private InputField serialPortInputField;

    [SerializeField]
    private Text connectStatus;

    [SerializeField]
    private Text dbConnectionStatus;

    [SerializeField]
    private GameObject csvStatusFileHolder;
    private string sanitizedSerialPort;
    private float connectTimer = 0f;
    private float connectTimeout = 3f;
    private bool connectingToArduino = false;

    private EventSystem eventSystem;

    void Start()
    {
        eventSystem = EventSystem.current;
    }

    
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Selectable next = eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
        
            if (next!= null) {
                            
                InputField inputfield = next.GetComponent<InputField>();
                if (inputfield !=null) inputfield.OnPointerClick(new PointerEventData(eventSystem));
                            
                eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(eventSystem));
            }        
        }

        // Catching exceptions from Serialport.open() freezes Unity,
        // so our error-handling work-around is a timer.
        if (connectingToArduino) {
            connectTimer += Time.deltaTime;
            if (connectTimer > connectTimeout) {
                displayArduinoError();
            }
        }
    }

    private void displayArduinoError() {
        connectStatus.text = "Could not connect to Arduino on port: " + sanitizedSerialPort.ToString();
        connectStatus.color = errorColor;
        serialPortInputField.image.color = inputfieldErrorColor;
        connectingToArduino = false;
        connectTimer = 0f;
    }

    public void ConnectPressed() {
        connectStatus.text = "Connecting...";
        string regex = @"(^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+.[a-zA-Z0-9-.]+$)";
        var match = Regex.Match(emailInputField.text, regex, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            connectStatus.text = "Please Enter a valid E-mail address.";
            connectStatus.color = errorColor;
            emailInputField.image.color = inputfieldErrorColor;
            return;
        }

        sanitizedSerialPort = new string((from c in serialPortInputField.text where char.IsLetterOrDigit(c) || char.IsPunctuation(c) select c).ToArray());
        UnityEngine.Debug.Log(sanitizedSerialPort);
        arduinoDataReceiver.SetSerialPort(sanitizedSerialPort);
        connectingToArduino = true;
        bool connected = arduinoDataReceiver.OpenConnection();
        if (connected) {
            connectingToArduino = false;
            connectTimer = 0f;
            connectStatus.text = "Connected to Arduino on port: " + sanitizedSerialPort;
            connectStatus.color = connectedColor;
            logToDisk.SetFilePath();
            dbConnectionStatus.gameObject.SetActive(true);
            csvStatusFileHolder.SetActive(true);
            StartCoroutine (arduinoDataReceiver.ReceiveDataRoutine ());
        } else {
            displayArduinoError();
        }
    }

    public void serialPortInputFieldChange() {
        serialPortInputField.image.color = Color.white;
    }

    public void emailInputFieldChange() {
        emailInputField.image.color = Color.white;
    }
}
