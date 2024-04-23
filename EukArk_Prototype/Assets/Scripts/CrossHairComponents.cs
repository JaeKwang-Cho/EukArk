using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CrossHairComponents : MonoBehaviour
{
    SpriteRenderer crosshairSprite;
    GameObject playerObject;
    PlayerMovement playerMovement;
    CircleCollider2D aimGravityCircle;
    GameObject currAimMonster = null;
    List<Collider2D> allCollisionMonsters;

    float timer = 0f;
    float refreshRate = 0.1f;

    void Start()
    {
        crosshairSprite = GetComponent<SpriteRenderer>();
        aimGravityCircle = GetComponentInChildren<CircleCollider2D>();

        playerObject = transform.parent.gameObject;
        playerMovement = playerObject.GetComponent<PlayerMovement>();
        playerMovement.crossHair = this;
        transform.parent = null;

        allCollisionMonsters = new List<Collider2D>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if(timer > refreshRate)
        {
            timer = 0f;
            FindMonsterAroundCrossHair();
        }

        MoveCircleWithMouse();
        AimGravity();
    }

    void FindMonsterAroundCrossHair()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(LayerMask.GetMask("Monster"));
        contactFilter.IsFilteringTrigger(aimGravityCircle);

        allCollisionMonsters.Clear();
        aimGravityCircle.OverlapCollider(contactFilter, allCollisionMonsters);

        StartCoroutine(SelectMonsterToAim());
        //Debug.Log("FindMonsterAroundCrossHair()");
    }

    IEnumerator SelectMonsterToAim()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if(allCollisionMonsters.Count > 0 )
        {
            int index = Random.Range(0, allCollisionMonsters.Count);
            currAimMonster = allCollisionMonsters[index].gameObject;
        }
        else
        {
            //Debug.Log("Empty");
            currAimMonster = null;
        }
        aimGravityCircle.gameObject.transform.localPosition = Vector3.zero;
    }

    void AimGravity()
    {
        if (currAimMonster == null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0;
            transform.position = mousePos;
        }
        else
        {
            Vector3 mousePos = currAimMonster.transform.position;
            mousePos.z = 0;
            transform.position = mousePos;
        }

    }

    void MoveCircleWithMouse()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        aimGravityCircle.gameObject.transform.position = mousePos;
    }
}
