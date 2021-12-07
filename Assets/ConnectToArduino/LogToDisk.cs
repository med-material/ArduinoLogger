using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEditor;
using SFB;

public class LogToDisk : MonoBehaviour
{
    [SerializeField]
    private Text SendingDoneTitleText;

    [SerializeField]
    private Text SendingDoneButtonText;

    [SerializeField]
    private Button SaveButton;

    private string filepath = "";

    [SerializeField]

    private StreamWriter writer;

    private string directory = "";

    [SerializeField]
    private bool shouldLog = true;

    [SerializeField]
    private Arduino arduinoObject;

    private List<string> headers;
    private string sep = ";";
    private string path;
    public string dialogTitle = "Choose CSV File Destination..";
    public string filename = "testlog";
    public string datatype = "csv";

    private LoggingManager loggingManager;

    void Start()
    {
        arduinoObject = GameObject.Find("Arduino").GetComponent<Arduino>();
        loggingManager = arduinoObject.LoggingManager;
    }

    public void ShowSaveDialog()
    {
        path = StandaloneFileBrowser.OpenFolderPanel(dialogTitle, "", false).First();
        Debug.Log("save dialog finished");
        filepath = path;
        arduinoObject.LoggingManager.SetSavePath(path);
    }


    public void Log(Dictionary<string, List<string>> logCollection)
    {
        arduinoObject.LoggingManager.SaveLog(arduinoObject.GetOutputLabel(), true, TargetType.CSV);
        Debug.Log("Data logged to: " + filepath);
        SendingDoneTitleText.text = "Data saved in " + filepath;
        SendingDoneButtonText.text = "CSV File Saved";
        SendingDoneButtonText.color = Color.grey;
        SaveButton.interactable = false;
    }

    public void resettextbutton()
    {
        SendingDoneButtonText.text = "Save Logs to CSV";
        SendingDoneButtonText.color = Color.black;
    }
}