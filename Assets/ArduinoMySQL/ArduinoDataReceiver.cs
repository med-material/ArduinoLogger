/**
 * ArduinoScript.cs
 * - Receive data from the Arduino serial port into Unity3D
 * - Change COM port and baut rate in ArduinoReceiver.cs
 * Version 1.0
 * Author: Jacob B. Madsen
 *         jbm@create.aau.dk
 **/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using UnityEngine.UI;


public class ArduinoDataReceiver : MonoBehaviour
{
	#region Variables

	[SerializeField]
	private ConnectToMySQL mySQL;

	private static SerialPort serialport;
	// According to : http://forum.arduino.cc/index.php?topic=56728.0;wap2

	[Header("-- Settings --")]
	public int baudRate = 9600;
	public string portName;
	public string aauEmail;

	bool isData = false;
	private int columnLength = 4;
	#endregion
	
	#region Default Unity functions
    // Default Unity Start Function

	string mode = "reactionTime";

	[SerializeField]
	private Text arduinoOutputField;

	[SerializeField]
	private Text arduinoOutputType;

	[SerializeField]
    private Color errorColor;  

	[SerializeField]
	private LogToDisk logToDisk;

	[SerializeField]
	private Text arduinoLoggingStatus;

	[SerializeField]
	private Text arduinoLoggingSubtext;

	private Color defaultColor;

	private bool isLogging = false;

	private Dictionary<string, List<string>> logCollection;
    private void Start()
    {
		defaultColor = arduinoOutputType.color;
    }

	// Default Unity Function, Called when exiting or stopping the application
    private void OnApplicationQuit()
    {
        CloseConnection();
    }
	#endregion

	public void SetSerialPort(string newSerialPort) {
		portName = newSerialPort;
		serialport = new SerialPort (portName, baudRate);
	}
	
	#region Receive and parse data functions
    // Coroutine handling the incomming data
    public IEnumerator ReceiveDataRoutine()
    {
		string[] splitStr = null;

		yield return new WaitForSeconds (2f); // Delay for two seconds before test

        while (serialport.IsOpen)
        {
            // Yield untill end of frame, to allow Unity to continue as normal
            yield return null;

			// Reset splitstr in order to ensure no data left
			splitStr = null;
			
			try
			{
				// Read line from seial in
				string s = serialport.ReadLine();
				if (isLogging) {
					arduinoLoggingStatus.text = "Arduino is sending data..";
					arduinoLoggingSubtext.text = "";
				}
				arduinoOutputField.text += (s + '\n');
				int numLines = arduinoOutputField.text.Split('\n').Length;
				if (numLines > 33) {
				int index = arduinoOutputField.text.IndexOf(System.Environment.NewLine);
 				arduinoOutputField.text = arduinoOutputField.text.Substring(index + System.Environment.NewLine.Length);
				}
				Debug.Log(s);
				// split based on tabs
            	splitStr = s.Split('\t');
				logToDisk.ResetLoggedState();
			}
			catch(System.Exception) { 
			}

			if (splitStr != null) {
				isData = false;
				isData = char.IsDigit(splitStr[0],0); // skips lines whose first column is not a number.
			}

			if (splitStr != null && splitStr [0].Contains ("LOG BEGIN")) {
				isLogging = true;
				if(splitStr[0].Contains("SYNC TEST")) {
					arduinoOutputType.text = "SYNC TEST";
					arduinoOutputType.color = defaultColor;
					mode = "SyncTest";
					columnLength = 5;
					logToDisk.SetFilePath("rtii_synctest_output.csv", "sync_test");

					// Initialize the log dictionary
					logCollection = new Dictionary<string, List<string>>();

					// Add the database columns
					logCollection.Add("Email", new List<string>());
					logCollection.Add("TrialNo", new List<string>());
					logCollection.Add("Modal", new List<string>());
					logCollection.Add("Intens", new List<string>());
					logCollection.Add("ReactionTime", new List<string>());
					logCollection.Add("DateAdded", new List<string>());
					logCollection.Add("MusicalAbility", new List<string>());

				} else if (splitStr[0].Contains("REACTION TIME")) {
					arduinoOutputType.text = "REACTION TIME";
					arduinoOutputType.color = defaultColor;
					columnLength = 5;
					mode = "ReactionTime";
					logToDisk.SetFilePath("rtii_reactiontimetest_output.csv", "reaction_time");

					// Initialize the log dictionary
					logCollection = new Dictionary<string, List<string>>();

					// Add the database columns
					logCollection.Add("Email", new List<string>());
					logCollection.Add("TrialNo", new List<string>());
					logCollection.Add("Modal", new List<string>());
					logCollection.Add("Intens", new List<string>());
					logCollection.Add("ReactionTime", new List<string>());
					logCollection.Add("DateAdded", new List<string>());
				} else {
					arduinoOutputType.text = "UNKNOWN OUTPUT";
					arduinoOutputType.color = errorColor;
				}
			}

			if (splitStr != null && splitStr.Length == columnLength && isData) // Ignore if not 4, as that means corrupt data
			{
				
				// Add the arduino data to our logEntries dictionary.
				logCollection["Email"].Add(aauEmail);
				logCollection["TrialNo"].Add(splitStr[0]);
				logCollection["Modal"].Add(splitStr[1]);
				logCollection["Intens"].Add(splitStr[2]);
				string reactionTime = new string((from c in splitStr[3] where char.IsNumber(c) || c == '-' select c).ToArray());
				logCollection["ReactionTime"].Add(reactionTime);
				logCollection["DateAdded"].Add(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				if (mode == "SyncTest") {
					string musicalAbility = new string((from c in splitStr[4] where char.IsLetterOrDigit(c) select c).ToArray());
					logCollection["MusicalAbility"].Add(musicalAbility);
				}

			}		

			// Upload the data recieved after test is done
			if (splitStr != null && splitStr [0].Contains ("LOG END")) {

				if(splitStr[0].Contains("REACTION TIME")) {
					mySQL.SetDatabaseTable("reaction_time");
					mySQL.AddToUploadQueue(logCollection);
					mySQL.UploadNow();
					Debug.Log ("RT test has ended");
				}

				if(splitStr[0].Contains("SYNC TEST")) {
					mySQL.SetDatabaseTable("synch");
					mySQL.AddToUploadQueue(logCollection);
					mySQL.UploadNow();
					Debug.Log ("Sync test has ended");
				}

				logToDisk.Log(logCollection);
				mode = "";
				arduinoLoggingStatus.text = "Ready to log data from Arduino.";
				arduinoLoggingSubtext.text = "When you start the Arduino exercise, a CSV file is logged to the folder below.";
				isLogging = false;
			}

		}
    }


	#endregion
	
	#region Open and close connections
    // Opens the connection to the Arduino
    // Is run from the Unity Start function

    public bool OpenConnection()
    {
		//try
		if (serialport != null)
		{
			if (serialport.IsOpen)
			{
				//Serial port is already open. We ignore it for now, or we can close it.
				//serialport.Close();
			}
			else
			{
				//Open the connection to read data
				serialport.Open();
				//Set time-out value before reporting error
				serialport.ReadTimeout = 100;
				Debug.Log("Connected to Arduino, on port: " + serialport.PortName);
				arduinoLoggingStatus.text = "Arduino is ready to be logged.";
				arduinoLoggingSubtext.text = "When you start the Arduino exercise, a CSV file is logged to the folder below.";
				return true;
			}
		}
		return false;
				/*
	            if (serialport.IsOpen)
	            {
	                //Port is already open
	                Debug.LogWarning("The port is already open. Port: " + serialport.PortName);
	            }
	            else
	            {
	                //The port does not appear to exist
	                Debug.LogWarning("The port does not appear to exist. Port: " + serialport.PortName);
	            } */
		//catch (System.Exception e)
		//{
		//	Debug.LogWarning("The system could not recognize the PORT NAME for the Arduino connection. Cannot open connection. ERROR code: " + e.ToString());
		//}
    }

    // Closes the connection to the Arduino
    // Should be run before closing the Unity program
    public void CloseConnection()
    {
        if (serialport.IsOpen)
        {
            Debug.Log("Closing connection to Arduino, on port: " + serialport.PortName);
            //If the connection is open, we close it before ending the program
            serialport.Close();
        }
    }
	#endregion

}

