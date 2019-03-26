using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SFB;

public class FileBrowser : MonoBehaviour
{

    public class FilePathReady : UnityEvent<string> { }
    public FilePathReady onFilePathReady;

    private string path;
    public string dialogTitle = "Choose CSV File Destination..";
    public string filename = "log";
    public string datatype = "csv";


    // Start is called before the first frame update
    void Start()
    {
        
    }

	public void ShowSaveDialog() {
		path = StandaloneFileBrowser.SaveFilePanel(dialogTitle, "", filename, datatype);
		onFilePathReady.Invoke(path);
    }
}
