using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Timers;

public class ConnectToArduino : MonoBehaviour
{
    [SerializeField]
    private Color connectedColor;

    [SerializeField]
    private Color errorColor;

    [SerializeField]
    private Color inputfieldErrorColor;

    [SerializeField]
    private InputField emailInputField;

    [SerializeField]
    private InputField CommentInputField;

    [SerializeField]
    private InputField serialPortInputField;

    [SerializeField]
    private InputField baudRateInputField;

    [SerializeField]
    private InputField PIDInputField;

    [SerializeField]
    private GameObject ConnectionPanel;


    [SerializeField]
    private UnityEngine.UI.Dropdown arduinoDropdown;

    [SerializeField]
    private Text connectStatus;

    [SerializeField]
    private string redirectScene;

    [SerializeField]
    private Button refreshButton;

    public string sanitizedSerialPort = "";

    public int sanitizedBaudRate = -1;
    private float connectTimer = 0f;
    private float connectTimeout = 3f;
    private bool connectingToArduino = false;
    private bool shouldRefresh = true;

    public string email;
    public string pid;

    public string comment;
    private EventSystem eventSystem;
    public SerialPort serialport;
    private IEnumerator refreshTimer;

    private readonly char[] charsToRemoveFromComment = new char[] { ';', ',' };
    public static ConnectToArduino Instance = null;

    void Awake()
    {
        eventSystem = EventSystem.current;
        string[] ports = SerialPort.GetPortNames();
        DisplayAvailablePorts();

        // If there isn't already an instance of ConnectToArduino, set it to this. 
        if (Instance == null)
        {
            Instance = this;
        } 
        // If there is an existing instance, destroy it. 
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        //Setting dontdestroyonload to our soundmanager so it will keep being there when reloading the scene.
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += this.OnLoadCallback;
    }

    void OnLoadCallback(Scene scene, LoadSceneMode sceneMode) {
        // only find connect button when we are in connecting scene
        if (scene.name == "ConnectToArduino") {
            GameObject.Find("ConnectButton").GetComponent<Button>().onClick.AddListener(ConnectPressed);

            emailInputField = GameObject.Find("emailInputField").GetComponent<InputField>();
            CommentInputField = GameObject.Find("CommentInputField").GetComponent<InputField>();
            // input field is inactive so have to find it through parent.
            Transform[] trs= GameObject.Find("ArduinoInputHolder").GetComponentsInChildren<Transform>(true);
            foreach(Transform t in trs){
                if(t.name == "arduinoInputField"){
                    serialPortInputField = t.gameObject.GetComponent<InputField>();
                }
                if(t.name == "Dropdown"){
                    arduinoDropdown = t.gameObject.GetComponent<Dropdown>();
                }
            }
            baudRateInputField = GameObject.Find("baudInputField").GetComponent<InputField>();
            PIDInputField = GameObject.Find("PIDInputField").GetComponent<InputField>();
            Transform[] ctrs= GameObject.Find("ConnectButtonHolder").GetComponentsInChildren<Transform>(true);
            foreach(Transform t in ctrs){
                if(t.name == "ConnectingText"){
                    connectStatus = t.gameObject.GetComponent<Text>();
                    break;
                }
            }
            refreshButton = GameObject.Find("RefreshButton").GetComponent<Button>();
        }
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


    private void DisplayAvailablePorts()
    {
        arduinoDropdown.ClearOptions();
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length > 0)
        {
            // OSX reports ports with "tty.*" extension but we need to use "cu.*"  to access it.
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
                foreach (string port in ports) {
                    string p = port.Replace("/dev/tty","/dev/cu");
                    arduinoDropdown.AddOptions(new List<string> { p});
                }
            } else {
                arduinoDropdown.AddOptions(ports.ToList());
            }
            arduinoDropdown.AddOptions(new List<string> { "Custom.." });
            serialPortInputField.text = arduinoDropdown.options[arduinoDropdown.value].text;
            arduinoDropdown.gameObject.SetActive(true);
            serialPortInputField.gameObject.SetActive(false);
        }
        else
        {
            arduinoDropdown.gameObject.SetActive(false);
            serialPortInputField.gameObject.SetActive(true);
            shouldRefresh = false;
        }
    }



    public void OnRefreshClick()
    {
        DisplayAvailablePorts();
    }


    public void RedirectToScene() {
        SceneManager.LoadSceneAsync(redirectScene);

    }

    private void displayArduinoError() {
        connectStatus.text = "Could not connect to Arduino on port: " + sanitizedSerialPort;
        connectStatus.text += '\n' + "(Is the Arduino Monitor open?)";
        connectStatus.color = errorColor;
        serialPortInputField.image.color = inputfieldErrorColor;
        connectingToArduino = false;
        connectTimer = 0f;
    }

    private void displayBaudRateError() {
        connectStatus.text = "Invalid Baud Rate (" + sanitizedBaudRate + "). (Use fx. 115200)";
        connectStatus.color = errorColor;
        baudRateInputField.image.color = inputfieldErrorColor;
        connectingToArduino = false;
        connectTimer = 0f;
    }

    public void dropdown_Changed() {
        if (arduinoDropdown.value == arduinoDropdown.options.Count()-1) {
            // Custom Option
            shouldRefresh = false;
            arduinoDropdown.gameObject.SetActive(false);
            serialPortInputField.gameObject.SetActive(true);
            serialPortInputField.text = "";
        } else {
            serialPortInputField.text = arduinoDropdown.options[arduinoDropdown.value].text;
        }
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
        string baudrateString = new string((from c in baudRateInputField.text where char.IsDigit(c) select c).ToArray());
        if (string.IsNullOrEmpty(baudrateString)) {
            displayBaudRateError();
            return;
        }
        try {
            sanitizedBaudRate = int.Parse(baudrateString);
        } catch (System.FormatException e) {
            displayBaudRateError();
        }

        UnityEngine.Debug.Log(sanitizedSerialPort);
        UnityEngine.Debug.Log(sanitizedBaudRate);
        serialport = new SerialPort (sanitizedSerialPort, sanitizedBaudRate);
        email = GameObject.Find("emailInputField").GetComponent<InputField>().text;
        comment = GameObject.Find("CommentInputField").GetComponent<InputField>().text;
        pid = GameObject.Find("PIDInputField").GetComponent<InputField>().text;
        Debug.Log(email);
        if( string.IsNullOrEmpty( comment ))
        {
            comment = "NULL";
            Debug.Log("Comment is empty");
        }
        else
        {
            foreach (char c in charsToRemoveFromComment)
            {
                if (comment.Contains(c))
                {
                    comment = comment.Replace(c, ' ');
                }
            }
            Debug.Log("Commentary: " + comment);
        }
        if( string.IsNullOrEmpty( PIDInputField.text ))
        {
            pid = "NULL";
        }
        connectingToArduino = true;
        bool connected = OpenConnection();
        if (connected) {
            connectingToArduino = false;
            connectTimer = 0f;
            connectStatus.text = "Connected to Arduino on port: " + sanitizedSerialPort;
            connectStatus.color = connectedColor;
            CloseConnection();
            RedirectToScene();
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

    public bool OpenConnection()
    {
        if (serialport != null)
        {
            if (serialport.IsOpen)
            {
                //Serial port is already open. We ignore it for now.
            }
            else
            {
                serialport.ReadTimeout = 100;
                try
                {
                    serialport.Open();
                }
                catch (IOException)
                {
                    return false;
                }
                return true;
            }
        }
        return false;
    }


    // Closes the connection to the Arduino
    // Should be run before closing the Unity program
    public void CloseConnection()
    {
        if (serialport != null && serialport.IsOpen)
        {
            //If the connection is open, we close it before ending the program
            serialport.Close();
        }
    }

}
