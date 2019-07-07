using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearKey : KeyBase
{
    public override void UpdateText(ref string currentText)
    {
        currentText = "";
    }
}
