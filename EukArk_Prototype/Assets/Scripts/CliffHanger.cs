using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CliffHanger : MonoBehaviour
{
    public bool isTriggerTopCorner = false;
    public bool isOnGround = false;
    BoxCollider2D feetCollider2d;
    public GameObject handAttach
    {
        get; set;
    } = null;

    public bool IsOnTopCorner()
    {
        return !isOnGround && isTriggerTopCorner;
    }

    public bool IsEscapeTopCorner()
    {
        return !isTriggerTopCorner;
    }

    public void SetFeetCollider2d(BoxCollider2D _feetCollider2d)
    {
        feetCollider2d = _feetCollider2d;
    }

    private void Update()
    {
        if (feetCollider2d.IsTouchingLayers(LayerMask.GetMask("Ground")))
        {
            isOnGround = true;
        }
        else
        {
            isOnGround = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.CompareTag("TopCorner"))
        {
            isTriggerTopCorner = true;
            if(_other.transform.childCount >= 2)
            {
                if (transform.parent.GetComponent<PlayerMovement>().spriteDirection == 1)
                {
                    handAttach = _other.transform.GetChild(0).gameObject;
                }
                else
                {
                    handAttach = _other.transform.GetChild(1).gameObject;
                }
            }
            else
            {
                handAttach = _other.transform.GetChild(0).gameObject;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D _other)
    {
        if (_other.CompareTag("TopCorner"))
        {
            isTriggerTopCorner = false;
            handAttach = null;
        }
    }
}
