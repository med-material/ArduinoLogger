using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class ConnectToMySQL : MonoBehaviour {
	public static string response = "";
	public static bool dataReceived = false;
	public static ConnectToMySQL instance = null;

	private static bool isConnected = false;
	private int retries = 0;
	private string secHash;
    private string dataHash;
	private string colsHash;

	private StreamWriter writer;
	private string directory;
	private string fileName;
	private List<Dictionary<string, List<string>>> logsToUpload;
	private string sep = ",";
	private List<string> dataDumps;
	private List<string> colDumps;
	private List<string> tableNameDumps;
	
	[SerializeField]
	private TextAsset builtInCredentials;
	private Dictionary<string, string> credentials;

	private float timeout = 5;
	private float lastUploadTime = -1f;
	private bool dumplock = false;

	[SerializeField]
	private Text statusMessage;

	[SerializeField]
	private Button SendingButton;
    
	[SerializeField]
	private Text SendingDoneTitleText;

	[SerializeField]
	private Text SendingDoneButtonText;
	
	[SerializeField]
	private Color errorColor;
	private Color defaultColor;

	private GameObject mysqlCred;
	private InputField emailInputField;
	private InputField dbSecKeyInputField;
	private InputField dbNameField;
	private InputField tableNameField;
	private InputField userNameField;
	private InputField passwordField;
	private InputField dbURLField;
	private GameObject connectButton;
	void Awake() {
		defaultColor = statusMessage.color;
		dataDumps = new List<string>();
		colDumps = new List<string>();
		tableNameDumps = new List<string>();
		credentials = new Dictionary<string, string>();
		logsToUpload = new List<Dictionary<string, List<string>>>();

		directory = Application.persistentDataPath + "/Data/";
		string authDirectory = Application.persistentDataPath + "/Auth/";

		if(!Directory.Exists(directory)) {
			Directory.CreateDirectory(directory);
		}

		if(!Directory.Exists(authDirectory)) {
			Directory.CreateDirectory(authDirectory);
		}

		if (builtInCredentials == null)  {
			DetectCredentialsOnDisk();
		} else {
			LoadBuiltInCredentials();
		}

		if (instance == null) {
			instance = this;
			if (!isConnected && credentials.Keys.Count > 0) {
				TestConnectionToServer();
			}
		}

		DetectDumpedLogs ();
	}

	public void TestConnectionToServer () {
				WWWForm testForm = new WWWForm ();
				secHash = Md5Sum (credentials["dbSecKey"]);
				testForm.AddField ("secHashPost", "connectiontest");
				StartCoroutine(ConnectToServer (testForm));
	}

	IEnumerator ConnectToServer(WWWForm form) {

		if (credentials["dbURL"] != "") {
			UnityWebRequest www = UnityWebRequest.Post(credentials["dbURL"], form);

			yield return www.SendWebRequest();

			if(www.isNetworkError || www.isHttpError) {
				Debug.LogError(www.error);
				Debug.LogError(www.downloadHandler.text);				
				yield return new WaitForSeconds(2.0f);
				retries++;
				if (retries < 3) {
					StartCoroutine (ConnectToServer (form));
					statusMessage.text = "Establishing Connection..".ToUpper();
					statusMessage.color = defaultColor;
				} else {
					statusMessage.text = "DB Error: Could not reach the database.".ToUpper();
					statusMessage.color = errorColor;
					if (connectButton != null) {
						connectButton.SetActive(true);
					}
				}
			} else {
				print ("Connected to Server");
				statusMessage.text = "Connected to Server".ToUpper();
				statusMessage.color = defaultColor;
				isConnected = true;
				SaveCredentialsToDisk();
				Destroy(mysqlCred);
			}
		} else {
			Debug.LogError("No URL was assigned");
			connectButton.SetActive(true);
		}
	}

	public void AddToUploadQueue(Dictionary<string, List<string>> logCollection) {
		
		if (logCollection.Count == 0) {
			Debug.LogError("logCollection was not initialized with any List<string> columns!");
			return;
		}

		if (!logCollection.ContainsKey("Email")) {
			Debug.LogError("Log Collection does not contain a column with AAU Email Addresses!");
			return;
		}

		logsToUpload.Add(new Dictionary<string, List<string>>(logCollection));

		Debug.Log ("Log Collection with " + logCollection.Count + " columns prepared for upload.");
	}

	public void UploadNow() {

		if (Time.time - lastUploadTime < timeout) {
			Debug.LogWarning("Don't make frequent calls to MySQL.UploadNow(), uploading is an async operation!");
		}

		lastUploadTime = Time.time;

		if (logsToUpload.Count == 0) {
			Debug.LogError("No logs in the upload queue. Use AddToUploadQueue() to add a logCollection to the queue before calling UploadNow().");
			return;
		}
		if (logsToUpload.Count > 0) {
			foreach (Dictionary<string, List<string>> logCollection in logsToUpload) {
				Debug.Log ("Attempting to upload logCollection with " + logCollection.Count + " rows.");
				string dbCols = string.Join(sep,logCollection.Keys);
				string dataString = ParseDataToString(logCollection);
				WWWForm form = PrepareForm(dbCols, dataString);

				// dump the data in case submission fails
				dataDumps.Add(dataString);
				colDumps.Add(dbCols);
				tableNameDumps.Add(credentials["tableName"]);

				StartCoroutine (SubmitLogs (form));
			}
			logsToUpload.Clear();
		}
		
	}
	private string ParseDataToString(Dictionary<string, List<string>> logCollection) {
		// Create a string with the data
		string dataString = "";
		for(int i = 0; i < logCollection["Email"].Count; i++) {
			List<string> row = new List<string>();
			foreach(string key in logCollection.Keys) {
				row.Add(logCollection[key][i]);
			}
			if(i != 0) {
				dataString += ";";
			}
			dataString += string.Join(sep,row);
		}
		return dataString;
	}

	private WWWForm PrepareForm(string dbCols, string dataString, string dumpedTableName = "") {
		WWWForm form = new WWWForm ();

		// Add credentials to form
		form.AddField ("dbnamePost", credentials["dbName"]);

		if (string.IsNullOrEmpty(dumpedTableName)) {
			form.AddField ("tablePost", credentials["tableName"]);
		} else {
			form.AddField ("tablePost", dumpedTableName);
		}
		
		form.AddField ("usernamePost", credentials["username"]);
		form.AddField ("passwordPost", credentials["password"]);
		form.AddField ("secHashPost",Md5Sum (credentials["dbSecKey"]));

		foreach(KeyValuePair<string,string> cred in credentials) {
		Debug.Log(cred.Key + " = " + cred.Value);
		}

        colsHash = Md5Sum(dbCols);
		form.AddField("db_hash", colsHash);
		Debug.Log ("columns to insert: " + dbCols);
		form.AddField ("dbCol", dbCols);

        dataHash = Md5Sum(dataString);
        form.AddField("dataHashPost", dataHash);
		Debug.Log ("data to submit: " + dataString);
		form.AddField ("dataPost", dataString);
		return form;
	}

	private IEnumerator SubmitLogs(WWWForm form) {
		
		Debug.Log ("Submitting logs..");
		statusMessage.text = "Submitting logs..".ToUpper();
		statusMessage.color = defaultColor;
		UnityWebRequest www = UnityWebRequest.Post(credentials["dbURL"], form);

		yield return www.SendWebRequest();

		if(www.isNetworkError || www.isHttpError) {
            Debug.LogError(("Unable to submit logs: " + www.error));
			SendingDoneTitleText.text = "Unable To Connect to the database :" + www.error + " data are not pushed to the database";
			statusMessage.text = (www.downloadHandler.text).ToUpper();
			statusMessage.color = errorColor;
            Debug.LogError(www.downloadHandler.text);
			while (dumplock) {
				yield return new WaitForSeconds(1f);
			}
			dumplock = true;
			DumpLogsToUpload();				
			dumplock = false;
        } else {
			Debug.Log ("Posted successfully");
			statusMessage.text = "Posted successfully".ToUpper();
			SendingDoneTitleText.text = "Data sended to the database!";
			SendingDoneButtonText.text = "LogPublished";
			SendingDoneButtonText.color = Color.grey;
			SendingButton.interactable=false;
			statusMessage.color = defaultColor;			
			// Clear datadump structures in case we are submitting dumped data
			dataDumps.Clear();
			colDumps.Clear();
			tableNameDumps.Clear();
		}

	}

public void resettextbutton()
{
	SendingDoneButtonText.text = "Publish Log";
	SendingDoneButtonText.color = Color.black;

}
	private void DumpLogsToUpload() {
		if (dataDumps.Count > 0 && colDumps.Count > 0 && tableNameDumps.Count > 0) {
			for (int i = 0; i < dataDumps.Count; i++) {
				var fileDumps = Directory.GetFiles(directory, "logdump*");
				using (StreamWriter writer = File.AppendText (directory + "logdump" + fileDumps.Length)) {
					writer.WriteLine (dataDumps[i]);
				}

				// If a coldump file already exists, we overwrite it.
				if (File.Exists (directory + "coldump" + fileDumps.Length)) {
					Debug.LogWarning("Overwriting coldump..");
					File.Delete (directory + "coldump" + fileDumps.Length);
				}

				using (StreamWriter writer = File.AppendText (directory + "coldump" + fileDumps.Length)) {
					writer.WriteLine (colDumps[i]);
				}

				if (File.Exists (directory + "tableName" + fileDumps.Length)) {
					Debug.LogWarning("Overwriting coldump..");
					File.Delete (directory + "coldump" + fileDumps.Length);
				}

				using (StreamWriter writer = File.AppendText (directory + "tableName" + fileDumps.Length)) {
					writer.WriteLine (tableNameDumps[i]);
				}

			}
			dataDumps.Clear();
			colDumps.Clear();
			tableNameDumps.Clear();
			Debug.Log ("Dumping logdump to disk at: " + directory);
		}	
	}

	private void DetectDumpedLogs() {

		var fileDumps = Directory.GetFiles(directory, "logdump*");

		if (fileDumps == null) {
			return;
		}

		for (int i = 0; i < fileDumps.Length; i++) {
			// Remove logdump if we don't have a corresponding coldump file.
			// TODO: Make a proper removal of files (tableName, logdump, coldump).
			if (!File.Exists (directory + "coldump" + i)) {
				Debug.LogWarning("logdump " + i + " was malformed, deleting logdump..");
				File.Delete (directory + "logdump" + i);
				File.Delete (directory + "tableName" + i);
				continue;
			}

			string line;

			string dataString = "";

			using (StreamReader reader = new StreamReader(directory + "logdump" + i)) {
				while((line = reader.ReadLine()) != null)  
				{  
					dataString += line;
				}
			}

			string dbCols = "";

			using (StreamReader reader = new StreamReader(directory + "coldump" + i)) {
				while((line = reader.ReadLine()) != null)  
				{  
					dbCols += line;
				}
			}

			string dumpedTableName = "";

			using (StreamReader reader = new StreamReader(directory + "tableName" + i)) {
				while((line = reader.ReadLine()) != null)  
				{  
					dumpedTableName += line;
				}
			}

			File.Delete (directory + "logdump" + i);
			File.Delete (directory + "tableName" + i);
			File.Delete (directory + "coldump" + i);

			Debug.Log ("Dumped Logs Detected.");
			
			// dump the data in case submission fails
			dataDumps.Add(dataString);
			colDumps.Add(dbCols);
			tableNameDumps.Add(dumpedTableName);

			WWWForm form = PrepareForm(dbCols, dataString, dumpedTableName);
			StartCoroutine (SubmitLogs (form));
		}
	}

	private static string Md5Sum(string strToEncrypt) {
		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);

		// encrypt bytes
		System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
		}

		return hashString.PadLeft(32, '0');
	}

	public void SetDatabaseTable(string tableName) {
		credentials["tableName"] = tableName;

	}


	public void Toggle_AuthConnect() {
		credentials["email"] = emailInputField.text;
		credentials["dbSecKey"] = dbSecKeyInputField.text;
		credentials["dbName"] = dbNameField.text;
		credentials["tableName"] = tableNameField.text;
		credentials["username"] = userNameField.text;
		credentials["password"] = passwordField.text;
		credentials["dbURL"] = dbURLField.text;

		TestConnectionToServer();
	}


	private void SaveCredentialsToDisk() {
		string authDirectory = Application.persistentDataPath + "/Auth/";



		if (File.Exists (authDirectory + "mysql_auth.txt")) {
			File.Delete (authDirectory + "mysql_auth.txt");
		}

		foreach(KeyValuePair<string,string> cred in credentials) {
			using (StreamWriter writer = File.AppendText (authDirectory + "mysql_auth.txt")) {
				writer.WriteLine (cred.Key + "=" + cred.Value);
			}
		}

		Debug.Log("Credentials saved to: " + authDirectory + "mysql_auth.txt");
	}

	private void LoadBuiltInCredentials() {
		string[] cred;
		string[] lines = builtInCredentials.text.Split('\n');
		foreach (var line in lines) {
			if (!string.IsNullOrEmpty(line)) {
				cred = line.Split('=');

				// Trim the loaded credentials for any non-ASCII characters.
				// Fixes an odd problem in Windows which produced an invalid Md5hash.
				string pattern = "[^ -~]+";
		    	Regex reg_exp = new Regex(pattern);
				credentials[reg_exp.Replace(cred[0], "")] = reg_exp.Replace(cred[1], "");;
			}
		}
	}

	private void DetectCredentialsOnDisk() {
		string authDirectory = Application.persistentDataPath + "/Auth/";

		if (File.Exists (authDirectory + "mysql_auth.txt")) {
			Debug.Log("Loading credentials from: " + authDirectory + "mysql_auth.txt");
			string line;
			string[] cred;
			using (StreamReader reader = new StreamReader(authDirectory + "mysql_auth.txt")) {
				while((line = reader.ReadLine()) != null)  
				{  
					if (!string.IsNullOrEmpty(line)) {
						cred = line.Split('=');
						credentials[cred[0]] = cred[1];
					}
				}
			}
		} else {
			mysqlCred = Instantiate(Resources.Load("MySQLAuthCanvas", typeof(GameObject))) as GameObject;
			mysqlCred.SetActive(true);

			var inputFields = mysqlCred.GetComponentsInChildren<InputField>();
			emailInputField = inputFields[0];
			dbSecKeyInputField = inputFields[1];
			dbNameField = inputFields[2];
			tableNameField = inputFields[3];
			userNameField = inputFields[4];
			passwordField = inputFields[5];
			dbURLField = inputFields[6];

			connectButton = GameObject.Find("MySQLConnectButton");
			Button button = connectButton.GetComponent<Button>();
			button.onClick.AddListener(() => Toggle_AuthConnect());
		}
	}
		
}
