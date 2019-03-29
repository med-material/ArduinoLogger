using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEditor;
public class LogToDisk : MonoBehaviour
{

    private string filepath = "";
	private string customFilepath = "";

    [SerializeField]
    private Text filepathText;

	[SerializeField]

	private StreamWriter writer;

    private string directory = "";

	[SerializeField]
	private bool shouldLog = true;

	[SerializeField]
	private Text filePathDescText;

	private string sep = ";";

    void Start()
    {
        SetFilePathFromArduino();
		Arduino.NewDataEvent += ContinuousLog;
		Arduino.NewHeaderEvent += LogHeader;
    }

	public void SetCustomFilePath(string path) {
		filepath = path;
		filePathDescText.text = "Filepath set by user.";
		filepathText.text = path;
	}
    public void SetFilePathFromArduino(string identifier = "logs") {
		if (string.IsNullOrEmpty(customFilepath)) {

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
			filepath = directory + identifier + "_output";
			if (sep == "\t") {
				filepath += ".tsv";
			} else {
				filepath += ".csv";
			}
			Debug.Log("Filepath: " + filepath);
			filepathText.text = filepath;
		}

		if(!Directory.Exists(directory)){
			Directory.CreateDirectory(directory);
		}    
    }
	public void LogHeader(List<string> headers) {
		if (!File.Exists(filepath)) {
			string headerline = "Email" + sep + string.Join(sep, headers.ToArray()).Replace("\n",string.Empty);
			using (StreamWriter writer = File.AppendText (filepath)) {
				writer.WriteLine (headerline + ";");
			}			
		}
	}

	public void StartLogging() {
		shouldLog = true;
	}

	public void StopLogging() {
		shouldLog = false;
	}
	public void ContinuousLog(Dictionary<string, List<string>> data) {
		if (shouldLog) {
			using (StreamWriter writer = File.AppendText (filepath)) {
				string line = "";
				foreach (var header in data.Keys) {
					line += data[header][data[header].Count-1] + sep;
				}
				line = line.Replace("\n",string.Empty);
				line = line.Substring(0,line.Length-1);
				line = line + ";";
				writer.WriteLine (line);
			}		
		}
	}

	public void Log(Dictionary<string, List<string>> logCollection) {

		if(!Directory.Exists(directory)){
			Directory.CreateDirectory(directory);
		}

        if (string.IsNullOrEmpty(filepath)) {
            Debug.LogError("Filepath was not set!");
        }

		// Overwriting The existing file is disabled for now.
		if (!File.Exists(filepath)) {
		//	Debug.LogWarning("Overwriting CSV file: " + filepath);
			//File.Delete (filepath);
			string[] keys = new string[logCollection.Keys.Count];
			logCollection.Keys.CopyTo(keys,0);
			string dbCols = string.Join(sep, keys).Replace("\n",string.Empty);

			using (StreamWriter writer = File.AppendText (filepath)) {
				writer.WriteLine (dbCols);
			}
		} 

		List<string> dataString = new List<string>();
		// Create a string with the data
		for(int i = 0; i < logCollection["Email"].Count; i++) {
			List<string> row = new List<string>();
			foreach(string key in logCollection.Keys) {
				row.Add(logCollection[key][i]);
			}
			dataString.Add(string.Join(sep,row.ToArray()) + ";");
		}
		
		foreach (var log in dataString) {
			using (StreamWriter writer = File.AppendText (filepath)) {
				writer.WriteLine (log.Replace("\n",string.Empty));
			}
		}

		Debug.Log("Data logged to: " + filepath);
	}

}
