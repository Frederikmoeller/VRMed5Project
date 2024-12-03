using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public List<NPCGroup> npcGroups = new List<NPCGroup>();
    public bool BigTestingDay = true;
    [SerializeField] private InputActionReference _lookButtonAction;
    public bool _lookButtonPressed;
    public bool _lookingAtPlayer;

    // Update groups based on proximity
    void Update()
    {
        ManageGroups();
        OnLookButtonPressed();
    }
    
    private void OnLookButtonPressed()
    {
        // Set the NPCs to look at the player
        _lookButtonPressed = _lookButtonAction.action.IsPressed();
    }

    void ManageGroups()
    {
        // Clear previous groupings
        foreach (var group in npcGroups)
        {
            if (!_lookingAtPlayer)
            {
                group.UpdateSpeakerRotation(); // Update each group's speaker rotation logic
            }
        }

        // Find new groupings based on proximity
        Collider[] npcs = Physics.OverlapSphere(transform.position, 15f, LayerMask.GetMask("NPC"));
        foreach (var npcCollider in npcs)
        {
            NPCFOV npcFov = npcCollider.GetComponent<NPCFOV>();
            if (npcFov != null)
            {
                bool addedToGroup = false;
                foreach (var group in npcGroups)
                {
                    if (group.IsNear(npcFov))
                    {
                        group.AddToGroup(npcFov);
                        npcFov.CurrentGroup = group;  // Set the NPC's group reference
                        addedToGroup = true;
                        break;
                    }
                }

                // If NPC isn't part of any group, create a new group for them
                if (!addedToGroup)
                {
                    NPCGroup newGroup = new NPCGroup();
                    newGroup.AddToGroup(npcFov);
                    npcFov.CurrentGroup = newGroup;  // Set the NPC's group reference
                    npcGroups.Add(newGroup);
                }
            }
        }
    }
}

[System.Serializable]
public class NPCGroup
{
    public List<NPCFOV> members = new List<NPCFOV>();
    public NPCFOV currentSpeaker;
    private float timer = 0f; // Timer for the speaker change

    // Check if the NPC is close enough to be part of this group
    public bool IsNear(NPCFOV npc)
    {
        foreach (var member in members)
        {
            if (Vector3.Distance(npc.transform.position, member.transform.position) < 5f)
            {
                return true;
            }
        }
        return false;
    }

    // Add an NPC to the group
    public void AddToGroup(NPCFOV npc)
    {
        if (!members.Contains(npc))
        {
            members.Add(npc);
        }
    }

    // Remove NPC from the group
    public void ClearGroup()
    {
        members.Clear();
    }

    // Assign a speaker (this can be randomized or based on other logic)
    public void AssignSpeaker()
    {
        if (members.Count > 0)
        {
            int index = Random.Range(0, members.Count);
            currentSpeaker = members[index];
            currentSpeaker.speakerDuration = Random.Range(2, 9);
            members[index]._isTalking = true;
            
        }
    }

    public void DeassignSpeaker()
    {
        if (currentSpeaker != null)
        {
            currentSpeaker._isTalking = false;
            currentSpeaker = null;
        }
    }
    
    // Update logic for rotating the speaker
    public void UpdateSpeakerRotation()
    {
        timer += Time.deltaTime;

        if (timer >= (currentSpeaker?.speakerDuration ?? 0f))
        {
            // Assign a new speaker when the timer is up
            if (members.Count > 1)
            {
                if (currentSpeaker != null)
                {
                    DeassignSpeaker();  // Remove the previous speaker
                }
                AssignSpeaker();  // Assign a new speaker
            }
            timer = 0f; // Reset the timer
        }
    }
}
