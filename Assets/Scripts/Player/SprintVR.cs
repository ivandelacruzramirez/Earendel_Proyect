using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class SprintVR : MonoBehaviour
{
    public ContinuousMoveProvider moveProvider;

    public float normalSpeed = 1f;
    public float sprintSpeed = 3f;

    public InputActionReference sprintAction;

    void Update()
    {
        if (sprintAction.action.IsPressed())
        {
            moveProvider.moveSpeed = sprintSpeed;
        }
        else
        {
            moveProvider.moveSpeed = normalSpeed;
        }
    }
}