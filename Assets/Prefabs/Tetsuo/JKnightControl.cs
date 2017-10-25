﻿using UnityEngine;

/// <summary>
/// Handles control behaviour for player character.
/// Properties: Moving
/// </summary>
public class JKnightControl : MonoBehaviour
{
    // Enumerated animation states for easy referencing
    private enum ANIMATION
    {
        ATTACK_BASIC, ATTACK_SPECIAL, ATTACK_ULTIMATE, ATTACK_KICK, ATTACK_SHIELD, PARRY, BUFF, DEATH, JUMP
    }

    // Variables exposed in the editor.
    [SerializeField] float m_horizontalMod, m_linearMod, m_jumpForce, m_groundTriggerDistance;
    
    // References to attached components.
    Animator m_animator;
    Rigidbody m_rb;

    // Public property used to check whether knight is moving.
    public bool Moving { get; set; }

    void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
    }

    void Start ()
    {
        Moving = false;
    }

    void Update()
    {
        ManualInput();

        // Auto movement goes here...
    }

    void ManualInput()
    {
        // Take the input variables from universal input.
        var linearValue = Input.GetAxis("Vertical");
        var lateralValue = Input.GetAxis("Horizontal");

        // Construct a vector that points in the movement direction, modified by scalar.
        var movementVec = new Vector3(lateralValue * Time.deltaTime * m_horizontalMod, 0, linearValue * Time.deltaTime * m_linearMod);

        // This variable will be constructed for later use of LookAt.
        Vector3 lookPoint;

        // Apply movement appropriately and also update the animator and LookAt target.
        if (movementVec != Vector3.zero)
        {
            Moving = true;
            transform.position += movementVec;
            lookPoint = transform.position + movementVec.normalized;
            m_animator.SetFloat("MovementBlend", 0);

            // Using LookAt is an easy way to maintain the correct visual orientation of the knight.
            transform.LookAt(lookPoint);
        }
        else
        {
            Moving = false;
            m_animator.SetFloat("MovementBlend", 1);
        }

        // Take current inputs and handle behaviour
        InputHandler();
    }

    // Interacts with universal input
    private void InputHandler()
    {
        // If waterfall - Chosen as it allows for easy prioritisation of inputs.
        if (Input.GetButtonDown("AttackBasic"))
        {
            TriggerAnimation(ANIMATION.ATTACK_BASIC);
        }
        else if (Input.GetButtonDown("AttackSpecial"))
        {
            TriggerAnimation(ANIMATION.ATTACK_SPECIAL);
        }
        else if (Input.GetButtonDown("AttackUltimate"))
        {
            TriggerAnimation(ANIMATION.ATTACK_ULTIMATE);
        }
        else if (Input.GetButtonDown("AttackKick"))
        {
            TriggerAnimation(ANIMATION.ATTACK_KICK);
        }
        else if (Input.GetButtonDown("AttackShield"))
        {
            TriggerAnimation(ANIMATION.ATTACK_SHIELD);
        }
        else if (Input.GetButtonDown("Parry"))
        {
            TriggerAnimation(ANIMATION.PARRY);
        }
        else if (Input.GetButtonDown("Buff"))
        {
            TriggerAnimation(ANIMATION.BUFF);
        }
        else if (Input.GetButtonDown("Death"))
        {
            TriggerAnimation(ANIMATION.DEATH);
        }
        else if (Input.GetButtonDown("Jump"))
        {
            if (!IsAirborne())
            {
                TriggerAnimation(ANIMATION.JUMP);
                m_rb.AddForce(Vector3.up * m_jumpForce);
            }
        }
    }

    // Triggers appropriate animation. Is set to interrupt the current animation, and then trigger
    // the appropriate one.
    private void TriggerAnimation(ANIMATION anim)
    {
        switch (anim)
        {
            case ANIMATION.ATTACK_BASIC:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("A1Start");
                break;
            case ANIMATION.ATTACK_SPECIAL:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("A2Start");
                break;
            case ANIMATION.ATTACK_ULTIMATE:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("A3Start");
                break;
            case ANIMATION.ATTACK_KICK:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("KickStart");
                break;
            case ANIMATION.ATTACK_SHIELD:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("ShieldStart");
                break;
            case ANIMATION.PARRY:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("ParryStart");
                break;
            case ANIMATION.BUFF:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("BuffStart");
                break;
            case ANIMATION.DEATH:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("DeathStart");
                break;
            case ANIMATION.JUMP:
                m_animator.SetTrigger("Interrupt");
                m_animator.SetTrigger("JumpStart");
                break;
        }
    }

    // Helper function that uses a raycast to check if the knight is touching a surface with their feet.
    bool IsAirborne()
    {
        if (Physics.Raycast(transform.position + (Vector3.up * 0.5f), Vector3.down, m_groundTriggerDistance))
        {
            m_animator.SetTrigger("JumpEnd");

            return false;
        }

        return true;
    }
}