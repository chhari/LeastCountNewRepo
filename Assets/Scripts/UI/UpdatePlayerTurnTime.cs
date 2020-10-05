using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdatePlayerTurnTime : MonoBehaviour
{
    public static UpdatePlayerTurnTime current;
    int playerTime;
    int PlayerTurnTime = 20;
    float currentTurntime;

    bool stopTimer;
    int currentImage;
    Image PlayerImageClock;
    Image Player2ImageClock;
    bool offlineMode;
    bool timeSoundsStarted;

    int hideBubbleAfter = 3;
    Text MessageText;

    AudioSource[] audioSources;


    public void Awake()
    {
        // Debug.Log("started");
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



    // Start is called before the first frame update
    void Update()
    {
        if (!stopTimer)
        {
            updateClock();
        }
    }


    private void updateClock()
    {
        float minus;
        if (currentImage == 1)
        {
            playerTime = PlayerTurnTime;
            if (offlineMode)
            {
                minus = 1.0f / playerTime * Time.deltaTime;
                currentTurntime -= minus;
                PlayerImageClock.fillAmount = currentTurntime;
            }

            if (PlayerImageClock.fillAmount < 0.25f && !timeSoundsStarted)
            {
                audioSources[0].Play();
                timeSoundsStarted = true;
            }

            if (PlayerImageClock.fillAmount == 0)
            {

                audioSources[0].Stop();
                stopTimer = true;
                if (!offlineMode)
                {

                }
                else
                {


                }
                showMessage("You " + "ran out of time");

                if (!offlineMode)
                {
                    minus = 1.0f / playerTime * Time.deltaTime;
                    currentTurntime -= minus;
                    PlayerImageClock.fillAmount = currentTurntime;
                }

            }

        }
        else
        {

            playerTime = PlayerTurnTime;
            if (offlineMode)
            {
                minus = 1.0f / playerTime * Time.deltaTime;
                Player2ImageClock.fillAmount -= minus;
            }

            if (offlineMode && Player2ImageClock.fillAmount < 0.25f && !timeSoundsStarted)
            {
                audioSources[0].Play();
                timeSoundsStarted = true;
            }

            if (Player2ImageClock.fillAmount == 0)
            {
                stopTimer = true;

                if (offlineMode)
                {
                    showMessage("You " + "ran out of time");
                }
                else
                {
                    showMessage("nameOpponent" + " " + "ran out of time");
                }


                if (offlineMode)
                {

                }
            }
        }

    }

    public void showMessage(string message)
    {

        float timeDiff = Time.time;

        Debug.Log("Time diff: " + timeDiff);

        if (timeDiff > hideBubbleAfter + 1.0f)
        {
            MessageText.text = message;

            if (!message.Contains("waitingForOpponent"))
                Invoke("hideBubble", hideBubbleAfter);
            else
            {

            }

        }
        else
        {
            Debug.Log("Show message with delay");

        }
    }

}
