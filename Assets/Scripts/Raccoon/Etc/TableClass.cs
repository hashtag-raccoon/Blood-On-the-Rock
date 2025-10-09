using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TableClass : MonoBehaviour
{
    public TableManager tableManager;
    public bool isCustomerSeated = false; // 손님이 앉아있는지 여부
    public List<GameObject> Seated_Customer = new List<GameObject>(); // 앉아있는 손님 오브젝트
    public int MAX_Capacity = 2; // 최대 좌석 수

    private void Awake()
    {
        tableManager.tables.Add(this.gameObject);
    }
}