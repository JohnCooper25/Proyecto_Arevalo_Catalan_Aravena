using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    public float moveSpeed = 15f;
    public float fastMoveSpeed = 50f;
    public float lookSensitivity = 2f;
    public Terrain biomaPermitido;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
        // 1. CONTROL DEL RATÓN: Solo rotamos la cámara si mantenemos pulsado el CLIC DERECHO
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotationX += -Input.GetAxis("Mouse Y") * lookSensitivity;
            rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            // Si soltamos el clic, liberamos el ratón para usar la interfaz
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. MOVIMIENTO (Con WASD)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float moveY = 0f;

        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) moveY = 1f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) moveY = -1f;

        Vector3 move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;
        Vector3 nuevaPosicion = transform.position + move * currentSpeed * Time.deltaTime;

        // 3. SISTEMA DE LÍMITES
        if (biomaPermitido != null)
        {
            Vector3 minBounds = biomaPermitido.transform.position;
            Vector3 maxBounds = minBounds + biomaPermitido.terrainData.size;

            nuevaPosicion.x = Mathf.Clamp(nuevaPosicion.x, minBounds.x, maxBounds.x);
            nuevaPosicion.z = Mathf.Clamp(nuevaPosicion.z, minBounds.z, maxBounds.z);

            float alturaSuelo = biomaPermitido.SampleHeight(nuevaPosicion) + minBounds.y;
            if (nuevaPosicion.y < alturaSuelo + 2f) nuevaPosicion.y = alturaSuelo + 2f;
        }

        transform.position = nuevaPosicion;
    }
}