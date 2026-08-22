using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System.Collections;

namespace Connection
{
    public class PuppetMasterPlayerMissile : NetworkBehaviour
    {
        [Header("Movement")]
        public float fallSpeed = 12f;
        public float lateralSpeed = 8f;
        public float maxLateralAngle = 15f;

        [Header("References")]
        public SpriteRenderer sr;
        public Collider2D col;

        private Joystick joystick;
        private bool isLateralMode;
        private bool isDead = false;

        public override void OnStartLocalPlayer()
        {
            isLateralMode = PlayerPrefs.GetInt("MovementMode", 0) == 1;
            
            // Trouver le joystick dans la scene (UI)
            joystick = FindObjectOfType<Joystick>(); 
            if (joystick != null)
            {
                joystick.gameObject.SetActive(!isLateralMode);
            }

            // Faire en sorte que la camera suive CE missile (Cinemachine)
            if (UIManager.instance != null && UIManager.instance.vcam != null)
            {
                UIManager.instance.vcam.Follow = transform;
                UIManager.instance.vcam.LookAt = transform;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || isDead) return;

            // 1. Chute constante vers le bas
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // 2. Input Lateral (comme un avion)
            float moveX = 0f;
            if (isLateralMode)
            {
                if (Pointer.current != null && Pointer.current.press.isPressed)
                {
                    float xPos = Pointer.current.position.ReadValue().x;
                    moveX = (xPos < Screen.width / 2f) ? -1f : 1f;
                }
            }
            else
            {
                if (joystick != null) moveX = joystick.Horizontal;
            }

            // Application du mouvement lateral
            transform.position += new Vector3(moveX * lateralSpeed * Time.deltaTime, 0, 0);

            // 3. Inclinaison visuelle pour le "Juice"
            float targetAngle = -moveX * maxLateralAngle;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), 10f * Time.deltaTime);
        }

        [ServerCallback]
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isDead) return;

            if (collision.CompareTag("Player"))
            {
                isDead = true;
                
                // Enregistrer le score pour le Missile (J2 au Round 1, ou J1 au Round 2)
                PuppetMasterManager.instance.RegisterKill(connectionToClient);
                
                // Declenche la destruction/respawn du Pilote
                PlayerMovementOnline pilot = collision.GetComponent<PlayerMovementOnline>();
                if (pilot != null)
                {
                    pilot.DieAndRespawn();
                }

                ExplodeAndRespawn();
            }
            else if (collision.CompareTag("Enemy") || collision.CompareTag("BoundaryBottom")) 
            {
                // Si on sort de l'ecran ou on touche le decor -> on rate, on respawn sans donner de point
                isDead = true;
                ExplodeAndRespawn();
            }
        }

        [Server]
        private void ExplodeAndRespawn()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            // Cache le missile sur tous les clients
            RpcHide();
            
            // Attente de 2 secondes (Cooldown)
            yield return new WaitForSeconds(2f);
            
            // Demande au NetworkManager de spawner un NOUVEAU missile pour ce client
            if (TooCloseNetworkManager.instance != null)
            {
                TooCloseNetworkManager.instance.RespawnPuppetMissile(connectionToClient);
            }
            
            // Detruit le vieil objet
            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        private void RpcHide()
        {
            isDead = true;
            if (sr != null) sr.enabled = false;
            if (col != null) col.enabled = false;
            
            // FX d'explosion ici (Instantiate explosion prefab)
        }
    }
}
