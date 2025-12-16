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
    public const float WhiteSecond = 0.075f;
    public const float GaugeReduce = 0.3f;

    public const float JumpCoolTime = 1.0f;
    public const float DownJumpCoolTime = 1.0f;
    
    public const float DefaultLinearDamping = 0f;
    public const float DefaultAngularDamping = 0.05f;

    public const float BungeePosY = -8.0f;

    public const float BlinkSecond = 0.1f; // 사라질떄 깜빡이는거
    
    public const float DialogDelay1 = 2.5f;
    public const float DialogDelay2 = 1.0f;
    
    public const string PrefabFolder = "Assets/Prefab";
    public const string SoundFolder = "Assets/Sound";
    public const string RoomFolder = "Assets/Room";
    
    public const string AtlasClone = "(Clone)";
    public const string BgSunHill = "Bg_SunHill";
    public const string BgSunHillNight = "Bg_SunHill_Night";

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
    public const string SkillTooltip = "SkillTooltip";
    public const string SpeechFrame1 = "SpeechFrame1";
    public const string SpeechFrame2 = "SpeechFrame2";
    public const string SpeechFrameStrong = "SpeechFrame_Strong";
    public const string SpeechFrameTitle = "SpeechFrame_Title";

    public const string BangEffect = "BangEffect";
    public const string Effect = "Effect";
    public const string FadeBg = "FadeBg";
    public const string FadeUI = "FadeUI";
    public const string AttributeUpEffect = "AttributeUpEffect";
    public const string AttributeDownEffect = "AttributeDownEffect";
    
    public const string Explosion = "Explosion";
    public const string DashEffectUI = "DashEffectUI";
    public const string DashFrameUI = "DashFrameUI";
    public const string Guide = "Guide";

    public const string Object = "Object";

    public const string Berserker = "Berserker";
    public const string Gunner = "Gunner";
    
    public const string Idle = "Idle";
    public const string Move = "Move";
    public const string Jump = "Jump";
    public const string JumpDown = "JumpDown";
    public const string Leap = "Leap";
    public const string Attack = "Attack";
    public const string Attack1 = "Attack1";
    public const string Attack2 = "Attack2";
    public const string Attack3 = "Attack3";
    public const string Attack3Ready = "Attack3_Ready";
    public const string JumpAttack1 = "JumpAttack1";
    public const string JumpAttack2 = "JumpAttack2";
    public const string JumpAttack3 = "JumpAttack3";
    public const string JumpAttack2Start = "JumpAttack2_Start";
    public const string JumpAttack2Drop = "JumpAttack2_Drop";
    public const string JumpAttack2End = "JumpAttack2_End";
    public const string Event = "Event";
    public const string PunchPose = "PunchPose";
    public const string LandingPose = "LandingPose";
    public const string JumpPose = "JumpPose";
    
    // 대화 애니메이션
    public const string DialogJump = "DialogJump";
    public const string DialogPose = "DialogPose";
    public const string DialogShot = "DialogShot";
    public const string DialogGround = "DialogGround";
    public const string DialogGroundLaugh = "DialogGroundLaugh";
    public const string Arrive = "Arrive";
    public const string Thumbs = "Thumbs";
    public const string Point = "Point";

    public const string None = "None";
    public const string Normal = "Normal";
    public const string Pattern = "Pattern";
    public const string Appear = "Appear";
    public const string AppearEnd = "AppearEnd";
    
    public const string SuperArmor = "SuperArmor";
    public const string HighUpper = "HighUpper";
    public const string JumpUpper = "JumpUpper";
    public const string PiercingFire = "PiercingFire";
    public const string Pierce = "Pierce";
    
    public const string Missile = "Missile";
    public const string Grenade = "Grenade";
    public const string Player = "Player";
    public const string Monster = "Monster";
    public const string WallBody = "WallBody";
    
    // 룸 하위 오브젝트 이름들
    public const string MonsterArray = "MonsterArray";
    public const string BossArray = "BossArray";
    public const string NpcArray = "NpcArray";
    public const string TrapArray = "TrapArray";
    public const string PlayerPosArray = "PlayerPosArray";
    public const string EntranceArray = "EntranceArray";
    public const string BossGateArray = "BossGateArray";
    public const string ProductTriggerArray = "ProductTriggerArray";
    public const string ShortcutArray = "ShortcutArray";
    public const string GroundGrid = "GroundGrid";
    public const string ShortcutTileMap = "ShortcutTileMap";
    public const string LeftPlayerPos = "LeftPlayerPos";
    public const string RightPlayerPos = "RightPlayerPos";
    public const string UpPlayerPos = "UpPlayerPos";
    public const string DownPlayerPos = "DownPlayerPos";
    public const string LeftEntrance = "LeftEntrance";
    public const string RightEntrance = "RightEntrance";
    public const string UpEntrance = "UpEntrance";
    public const string DownEntrance = "DownEntrance";
    public const string LeftBossGate = "LeftBossGate";
    public const string RightBossGate = "RightBossGate";
    public const string UpBossGate = "UpBossGate";
    public const string DownBossGate = "DownBossGate";
    
    public const string Skill = "Skill";
    public const string SkillAttribute = "SkillAttribute";
    public const string PlayerSkill = "PlayerSkill";
    public const string PlayerSkillKey = "PlayerSkillKey";
    public const string Dash = "Dash";
    public const string DashEffect = "DashEffect";
    
    public const string TreasureBoxOpen = "TreasureBox_Open";
    public const string TreasureBoxClose = "TreasureBox_Close";
    
    public const string Rooms = "Rooms";
    public const string Minimap = "Minimap";
    public const string MiniMapVisitedCells = "MiniMapVisitedCells";
    public const string MiniMapShortcutCells = "MiniMapShortcutCells";
    public const string MiniMapCheckers = "MiniMapCheckers";
    public const string SavePoint = "SavePoint";
    public const string SaveObject = "SaveObject";
    public const string InteractionUI = "InteractionUI";
    public const string InteractionSelectUI = "InteractionSelectUI";
    
    public const string Npc = "Npc";
    public const string Dialogue = "Dialogue";
    public const string DialogueChoice = "DialogueChoice";
    public const string ProductDialogue = "ProductDialogue";
    public const string Talk = "Talk";
    public const string TreasureBox = "TreasureBox";

    public const string FirstCharacter = "FirstCharacter";
    public const string SecondCharacter = "SecondCharacter";
    
    public const string Episode1Title = "Episode1_Title";
    public const string Episode2Title = "Episode2_Title";
    public const string FirstGetSkill = "FirstGetSkill";
    public const string FirstGetAttribute = "FirstGetAttribute";
    public const string Product1 = "Product1";
    public const string Product2 = "Product2";
    public const string Product3 = "Product3";
    public const string Product4 = "Product4";
    public const string Product5 = "Product5";
    
    public const string Stopping = "Stopping";
    public const string Moving = "Moving";
    public const string Grabbed = "Grabbed";
    public const string Airborne = "Airborne";
    public const string AirborneDown = "AirborneDown";
    public const string Stun = "Stun";
    public const string Down = "Down";
    public const string Damaged = "Damaged";
    public const string Die = "Die";
    public const string Stagger = "Stagger";
    public const string StaggerExplosionUI = "StaggerExplosionUI";
    
    public const string Landing = "Landing";
    public const string Jumping = "Jumping";
    
    public const string ComboAttack = "ComboAttack";
    public const string FinalAttack = "FinalAttack";
    public const string ChangeAttack = "ChangeAttack";
    
    public const string Ground = "Ground";
    public const string Trap = "Trap";
    public const string Stage = "Stage";
    public const string Flip = "Flip";
    public const string Stop = "Stop";
    public const string StageWallLeft = "StageWallLeft";
    public const string StageWallRight = "StageWallRight";
    public const string Platform = "Platform";
    public const string DestroyPlatform = "DestroyPlatform";

    public const string Gold = "Gold";
    public const string FollowGold = "FollowGold";
    public const string GoldExplosion = "GoldExplosion";
    public const string SlotEquip = "SlotEquip";
    public const string AttributePoint = "AttributePoint";
    
    public const string GetSkill = "GetSkill";
    public const string GetSkillExplosion = "GetSkillExplosion";
    public const string ShortcutCrashEffect = "ShortcutCrashEffect";
    public const string ShortcutCrashExplosion = "ShortcutCrashExplosion";
    
    public const string SpawnedObject = "SpawnedObject";
    public const string Animations = "Animations";

    public const string MoveSpeed = "MoveSpeed";
    public const string AttackSpeed = "AttackSpeed";
    
    public const string ChangeCharacter = "ChangeCharacter";
    public const string ChangeCharacterKey = "ChangeCharacterKey";
    
    // 직업 공용
    public const string Face = "Face";
    public const string DownDust = "DownDust";
    public const string FireFlash = "FireFlash";
    public const string GreenFlash = "GreenFlash";
    public const string Warning = "Warning";
    public const string WarningArea = "WarningArea";
    public const string PlatformFragments = "PlatformFragments";
    public const string PlatformExplosion = "PlatformExplosion";

    public const string BerserkerSlash = "Berserker_Slash";
    public const string BerserkerFlash = "Berserker_Flash";
    public const string BerserkerAttack1 = "Berserker_Attack1";
    public const string BerserkerAttack2 = "Berserker_Attack2";
    public const string BerserkerAttack3 = "Berserker_Attack3";
    public const string BerserkerJumpAttack1 = "Berserker_JumpAttack1";
    public const string BerserkerJumpAttack2 = "Berserker_JumpAttack2";
    public const string BerserkerJumpAttack2Effect = "Berserker_JumpAttack2_Effect";
    public const string BerserkerAttackHitCrit = "Berserker_Attack_Hit_Crit";
    public const string BerserkerCrashHitEffect = "Berserker_Crash_HitEffect";

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
    public const string GunnerAttackHitCrit = "Gunner_Attack_Hit_Crit";

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
    
    public const string FighterStrongPunch = "Fighter_StrongPunch";

    public const string UIPool = "UIPool";
    public const string PopupPool = "PopupPool";
    public const string Prefab = ".prefab";

    // 몬스터
    public const string MonsterSpinach = "Monster_Spinach";
    public const string MonsterCoal = "Monster_Coal";
    public const string MonsterPurple = "Monster_Purple";
    public const string MonsterCharge = "Monster_Charge";
    public const string MonsterChargeEventJumpEffect = "Monster_Charge_EventJumpEffect";
    public const string MonsterIceWizzardAttack = "Monster_IceWizzard_Attack";

    public const string MonsterSun = "Monster_Sun";
    public const string MonsterSunAttack1 = "Monster_Sun_Attack1";
    public const string MonsterSunAttack2 = "Monster_Sun_Attack2";
    public const string MonsterSunAttack2SpinObject = "Monster_Sun_Attack2_SpinObject";
    public const string MonsterSunPillar = "Monster_Sun_Pillar";
    public const string MonsterSunLaugh = "Monster_Sun_Laugh";
    
    public const string MonsterMoon = "Monster_Moon";
    public const string MonsterMoonEffect = "Monster_Moon_Effect";
    public const string MonsterMoonAttack1Object = "Monster_Moon_Attack1_Object";
    public const string MonsterMoonAttack2 = "Monster_Moon_Attack2";
    public const string MonsterMoonAttack2SpinObject = "Monster_Moon_Attack2_SpinObject";
    public const string MonsterMoonAttack3 = "Monster_Moon_Attack3";
    public const string MonsterMoonAttack3DelayObject = "Monster_Moon_Attack3_DelayObject";
    
    public const string MonsterBigCharge = "Monster_BigCharge";
    public const string Meteor = "Meteor";
    
    public const string MonsterMouseCursorWarning = "Monster_MouseCursor_Warning";

    // Npc
    public const string NpcCitizen = "Npc_Citizen";
    public const string NpcSystem = "Npc_System";
    public const string NpcGameSystem = "Npc_GameSystem";
    public const string NpcGameSystemDie = "NPC_GameSystem_Die";

    // 함정
    public const string TrapPillar = "Trap_Pillar";
    
    public static readonly Color WhiteColor = Color.white;
    public static readonly Color WhiteColorAlpha0 = new Color(1, 1, 1, 0);
    public static readonly Color BlackColor = Color.black;
    public static readonly Color GrayColor = Color.gray;
    public static readonly Color RedColor = Color.red;
    public static readonly Color OrangeColor = new Color(1, 0.55f, 0);
    public static readonly Color YellowColor = Color.yellow;
    public static readonly Color GreenColor = Color.green;
    public static readonly Color BlueColor = Color.blue;
    public static readonly Color CyanColor = Color.cyan;
    public static readonly Color MagentaColor = Color.magenta;

    public const string Episode = "Episode";
    public const string DialogStep = "DialogStep";
    public const string CustomMoveStep = "CustomMoveStep";
    public const string PlayerStep = "PlayerStep";
    public const string CurStep = "CurStep";
    public const string Combo = "Combo";
    
    public const string Episode1 = "Episode1";
    public const string Episode2 = "Episode2";
    
    // 아이콘
    public const string IconAttributePoint = "Icon_AttributePoint";

    // BGM
    public const string BGMTitle = "BGM_Title";
    public const string BGMEpisodeStart = "BGM_EpisodeStart";
    public const string BGMSunHill = "BGM_SunHill";
    public const string BGMUnderGround = "BGM_Underground";
    public const string BGMEpisode2 = "BGM_Episode2";
    public const string BGMEpisode2Battle = "BGM_Episode2Battle";
    
    // 사운드
    public const string Laugh = "Laugh";
    public const string Scream = "Scream";
    public const string GunnerLaugh = "Gunner_Laugh";
    public const string PlayerDamaged1 = "Player_Damaged1";
    public const string PlayerScream = "Player_Scream";
    public const string Upgrade = "Upgrade";
    public const string Jump1 = "Jump1";
    public const string Jump2 = "Jump2";
    public const string NormalButton = "NormalButton";
    public const string NormalButton2 = "NormalButton_2";
    public const string Popup = "Popup";
    public const string RewardPage = "RewardPage";
    public const string SpeechFrame = "SpeechFrame";
    public const string WarningSound = "WarningSound";
    public const string ChickenCock = "ChickenCock";
    public const string PlayerFlash = "Player_Flash";
    public const string MonsterBigTreeLog = "Monster_BigTree_Log";
}
