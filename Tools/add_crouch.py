from pathlib import Path
p=Path(r'D:\github\NeonBreach\Assets\NeonBreach\Scripts\Gameplay\PlayerMotor.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('public bool Sprinting { get; private set; }','public bool Sprinting { get; private set; }\n        public bool Crouching { get; private set; }')
s=s.replace('Vector3 cameraHome = new Vector3(0, 1.65f, 0);','Vector3 cameraHome = new Vector3(0, 1.65f, 0);\n        readonly Vector3 standingCamera = new Vector3(0, 1.65f, 0);\n        readonly Vector3 crouchingCamera = new Vector3(0, 1.05f, 0);')
s=s.replace('Health = 100; Stamina = 1; Movement = 0; Sprinting = false;', 'Health = 100; Stamina = 1; Movement = 0; Sprinting = false; Crouching = false;')
s=s.replace('Sprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !PlayerMotor.IsAiming && !sprintExhausted;', '''bool crouchHeld = Input.GetKey(KeyCode.C);
            Crouching = crouchHeld || (Sprinting && Input.GetKeyDown(KeyCode.C));
            Sprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !PlayerMotor.IsAiming && !Crouching && !sprintExhausted;''')
s=s.replace('float speed = Sprinting ? 8.4f : PlayerMotor.IsAiming ? 3.4f : 5.5f;', 'float speed = Sprinting ? 8.4f : Crouching ? 2.8f : PlayerMotor.IsAiming ? 3.4f : 5.5f;')
s=s.replace('controller.Move((transform.TransformDirection(input) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);', '''float targetHeight = Crouching ? 1.15f : 1.8f;
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 14);
            controller.center = new Vector3(0, controller.height * 0.5f, 0);
            cameraHome = Vector3.Lerp(cameraHome, Crouching ? crouchingCamera : standingCamera, Time.deltaTime * 14);
            controller.Move((transform.TransformDirection(input) * speed + Vector3.up * verticalSpeed) * Time.deltaTime);''')
s=s.replace('Sprinting ? 87 : BaseFieldOfView', 'Sprinting ? BaseFieldOfView + 7 : BaseFieldOfView')
s=s.replace('player.Sprinting ? 15 : 10', 'player.Sprinting ? 15 : player.Crouching ? 7 : 10')
p.write_text(s,encoding='utf-8')
p=Path(r'D:\github\NeonBreach\Assets\NeonBreach\Scripts\Presentation\ArenaHud.cs')
s=p.read_text(encoding='utf-8-sig').replace('(player.Sprinting ? "SPRINT ACTIVE" : "READY")','(player.Crouching ? "CROUCHING" : player.Sprinting ? "SPRINT ACTIVE" : "READY")')
s=s.replace('ALT  AIM      R  RELOAD      SHIFT  SPRINT      SPACE  JUMP','ALT  AIM      R  RELOAD      SHIFT  SPRINT      C  CROUCH      SPACE  JUMP')
p.write_text(s,encoding='utf-8')
p=Path(r'D:\github\NeonBreach\README.md'); s=p.read_text(encoding='utf-8-sig')
s=s.replace('| 空格 | 跳跃 |','| C（按住） | 蹲伏、降低身高和移动噪声 |\n| 空格 | 跳跃 |')
s=s.replace('SHIFT  SPRINT      SPACE  JUMP','SHIFT  SPRINT      C  CROUCH      SPACE  JUMP')
s=s.replace('左 Shift 冲刺 | Space 跳跃 | Esc 暂停 | M 静音','左 Shift 冲刺 | C 蹲伏 | Space 跳跃 | Esc 暂停 | M 静音')
p.write_text(s,encoding='utf-8')
p=Path(r'D:\github\NeonBreach\Release\NeonBreach-1.0.0-Windows\README-RELEASE.txt')
if p.exists():
 s=p.read_text(encoding='utf-8').replace('左 Shift 冲刺 | Space 跳跃 | Esc 暂停 | M 静音','左 Shift 冲刺 | C 蹲伏 | Space 跳跃 | Esc 暂停 | M 静音'); p.write_text(s,encoding='utf-8')
