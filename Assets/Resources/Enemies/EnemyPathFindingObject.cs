﻿using UnityEngine;
using PathFinding;
using System;
using Entities;
using System.Collections;
using Localisation;

public class EnemyPathFindingObject : PathFindingObject, ITargetable, IAttackable, IAttacker
{
    #region 00_MEMBER_VARIABLES_AND_PROPERTIES --------------------------------------------------------------------------------------------
    // Const object data

    // State variables
    EnemyStateData m_enemyStateData;

    // References to attached components.
    Animator m_animator;
    Rigidbody m_rb;

    // Reference to UI objects
    EnemyTitleTextField m_enemyTextField;
    EnemyHealthBar m_healthbar;

    // Properties
    public IAttackable CurrentTarget { get; set; }
    public int DeathTime { get; set; }
    public float Health { get; set; }
    public int ID { get; set; }
    public bool IsBossUnit { get; set; }

    // Exposed variables for setting unit data
    [SerializeField] float m_attackRange;
    [SerializeField] float m_attacksPerSecond;
    [SerializeField] float m_baseDamage;
    [SerializeField] float m_baseHealth;
    [SerializeField] Enums.ENEMY_TYPE m_enemyType;
    [SerializeField] int m_level;
    [Tooltip("0 = en, 1 = jp")][SerializeField] string[] m_unitName;

    // Exposed variables for setting component/child/instantiation object references
    [SerializeField] SkinnedMeshRenderer m_meshRenderer;
    [SerializeField] GameObject m_projectile;
    [SerializeField] Transform m_projectileTransform;
    [SerializeField] GameObject m_psDeath;
    #endregion 00_MEMBER_VARIABLES_AND_PROPERTIES ---------------------------------------------------------------------------------------------

    #region 01_UNITY_OVERRIDES ------------------------------------------------------------------------------------------------------------
    void Awake()
    {
        LineRenderer = GetComponent<LineRenderer>();

        m_animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
        m_enemyStateData.LastAttackTime = -1;
        m_enemyStateData.CurrentAffliction = Enums.AFFLICTION.NONE;

        GameManager.OnStartRun += OnStartRun;
    }

    void Update()
    {
        // Early escape for death, pre-load etc.
        if (!Running) return;

        // Handle afflictions (only STUN has been implemented)
        switch (m_enemyStateData.CurrentAffliction)
        {
            case Enums.AFFLICTION.STUN:
                if (Time.time > m_enemyStateData.AfflictionEndTime)
                {
                    m_animator.enabled = true;
                    m_meshRenderer.material.color = Color.white;
                    m_enemyStateData.CurrentAffliction = Enums.AFFLICTION.NONE;
                    Speed = m_enemyStateData.SpeedTemp;
                }
                else
                {
                    if (m_animator.enabled)
                    {
                        m_animator.enabled = false;
                        m_meshRenderer.material.color = Color.cyan;
                        m_enemyStateData.SpeedTemp = Speed;
                        Speed = 0;
                    }
                }
                break;
            default:
                break;
        }

        // Handle object deletion
        if (!m_enemyStateData.Deleted)
        {
            bool offgrid = ASGrid.IsOffGrid(transform.position);
            if (offgrid || transform.position.y < 0)
            {
                if (offgrid)
                {
                    transform.position = new Vector3(0, -1000, 0);
                    if (m_psDeath) Instantiate(m_psDeath, transform.position + Vector3.up, Quaternion.identity);
                }

                ((PlayerPathFindingObject)CurrentTarget).GiveXp((int)GetUnitHealth());

                m_enemyStateData.Deleted = true;
                Running = false;
                IsDead = true;

                AI.RemoveUnit(Platforms.PlayerPlatform, this);

                Disposal.Dispose(gameObject);
            }
        }

        // If unit is a boss, perform the following initialisation steps
        if (m_enemyType > Enums.ENEMY_TYPE.BOW && (transform.position - CurrentTarget.GetPosition()).sqrMagnitude < 20)
        {
            if (!m_enemyStateData.BossFightInit)
            {
                Audio.BlendMusicTo(Audio.BGM.LOUD, 1);
                (CurrentTarget as PlayerPathFindingObject).IsAtBoss = true;
            }

            m_enemyStateData.BossFightInit = true;

            IsChangingView = true;
            (CurrentTarget as PlayerPathFindingObject).IsChangingView = true;

            if (!CameraFollow.SwitchViewRear()) return;
            IsChangingView = false;
            (CurrentTarget as PlayerPathFindingObject).IsChangingView = false;
            (CurrentTarget as PlayerPathFindingObject).SetAttackRange(1.5f);

            return;
        }

        // If this point is reached, the unit is clear to perform an attack (normal, spin or kick)
        PerformAttack(CurrentTarget);
    }

    void FixedUpdate()
    {
        UpdateMovement();
    }

    #endregion 01_UNITY_OVERRIDES ---------------------------------------------------------------------------------------------------------

    #region 02_PUBLIC_MEMBER_FUNCTIONS ----------------------------------------------------------------------------------------------------
    #region 02A_ANIMATION_TRIGGERS --------------------------------------------------------------------------------------------------------
    void AnimationTriggerProjectileFired()
    {
        var newProjectile = GameObject.Instantiate(m_projectile, m_projectileTransform.position, Quaternion.identity) as GameObject;

        var refToScript = newProjectile.GetComponent<Projectile>();

        refToScript.Init(CurrentTarget, this, 2, 5, LevelScaling.GetScaledDamage(m_level, (int)m_baseDamage));
    }
    #endregion 02A_ANIMATION_TRIGGERS -----------------------------------------------------------------------------------------------------

    #region 02B_EVENT_HANDLERS ------------------------------------------------------------------------------------------------------------
    public void OnAfflict(Enums.AFFLICTION status, float duration)
    {
        m_enemyStateData.CurrentAffliction = status;
        m_enemyStateData.AfflictionEndTime = Time.time + duration;
    }

    public void OnDamage(IAttacker attacker, float dmg, FCT_TYPE type)
    {
        if (IsDead || dmg == 0) return;

        // This means we are applying percentage damage
        bool percentage = dmg < 0;

        if (percentage)
        {
            dmg = GetUnitHealth() * (Mathf.Abs(dmg) / 100f);
        }

        dmg *= ((m_enemyStateData.CurrentAffliction == Enums.AFFLICTION.STUN) ? 2 : 1);

        Health -= dmg;

        var screenPos = Camera.main.WorldToScreenPoint(transform.position);
        var dir = screenPos - Camera.main.WorldToScreenPoint(attacker.GetPosition());

        FCTRenderer.AddFCT(type, dmg.ToString("F0"), transform.position + Vector3.up);

        m_healthbar.UpdateHealthDisplay(Mathf.Max(Health / (int)GetUnitHealth(), 0), (int)GetUnitHealth());

        if (Health <= 0)
        {
            if (m_psDeath) Instantiate(m_psDeath, transform.position + Vector3.up, Quaternion.identity);

            Running = false;
            InterruptAnimator();
            TriggerAnimation(ANIMATION.DEATH);
            IsDead = true;

            bool isBoss = (m_enemyType > Enums.ENEMY_TYPE.BOW);

            attacker.OnTargetDied(this, isBoss);

            int xp = (int)GetUnitHealth();

            xp = (m_enemyType > Enums.ENEMY_TYPE.BOW) ? xp * 2 : xp;

            ((PlayerPathFindingObject)attacker).GiveXp(xp);

            return;
        }
    }

    public void OnKnockBack(Vector2 sourcePos, float strength)
    {
        if (m_enemyType > Enums.ENEMY_TYPE.BOW) return;

        var forceVec = (transform.position - new Vector3(sourcePos.x, transform.position.y, sourcePos.y)).normalized * strength;
        m_rb.AddForce(forceVec);
    }

    public void OnSetAsRenderTarget(bool on)
    {
        GetComponentInChildren<Camera>().enabled = on;

        if (on)
        {
            gameObject.SetLayerRecursively(12);
            m_healthbar.UpdateHealthDisplay(Health / GetUnitHealth(), (int)GetUnitHealth());
            m_enemyTextField.UpdateTextData(m_unitName[0], m_unitName[1]);
        }
        else
        {
            gameObject.SetLayerRecursively(10);
        }
    }

    public void OnTargetDied(IAttackable target, bool boss = false)
    {
        throw new NotImplementedException();
    }
    #endregion 02B_EVENT_HANDLERS ---------------------------------------------------------------------------------------------------------

    #region 02C_UPDATERS_AND_SETTERS ------------------------------------------------------------------------------------------------------
    public void SetLevel(int level)
    {
        m_level = level;
    }

    #endregion 02C_UPDATERS_AND_SETTERS ---------------------------------------------------------------------------------------------------

    #region 02D_GETTERS -------------------------------------------------------------------------------------------------------------------
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public ITargetable GetTargetableInterface()
    {
        return this;
    }

    public Transform GetTransform()
    {
        return transform;
    }

    #endregion 02D_GETTERS ----------------------------------------------------------------------------------------------------------------
    #endregion 02_PUBLIC_MEMBER_FUNCTIONS -------------------------------------------------------------------------------------------------

    #region 02_PRIVATE_MEMBER_FUNCTIONS ---------------------------------------------------------------------------------------------------
    void AttackSequence(IAttackable target)
    {
        int rand = UnityEngine.Random.Range(0, 30);

        switch (m_enemyType)
        {
            case Enums.ENEMY_TYPE.AXE:
                if (rand > 9)
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC1);
                    StartCoroutine(ApplyDamageDelayed(1, 7, target));
                }
                else if (rand > 2)
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC2);
                    StartCoroutine(ApplyDamageDelayed(1, 7, target));
                    StartCoroutine(ApplyDamageDelayed(1, 19, target));
                }
                else
                {
                    TriggerAnimation(ANIMATION.ATTACK_ULTIMATE);
                    StartCoroutine(ApplyDamageDelayed(5, 16, target));
                }

                m_enemyStateData.LastAttackTime = Time.time;
                break;
            case Enums.ENEMY_TYPE.SPEAR:
                if (rand > 2)
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC1);
                    StartCoroutine(ApplyDamageDelayed(2, 9, target));

                }
                else
                {
                    TriggerAnimation(ANIMATION.ATTACK_ULTIMATE);
                    StartCoroutine(ApplyDamageDelayed(5, 16, target));
                }

                m_enemyStateData.LastAttackTime = Time.time;
                break;
            case Enums.ENEMY_TYPE.DAGGER:
                if (rand > 2)
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC1);
                    StartCoroutine(ApplyDamageDelayed(1, 7, target));
                }
                else
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC2);
                    StartCoroutine(ApplyDamageDelayed(2, 7, target));
                    StartCoroutine(ApplyDamageDelayed(2, 19, target));
                }

                m_enemyStateData.LastAttackTime = Time.time;
                break;
            case Enums.ENEMY_TYPE.BOW:
                TriggerAnimation(ANIMATION.ATTACK_BASIC1);

                m_enemyStateData.LastAttackTime = Time.time;
                break;
            case Enums.ENEMY_TYPE.PALADIN:
                if (rand > 10)
                {
                    TriggerAnimation(ANIMATION.ATTACK_BASIC1);
                }
                else
                {
                    TriggerAnimation(ANIMATION.ATTACK_ULTIMATE);
                }

                m_enemyStateData.LastAttackTime = Time.time;
                break;
        }
    }

    void GetInRange(ITargetable target)
    {
        UpdatePathTarget(PathingTarget);
    }

    float GetUnitHealth()
    {
        return LevelScaling.GetScaledHealth(m_level, (int)m_baseHealth);
    }

    void InterruptAnimator()
    {
        m_animator.SetTrigger("Interrupt");

        m_animator.ResetTrigger("A1Start");
        m_animator.ResetTrigger("A2Start");
        m_animator.ResetTrigger("A3Start");
        m_animator.ResetTrigger("DeathStart");
    }

    void PerformAttack(IAttackable target)
    {
        var distanceToTarget = Vector3.Distance(transform.position, target.GetPosition());

        if (distanceToTarget <= m_attackRange && !target.IsDead)
        {
            StopMovement();
            OnFollowPath(0);

            var targetRotation = Quaternion.LookRotation(target.GetPosition() - transform.position);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4);

            float delay = 1000f / (1000f * m_attacksPerSecond);

            if (m_enemyStateData.LastAttackTime == -1 || Time.time - m_enemyStateData.LastAttackTime > delay)
            {
                InterruptAnimator();

                AttackSequence(target);
            }
        }
        else
        {
            if (m_enemyStateData.CurrentAffliction != Enums.AFFLICTION.STUN) GetInRange(CurrentTarget.GetTargetableInterface());
        }
    }

    void TriggerAnimation(ANIMATION anim)
    {
        switch (anim)
        {
            case ANIMATION.ATTACK_BASIC1:
                InterruptAnimator();
                m_animator.SetTrigger("A1Start");
                break;
            case ANIMATION.ATTACK_BASIC2:
                InterruptAnimator();
                m_animator.SetTrigger("A2Start");
                break;
            case ANIMATION.ATTACK_ULTIMATE:
                InterruptAnimator();
                m_animator.SetTrigger("A3Start");
                break;
            case ANIMATION.DEATH:
                if (m_enemyType > Enums.ENEMY_TYPE.BOW)
                {
                    if (!m_animator.enabled)
                    {
                        m_animator.enabled = true;
                        m_meshRenderer.material.color = Color.white;
                        Speed = m_enemyStateData.SpeedTemp;
                    }

                    InterruptAnimator();
                    m_animator.SetTrigger("DeathStart");
                }
                else
                {
                    AI.RemoveUnit(Platforms.PlayerPlatform, this);

                    transform.position = new Vector3(0, -1000, 0);

                    Disposal.Dispose(gameObject);

                    m_enemyStateData.Deleted = true;
                }


                break;
        }
    }
    #endregion 02_PRIVATE_MEMBER_FUNCTIONS ------------------------------------------------------------------------------------------------

    #region 03_OVERRIDE_MEMBER_FUNCTIONS --------------------------------------------------------------------------------------------------
    public override void OnFollowPath(float speedPercent)
    {
        if (speedPercent > 0) speedPercent = Mathf.Clamp01(speedPercent + 0.5f);

        m_animator.SetFloat("MovementBlend", 1 - speedPercent);
    }

    public override void OnStartRun()
    {
        PreviousPos = transform.position;

        CurrentTarget = GameManager.GetCurrentPlayerReference();

        PathingTarget = CurrentTarget as ITargetable;

        m_healthbar = GameUIManager.GetEnemyHealthBarReference();

        m_enemyTextField = GameUIManager.GetEnemyTextField();

        Health = GetUnitHealth();

        StartCoroutine(RefreshPath());

        GetInRange(PathingTarget);
    }
    #endregion 03_OVERRIDE_MEMBER_FUNCTIONS -----------------------------------------------------------------------------------------------

    #region 04_MEMBER_IENUMERATORS --------------------------------------------------------------------------------------------------------
    IEnumerator ApplyDamageDelayed(int dmgMultiplier, int frameDelay, IAttackable target)
    {
        yield return new WaitForSeconds(Time.fixedDeltaTime * frameDelay);

        target.OnDamage(this, LevelScaling.GetScaledDamage(m_level, (int)m_baseDamage), FCT_TYPE.ENEMYHIT);

        Audio.PlayFX(Audio.FX.ENEMY_ATTACK_IMPACT, transform.position);
    }

    #endregion 04_MEMBER_IENUMERATORS -----------------------------------------------------------------------------------------------------
}