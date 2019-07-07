using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterKey : KeyBase
{
    [SerializeField]
    private string character;

    public override void UpdateText(ref string currentText)
    {
        currentText += character;
    }
}
