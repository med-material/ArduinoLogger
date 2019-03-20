using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class LogToDisk : MonoBehaviour
{

    private string filepath = "";

    [SerializeField]
    private Text filepathText;

	private StreamWriter writer;

    private string directory = "";

    void Start()
    {
        SetFilePath();
    }

    public void SetFilePath(string identifier = "logs") {

		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
			directory = "C:\\rtii\\" + identifier + "\\";
			print ("Windows");
		}
		else if(Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor) {
			directory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/rtii/" + identifier + "/";
			print("Linux");
		} else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
			directory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/rtii/" + identifier + "/";
			print("Mac OSX");
		} else {
            directory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/rtii/" + identifier + "/";
            print("Unknown");
        }

		filepath = directory + identifier + "_output.csv";
        Debug.Log("Filepath: " + filepath);
        filepathText.text = filepath;
        
    }

	public void Log(Dictionary<string, List<string>> logCollection) {

		if(!Directory.Exists(directory)){
			Directory.CreateDirectory(directory);
		}

        if (string.IsNullOrEmpty(filepath)) {
            Debug.LogError("Filepath was not set!");
        }

		// Overwriting The existing file is disabled for now.
		//if (File.Exists(filepath)) {
		//	Debug.LogWarning("Overwriting CSV file: " + filepath);
		//	File.Delete (filepath);
		//}

		string dbCols = string.Join(",",logCollection.Keys);

		using (StreamWriter writer = File.AppendText (filepath)) {
			writer.WriteLine (dbCols);
		}

		List<string> dataString = new List<string>();
		// Create a string with the data
		for(int i = 0; i < logCollection["Email"].Count; i++) {
			List<string> row = new List<string>();
			foreach(string key in logCollection.Keys) {
				row.Add(logCollection[key][i]);
			}
			dataString.Add(string.Join(",",row) + ";");
		}
		
		foreach (var log in dataString) {
			using (StreamWriter writer = File.AppendText (filepath)) {
				writer.WriteLine (log);
			}
		}

		Debug.Log("Data logged to: " + filepath);
	}

}
