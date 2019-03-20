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

    public enum ReceiverState {
        Standby,
		ReadingHeader,
        ReadingData,
		LoggingFinished
    }

    public enum TestType {
        SyncTest,
        ReactionTimeTest,
		Unknown
    }

	private ReceiverState receiverState;
	private TestType testType;

	[SerializeField]
	private ConnectToMySQL mySQL;

	private static SerialPort serialport;
	// According to : http://forum.arduino.cc/index.php?topic=56728.0;wap2

	[Header("-- Settings --")]
	public int baudRate = 9600;
	public string portName;
	public string aauEmail;

	bool isData = false;
	private int columnLength = 1;
	private string separator = "\t";
	private string label;
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
	private List<string> headers;
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

	public void SetEmail(string newEmail) {
		aauEmail = newEmail;
	}
	
	#region Receive and parse data functions

	private void ParseDataArguments(string s) {
		var start = s.IndexOf("(");
		var end = s.LastIndexOf(")");
		string[] dataArgs = s.Substring(start, end - start).Split(',');

		foreach (var arg in dataArgs) {
			string[] valPair = arg.Split('=');

			var param = new string((from c in valPair[0] where char.IsLetterOrDigit(c) || char.IsPunctuation(c) select c).ToArray());
			var val = new string((from c in valPair[1] where char.IsLetterOrDigit(c) || char.IsPunctuation(c) select c).ToArray());

			if (param == "sep") {
					if (val == "tab") {
						separator = "\t";
					} else if (val == "comma") {
						separator = ",";
					} else {
						Debug.LogError("Could not parse separator argument: " + val + " - use LOG BEGIN (sep=comma) or (sep=tab).");
					}
			} else if (param == "col") {
					var result = int.TryParse(val, out columnLength);
					if (!result) {
						Debug.LogError("Could not parse column length argument: " + val + " - use fx LOG BEGIN (col=5).");
					}
			} else if (param == "label") {
					label = val;
			}
			} else {
				Debug.LogWarning("Arduino reported an unknown parameter: " + param);
			}
		}
	}

	private void DetermineTestType(string s) {
		if (s.Contains("SYNC TEST")) {
			testType = TestType.SyncTest;

			logToDisk.SetFilePath("rtii_synctest_output.csv", "sync_test");
			mySQL.SetDatabaseTable("synch");
			// Initialize the log dictionary
			logCollection = new Dictionary<string, List<string>>();

		} else if (s.Contains("REACTION TIME")) {
			testType = TestType.ReactionTimeTest;
			logToDisk.SetFilePath("rtii_reactiontimetest_output.csv", "reaction_time");
			mySQL.SetDatabaseTable("reaction_time");
			
		} else {
			Debug.LogError("Could not recognize Test Type!");
			testType = TestType.Unknown;
		}
	}

    // Coroutine handling the incomming data	
    public IEnumerator ReceiveDataRoutine()
    {
		//string[] splitStr = null;

		yield return new WaitForSeconds (2f); // Delay for two seconds before test

        while (serialport.IsOpen)
        {
			string s = String.Empty;
			try {
				// Read line from Arduino
				s = serialport.ReadLine();
				Debug.Log(s);
			} catch(System.Exception) { 
				// Arduino Disconnected? TODO.
				// Or just no lines to read atm.
			}

			// If our string is empty, continue the while loop.
			if (String.IsNullOrEmpty(s)) {
				continue;
			}

			// Send line to ArduinoOutput (if output exists)
			// TODO: Put ArduinoOutputField in a scroll rect.
			if (arduinoOutputField) {
				arduinoOutputField.text += (s + '\n');
			}

			// Read what is our current state
			if (receiverState == ReceiverState.Standby) {
				// Check for "BEGIN" string.
				if (s.Contains ("LOG BEGIN")) {
					// Parse Reported Column and Separator
					ParseDataArguments(s);

					// Determine Test Type
					DetermineTestType(s);

					// Initialize the log dictionary
					logCollection = new Dictionary<string, List<string>>();

					receiverState = ReceiverState.ReadingHeader;					
				}
			} else if (receiverState == ReceiverState.ReadingHeader) {
				// Parse header
				headers = new List<string>();				
				headers = s.Split('\t').ToList();

				// Check that header contains the expected number of columns. 
				if (headers.Count == columnLength) {
					foreach (var header in headers) {
						logCollection.Add(header, new List<string>());
					}
					receiverState = ReceiverState.ReadingData;
				} else {
					// Otherwise error out and go to Standby Mode.
					Debug.LogError("Received " + headers.Count + "columns, but Arduino reported " + columnLength + "! Data Discarded..");
					receiverState = ReceiverState.Standby;
				}
			} else if (receiverState == ReceiverState.ReadingData) {
				// Check for "END" strings
				if (s.Contains ("LOG END")) {
					receiverState = ReceiverState.LoggingFinished;
					continue;
				}
				// Parse data
				var bodyData = s.Split('\t');

				// Check that bodyData contains the expected number of columns. 
				if (bodyData.Length == columnLength) {
					for (int i = 0; i < bodyData.Length; i++) {
						string header = headers[i];
						string sanitizedValue = new string((from c in bodyData[i] where char.IsLetterOrDigit(c) || char.IsPunctuation(c) select c).ToArray());
						logCollection[header].Add(sanitizedValue);
					}
				} else {
					// Otherwise error out and go to Standby Mode.
					Debug.LogError("Received " + bodyData.Length + "columns, but Arduino reported " + columnLength + "! Data Discarded..");
					receiverState = ReceiverState.Standby;
				}

			} else 	if (receiverState == ReceiverState.LoggingFinished) {
				// Finalize Data Collection
				mySQL.AddToUploadQueue(logCollection);
				mySQL.UploadNow();				
				logToDisk.Log(logCollection);

				// Reset receiverState to Standby.
				receiverState = ReceiverState.Standby;
				logToDisk.ResetLoggedState();

			}
		}
	}

/*
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
					columnLength = 4;
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
*/

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

