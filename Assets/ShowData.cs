using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowData : MonoBehaviour
{
    
    string webapplication = "";
    string email;

    [SerializeField]
    private GameObject showDataButton;

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

    public void SetTargetDatabaseTable(string tablename) {
        if (tablename == "reactiontime" || tablename == "synch" || tablename == "EDAIBISerial") {
            webapplication = "http://create-rapps01.srv.aau.dk/reaction-synch-tests/" + "?email=" + email + "&subject=" + tablename;
            // format: http://create-rapps01.srv.aau.dk/reaction-synch-tests/?email=buildwin@aau.dk&subject=synch
        } 
    }

    public void TriggerShowDataVisibility(LogUploadResult logUploadResult) {
        if (logUploadResult.status == LogUploadStatus.Success) {
            showDataButton.SetActive(true);
        }
    }

    public void ShowRShinyWebApplication() {
        Application.OpenURL(webapplication);
    }
}
