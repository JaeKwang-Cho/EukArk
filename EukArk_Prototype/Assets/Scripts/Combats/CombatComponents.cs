using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CombatComponents : MonoBehaviour
{
    [Header("Effect Attributes")]
    [SerializeField] ParticleSystem hitEffect;

    [Header("Numeric Attributes")]
    [SerializeField] float healthPoints = 10f;
    [SerializeField] float damagePoints = 10f;
    [SerializeField] float attackableTime = 0.1f;

    [Header("AI Attributes")]
    [SerializeField] bool isAI = true;
    [SerializeField] float attackReactTime = 1f;
    [SerializeField] float attackCooldown = 3f;
    [SerializeField] float attackAfterDelay = 1f;
    [SerializeField] float hitAfterImmune = 0.3f;

    PlayerMovement playerMovement = null;
    MonsterMovement monsterMovement = null;
    CrossHairComponents crosshairComponents = null;

    [Header("Attack Components")]
    [SerializeField]  GameObject parryingPrefab;
    GameObject emptyTemp = null;
    GameObject parryingObject = null;
    ParryingComp parryingComp;
    [SerializeField] GameObject obsidianProjectile;
    [SerializeField] GameObject cyanProjectile;
    [SerializeField] GameObject magentaProjectile;
    [SerializeField] GameObject yellowProjectile;

    public bool isAlive = true;
    public bool isAttacking = false;
    public bool isInAttackRange = false;
    public bool isAttackCooldown = false;
    public bool isAttackAfterDelay = false;
    public bool isAttackSuccess = false;
    public bool isImmune = false;
    public bool isColorFilled = false;

    GameObject MeleeImpactRange;
    CapsuleCollider2D MeleeAttackCapsule;
    BoxCollider2D MeleeBoxCollider;
    GameObject parentGameObject;
    public List<CombatComponents> combatCompListInAttackRange;
    HashSet<CombatComponents> checkDupHit = new HashSet<CombatComponents>();
    public SpriteRenderer slashSpriteForPlayer = null;

    void Start()
    {
        if (!isAI)
        {
            MeleeImpactRange = transform.GetChild(0).gameObject;
            MeleeAttackCapsule = GetComponentInChildren<CapsuleCollider2D>();
            slashSpriteForPlayer = MeleeImpactRange.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>();
            slashSpriteForPlayer.color = Color.clear;
        }
        else
        {
            MeleeBoxCollider = GetComponent<BoxCollider2D>();   
        }
        
        parentGameObject = gameObject.transform.parent.gameObject;
        combatCompListInAttackRange = new List<CombatComponents>();
        if (!isAI)
        {
            playerMovement = parentGameObject.GetComponent<PlayerMovement>();
            crosshairComponents = playerMovement.crossHair;
        }
        else
        {
            monsterMovement = parentGameObject.GetComponent<MonsterMovement>();
        }
    }

    public void Hit(float _damage)
    {
        if (isImmune || !isColorFilled)
        {
            return;
        }
        isImmune = true;
        if (monsterMovement.HasArmor())
        {
            monsterMovement.DestroyArmor();
        }
        else
        {
            healthPoints -= _damage;
        }
        //Debug.Log("Is Ai : " + isAI + " Hit...");
        if (healthPoints <= 0)
        {
            isAlive = false;
        }

        if (!isAI)
        {
            playerMovement = parentGameObject.GetComponent<PlayerMovement>();
            if (isAlive)
            {
                StartCoroutine(HitAfterImmune());
                playerMovement.SetHitState();
            }
            else
            {
                playerMovement.SetDieState();
            }
        }
        else
        {
            monsterMovement = parentGameObject.GetComponent<MonsterMovement>();

            StopAllCoroutines();
            isAttacking = false;
            isAttackAfterDelay = false;
            isAttackSuccess = false;

            isAttackCooldown = true;
            StartCoroutine(AIAttackCooldown());

            if (isAlive)
            {
                StartCoroutine(HitAfterImmune());
                monsterMovement.SetHitState();
            }
            else
            {
                monsterMovement.SetDieState();
            }

            Debug.Log("Monster - Hit");
        }
        PlayHitEffect();
    }

    public void SetColorFilled()
    {
        isColorFilled = true;
    }

    IEnumerator HitAfterImmune()
    {
        yield return new WaitForSecondsRealtime(hitAfterImmune);
        isImmune = false;
    }

    void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem particle = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(particle.gameObject, particle.main.duration + particle.main.startLifetime.constantMax);
        }
    }

    public void DealDamage(CombatComponents _other)
    {
        _other.Hit(damagePoints);
    }

    public void SetSlashTransform(float _xOffset, float _yOffset)
    {
        Vector2 localPosition = Vector2.zero;
        Vector3 localRotation = Vector3.zero;

        localPosition.Set(_xOffset + transform.position.x, _yOffset + transform.position.y);
        //Debug.Log(transform.position.x + ", " + transform.position.y);
        //Debug.Log(_xOffset + ", " + _yOffset);
        float radian = Mathf.Atan2(_yOffset, _xOffset);
        //Debug.Log(radian);
        float degree = radian * Mathf.Rad2Deg;
        //Debug.Log(degree);
        localRotation.Set(0f, 0f, degree + 90f);

        MeleeImpactRange.transform.position = localPosition;
        MeleeImpactRange.transform.rotation = Quaternion.Euler(localRotation);
    }

    public void MeleeAttack()
    {
        if (!isAlive)
        {
            return;
        }

        ContactFilter2D contactFilter = new ContactFilter2D();
        List<Collider2D> allCollisionMonsters = new List<Collider2D>();

        if (!isAI)
        {
            contactFilter.SetLayerMask(LayerMask.GetMask("Monster"));
            contactFilter.IsFilteringTrigger(MeleeAttackCapsule);
            MeleeAttackCapsule.OverlapCollider(contactFilter, allCollisionMonsters);
        }
        else
        {
            contactFilter.SetLayerMask(LayerMask.GetMask("Player"));
            contactFilter.IsFilteringTrigger(MeleeBoxCollider);
            MeleeBoxCollider.OverlapCollider(contactFilter, allCollisionMonsters);
        }

        foreach (Collider2D monCol in allCollisionMonsters)
        {
            CombatComponents combatComp = monCol.gameObject.GetComponentInChildren<CombatComponents>();
            //Debug.Log(monCol.gameObject);
            if (combatComp != null && checkDupHit.Add(combatComp))
            {
                combatComp.Hit(damagePoints);
                //Debug.Log("Melee Hit");
            }
        }
    }

    public void ResetMeleeAttack()
    {
        checkDupHit.Clear();
    }

    public void MissileAttack()
    {
        if (!isAlive)
        {
            return;
        }

        if (!isAI)
        {
            GameObject empty = new GameObject();
            empty.SetActive(false);
            GameObject projectile = Instantiate(obsidianProjectile, empty.transform);

            GameObject currAimedMonster = crosshairComponents.currAimMonster;
            ObsidianProjectile obsidianProjectileComps = projectile.GetComponent<ObsidianProjectile>();
            obsidianProjectileComps.SetPlayerCombatComp(this);
            if (currAimedMonster != null)
            {
                //Debug.Log("currAimedMonster != null");
                obsidianProjectileComps.SetAimedGameObject(currAimedMonster);
            }
            else
            {
                //projectileSpeed
                Vector2 direction = crosshairComponents.transform.position - transform.position;
                direction.Normalize();
                obsidianProjectileComps.SetAimedGameObject(null);
                projectile.GetComponent<ObsidianProjectile>().SetProjectileVelocity(direction);
            }
            projectile.transform.parent = null;
            Destroy(empty);
            projectile.transform.position = transform.position;
            projectile.SetActive(true);
        }
    }

    void ColorMissileAttack(Vector3 _firePosition, Color _color, GameObject _colorProjectile)
    {
        if (!isAlive)
        {
            return;
        }

        if (!isAI)
        {
            GameObject empty = new GameObject();
            empty.transform.position = _firePosition;

            empty.SetActive(false);
            GameObject projectile = Instantiate(_colorProjectile, empty.transform);

            GameObject currAimedMonster = crosshairComponents.currAimMonster;
            ColorProjectile colorProjectileComps = projectile.GetComponent<ColorProjectile>();
            colorProjectileComps.SetPlayerCombatComp(this, _color, _firePosition);
            if (currAimedMonster != null)
            {
                //Debug.Log("currAimedMonster != null");
                colorProjectileComps.SetAimedGameObject(currAimedMonster);
            }
            else
            {
                //projectileSpeed
                Vector2 direction = crosshairComponents.transform.position - _firePosition;
                direction.Normalize();
                colorProjectileComps.SetAimedGameObject(null);
                projectile.GetComponent<ColorProjectile>().SetProjectileVelocity(direction);
            }
            projectile.transform.parent = null;
            Destroy(empty);
            projectile.transform.position = _firePosition;
            projectile.SetActive(true);
        }
    }

    public void CyanMissileAttack(Vector3 _firePosition, Color _color)
    {
        ColorMissileAttack(_firePosition, _color, cyanProjectile);
    }

    public void MagentaMissileAttack(Vector3 _firePosition, Color _color)
    {
        ColorMissileAttack(_firePosition,_color, magentaProjectile);
    }

    public void YellowMissileAttack(Vector3 _firePosition, Color _color)
    {
        ColorMissileAttack(_firePosition, _color, yellowProjectile);
    }

    public void Parrying()
    {
        if(emptyTemp != null || parryingObject != null)
        {
            return;
        }
        emptyTemp = new GameObject("temp");

        StartCoroutine(WaitUntilCreateEmpty());
    }

    IEnumerator WaitUntilCreateEmpty()
    {
        yield return new WaitUntil(IsEmptyValid);
        if(emptyTemp == null)
        {
            Debug.Log("Empty is null");
        }
        emptyTemp.transform.position = transform.position;
        //temp.SetActive(false);

        //Assert.IsTrue(emptyTemp != null);

        parryingObject = Instantiate(parryingPrefab, emptyTemp.transform);
        StartCoroutine(WaitUntilCreateparryingComp());
    }

    bool IsEmptyValid()
    {
        return emptyTemp.scene.IsValid();
    }

    IEnumerator WaitUntilCreateparryingComp()
    {
        yield return new WaitUntil(IsParryingValid);
        if (parryingObject == null)
        {
            Debug.Log("parrying is null");
        }

        parryingComp = parryingObject.GetComponent<ParryingComp>();
        if(parryingComp == null)
        {
            Debug.Log("parryingComp is null");
        }

        parryingComp.SetParryingColor(Color.black);
        parryingObject.transform.position = transform.position;
        parryingObject.transform.parent = null;
        Destroy(emptyTemp);
        parryingObject.SetActive(true);

        StartCoroutine(DestroyParryingComp());
    }

    bool IsParryingValid()
    {
        return parryingObject.scene.IsValid();
    }

    IEnumerator DestroyParryingComp()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        parryingComp.DoParrying();
        Destroy(parryingObject);
        parryingObject = null;
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (!isAI)
        {
            // if object is Player, Add to List;
            if (_other.CompareTag("Monster"))
            {
                combatCompListInAttackRange.Add(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
        else
        {
            // if object is AI, try to attack.
            if (_other.CompareTag("Player") && combatCompListInAttackRange.Count == 0)
            {
                isInAttackRange = true;
                combatCompListInAttackRange.Add(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
    }

    private void OnTriggerStay2D(Collider2D _other)
    {
        if (isAttacking && _other.CompareTag("Player"))
        {
            isAttackSuccess = true;
        }
    }

    private void OnTriggerExit2D(Collider2D _other)
    {
        if (!isAI)
        {
            // if object is Player, Remove from List;
            if (_other.CompareTag("Monster"))
            {
                combatCompListInAttackRange.Remove(_other.gameObject.GetComponentInChildren<CombatComponents>());
            }
        }
        else
        {
            // if object is AI, try to chase.
            if (_other.CompareTag("Player"))
            {
                isInAttackRange = false;
                combatCompListInAttackRange.Clear();
            }
        }
    }

    public bool AITryAttackProgress()
    {
        if (isAttackCooldown)
        {
            return false;
        }
        CombatComponents playerCombatComp = combatCompListInAttackRange[0];
        if (playerCombatComp.isImmune)
        {
            return false;
        }

        return true;
    }

    public void AIStunedStopAllCoroutines()
    {
        StopAllCoroutines();
    }

    public void AIAttackProgress()
    {
        isAttackCooldown = true;
        isAttackAfterDelay = true;
        StartCoroutine(AIAttackReact());
    }

    IEnumerator AIAttackReact()
    {
        yield return new WaitForSecondsRealtime(attackReactTime);
        isAttacking = true;
        ResetMeleeAttack();
        MeleeAttack();
        StartCoroutine(CheckAttacking());
    }

    IEnumerator CheckAttacking()
    {
        yield return new WaitForSecondsRealtime(attackableTime);
        isAttacking = false;
        StartCoroutine(AIAttckAfterDelay());
    }

    IEnumerator AIAttckAfterDelay()
    {
        yield return new WaitForSecondsRealtime(attackAfterDelay);
        isAttackAfterDelay = false;
        StartCoroutine(AIAttackCooldown());
    }

    IEnumerator AIAttackCooldown()
    {
        yield return new WaitForSecondsRealtime(attackCooldown);
        isAttackCooldown = false;
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public bool IsInAttackRange()
    {
        return isInAttackRange;
    }

    public bool IsAttackSuccess()
    {
        return isAttackSuccess;
    }

    public bool IsAttackCooldown()
    {
        return isAttackCooldown;
    }

    public bool IsAttackAfterDelay()
    {
        return isAttackAfterDelay;
    }

}
