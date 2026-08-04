// PlayerPrefs 기반 설정 저장 (키 바인딩·볼륨·게임 설정)

using UnityEngine;

public static class KeyBinding
{
    // 저장할 때
    public static void SaveKey(string prefKey, KeyCode key)
    {
        GameLog.Info($"{prefKey}를 {key}로 저장");
        PlayerPrefs.SetInt(prefKey, (int)key);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 키도 지정 가능)
    public static KeyCode LoadKey(string prefKey, KeyCode defaultKey)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            GameLog.Info($"저장된 키값 {PlayerPrefs.GetInt(prefKey)}을 불러왔습니다");
            return (KeyCode)PlayerPrefs.GetInt(prefKey);
        }
        
        // 처음 실행 시 디폴트 키를 저장
        SaveKey(prefKey, defaultKey);
        return defaultKey;
    }
}

public static class VolumeBinding
{
    // 저장할 때
    public static void SaveVolume(string prefKey, float volume)
    {
        PlayerPrefs.SetFloat(prefKey, volume);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 볼륨도 지정 가능)
    public static float LoadVolume(string prefKey, float defaultVolume)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            GameLog.Info($"저장된 {prefKey}: {PlayerPrefs.GetFloat(prefKey)}");
            return PlayerPrefs.GetFloat(prefKey);
        }
        
        // 처음 실행 시 디폴트 볼륨 저장
        GameLog.Info($"최초 {prefKey} 설정: {defaultVolume}");
        SaveVolume(prefKey, defaultVolume);
        return defaultVolume;
    }
}

public static class SettingStringBinding
{
    // 저장할 때
    public static void SaveGameSetting(string prefKey, string value)
    {
        PlayerPrefs.SetString(prefKey, value);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 설정값도 지정 가능)
    public static string LoadSetting(string prefKey, string defaultValue)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            GameLog.Info($"저장된 {prefKey}: {PlayerPrefs.GetString(prefKey)}");
            return PlayerPrefs.GetString(prefKey);
        }
        
        // 처음 실행 시 디폴트 설정 저장
        GameLog.Info($"최초 {prefKey} 설정: {defaultValue}");
        SaveGameSetting(prefKey, defaultValue);
        return defaultValue;
    }
}

public static class SettingIntBinding
{
    // 저장할 때
    public static void SaveGameSetting(string prefKey, int value)
    {
        PlayerPrefs.SetInt(prefKey, value);
        PlayerPrefs.Save();
    }
  
    // 불러올 때 (디폴트 설정값도 지정 가능)
    public static int LoadSetting(string prefKey, int defaultValue)
    {
        if (PlayerPrefs.HasKey(prefKey))
        {
            GameLog.Info($"저장된 {prefKey}: {PlayerPrefs.GetInt(prefKey)}");
            return PlayerPrefs.GetInt(prefKey);
        }
        
        // 처음 실행 시 디폴트 설정 저장
        GameLog.Info($"최초 {prefKey} 설정: {defaultValue}");
        SaveGameSetting(prefKey, defaultValue);
        return defaultValue;
    }
}
