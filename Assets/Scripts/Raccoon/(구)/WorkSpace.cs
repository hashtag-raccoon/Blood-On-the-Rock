using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorkSpace : MonoBehaviour
{
    public GameObject PressUIObject;
    public GameObject workspaceUIObject;
    private bool isDIsplayed = false;
    private GameObject player;
    private bool playerInTrigger = false;

    void Start()
    {
        if (PressUIObject != null)
            PressUIObject.SetActive(false);
        if (workspaceUIObject != null)
            workspaceUIObject.SetActive(false);
    }

    void Update()
    {
        if (PressUIObject != null && PressUIObject.activeSelf)
        {
            float offsetY = 1.0f;
            PressUIObject.transform.position = transform.position + new Vector3(0, offsetY, 0);
        }

        if (player != null)
        {
            if (!isDIsplayed)
            {
                player.GetComponent<PlayerableController>().PlzStop = false;
                workspaceUIObject.SetActive(false);
            }
            else
            {
                player.GetComponent<PlayerableController>().PlzStop = true;
                workspaceUIObject.SetActive(true);
            }
        }

        if (playerInTrigger && player != null && player.GetComponent<PlayerableController>().isSelected)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isDIsplayed = !isDIsplayed;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Playerable"))
        {
            PressUIObject.SetActive(true);
            player = collision.gameObject;
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Playerable"))
        {
            if (PressUIObject != null)
                PressUIObject.SetActive(false);
            if (workspaceUIObject != null)
                workspaceUIObject.SetActive(false);

            playerInTrigger = false;
            player = null;
            isDIsplayed = false;
        }
    }
}
