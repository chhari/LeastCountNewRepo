using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClickHandler : MonoBehaviour
{
    void Start()
    {

        if (GetComponent<Button>() != null)
        {
            GetComponent<Button>().onClick.AddListener(() => UIManager.SharedInstance.mainMenuEvents(gameObject.name));
        }
        else if (GetComponent<Text>() != null)
        {
            UIManager.SharedInstance.assignTextInstanceToObject(gameObject.name, gameObject);
        }
        else if (GetComponent<TextMesh>() != null)
        {
            UIManager.SharedInstance.assignTextInstanceToObject(gameObject.name, gameObject);
        }

        //AnimationManager.SharedInstance.initObj (gameObject);

    }
}
