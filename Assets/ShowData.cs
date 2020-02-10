using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowData : MonoBehaviour
{
    
    string webapplication = "";
    string email;
    // Start is called before the first frame update
    void Start()
    {
        var connectToArduino = GameObject.Find("ConnectToArduino").GetComponent<ConnectToArduino>();
        email = connectToArduino.email;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetTargetDatabaseTable(string tablename) {
        if (tablename == "reactiontime" || tablename == "synch" || tablename == "EDAIBISerial") {
            webapplication = "http://create-rapps01.srv.aau.dk/reaction-synch-tests/" + "?email=" + email + "&subject=" + tablename;
            // format: http://create-rapps01.srv.aau.dk/reaction-synch-tests/?email=buildwin@aau.dk&subject=synch
        } 
    }

    public void ShowRShinyWebApplication(LogUploadResult logUploadResult) {
        if (logUploadResult.status == LogUploadStatus.Success) {
            Application.OpenURL(webapplication);
        }
    }
}
