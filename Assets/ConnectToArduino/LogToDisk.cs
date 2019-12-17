using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
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

    void Start()
    {
        Arduino.NewHeaderEvent += saveheader;

    }

    public void ShowSaveDialog()
    {
        path = StandaloneFileBrowser.SaveFilePanel(dialogTitle, "", filename, datatype);
        Debug.Log("save dialog finished");
        filepath = path;
        LogHeader(headers);
    }

    public void LogHeader(List<string> headers)
    {
    
        if (!File.Exists(filepath))
        {
            string headerline = "Email" + sep + string.Join(sep, headers.ToArray()).Replace("\n", string.Empty);
            using (StreamWriter writer = File.AppendText(filepath))
            {
                writer.WriteLine(headerline + ";");
            }
        }
    }

    public void saveheader(List<string> listheaders)
    {
        headers = listheaders;
    }

    public void Log(Dictionary<string, List<string>> logCollection)
    {

        if (string.IsNullOrEmpty(filepath))
        {
            Debug.LogError("Filepath was not set!");
        }

            if (!File.Exists(filepath))
            {
                //Debug.LogWarning("Overwriting CSV file: " + filepath);
                //File.Delete(filepath);
                string[] keys = new string[logCollection.Keys.Count];
                logCollection.Keys.CopyTo(keys, 0);
                string dbCols = string.Join(sep, keys).Replace("\n", string.Empty);

                using (StreamWriter writer = File.AppendText(filepath))
                {
                    writer.WriteLine(dbCols);
                }
            }
        

        List<string> dataString = new List<string>();
        // Create a string with the data
        for (int i = 0; i < logCollection["Email"].Count; i++)
        {
            List<string> row = new List<string>();
            foreach (string key in logCollection.Keys)
            {
                row.Add(logCollection[key][i]);
            }
            dataString.Add(string.Join(sep, row.ToArray()) + sep);
        }

        foreach (var log in dataString)
        {
            using (StreamWriter writer = File.AppendText(filepath))
            {
                writer.WriteLine(log.Replace("\n", string.Empty));
            }
        }

        Debug.Log("Data logged to: " + filepath);
        SendingDoneTitleText.text = "Data saved in " + filepath;
        SendingDoneButtonText.text = "CSV File Saved";
        SendingDoneButtonText.color = Color.grey;
        SaveButton.interactable=false;
    }

    public void resettextbutton()
    {
		SendingDoneButtonText.text = "Save Logs to CSV";
        SendingDoneButtonText.color = Color.black;
        
    }
}

