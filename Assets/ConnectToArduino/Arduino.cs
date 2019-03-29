/* Arduino-Unity Communication
 * 
 * Including custom SerialPort buffers that avoid many of the class' problems
 * http://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport
 * 
 * The Arduino script uses coroutines instead of Update(), to enable faster serial
 * communication than the frame rate.
 *
 * Created by Martin Kibsgaard, Aalborg University
 * kibsgaard@creatae.aau.dk
 * martin.kibsgaard@gmail.com
 * 
 * Modified by Bastian Ilso
 * biho@create.aau.dk
 * contact@bastianilso.com
 */

//To subscribe to the data from the Arduino you can write:
//  Arduino.NewDataEvent += NewData;

//where NewData is the name of a function that should be called when an event fires, e.g.:
//  void NewData(Arduino arduino)
//  {
//    doSomething();
//  }


using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using UnityEngine.UI;

public class Arduino : MonoBehaviour {

    public enum ReceiverState {
        Standby,
		ReadingHeader,
        ReadingData,
		LoggingFinished
    }

	private ReceiverState receiverState;


    /* 
    * Arduino Connection Setup
    */
    [Header("-- Settings --")]
    public string PortName = "COM5";
    public int BaudRate = 115200;
    private char StartFlag = '#';
    private int PollingRate = 100;
    private int PackagesLost = 0;
    private int readTimeouts = 0;
    private SerialPort arduino; 
    private int writeTimeouts = 0;
    private int retries = 0;
    private IEnumerator SerialUpdate;

    /* 
    * Incoming data
    */
    [Header("Arduino Output")]
    public bool ParseIncomingData = true;
    private Dictionary<string, string> NewestIncomingData;
    private int numberOfColumns = 1;
    public string separator = "\t";
    private string outputLabel;
    private string email;
    private Dictionary<string, List<string>> logCollection;
    public List<string> headers;

    /* 
    * Event Handler
    */
    public delegate void NewDataEventHandler(Dictionary<string, List<string>> readData);
    public static event NewDataEventHandler NewDataEvent;

    public delegate void NewRawSerialEventHandler(Arduino arduino);
    public static event NewRawSerialEventHandler NewRawSerialEvent;

    public delegate void NewHeaderEventHandler(List<string> arduinoHeaders);
    public static event NewHeaderEventHandler NewHeaderEvent;
    [Serializable]
    public class OnLoggingFinished : UnityEvent<Dictionary<string, List<string>>> { }
    public OnLoggingFinished onLoggingFinished;

    [Serializable]
    public class OnLoggingStarted : UnityEvent<string> { }
    public OnLoggingStarted onLoggingStarted;

    [Serializable]
    public class OnLoggingInterrupted : UnityEvent<string> { }
    public OnLoggingStarted onLoggingInterrupted;

    // Use this for initialization
    void Start () {
        var connectToArduino = GameObject.Find("ConnectToArduino").GetComponent<ConnectToArduino>();
        BaudRate = connectToArduino.sanitizedBaudRate;
        PortName = connectToArduino.sanitizedSerialPort;
        email = connectToArduino.email;
        OpenPort(); //Open the serial port when the scene is loaded.
    }

    //Process the data we get from our Arduino (this function might be called more often than Update(), depending on the chosen polling rate)
    private void ProcessInputFromArduino(string serialInput) {
        if (!ParseIncomingData) {
            return;
        }

        // Read what is our current state
        if (receiverState == ReceiverState.Standby) {
            // Check for "BEGIN" string.
            if (serialInput.Contains ("LOG BEGIN")) {
                // Parse Reported Column and Separator
                ParseDataArguments(serialInput);
                onLoggingStarted.Invoke(outputLabel);

                // Initialize the log dictionary
                logCollection = new Dictionary<string, List<string>>();

                receiverState = ReceiverState.ReadingHeader;					
            }
        } else if (receiverState == ReceiverState.ReadingHeader) {
            // Parse header
            headers = new List<string>();				
            headers = serialInput.Split('\t').ToList();
            if (NewHeaderEvent != null)   //Check that someone is actually subscribed to the event
                NewHeaderEvent(headers);     //Fire the event in case someone is subscribed            
            logCollection.Add("Email", new List<string>());
            // Check that header contains the expected number of columns. 
            if (headers.Count == numberOfColumns) {
                foreach (var header in headers) {
                    logCollection.Add(header, new List<string>());
                }
                receiverState = ReceiverState.ReadingData;
            } else {
                // Otherwise error out and go to Standby Mode.
                Debug.LogError("Received " + headers.Count + "columns, but Arduino reported " + numberOfColumns + "! Data Discarded..");
                receiverState = ReceiverState.Standby;
                onLoggingInterrupted.Invoke(outputLabel);
            }
        } else if (receiverState == ReceiverState.ReadingData) {
            // Check for "END" strings
            if (serialInput.Contains ("LOG END")) {
                receiverState = ReceiverState.LoggingFinished;
            } else {
                // Parse data
                var bodyData = serialInput.Split('\t');

                // Check that bodyData contains the expected number of columns. 
                if (bodyData.Length == numberOfColumns) {
                    logCollection["Email"].Add(email);
                    for (int i = 0; i < bodyData.Length; i++) {
                        string header = headers[i];
                        string sanitizedValue = new string((from c in bodyData[i] where char.IsLetterOrDigit(c) || char.IsPunctuation(c) select c).ToArray());
                        logCollection[header].Add(sanitizedValue);
                    }
                    //When ever new data arrives, the scripts fires an event to any scripts that are subscribed, to let them know there is new data available (e.g. my Arduino Logger script).
                    if (NewDataEvent != null) {   //Check that someone is actually subscribed to the event
                        NewDataEvent(logCollection);     //Fire the event in case someone is subscribed
                    }
                } else {
                    // Otherwise error out and go to Standby Mode.
                    Debug.LogError("Received " + bodyData.Length + "columns, but Arduino reported " + numberOfColumns + "! Data Discarded..");
                    //receiverState = ReceiverState.Standby;
                    //onLoggingInterrupted.Invoke(outputLabel);
                }
            }
        }
        
        if (receiverState == ReceiverState.LoggingFinished && logCollection.Count > 0) {
            onLoggingFinished.Invoke(logCollection);

            // Reset receiverState to Standby.
            receiverState = ReceiverState.Standby;

        }

        // ----- INPUT FROM ARDUINO TO UNITY ----- //
        //From here you can do what ever you want with the data.
        //As an example, I parse the data into public variables that can be accessed from other classes/scripts:

        //string[] values = serialInput.Split('\t');  //Split the string between the chosen delimiter (tab)

        //ArduinoMillis = uint.Parse(values[0]);      //Pass the first value to an unsigned integer
        //RawEDA = int.Parse(values[1]);              //Pass the second value to an integer
        //int tmpIBI = int.Parse(values[2]);
        //if (tmpIBI > 0)
        //    IBI = tmpIBI;
        //RawPulse = int.Parse(values[3]);
        //rawPressure = int.Parse(values[4]);
        //testStart = int.Parse(values[5]);

        //Feel free to add new variables (both here and in the Arduino script).
    }

	private void ParseDataArguments(string s) {
		var start = s.IndexOf("(")+1;
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
					var result = int.TryParse(val, out numberOfColumns);
					if (!result) {
						Debug.LogError("Could not parse column length argument: " + val + " - use fx LOG BEGIN (col=5).");
					}
			} else if (param == "label") {
					outputLabel = val;
			} else {
				Debug.LogWarning("Arduino reported an unknown parameter: " + param);
			}
		}
    }

    // ----- SERIAL COMMUNICATION ----- //

    //Buffers used for serial input
    private byte[] readBuffer = new byte[4096];
    private string inputBuffer = "";
    public string rawSerialEvent = "";
    private IEnumerator ReadIncomingData()
    {
        System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
        while (true) //Loop until stopped by StopCoroutine()
        {
            try
            {
                //Read everything currently in the system input buffer
                int bytesRead = arduino.Read(readBuffer, 0, readBuffer.Length);
                //Convert the byte to ASCII (a string)
                string serialInput = encoder.GetString(readBuffer, 0, bytesRead);
                //Add the new data to our own input buffer
                inputBuffer += serialInput;
                rawSerialEvent = serialInput;

                if (NewRawSerialEvent != null)   //Check that someone is actually subscribed to the event
                    NewRawSerialEvent(this);     //Fire the event in case someone is subscribed

                //Find a new line flag (indicates end of a data package)
                int endFlagPosition = inputBuffer.IndexOf('\n');
                //If we found a flag, process it further
                while (endFlagPosition > -1)
                {
                    //Check if the start flag is also there (i.e. we have recieved an entire data package
                    if (inputBuffer[0] == StartFlag)
                    {
                        //Hand the data to the function above
                        ProcessInputFromArduino(inputBuffer.Substring(1, endFlagPosition));
                        readTimeouts = 0;
                    }
                    else
                    //If the start flag isn't there, we have only recieved a partial data package, and thus we throw it out
                    {
                        if (PackagesLost > 0) //Don't complain about first lost package, as it usually happens once at startup
                            Debug.Log("Start flag not found in serial input (corrupted data?)");
                        PackagesLost++; //Count how many packages we have lost since the start of the scene.
                    }

                    //Remove the data package from our own input buffer (both if it is partial and if it is complete)
                    inputBuffer = inputBuffer.Remove(0, endFlagPosition + 1);
                    //Check if there is another data package available in our input buffer (while-loop). Makes sure we're not behind and only read old data (e.g. if Unity hangs for a second, the Arduino would have send a lot of packages meanwhile that we need to handle)
                    endFlagPosition = inputBuffer.IndexOf('\n');
                }
                //Reset the timeout counter (as we just recieved some data)
                readTimeouts = 0;
            }
            catch (System.Exception e)
            {
                //Catch any timeout errors (can happen if the Arduino is busy with something else)
                readTimeouts++;

                //If we time out many times, then something is propably wrong with the serial port, in which case we will try to reopen it.
                if (readTimeouts > 5000)
                {
                    Debug.Log("No data recieved for a long time (" + PortName + ").\n" + e.ToString());
                    ReopenPort();
                } 
            }
            //Make the coroutine take a break, to allow Unity to also use the CPU.
            //This currently doesn't account for the time the coroutine actually takes to run (~1ms) and thus isn't the true polling rate.
            yield return new WaitForSeconds(1.0f / PollingRate);
        }
    }

    void ReopenPort()
    {
        Debug.Log("Trying to reopen SerialPort with name " + PortName + ". Try #" + retries);
        StopCoroutine(SerialUpdate);
        arduino.Close();
        readTimeouts = 0;
        PackagesLost = 0;
        retries++;
        if (retries > 5)
        {
            Debug.LogError("Couldn't open serial port with name " + PortName);
            gameObject.SetActive(false);
            return;
        }
        Invoke("OpenPort",5f);
        
    }

    void OpenPort()
    {
        arduino = new SerialPort(PortName, BaudRate);
        arduino.ReadTimeout = 1000;
        arduino.WriteTimeout = 50; //Unfortunatly 

        // We toggle DtrEnable to reset the Arduino when we open connection it. Otherwise, we may in some cases not receive the header.
        arduino.DtrEnable = true;
        try
        {
            arduino.Open();
            System.Threading.Thread.Sleep(1000);
            arduino.DtrEnable = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Serial Port with name " + PortName + " could not be opened. Try one of these instead:");
            foreach (var portName in SerialPort.GetPortNames())
            {
                Debug.Log(portName);
            }
            
            return;
        }


        if (!arduino.IsOpen)
        {
            Debug.LogError("Couldn't open Serial Port with name " + PortName);
            gameObject.SetActive(false);
            return;
        }

        //Clear any data in the buffer (the C# methods made for this in the Serial class are not implemented in this version of Mono)
        
        try
        {
            byte[] buffer = new byte[arduino.ReadBufferSize];
            arduino.Read(buffer, 0, buffer.Length);
        }
        catch (System.Exception)
        {
            // ignored
        }
         


        arduino.ReadTimeout = 1; //We don't want Unity to hang in case there's no data yet. Better to timeout the reading and let Unity do other things while waiting for new data to arrive

        SerialUpdate = ReadIncomingData();
        StartCoroutine(SerialUpdate); 
    }

    void OnDisable()
    {
        StopCoroutine(ReadIncomingData());
        arduino.Close();
    }
}
