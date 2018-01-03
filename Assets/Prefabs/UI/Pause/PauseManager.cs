﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject m_pauseScreen, m_gameScreen, m_bonusScreen;

    public static bool Paused;

	void Start ()
    {
		
	}
	
	void Update ()
    {
		
	}

    // 0 = pausemenu, 1 = bonusmenu
    public void OnStartPause(int nextScreen)
    {
        m_gameScreen.SetActive(false);

        switch (nextScreen)
        {
            case 0: m_pauseScreen.SetActive(true); break;
            case 1: m_bonusScreen.SetActive(true); break;
            default: break;
        }
        Time.timeScale = 0;
        Camera.main.gameObject.GetComponent<UnityStandardAssets.ImageEffects.Blur>().enabled = true;
        Paused = true;
    }

    // 0 = pausemenu, 1 = bonusmenu
    public void OnEndPause()
    {
        m_pauseScreen.SetActive(false);
        m_bonusScreen.SetActive(false);

        m_gameScreen.SetActive(true);

        Time.timeScale = 1;
        Camera.main.gameObject.GetComponent<UnityStandardAssets.ImageEffects.Blur>().enabled = false;
        Paused = false;
    }
}
