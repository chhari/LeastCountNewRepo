using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager current;


    void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            current = this;
        }


    }
    private static UIManager _instance = null;

    public static UIManager SharedInstance
    {
        get
        {
            // if the instance hasn't been assigned then search for it
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(UIManager)) as UIManager;
            }
            return _instance;
        }
    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void mainMenuEvents(string eventName)
    {
        switch (eventName)
        {
            case "welcome":

                break;

            case "spin":

                break;


            case "home":

                break;

        }
    }
    public void assignTextInstanceToObject(string objectname, GameObject assignedObject)
    {
        objectname = objectname.ToLower();
    }
}
