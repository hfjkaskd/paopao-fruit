using System;
using System.Collections;
using Module.Setting;

/// <summary>设置存档模型：音乐/音效/震动/语言/评分/通知。</summary>
public class BMNFNJFCPHG : global::IAJDGNOGFGO<SettingData, BMNFNJFCPHG>
{
	public static event Action OnLanguageSwitch;

	public SettingData SettingData => data;

	public override void Init()
	{
		base.Init();
        data.music=SaveDataUtils.SettingData.enableMusic;
        data.sound=SaveDataUtils.SettingData.enableSound;
        data.vibrate=SaveDataUtils.SettingData.enableVibrate;
		GameAudio.SetMusicEnabled(data.music);
		GameAudio.SetSfxEnabled(data.sound);
	}

	protected override void AfterFirstInitData()
	{
		data.music = true;
		data.sound = true;
		data.vibrate = true;
		data.notification = false;
		data.localizationType = OJEEJGGLNPC.GetDefaultLocalizationType();
		data.levelIdOf5Star = 0;
		data.hasRatedOf5Star = false;
	}

	protected override string GetKey()
	{
		return "SettingModel";
	}

	public void UpdateSaveData()
	{
		SaveData();
	}

	public bool HasRated()
	{
		return data.hasRatedOf5Star;
	}

	public int RateUsLevelId()
	{
		return data.levelIdOf5Star;
	}

	public static IEnumerator RequestNotificationPermission()
	{
		yield break;
	}

	public static bool CheckNotifyIsAllowed()
	{
		return false;
	}

	public static bool IsAndroid13OrNewer()
	{
		return false;
	}

	public static void JumpToAppNotifyPermissionRequest()
	{
	}

	public void SetLocalizationType(EDMAJFIKJLE localizationType)
	{
		data.localizationType = localizationType;
		SaveData();
		OJEEJGGLNPC.Instance.ReLoadLocalText();
		SpreadLaguageSwitch();
	}

	public void SetHasRated(bool rated)
	{
		data.hasRatedOf5Star = rated;
		SaveData();
	}

	public void SpreadLaguageSwitch()
	{
		OnLanguageSwitch?.Invoke();
	}

	public void TryRefreshNotificationState()
	{
	}
}
