using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackspaceKey : KeyBase
{
    public override void UpdateText(ref string currentText)
    {
        int length = currentText.Length;
        if (length == 0)
        {
            return;
        }

        currentText = currentText.Remove(length - 1);
    }
}
