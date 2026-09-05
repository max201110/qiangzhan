using UnityEngine;

namespace NeonBreach
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        public const int PlayerLayer = 8;
        public static bool IsAiming => Input.GetKey(KeyCode.LeftAlt);
        public Camera View { get; private set; }
        public RifleController Weapon { get; private set; }
        public float Health { get; private set; } = 100;
        public float Stamina { get; private set; } = 1;
        public float DamageFlash { get; private set; }
        public float Sensitivity { get; set; } = 1.7f;
        public float BaseFieldOfView { get; set; } = 80;
        public Vector3 LastDamageSource { get; private set; }
        public float DamageDirectionTime { get; private set; }
        public float Movement { get; private set; }
        public bool Sprinting { get; private set; }
        public bool Crouching { get; private set; }
        GameSession session;
        CharacterController controller;
        float pitch, yaw, verticalSpeed, bob, shake;
        bool sprintExhausted;
        Vector3 cameraHome = new Vector3(0, 1.65f, 0);
        readonly Vector3 standingCamera = new Vector3(0, 1.65f, 0);
        readonly Vector3 crouchingCamera = new Vector3(0, 1.05f, 0);

        public void Initialize(GameSession game)
        {
            session = game; gameObject.layer = PlayerLayer;
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("NeonBreach.Sensitivity", 1.7f), 0.3f, 4);
            BaseFieldOfView = Mathf.Clamp(PlayerPrefs.GetFloat("NeonBreach.FOV", 80), 70, 100);
            controller = gameObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = 0.32f;
            controller.center = new Vector3(0, 0.9f, 0); controller.stepOffset = 0.3f; controller.skinWidth = 0.035f;
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(transform, false);
            View = cameraObject.AddComponent<Camera>(); View.nearClipPlane = 0.035f; View.farClipPlane = 200;
            View.fieldOfView = BaseFieldOfView; View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(0.018f, 0.035f, 0.062f);
            View.allowHDR = true; cameraObject.AddComponent<AudioListener>();
            Weapon = cameraObject.AddComponent<RifleController>(); Weapon.Initialize(game, this);
            ShowOverview();
        }
        public void ResetOperator()
        {
            controller.enabled = false; transform.position = session.World.PlayerSpawn; transform.rotation = Quaternion.identity; controller.enabled = true;
            Health = 100; Stamina = 1; Movement = 0; Sprinting = false; Crouching = false; DamageFlash = 0; pitch = 0; yaw = 0; verticalSpeed = 0; bob = 0; shake = 0; sprintExhausted = false;
            View.transform.localPosition = cameraHome; View.transform.localRotation = Quaternion.identity; View.fieldOfView = BaseFieldOfView;
            DamageDirectionTime = 0; Weapon.ResetRifle(); Weapon.SetVisible(true);
        }
        public void ShowOverview()
        {
            controller.enabled = false; transform.position = Vector3.zero; transform.rotation = Quaternion.identity;
            View.transform.position = new Vector3(17, 12, -23); View.transform.LookAt(new Vector3(0, 1.6f, 3)); View.fieldOfView = 68;
            Weapon.SetVisible(false);
        }
        void Update()
        {
            if (session.State != MatchState.Playing) return;
            DamageDirectionTime = Mathf.Max(0, DamageDirectionTime - Time.deltaTime);
            DamageFlash = Mathf.MoveTowards(DamageFlash, 0, Time.deltaTime * 1.6f);
            float lookSensitivity = Sensitivity * (PlayerMotor.IsAiming ? 0.65f : 1);
            yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity, -85, 85);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1); Movement = input.magnitude;
            if (Stamina <= 0.01f) sprintExhausted = true;
            if (Stamina >= 0.3f) sprintExhausted = false;
            bool crouchHeld = Input.GetKey(KeyCode.C);
            Crouching = crouchHeld || (Sprinting && Input.GetKeyDown(KeyCode.C));
            Sprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !PlayerMotor.IsAiming && !Crouching && !sprintExhausted;
            Stamina = Mathf.Clamp01(Stamina + (Sprinting ? -0.24f : 0.19f) * Time.deltaTime);
            float speed = Sprinting ? 8.4f : Crouching ? 2.8f : PlayerMotor.IsAiming ? 3.4f : 5.5f;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space)) { verticalSpeed = 6.4f; session.Audio.Play(SoundCue.Jump, 0.5f); }
            verticalSpeed -= 19 * Time.deltaTime;
            float targetHeight = Crouching ? 1.15f : 1.8f;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 14);
            controller.center = new Vector3(0, controller.height * 0.5f, 0);
            cameraHome = Vector3.Lerp(cameraHome, Crouching ? crouchingCamera : standingCamera, Time.deltaTime * 14);
            controller.Move((transform.TransformDirection(input) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            bob += Time.deltaTime * (Sprinting ? 15 : 10) * Movement;
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 0.7f);
            Vector3 offset = new Vector3(Mathf.Cos(bob * 0.5f) * 0.018f, Mathf.Sin(bob) * 0.025f, 0) * Movement;
            View.transform.localPosition = cameraHome + offset + Random.insideUnitSphere * shake;
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            View.fieldOfView = Mathf.Lerp(View.fieldOfView, PlayerMotor.IsAiming ? BaseFieldOfView * 0.7125f : Sprinting ? BaseFieldOfView + 7 : BaseFieldOfView, Time.deltaTime * 12);
            if (transform.position.y < -8) TakeDamage(1000);
        }
        public void Recoil(float amount) { pitch = Mathf.Clamp(pitch - amount, -85, 85); shake = 0.012f; }
        public void TakeDamage(float amount, Vector3? source = null)
        {
            if (session.State != MatchState.Playing || Health <= 0) return;
            if (source.HasValue) { LastDamageSource = source.Value; DamageDirectionTime = 1.2f; }
            Health = Mathf.Max(0, Health - Mathf.Max(0, amount)); DamageFlash = 0.65f; shake = 0.055f;
            session.Audio.Play(SoundCue.Hurt, 0.75f);
            if (Health <= 0) session.Finish(false);
        }
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("NeonBreach.Sensitivity", Sensitivity);
            PlayerPrefs.SetFloat("NeonBreach.FOV", BaseFieldOfView); PlayerPrefs.Save();
        }
        void OnApplicationQuit() { SaveSettings(); }
        public void Heal(float amount) { Health = Mathf.Min(100, Health + Mathf.Max(0, amount)); }
    }
}
