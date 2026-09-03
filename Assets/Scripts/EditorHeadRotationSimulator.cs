using UnityEngine;
using UnityEngine.InputSystem;

public class EditorHeadRotationSimulator : MonoBehaviour
{
    [Header("Configurações de sensibilidade")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 currentRotation = transform.localEulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;
    }

    void Update()
    {
        #if UNITY_EDITOR
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f * (invertY ? 1 : -1);

        yaw += mouseX;
        pitch += mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        #endif
    }
}