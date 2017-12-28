﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class GameUIManager : MonoBehaviour
{
    float m_lUltiButtonAlpha, m_rUltiButtonAlpha;
    [SerializeField] Image m_leftSpinner, m_rightSpinner;
    bool m_lButtonDown, m_rButtonDown;
    bool m_isInUltimate;
    JPlayerUnit m_currentPlayer;

    static GameUIManager m_instance;

    void Awake()
    {
        m_instance = this;
    }

    void Start ()
    {
		
	}
	
	void Update ()
    {
        if (m_rUltiButtonAlpha > 0) m_rightSpinner.transform.Rotate(0, 0, m_rUltiButtonAlpha * 4);
        if (m_lUltiButtonAlpha > 0) m_leftSpinner.transform.Rotate(0, 0, -m_lUltiButtonAlpha * 4);

        if ((m_lUltiButtonAlpha >= 1 || m_rUltiButtonAlpha >= 1) && !m_isInUltimate)
        {
            UltiState(true);

            StartCoroutine(SimulateKeyPress(JPlayerUnit.ATTACKS.ULTIMATE));

            return;
        }

        if (m_lButtonDown)
        {
            m_lUltiButtonAlpha = Mathf.Clamp01(m_lUltiButtonAlpha + 0.01f);
            m_leftSpinner.color = new Color(1, 1, 1, m_lUltiButtonAlpha);
        }

        if (m_rButtonDown)
        {
            m_rUltiButtonAlpha = Mathf.Clamp01(m_rUltiButtonAlpha + 0.01f);
            m_rightSpinner.color = new Color(1, 1, 1, m_rUltiButtonAlpha);
        }


    }

    public IEnumerator SimulateKeyPress(JPlayerUnit.ATTACKS type)
    {
        m_instance.m_currentPlayer.SimulatePress(type);

        yield return new WaitForSecondsRealtime(0.5f);

        m_instance.m_currentPlayer.SimulateRelease(type);
    }

    public void OnUltiButtonDown(bool left)
    {

        if (left)
        {
            m_lButtonDown = true;
        }
        else
        {
            m_rButtonDown = true;
        }
    }

    public void OnUltiButtonUp(bool left)
    {
        if (left)
        {
            m_lButtonDown = false;

            if (!m_isInUltimate) DisableSpinner(true);

        }
        else
        {
            m_rButtonDown = false;

            if (!m_isInUltimate) DisableSpinner(false);
        }
    }

    public static void UltiState(bool state)
    {
        m_instance.m_isInUltimate = state;

        if (!state)
        {
            m_instance.DisableSpinner(true);
            m_instance.DisableSpinner(false);
        }
    }

    void DisableSpinner(bool left)
    {
        if (left)
        {
            m_leftSpinner.color = new Color(1, 1, 1, 0);
            m_lUltiButtonAlpha = 0;
        }
        else
        {
            m_rightSpinner.color = new Color(1, 1, 1, 0);
            m_rUltiButtonAlpha = 0;
        }
    }

    public static void SetCurrentPlayerReference(JPlayerUnit player)
    {
        m_instance.m_currentPlayer = player;
    }

    public void OnSimpleButtonDown(int button)
    {
        StartCoroutine(SimulateKeyPress((JPlayerUnit.ATTACKS)button));
    }
}
