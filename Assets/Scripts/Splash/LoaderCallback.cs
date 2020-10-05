using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoaderCallback : MonoBehaviour
{

    private static int count = 0;

    private void Update(){

        count++;
        Debug.Log(count.ToString());
        if (count == 40)
        {
            Debug.Log("Here 4");
            Loader.LoaderCallback();
        }
    }

}
