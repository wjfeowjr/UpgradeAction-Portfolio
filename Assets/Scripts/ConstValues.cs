using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstValues : Singleton<ConstValues>
{
    public const float BasicGravity = 1.0f;
    public const float DownSecond = 0.9f;
    public const float ReboundSecond = 0.05f;
    public const float ReboundForce = 2.5f;
    public const float KnockBackTime = 0.1f;
    public const float GrabbedSpeed = 30.0f;
    public const float GrabbedBoundX = 8.0f;
    public const float GrabbedBoundY = 10.0f;
    public const float WhiteSecond = 0.05f;
    public const float GaugeReduce = 0.3f;
    public const float GaugeFillSpeed = 0.02f;

    public const float JumpCoolTime = 1.0f;
    public const float DownJumpCoolTime = 1.0f;
    
    public const float DefaultLinearDamping = 0f;
    public const float DefaultAngularDamping = 0.05f;
    
    public const string PrefabFolder = "Assets/Prefab";

    public const string AtlasClone = "(Clone)";

    public const string TitleScene = "Title";
    public const string BattleScene = "Battle";

    public const string All = "All";
    public const string Shift = "Shift";
    
    public const string LeftMoveKey = "LeftMoveKey";
    public const string RightMoveKey = "RightMoveKey";
    public const string AttackKey = "AttackKey";
    public const string JumpKey = "JumpKey";
    public const string DownKey = "DownKey";
    public const string DashKey = "DashKey";
    public const string SkillKey1 = "SkillKey1";
    public const string SkillKey2 = "SkillKey2";
    public const string SkillKey3 = "SkillKey3";
    public const string SkillKey4 = "SkillKey4";
    public const string SkillKey5 = "SkillKey5";
    public const string SkillKey6 = "SkillKey6";
    public const string SkillKey7 = "SkillKey7";
    public const string SkillKey8 = "SkillKey8";

    public const string OptionKey = "OptionKey";
    
    public const string TotalBar = "TotalBar";
    public const string TextFont = "TextFont";
    public const string SkillImage = "SkillImage";
    public const string SpeechFrame = "SpeechFrame";
    public const string Effect = "Effect";

    public const string Berserker = "Berserker";
    public const string Gunner = "Gunner";

    public const string Idle = "Idle";
    public const string Move = "Move";
    public const string Jump = "Jump";
    public const string JumpDown = "JumpDown";
    public const string Attack = "Attack";
    public const string Attack1 = "Attack1";
    public const string Attack2 = "Attack2";
    public const string Attack3 = "Attack3";
    public const string Attack3Ready = "Attack3_Ready";
    public const string JumpAttack1 = "JumpAttack1";
    public const string JumpAttack2 = "JumpAttack2";
    public const string JumpAttack2Start = "JumpAttack2_Start";
    public const string JumpAttack2Drop = "JumpAttack2_Drop";
    public const string JumpAttack2End = "JumpAttack2_End";
    
    // 대화 애니메이션
    public const string DialogJump = "DialogJump";

    public const string None = "None";
    public const string Normal = "Normal";
    public const string Pattern = "Pattern";
    public const string Appear = "Appear";
    public const string AppearEnd = "AppearEnd";
    
    public const string Missile = "Missile";
    public const string Grenade = "Grenade";
    public const string Player = "Player";
    public const string Monster = "Monster";

    public const string Skill = "Skill";
    public const string PlayerSkill = "PlayerSkill";
    public const string Dash = "Dash";
    public const string DashEffect = "DashEffect";

    public const string Stopping = "Stopping";
    public const string Moving = "Moving";
    public const string Grabbed = "Grabbed";
    public const string Airborne = "Airborne";
    public const string AirborneDown = "AirborneDown";
    public const string Stun = "Stun";
    public const string Down = "Down";
    public const string Damaged = "Damaged";
    public const string Die = "Die";
    
    public const string Landing = "Landing";
    public const string Jumping = "Jumping";
    
    public const string ComboAttack = "ComboAttack";
    public const string FinalAttack = "FinalAttack";
    public const string ChangeAttack = "ChangeAttack";
    
    public const string Ground = "Ground";
    public const string Wall = "Wall";
    public const string Platform = "Platform";
    
    public const string SpawnedObject = "SpawnedObject";
    public const string Animations = "Animations";

    public const string MoveSpeed = "MoveSpeed";
    public const string AttackSpeed = "AttackSpeed";

    public const string ChangeCharacter = "ChangeCharacter";
    public const string ChangeCharacterKey = "ChangeCharacter";
    
    public const string BerserkerFlash = "Berserker_Flash";
    public const string BerserkerAttack1 = "Berserker_Attack1";
    public const string BerserkerAttack2 = "Berserker_Attack2";
    public const string BerserkerAttack3 = "Berserker_Attack3";
    public const string BerserkerJumpAttack1 = "Berserker_JumpAttack1";
    public const string BerserkerJumpAttack2 = "Berserker_JumpAttack2";
    public const string BerserkerJumpAttack2Effect = "Berserker_JumpAttack2_Effect";

    public const string BerserkerDash = "Berserker_Dash";
    public const string BerserkerUpperSlash = "Berserker_UpperSlash";
    public const string BerserkerCrash = "Berserker_Crash";
    public const string BerserkerCrashSmash = "Berserker_Crash_Smash";
    public const string BerserkerCrashExplosion = "Berserker_Crash_Explosion";
    public const string BerserkerFireStrike = "Berserker_FireStrike";
    public const string BerserkerChargeCrash = "Berserker_ChargeCrash";
    public const string BerserkerChargeCrashSlash = "Berserker_ChargeCrash_Slash";
    public const string BerserkerChargeCrashSmash = "Berserker_ChargeCrash_Smash";
    public const string BerserkerChargeCrashSmashEffect = "Berserker_ChargeCrash_SmashEffect";
    
    public const string GunnerFlash = "Gunner_Flash";
    public const string GunnerAttack1Object = "Gunner_Attack1Object";
    public const string GunnerAttackEffect1 = "Gunner_AttackEffect1";
    public const string GunnerAttack2Object = "Gunner_Attack2Object";
    public const string GunnerAttackEffect2 = "Gunner_AttackEffect2";

    public const string GunnerDash = "Gunner_Dash";
    public const string GunnerDashShot = "Gunner_DashShot";
    public const string GunnerGrenade = "Gunner_Grenade";
    public const string GunnerGrenadeObject = "Gunner_Grenade_Object";
    public const string GunnerKnockBackShot = "Gunner_KnockBackShot";
    public const string GunnerKnockBackShotReady = "Gunner_KnockBackShot_Ready";
    public const string GunnerCrazyShot = "Gunner_CrazyShot";
    public const string GunnerCrazyShotEffect = "Gunner_CrazyShot_Effect";
    public const string GunnerBigShot = "Gunner_BigShot";
    public const string GunnerBigShotReady = "Gunner_BigShot_Ready";

    public const string UIPool = "UIPool";
    public const string PopupPool = "PopupPool";
    
    public const string Prefab = ".prefab";
    
    public static readonly Color WhiteColor = Color.white;
    public static readonly Color BlackColor = Color.black;
    public static readonly Color GrayColor = Color.gray;
    public static readonly Color RedColor = Color.red;
    public static readonly Color OrangeColor = new Color(1, 0.55f, 0);
    public static readonly Color YellowColor = Color.yellow;
    public static readonly Color GreenColor = Color.green;
    public static readonly Color BlueColor = Color.blue;
    public static readonly Color CyanColor = Color.cyan;
    public static readonly Color MagentaColor = Color.magenta;
    
    // 사운드
    public const string GunnerLaugh = "Gunner_Laugh";
}
