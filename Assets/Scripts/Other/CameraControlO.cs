using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControlO : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public CharacterController controller;
    void MovePlayerRelativeToCamera()
    {
        //Get Player Input
        float playerVerticalInput = Input.GetAxis("Vertical");
        float playerHorizontalInput = Input.GetAxis("Horizontal");
        //Get Camera Vectors
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward = Vector3.forward.normalized;
        cameraRight = Vector3.right.normalized;

        Vector3 forwardRelativeMovementVector =
            playerVerticalInput * cameraForward;
        Vector3 rightRelativeMovementVector =
            playerHorizontalInput * cameraRight;

        Vector3 cameraRelativeMoveMent =
            forwardRelativeMovementVector + rightRelativeMovementVector;

        controller.Move(cameraRelativeMoveMent);
    }
}
