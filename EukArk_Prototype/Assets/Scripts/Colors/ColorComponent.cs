using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorComponent : MonoBehaviour
{
    [SerializeField] protected Color baseColor = Color.white;
    protected Color currentColor;
    [SerializeField] protected int totalColorGauge = 4;
    public int currentColorGauge = 1;
    protected bool bFilledColor = false;

    void Start()
    {
        currentColor = new Color(baseColor.r, baseColor.g, baseColor.b, currentColorGauge / totalColorGauge);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void TryToFillGauge(Color _inputColor, int inputGauge)
    {
        if(baseColor == _inputColor)
        {
            currentColorGauge += inputGauge;
            if(totalColorGauge <= currentColorGauge)
            {
                bFilledColor = true;
                Debug.Log("Color good");
            }
        }
    }

}
