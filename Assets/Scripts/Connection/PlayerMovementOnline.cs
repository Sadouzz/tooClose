using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;
using Mirror;

namespace Connection
{
    public class PlayerMovementOnline : NetworkBehaviour
    {
        public enum GamePhase { Dodging, Shooting }

        [Header("Game Phase")]
        public GamePhase currentPhase = GamePhase.Dodging;

        public Joystick joystick;
        public float speed = 6f, rotationSpeed = 5f;
        public Rigidbody2D rb;

        [Header("Movement Settings")]
        public bool isLateralMode; // false = Joystick, true = Lateral

        [Header("Juice Settings")]
        public float maxTiltAngle = 20f;
        public float tiltSpeed = 10f;

        [Header("Player Audio")]
        public AudioClip shootSound;
        private AudioSource audioSource;

        public SpriteRenderer sr;
        public BoxCollider2D bc;
        public bool move = true;
        bool isInvincible;
        private float targetLateralAngle;

        public Transform cameraTargetProxy;
        
        [HideInInspector]
        public float cameraProxyY;

        private GamePhase lastPhase = GamePhase.Dodging;

        public override void OnStartLocalPlayer()
        {
            // Initialisation uniquement pour le joueur local
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.2f;

            targetLateralAngle = transform.eulerAngles.z;
            RefreshMovementMode();
            FollowProxy();
        }

        public void FollowProxy()
        {
            if (!isLocalPlayer) return;

            GameObject proxyObj = new GameObject("CameraFollowProxy");
            cameraTargetProxy = proxyObj.transform;
            cameraTargetProxy.position = transform.position;

            if (UIManager.instance != null && UIManager.instance.vcam != null)
            {
                UIManager.instance.vcam.Follow = cameraTargetProxy;
                UIManager.instance.vcam.LookAt = cameraTargetProxy;
            }
        }

        public void RefreshMovementMode()
        {
            isLateralMode = PlayerPrefs.GetInt("MovementMode", 0) == 1;
            
            // Le joystick doit etre assigne dans la scene
            if (joystick != null)
                joystick.gameObject.SetActive(!isLateralMode);
        }

        void Update()
        {
            // ----------------------------------------------------
            // IMPORTANT : Seul le joueur local execute les inputs !
            // La position sera synchronisee par le NetworkTransform
            // ----------------------------------------------------
            if (!isLocalPlayer) return;

            // Avance constante vers le "haut" local du vaisseau
            // Note: En multi, il faut s'assurer que "Inventory.instance.dead" est bien gere localement
            transform.position += transform.up * speed * Time.deltaTime;

            // Update proxy camera
            if (cameraTargetProxy != null)
            {
                cameraTargetProxy.position = transform.position;
            }

            if (!move) return;

            if (currentPhase == GamePhase.Dodging)
            {
                if (isLateralMode) HandleLateralMovement();
                else HandleJoystickMovement();
            }

            ApplyVisualTilt();
        }

        void ApplyVisualTilt()
        {
            float tiltTarget = 0f;

            if (isLateralMode)
            {
                if (Pointer.current != null && Pointer.current.press.isPressed)
                {
                    float xPos = Pointer.current.position.ReadValue().x;
                    tiltTarget = (xPos < Screen.width / 2f) ? maxTiltAngle : -maxTiltAngle;
                }
            }
            else
            {
                if (joystick != null)
                {
                    tiltTarget = -joystick.Horizontal * maxTiltAngle;
                }
            }

            Quaternion targetRotation = Quaternion.Euler(0, tiltTarget, 0);
            sr.transform.localRotation = Quaternion.Lerp(sr.transform.localRotation, targetRotation, tiltSpeed * Time.deltaTime);
        }

        void HandleJoystickMovement()
        {
            if (joystick == null) return;

            Vector2 direction = new Vector2(joystick.Horizontal, joystick.Vertical);
            if (direction.magnitude > 0.3f)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                float smoothAngle = Mathf.LerpAngle(
                    transform.eulerAngles.z,
                    targetAngle,
                    rotationSpeed * Time.deltaTime
                );
                transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
            }
        }

        void HandleLateralMovement()
        {
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                float xPos = Pointer.current.position.ReadValue().x;
                if (xPos < Screen.width / 2f) targetLateralAngle += rotationSpeed * Time.deltaTime * 50f;
                else targetLateralAngle -= rotationSpeed * Time.deltaTime * 50f;
            }

            float smoothAngle = Mathf.LerpAngle(
                transform.eulerAngles.z,
                targetLateralAngle,
                rotationSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
        }
    }
}
