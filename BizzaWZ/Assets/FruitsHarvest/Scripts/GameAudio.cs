using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 简化音频门面：替代原 OCES 中间件。
/// DLMJOHCOJKN uses explicit stable event IDs and lazy clip loading.
/// </summary>
public static class GameAudio
{
	private static GameObject root;

	private static AudioSource musicSource;

	private static AudioSource ambienceSource;

	private static readonly List<AudioSource> sfxSources = new List<AudioSource>();

	private static readonly Dictionary<uint, string> idToClip = new Dictionary<uint, string>();

	private static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

	private static AudioMixerGroup musicGroup;

	private static AudioMixerGroup sfxGroup;

	private static AudioMixerGroup ambienceGroup;

	private static AudioMixerGroup voiceGroup;

	private static bool inited;

	public static bool MusicEnabled { get; private set; } = true;

	public static bool SfxEnabled { get; private set; } = true;

	public static void Init()
	{
		if (inited)
		{
			return;
		}
		inited = true;
		root = new GameObject("GameAudio");
		Object.DontDestroyOnLoad(root);

		AudioMixer mixer = GameRes.Load<AudioMixer>("res/local/sound/Master");
		if (mixer != null)
		{
			musicGroup = FindGroup(mixer, "Music");
			sfxGroup = FindGroup(mixer, "Regular") ?? FindGroup(mixer, "SFX");
			ambienceGroup = FindGroup(mixer, "Ambience");
			voiceGroup = FindGroup(mixer, "Voice");
		}

		musicSource = root.AddComponent<AudioSource>();
		musicSource.loop = true;
		musicSource.outputAudioMixerGroup = musicGroup;
		musicSource.volume = 0.7f;

		ambienceSource = root.AddComponent<AudioSource>();
		ambienceSource.loop = true;
		ambienceSource.outputAudioMixerGroup = ambienceGroup;
		ambienceSource.volume = 0.8f;

		RegisterEventIds();
	}

	private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
	{
		AudioMixerGroup[] groups = mixer.FindMatchingGroups(name);
		return groups != null && groups.Length > 0 ? groups[0] : null;
	}

	private static void RegisterEventIds()
	{
        DLMJOHCOJKN.Play_sfx_ui_panel_common_open = 1u; idToClip[1u] = "sfx_ui_panel_common_open";
        DLMJOHCOJKN.Play_sfx_ui_panel_common_close = 2u; idToClip[2u] = "sfx_ui_panel_common_close";
        DLMJOHCOJKN.Play_sfx_ui_button_common = 3u; idToClip[3u] = "sfx_ui_button_common";
        DLMJOHCOJKN.Play_sfx_notice_guide = 4u; idToClip[4u] = "sfx_notice_guide";
        DLMJOHCOJKN.Play_sfx_notice_common_positive = 5u; idToClip[5u] = "sfx_notice_common_positive";
        DLMJOHCOJKN.Play_sfx_notice_common_neutral = 6u; idToClip[6u] = "sfx_notice_common_neutral";
        DLMJOHCOJKN.Play_sfx_notice_common_negative = 7u; idToClip[7u] = "sfx_notice_common_negative";
        DLMJOHCOJKN.Play_sfx_ui_labelSwitch = 8u; idToClip[8u] = "sfx_ui_labelSwitch";
        DLMJOHCOJKN.Play_sfx_ui_button_game_fruit = 9u; idToClip[9u] = "sfx_ui_button_game_fruit";
        DLMJOHCOJKN.Play_sfx_anim_game_fruit_clear = 10u; idToClip[10u] = "sfx_anim_game_fruit_clear";
        DLMJOHCOJKN.Play_sfx_notice_prop_undo = 11u; idToClip[11u] = "sfx_notice_prop_undo";
        DLMJOHCOJKN.Play_sfx_noitce_coin_show = 12u; idToClip[12u] = "sfx_noitce_coin_show";
        DLMJOHCOJKN.Play_sfx_anim_coin_fly = 13u; idToClip[13u] = "sfx_anim_coin_fly";
        DLMJOHCOJKN.Play_sfx_noitce_coin_land = 14u; idToClip[14u] = "sfx_noitce_coin_land";
        DLMJOHCOJKN.Play_sfx_noitce_game_start = 15u; idToClip[15u] = "sfx_noitce_game_start";
        DLMJOHCOJKN.Play_sfx_anim_chest_open = 16u; idToClip[16u] = "sfx_anim_chest_open";
        DLMJOHCOJKN.Play_sfx_notice_prop_magicWand = 17u; idToClip[17u] = "sfx_notice_prop_magicWand";
        DLMJOHCOJKN.Play_sfx_notice_prop_shuffle = 18u; idToClip[18u] = "sfx_notice_prop_shuffle";
        DLMJOHCOJKN.Play_sfx_notice_prop_addSpace = 19u; idToClip[19u] = "sfx_notice_prop_addSpace";
        DLMJOHCOJKN.Play_sfx_anim_reward_land = 20u; idToClip[20u] = "sfx_anim_reward_land";
        DLMJOHCOJKN.Play_sfx_ui_panel_game_win = 21u; idToClip[21u] = "sfx_ui_panel_game_win";
        DLMJOHCOJKN.Play_sfx_ui_panel_game_lose = 22u; idToClip[22u] = "sfx_ui_panel_game_lose";
        DLMJOHCOJKN.Play_sfx_anim_game_fruit_land = 23u; idToClip[23u] = "sfx_anim_game_fruit_land";
        DLMJOHCOJKN.Play_sfx_ui_toast_game_hard = 24u; idToClip[24u] = "sfx_ui_toast_game_hard";
        DLMJOHCOJKN.Play_sfx_anim_chest_idle = 25u; idToClip[25u] = "sfx_anim_chest_idle";
        DLMJOHCOJKN.Play_sfx_anim_game_backgroundChange = 26u; idToClip[26u] = "sfx_anim_game_backgroundChange";
        DLMJOHCOJKN.Play_sfx_ui_panel_newProp_open = 27u; idToClip[27u] = "sfx_ui_panel_newProp_open";
        DLMJOHCOJKN.Play_sfx_ui_panel_newProp_close = 28u; idToClip[28u] = "sfx_ui_panel_newProp_close";
        DLMJOHCOJKN.Play_sfx_anim_game_inspire = 29u; idToClip[29u] = "sfx_anim_game_inspire";
        DLMJOHCOJKN.Play_voice_male_good_low = 30u; idToClip[30u] = "voice_male_good_low";
        DLMJOHCOJKN.Play_voice_male_great_low = 31u; idToClip[31u] = "voice_male_great_low";
        DLMJOHCOJKN.Play_voice_male_excelent_low = 32u; idToClip[32u] = "voice_male_excelent_low";
        DLMJOHCOJKN.Play_voice_male_amazing_low = 33u; idToClip[33u] = "voice_male_amazing_low";
        DLMJOHCOJKN.Play_voice_male_unbelievable_low = 34u; idToClip[34u] = "voice_male_unbelievable_low";
        DLMJOHCOJKN.Play_sfx_anim_game_confetti = 35u; idToClip[35u] = "sfx_anim_game_confetti";
        DLMJOHCOJKN.Play_sfx_ui_panel_shop_open = 36u; idToClip[36u] = "sfx_ui_panel_shop_open";
        DLMJOHCOJKN.Play_sfx_ui_panel_shop_close = 37u; idToClip[37u] = "sfx_ui_panel_shop_close";
        DLMJOHCOJKN.Play_sfx_notice_purchased = 38u; idToClip[38u] = "sfx_notice_purchased";
        DLMJOHCOJKN.Play_sfx_notice_neutral = 39u; idToClip[39u] = "sfx_notice_neutral";
        DLMJOHCOJKN.Play_Bar_wav = 40u; idToClip[40u] = "Bar_wav";
        DLMJOHCOJKN.Play_Beat_wav = 41u; idToClip[41u] = "Beat_wav";
        DLMJOHCOJKN.Play_Grid_wav = 42u; idToClip[42u] = "Grid_wav";
        DLMJOHCOJKN.Plya_sfx_anim_game_fruit_clear = 43u; idToClip[43u] = "sfx_anim_game_fruit_clear";
        DLMJOHCOJKN.Plya_sfx_anim_game_fruit_land = 44u; idToClip[44u] = "sfx_anim_game_fruit_land";
        DLMJOHCOJKN.Plya_sfx_anim_game_toy_clear = 45u; idToClip[45u] = "sfx_anim_game_toy_clear";
        DLMJOHCOJKN.Plya_sfx_anim_game_toy_land = 46u; idToClip[46u] = "sfx_anim_game_toy_land";
	}

	private static AudioClip GetClip(string clipName)
	{
		if (string.IsNullOrEmpty(clipName))
		{
			return null;
		}
		if (clipCache.TryGetValue(clipName, out AudioClip cached))
		{
			return cached;
		}
		AudioClip clip = GameRes.LoadAudio(clipName);
		clipCache[clipName] = clip;
		return clip;
	}

	private static AudioSource GetFreeSfxSource()
	{
		for (int i = 0; i < sfxSources.Count; i++)
		{
			if (!sfxSources[i].isPlaying)
			{
				return sfxSources[i];
			}
		}
		AudioSource source = root.AddComponent<AudioSource>();
		source.playOnAwake = false;
		sfxSources.Add(source);
		return source;
	}

	public static void Play(uint eventId)
	{
		if (eventId == 0 || !idToClip.TryGetValue(eventId, out string clipName))
		{
			return;
		}
		Play(clipName);
	}

	public static void Play(string clipName)
	{
		if (!inited || !SaveDataUtils.SettingData.enableSound)
		{
			return;
		}
		AudioClip clip = GetClip(clipName);
		if (clip == null)
		{
			return;
		}
		AudioSource source = GetFreeSfxSource();
		source.outputAudioMixerGroup = clipName.StartsWith("voice_") ? voiceGroup : sfxGroup;
		source.pitch = 1f;
		source.PlayOneShot(clip);
	}

	/// <summary>连续合成音调：三消游戏中的连击音高递增。semitones 为升高的半音数。</summary>
	public static void PlayPitched(string clipName, int semitones)
	{
		if (!inited || !SaveDataUtils.SettingData.enableSound)
		{
			return;
		}
		AudioClip clip = GetClip(clipName);
		if (clip == null)
		{
			return;
		}
		AudioSource source = GetFreeSfxSource();
		source.outputAudioMixerGroup = sfxGroup;
		source.pitch = Mathf.Pow(2f, semitones / 12f);
		source.PlayOneShot(clip);
	}

	public static void PlayMusic(string clipName = "music")
	{
        SoundManager.Instance.PlayBGM("SFX_BGM");
	}

	public static void StopMusic()
	{
		if (inited)
		{
			musicSource.Stop();
		}
	}

	public static void PlayAmbience(string clipName)
	{
		if (!inited)
		{
			return;
		}
		AudioClip clip = GetClip(clipName);
		if (clip == null || (ambienceSource.clip == clip && ambienceSource.isPlaying))
		{
			return;
		}
		ambienceSource.clip = clip;
		if (MusicEnabled)
		{
			ambienceSource.Play();
		}
	}

	public static void StopAmbience()
	{
		if (inited)
		{
			ambienceSource.Stop();
		}
	}

	public static void SetMusicEnabled(bool enabled)
	{
		MusicEnabled = enabled;
		if (!inited)
		{
			return;
		}
		if (enabled)
		{
			if (musicSource.clip != null && !musicSource.isPlaying)
			{
				musicSource.Play();
			}
			if (ambienceSource.clip != null && !ambienceSource.isPlaying)
			{
				ambienceSource.Play();
			}
		}
		else
		{
			musicSource.Pause();
			ambienceSource.Pause();
		}
	}

	public static void SetSfxEnabled(bool enabled)
	{
		SfxEnabled = enabled;
	}
}
