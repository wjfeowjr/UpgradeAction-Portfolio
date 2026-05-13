using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstValues : Singleton<ConstValues>
{
    public const float BasicGravity = 1.0f;
    public const float DownSecond = 0.9f;
    public const float ReboundSecond = 0.05f;
    public const float ReboundForce = 3.0f;
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

    public const string User = "user";
    public const string Foot = "foot";
    public const string Center = "center";
    
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
    
    public const string LeftKey = "LeftKey";
    public const string RightKey = "RightKey";
    public const string UpKey = "UpKey";
    public const string DownKey = "DownKey";
    public const string MiniMapKey = "MiniMapKey";
    public const string CharacterInfoKey = "CharacterInfoKey";
    public const string AttackKey = "AttackKey";
    public const string JumpKey = "JumpKey";
    public const string DashKey = "DashKey";
    public const string SkillKey1 = "SkillKey1";
    public const string SkillKey2 = "SkillKey2";
    public const string SkillKey3 = "SkillKey3";
    public const string SkillKey4 = "SkillKey4";
    public const string SkillKey5 = "SkillKey5";
    public const string SkillKey6 = "SkillKey6";
    public const string SkillKey7 = "SkillKey7";
    public const string SkillKey8 = "SkillKey8";
    
    public const string PauseKey = "PauseKey";

    public const string ChangeCharacterLeftKey = "ChangeCharacterLeftKey";
    public const string ChangeCharacterRightKey = "ChangeCharacterRightKey";
    
    // 볼륨 믹서 이름
    public const string MasterVolume = "MasterVolume";
    public const string SFXVolume = "SFXVolume";
    public const string BGMVolume = "BGMVolume";
    
    // 설정
    public const string Language = "Language";
    public const string CameraShaking = "CameraShaking";
    
    // 언어
    public const string Korean = "Korean";
    public const string English = "English";
    
    public const string TotalBar = "TotalBar";
    public const string TextFont = "TextFont";
    public const string SkillImage = "SkillImage";
    public const string SkillTooltip = "SkillTooltip";
    public const string SpeechFrame1 = "SpeechFrame1";
    public const string SpeechFrame2 = "SpeechFrame2";
    public const string SpeechFrameStrong = "SpeechFrame_Strong";
    public const string SpeechFrameTitle = "SpeechFrame_Title";

    public const string ProjectileDestroyEffect = "ProjectileDestroyEffect";
    public const string BangEffect = "BangEffect";
    public const string DropEffect = "DropEffect";
    public const string Effect = "Effect";
    public const string FadeBg = "FadeBg";
    public const string FadeUI = "FadeUI";
    public const string AttributeUpEffect = "AttributeUpEffect";
    public const string AttributeDownEffect = "AttributeDownEffect";
    
    public const string Explosion = "Explosion";
    public const string DashEffectUI = "DashEffectUI";
    public const string DashFrameUI = "DashFrameUI";
    public const string WaitCharacterUI = "WaitCharacterUI";
    public const string Guide = "Guide";

    public const string Object = "Object";
    public const string Lock = "Lock";

    public const string Berserker = "Berserker";
    public const string Gunner = "Gunner";
    public const string Fighter = "Fighter";
    
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
    public const string JumpAttack = "JumpAttack";
    public const string JumpAttackEnd = "JumpAttack_End";
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
    public const string Buff = "Buff";
    public const string MonsterAttack = "MonsterAttack";
    
    // 대화 애니메이션
    public const string DialogJump = "DialogJump";
    public const string DialogPose = "DialogPose";
    public const string DialogShot = "DialogShot";
    public const string DialogGround = "DialogGround";
    public const string DialogGroundLaugh = "DialogGroundLaugh";
    public const string Arrive = "Arrive";
    public const string Thumbs = "Thumbs";
    public const string Point = "Point";

    public const string Normal = "Normal";
    public const string Pattern = "Pattern";
    public const string Appear = "Appear";
    public const string AppearEnd = "AppearEnd";
    
    // 하드코딩으로 적용되는 특성 ID
    public const string SwordBeam = "SwordBeam";
    public const string ComboSlash = "ComboSlash";
    public const string ChargingFire = "ChargingFire";
    public const string IronWall = "IronWall";
    public const string VibratingSteel = "VibratingSteel";
    public const string BullCharge = "BullCharge";
    public const string EarthQuake = "EarthQuake";
    public const string MagmaEruption = "MagmaEruption";
    public const string FuriousStrike = "FuriousStrike";
    public const string SecondaryExplosion = "SecondaryExplosion";
    public const string MadBomber = "MadBomber";
    public const string PowerfulGunpowder = "PowerfulGunpowder";
    public const string LongShot = "LongShot";
    public const string PiercingStreak = "PiercingStreak";
    public const string FinishShot = "FinishShot";
    public const string FinishingExplosion = "FinishingExplosion";
    public const string KickWave = "KickWave";
    public const string MovingPunch = "MovingPunch";
    public const string LightningStrike = "LightningStrike";
    public const string ShockSmash = "ShockSmash";
    public const string LightningIron = "LightningIron";
    public const string CounterPunch = "CounterPunch";

    // 스킬 특성 패시브
    public const string SuperArmor = "SuperArmor";
    public const string DestroyProjectile = "DestroyProjectile";
    public const string ExplosionObject =  "ExplosionObject";
    public const string LimitExplosion = "LimitExplosion";
    public const string PiercingMissile = "PiercingMissile";
    public const string IgnoreSuperArmor = "IgnoreSuperArmor";

    // 스킬 특성 수치조정
    public const string DamageUp = "DamageUp";
    public const string SizeUp = "SizeUp";
    public const string SpeedUp = "SpeedUp";
    public const string StackUp = "StackUp";
    public const string DurationUp = "DurationUp";
    public const string CoolTimeReduce = "CoolTimeReduce";
    public const string CoolTimeIncrease = "CoolTimeIncrease";
    public const string DamageMultiplier = "DamageMultiplier";
    public const string CriticalChanceUp = "CriticalChanceUp";
    public const string ReachUp = "ReachUp";
    public const string CountUp = "CountUp";
    public const string BuffCountUp = "BuffCountUp";
    public const string CanMove = "CanMove";

    public const string Big = "Big";
    public const string Missile = "Missile";
    public const string Grenade = "Grenade";
    public const string Item = "Item";
    public const string Relic = "Relic";
    public const string Player = "Player";
    public const string Monster = "Monster";
    public const string Body = "Body";
    public const string WallBody = "WallBody";
    
    // 룸 하위 오브젝트 이름들
    public const string MonsterArray = "MonsterArray";
    public const string BossArray = "BossArray";
    public const string NpcArray = "NpcArray";
    public const string TrapArray = "TrapArray";
    public const string InteractionArray = "InteractionArray";
    public const string GoldObjectArray = "GoldObjectArray";
    public const string PlayerPosArray = "PlayerPosArray";
    public const string GridObject = "GridObject";
    public const string LeftPlayerPos = "LeftPlayerPos";
    public const string RightPlayerPos = "RightPlayerPos";
    public const string UpPlayerPos = "UpPlayerPos";
    public const string DownPlayerPos = "DownPlayerPos";
    public const string LeftEntrance = "LeftEntrance";
    public const string RightEntrance = "RightEntrance";
    public const string UpEntrance = "UpEntrance";
    public const string DownEntrance = "DownEntrance";
    public const string TilemapObject = "TilemapObject";

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
    public const string SavePoint = "SavePoint";
    public const string PortalObject = "PortalObject";
    public const string Interaction = "Interaction";
    public const string InteractionUI = "InteractionUI";
    public const string InteractionSelectUI = "InteractionSelectUI";
    
    public const string Npc = "Npc";
    public const string Dialogue = "Dialogue";
    public const string DialogueChoice = "DialogueChoice";
    public const string ProductDialogue = "ProductDialogue";
    public const string Talk = "Talk";
    public const string First = "First";
    
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
    public const string Frozen = "Frozen";
    public const string Airborne = "Airborne";
    public const string AirborneDown = "AirborneDown";
    public const string Stun = "Stun";
    public const string Down = "Down";
    public const string Damaged = "Damaged";
    public const string Die = "Die";
    public const string Stagger = "Stagger";
    public const string StaggerExplosionUI = "StaggerExplosionUI";
    public const string FrozenEndEffect = "FrozenEndEffect";
    public const string BurnHitEffect = "BurnHitEffect";
    public const string ShockHitEffect = "ShockHitEffect";
    
    // 버프 Id
    public const string SwordCounterBuff = "SwordCounterBuff";
    public const string ElementalInfusionIceBuff = "ElementalInfusionIceBuff";
    public const string ElementalInfusionLightningBuff = "ElementalInfusionLightningBuff";
    public const string ElementalInfusionFireBuff = "ElementalInfusionFireBuff";
    
    // 버프 타입
    public const string PowerUpPercent = "PowerUpPercent";
    
    // 디버프 타입
    public const string ArmorBreak = "ArmorBreak";
    
    public const string Landing = "Landing";
    public const string Jumping = "Jumping";
    
    public const string ComboEnd = "ComboEnd";
    public const string ComboAttack = "ComboAttack";
    public const string ComboAttack2 = "ComboAttack2";
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

    public const string Icon = "Icon";
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
    public const string StoreItem = "StoreItem";
    public const string Animations = "Animations";
    public const string Arena = "Arena";
    
    public const string MoveSpeed = "MoveSpeed";
    public const string AttackSpeed = "AttackSpeed";
    public const string IgnoreTime = "IgnoreTime";
    
    public const string ChangeCharacter = "ChangeCharacter";
    public const string ChangeCharacterKey = "ChangeCharacterKey";
    
    // 기타 애니메이션
    public const string Left = "Left";
    public const string Right = "Right";
    public const string SwitchLeft = "SwitchLeft";
    public const string SwitchRight = "SwitchRight";

    public const string Open = "Open";
    public const string Close = "Close";
    public const string SwitchOpen = "SwitchOpen";
    
    public const string Hit = "Hit";
    public const string Break = "Break";
    public const string BreakImmediate = "BreakImmediate";
    
    // 직업 공용
    public const string Face = "Face";
    public const string DownDust = "DownDust";
    public const string FireFlash = "FireFlash";
    public const string LightningFlash = "LightningFlash";
    public const string IceFlash = "IceFlash";
    public const string GreenFlash = "GreenFlash";
    public const string Warning = "Warning";
    public const string WarningArea = "WarningArea";
    public const string PlatformFragments = "PlatformFragments";
    public const string PlatformExplosion = "PlatformExplosion";
    public const string UI = "UI";

    public const string BerserkerSlash = "Berserker_Slash";
    public const string BerserkerFlash = "Berserker_Flash";
    public const string BerserkerAttack1 = "Berserker_Attack1";
    public const string BerserkerAttack2 = "Berserker_Attack2";
    public const string BerserkerAttack3 = "Berserker_Attack3";
    
    public const string BerserkerJumpAttack = "Berserker_JumpAttack";

    // 옛날 점프공격 데이터
    public const string BerserkerJumpAttack1Old = "Berserker_JumpAttack1_Old";
    public const string BerserkerJumpAttack2Old = "Berserker_JumpAttack2_Old";
    public const string BerserkerJumpAttack2EffectOld = "Berserker_JumpAttack2_Effect_Old";
    
    public const string BerserkerAttackHitCrit = "Berserker_Attack_Hit_Crit";
    public const string BerserkerCrashHitEffect = "Berserker_Crash_HitEffect";

    public const string BerserkerDash = "Berserker_Dash";
    public const string BerserkerUpperSlash = "Berserker_UpperSlash";
    public const string BerserkerUpperSlashComboAttack = "Berserker_UpperSlash_ComboAttack";
    public const string BerserkerUpperSlashSwordBeam = "Berserker_UpperSlash_SwordBeam";
    public const string BerserkerCrash = "Berserker_Crash";
    public const string BerserkerCrashSpinAttack = "Berserker_Crash_SpinAttack";
    public const string BerserkerCrashEarthQuake = "Berserker_Crash_EarthQuake";
    public const string BerserkerCrashMagmaEruption = "Berserker_Crash_MagmaEruption";
    public const string BerserkerCrashSecondaryExplosion = "Berserker_Crash_SecondaryExplosion";
    public const string BerserkerCrashSecondaryExplosionEffect = "Berserker_Crash_SecondaryExplosion_Effect";
    public const string BerserkerCrashSmash = "Berserker_Crash_Smash";
    public const string BerserkerCrashExplosion = "Berserker_Crash_Explosion";
    public const string BerserkerFireStrike = "Berserker_FireStrike";
    public const string BerserkerSwordCounter = "Berserker_SwordCounter";
    public const string BerserkerSwordCounterJust = "Berserker_SwordCounter_Just";
    public const string BerserkerSwordCounterEffect = "Berserker_SwordCounterEffect";
    public const string BerserkerSwordCounterJustEffect = "Berserker_SwordCounterJustEffect";
    public const string BerserkerSwordCounterGuard = "Berserker_SwordCounter_Guard";
    public const string BerserkerSwordCounterCharge = "Berserker_SwordCounter_Charge";
    public const string BerserkerSwordCounterGuardEffect = "Berserker_SwordCounter_GuardEffect";
    public const string BerserkerSwordCounterStun = "Berserker_SwordCounter_Stun";
    public const string BerserkerFireStrikeAfterBurn = "Berserker_FireStrike_AfterBurn";
    public const string BerserkerFireStrikeChargeEffect = "Berserker_FireStrike_ChargeEffect";
    public const string BerserkerChargeCrash = "Berserker_ChargeCrash";
    public const string BerserkerChargeCrashSlash = "Berserker_ChargeCrash_Slash";
    public const string BerserkerChargeCrashSmash = "Berserker_ChargeCrash_Smash";
    public const string BerserkerChargeCrashSmashEffect = "Berserker_ChargeCrash_SmashEffect";
    
    public const string GunnerFlash = "Gunner_Flash";
    public const string GunnerAttack1Object = "Gunner_Attack1_Object";
    public const string GunnerAttack1Effect = "Gunner_Attack1_Effect";
    public const string GunnerAttack2Object = "Gunner_Attack2_Object";
    public const string GunnerAttack2Effect = "Gunner_Attack2_Effect";
    public const string GunnerAttackHitCrit = "Gunner_Attack_Hit_Crit";

    public const string GunnerDash = "Gunner_Dash";
    public const string GunnerDashShot = "Gunner_DashShot";
    public const string GunnerGrenade = "Gunner_Grenade";
    public const string GunnerGrenadeObject = "Gunner_Grenade_Object";
    public const string GunnerKnockBackShot = "Gunner_KnockBackShot";
    public const string GunnerKnockBackShotReady = "Gunner_KnockBackShot_Ready";
    public const string GunnerCrazyShot = "Gunner_CrazyShot";
    public const string GunnerCrazyShot2 = "Gunner_CrazyShot2";
    public const string GunnerCrazyShotEffect = "Gunner_CrazyShot_Effect";
    public const string GunnerCrazyShotFinishObject = "Gunner_CrazyShot_Finish_Object";
    public const string GunnerCrazyShotFinishPierce = "Gunner_CrazyShot_Finish_Pierce";
    public const string GunnerBigShot = "Gunner_BigShot";
    public const string GunnerElementalInfusion = "Gunner_ElementalInfusion";
    public const string GunnerElementalInfusionSelect = "Gunner_ElementalInfusion_Select";
    public const string GunnerBigShotReady = "Gunner_BigShot_Ready";
    public const string Fire = "Fire";
    public const string Lightning = "Lightning";
    public const string Ice = "Ice";
    public const string GunnerChargeEffect = "GunnerChargeEffect";
    public const string FireChargeEffect = "FireChargeEffect";
    public const string LightningChargeEffect = "LightningChargeEffect";
    public const string IceChargeEffect = "IceChargeEffect";

    public const string FighterFlash = "Fighter_Flash";
    public const string FighterAttack1 = "Fighter_Attack1";
    public const string FighterAttack2 = "Fighter_Attack2";
    public const string FighterAttack3 = "Fighter_Attack3";
    public const string FighterJumpAttack = "Fighter_JumpAttack";
    public const string FighterJumpAttackTrail = "Fighter_JumpAttack_Trail";
    
    public const string FighterDash = "Fighter_Dash";
    public const string FighterLightningEffect = "Fighter_LightningEffect";
    public const string FighterLightningTrail = "Fighter_LightningTrail";
    public const string FighterLightningKick = "Fighter_LightningKick";
    public const string FighterLightningKickWave = "Fighter_LightningKick_Wave";
    public const string FighterLightningPunch = "Fighter_LightningPunch";
    public const string FighterLightningPunchMissile = "Fighter_LightningPunch_Missile";
    public const string FighterLightningPunchFinish = "Fighter_LightningPunch_Finish";
    public const string FighterLightningPunchFinishMissile = "Fighter_LightningPunch_Finish_Missile";
    public const string FighterLightningPunchEffect = "Fighter_LightningPunch_Effect";
    public const string FighterLightningSmash = "Fighter_LightningSmash";
    public const string FighterLightningSmashWave = "Fighter_LightningSmash_Wave";
    public const string FighterLightningSmashLightning = "Fighter_LightningSmash_Lightning";
    public const string FighterLightningSmashLightningField = "Fighter_LightningSmash_LightningField";
    public const string FighterStrongPunch = "Fighter_StrongPunch";
    public const string FighterStrongPunchReady = "Fighter_StrongPunch_Ready";
    public const string FighterStrongPunchWave = "Fighter_StrongPunch_Wave";
    public const string FighterStrongPunchLightning = "Fighter_StrongPunch_Lightning";
    public const string FighterStrongPunchJust = "Fighter_StrongPunch_Just";
    public const string FighterPunchTrail = "Fighter_PunchTrail";
    
    public const string UIPool = "UIPool";
    public const string PopupPool = "PopupPool";
    public const string Prefab = ".prefab";

    // 몬스터
    public const string MonsterSpinach = "Monster_Spinach";
    public const string MonsterCoal = "Monster_Coal";
    public const string MonsterPurple = "Monster_Purple";
    public const string MonsterCharge = "Monster_Charge";
    public const string MonsterChargeEventJumpEffect = "Monster_Charge_EventJumpEffect";
    public const string MonsterIceWizardAttack = "Monster_IceWizard_Attack";

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

    public const string Aura = "Aura";
    
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
    public const string BGMArena = "BGM_Arena";
    public const string BGMBoss = "BGM_Boss";
    
    // 지역
    public const string SunHill = "SunHill";
    public const string BaseCamp = "BaseCamp";
    public const string Forest = "Forest";
    public const string Mine = "Mine";
    
    // 아이템
    public const string KeyForest = "Key_Forest";
    public const string KeyMine = "Key_Mine";
    
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
    public const string Star3 = "Star_3";
    public const string Lever = "Lever";
    public const string ElevatorHiss = "ElevatorHiss";
    public const string Pickup = "Pickup";
    public const string DestroyDoor = "DestroyDoor";
    public const string ProductMailDelivery = "ProductMailDelivery";
}
