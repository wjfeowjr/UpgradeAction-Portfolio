using UnityEngine;

public class Gunner_ElementalInfusionSelect : MonoBehaviour
{
    [SerializeField] private Keystring iceKeyString;
    [SerializeField] private Keystring lightningKeyString;
    [SerializeField] private Keystring fireKeyString;

    public void SetText(string iceText, string lightningText, string fireText)
    {
        iceKeyString.SetText(iceText);
        lightningKeyString.SetText(lightningText);
        fireKeyString.SetText(fireText);
    }
}
