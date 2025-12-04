using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Customer(손님) 데이터
/// </summary>
[Serializable]
public class CustomerData
{
    public int customer_id { get; private set; }
    public string customer_name { get; set; }
    public int race_id { get; private set; }
    public string personality { get; private set; }
    public bool is_vip { get; private set; }
    public string preferred_taste { get; private set; }
    public int affinity { get; private set; }
    public float tip_probability { get; private set; }
    public int avg_tip_amount { get; private set; }
    public string order_speed { get; private set; }
    public int patience_minutes { get; private set; }

    // 비주얼 데이터
    public String prefab_name { get; set; }
    public Sprite portraitSprite { get; set; } // 대화창 초상화 스프라이트

    // 생성자
    public CustomerData(int customer_id, string customer_name, int race_id, string personality, bool is_vip,
    string preferred_taste, int affinity, float tip_probability, int avg_tip_amount, string order_speed, int patience_minutes,
    String prefab_name, Sprite portraitSprite = null)
    {
        this.customer_id = customer_id;
        this.customer_name = customer_name;
        this.race_id = race_id;
        this.personality = personality;
        this.is_vip = is_vip;
        this.preferred_taste = preferred_taste;
        this.affinity = affinity;
        this.tip_probability = tip_probability;
        this.avg_tip_amount = avg_tip_amount;
        this.order_speed = order_speed;
        this.patience_minutes = patience_minutes;
        this.prefab_name = prefab_name;
        this.portraitSprite = portraitSprite;
    }
}