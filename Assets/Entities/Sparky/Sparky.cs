﻿using System.Collections;
using UnityEngine;

public class Sparky : MonoBehaviour
{
    JKnightControl m_knight;
    Light m_light;
    float m_vOffset;

    public bool IsMoving { get; set; }

    private void Awake()
    {
        m_knight = FindObjectOfType<JKnightControl>();
        m_light = GetComponentInChildren<Light>();
        m_vOffset = 0;
        transform.parent = null;

        GameManager.OnStartRun += OnStartRun;
    }

    private void OnStartRun()
    {
        IsMoving = true;
    }

    void LateUpdate ()
    {
        m_vOffset = Mathf.Sin(Time.time * 3);

        var diff = Vector3.up * m_vOffset * 0.1f;

        transform.position = new Vector3(m_knight.transform.position.x, 1, m_knight.transform.position.z) + diff;

        if (IsInFront())
        {
            StopAllCoroutines();

            if (IsLeft())
            {
                transform.Rotate(transform.up, -1);
            }
            else
            {
                transform.Rotate(transform.up, 1);
            }
        }
        else
        {
            if (!IsMoving) return;

            bool willMove = (Random.Range(0, 180) == 0);

            if (willMove)
            {
                StopAllCoroutines();

                StartCoroutine(Rotate());
            }
        }
	}

    IEnumerator Rotate()
    {
        int movement = Random.Range(0, 40);
        int limit = movement;
        int mod = (Random.Range(0, 2) == 0) ? -1 : 1;

        while (movement > 0 && !IsInFront())
        {
            float angle = Mathf.Deg2Rad * (((float)movement / (float)limit) * 180);

            float nextMovement = 10 * Mathf.Sin(angle);

            transform.Rotate(transform.up, nextMovement * mod);

            movement--;
            yield return new WaitForFixedUpdate();
        }
    }
    
    float GetAngle()
    {
        return Vector3.Angle((m_knight.transform.position - m_light.transform.position).normalized, m_knight.transform.forward);
    }

    bool IsInFront()
    {
        return (Vector3.Dot((m_knight.transform.position - m_light.transform.position).normalized, m_knight.transform.forward)) < -0.2f;
    }

    bool IsLeft()
    {
        return (Vector3.Cross((m_knight.transform.position - m_light.transform.position).normalized, m_knight.transform.forward)).y < 0;
    }
}