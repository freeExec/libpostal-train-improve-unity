using TMPro;
using UnityEngine;

public class PlayerVersionView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private void Start()
    {
        label.text = Application.version;
    }
}
