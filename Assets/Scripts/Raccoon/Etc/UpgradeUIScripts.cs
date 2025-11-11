using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUIScripts : MonoBehaviour
{
    public GameObject upgradeUIBuildingImage;
    public GameObject upgradeButtonCostImage;
    public GameObject upgradeButtonCostText;
    public GameObject upgradeRequiredTimeText;
    public GameObject upgradeRequiredPanel;
    public GameObject BuildingName;
    public GameObject CurrentBuildingLevel;
    public GameObject NextBuildingLevel;
    public Button UpgradeButton;
    public Button upgradeUICloseButton;
    
    void Start()
    {
        upgradeUICloseButton.onClick.AddListener(() =>
        {
            Destroy(this.gameObject);
        });
    }
}
