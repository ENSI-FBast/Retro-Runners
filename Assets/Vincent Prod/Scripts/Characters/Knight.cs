using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vincent_Prod.Scripts.Characters
{
    public class Knight : PlayerController
    {
        //Dash
        private bool _canDash = true;
        private bool _isDashing;
        public float dashPower = 12;
        private float _dashingTime = 0.2f;
        private float _dashingCooldown = 1f;
        private TrailRenderer _trail;

        //Attack
        public GameObject attackBox;
        public GameObject upAttackBox;
        public GameObject downAttackBox;

        //UI
        public Sprite portrait;

        //Visuel
        private SpriteRenderer _spriteRenderer;
        
        //Manager
        private PlayerManager _playerManager;
        public int listID;

        private void Awake() {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _jumpCount = 2;
            _trail = GetComponent<TrailRenderer>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _playerManager = FindObjectOfType<PlayerManager>();
            attackBox.SetActive(false);
            upAttackBox.SetActive(false);
            downAttackBox.SetActive(false);
            health = 150;
            deaths = 0;
            _canAttack = true;
            _rigidbody2D.velocity = Vector2.zero;
        }

        private void Start() {
            _playerManager.Players.Add(this.gameObject);
            listID = _playerManager.Players.Count;
            _spriteRenderer.color = _playerManager.Players.Count switch {
                1 => color1,
                2 => color2,
                3 => color3,
                4 => color4,
                _ => _spriteRenderer.color
            };
            upPointer.GetComponent<SpriteRenderer>().color = _spriteRenderer.color;
            leftPointer.GetComponent<SpriteRenderer>().color = _spriteRenderer.color;
            rightPointer.GetComponent<SpriteRenderer>().color = _spriteRenderer.color;
        }

        private void Update()
        {
            transform.Translate(new Vector3(movementInput.x, 0, 0) * speed * Time.deltaTime) ;
            //_rigidbody2D.AddForce(new Vector3(movementInput.x, 0, 0) * speed * Time.deltaTime, ForceMode2D.Impulse);
            transform.localScale = movementInput.x switch
            {
                < 0 => new Vector3(-1, 1, 1),
                > 0 => new Vector3(1, 1, 1),
                _ => transform.localScale
            };
            if (health <= 0)
            {
                deaths += 1;
                Respawn();
            }

            upPointer.transform.position = new Vector3(transform.position.x, 13.8f, 0);
            leftPointer.transform.position = new Vector3(-14.25f, transform.position.y, 0);
            leftPointer.transform.rotation = Quaternion.Euler(0, 0, 90);
            rightPointer.transform.position = new Vector3(14.25f, transform.position.y, 0);
            rightPointer.transform.rotation = Quaternion.Euler(0, 0, -90);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("UpOutZone"))
            {
                upPointer.SetActive(true);
            }

            if (other.CompareTag("LeftOutZone"))
            {
                leftPointer.SetActive(true);
            }

            if (other.CompareTag("RightOutZone"))
            {
                rightPointer.SetActive(true);
            }

            if (!other.CompareTag("Ground") || !groundCollider) return;
            _isGrounded = true;
            _jumpCount = 0;
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("UpOutZone"))
            {
                upPointer.SetActive(false);
            }

            if (other.CompareTag("LeftOutZone"))
            {
                leftPointer.SetActive(false);
            }

            if (other.CompareTag("RightOutZone"))
            {
                rightPointer.SetActive(false);
            }

            if (!other.CompareTag("Ground") || !groundCollider) return;
            _isGrounded = false;
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Attack") && !_damageTake || other.CompareTag("Arrow") && !_damageTake)
            {
                StartCoroutine(TakeDamage());
            }

            if (other.CompareTag("DeathZone"))
            {
                health = 0;
            }

        }
        
        public void OnMove(InputAction.CallbackContext ctx) {
            if (playerInput) {
                movementInput = ctx.ReadValue<Vector2>();
            }
        }
        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && playerInput) Jump();
        }
        public void OnDash(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && _canDash && playerInput)
            {
                switch (movementInput.x)
                {
                    case > 0 when dashPower < 0:
                    case < 0 when dashPower > 0:
                        dashPower = -dashPower;
                        break;
                }

                StartCoroutine(Dash());
            }
        }
        public void OnAttack(InputAction.CallbackContext ctx)
        {
            switch (ctx.performed)
            {
                case true when _canAttack && movementInput.y > 0.5f && playerInput:
                    StartCoroutine(AttackUp());
                    break;
                case true when _canAttack && movementInput.y < -0.5f && !_isGrounded && playerInput:
                    StartCoroutine(AttackDown());
                    break;
                case true when _canAttack && playerInput:
                    StartCoroutine(Attack());
                    break;
            }
        }
        
        
        private void Respawn() {
            transform.position = new Vector3(0, 15, 0);
            health = 150;
        }
        private void Jump() {
            if (_jumpCount > 1) return;
            //jumpForce = 650;
            //Vector2 jumpVec = new Vector2(0, jumpForce * Time.deltaTime);
            Vector2 jumpVec = new Vector2(0, jumpPower);
            _rigidbody2D.AddForce(transform.up * jumpVec, ForceMode2D.Impulse);
            _jumpCount += 1;
        }
        
        //Couroutine Dash
        private IEnumerator Dash() {
            _canDash = false;
            _isDashing = true;
            float originalGravity = _rigidbody2D.gravityScale;
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.velocity = new Vector2(dashPower, _rigidbody2D.velocity.y);
            _trail.emitting = true;
            yield return new WaitForSeconds(_dashingTime);
            _rigidbody2D.gravityScale = originalGravity;
            _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
            _trail.emitting = false;
            _isDashing = false;
            yield return new WaitForSeconds(_dashingCooldown);
            _canDash = true;
        }
        
        //Couroutines Attack
        private IEnumerator Attack() {
            _canAttack = false;
            attackBox.SetActive(true);
            yield return new WaitForSeconds(attackTime);
            attackBox.SetActive(false);
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }
        private IEnumerator AttackUp() {
            _canAttack = false;
            upAttackBox.SetActive(true);
            yield return new WaitForSeconds(attackTime);
            upAttackBox.SetActive(false);
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }
        private IEnumerator AttackDown() {
            _canAttack = false;
            downAttackBox.SetActive(true);
            yield return new WaitForSeconds(attackTime);
            downAttackBox.SetActive(false);
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }
        
        //Couroutine Damage
        private IEnumerator TakeDamage() {
            _damageTake = true;
            health -= 10;
            yield return new WaitForSeconds(_iFrame);
            _damageTake = false;
        }
    }
}