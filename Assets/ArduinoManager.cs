using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArduinoManager : MonoBehaviour
{

    public string portName;



    // Start is called before the first frame update
    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnApplicationQuit()
    {
        CloseConnection();
    }

	public void SetSerialPort(string newSerialPort) {
		portName = newSerialPort;
		serialport = new SerialPort (portName, baudRate);
	}

    public bool OpenConnection()
    {
		//try
		if (serialport != null)
		{
			if (serialport.IsOpen)
			{
				//Serial port is already open. We ignore it for now, or we can close it.
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


}
