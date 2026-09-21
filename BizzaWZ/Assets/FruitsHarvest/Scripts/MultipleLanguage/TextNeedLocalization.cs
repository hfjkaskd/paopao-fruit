using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultipleLanguage
{
	public class TextNeedLocalization : MonoBehaviour
	{
		public string key;

		private TextMeshProUGUI text;

		private Text oldText;

		private void OnEnable()
		{
			BMNFNJFCPHG.OnLanguageSwitch += UpdateText;
            BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, UpdateText, true);
			UpdateText();
		}

		private void OnDisable()
		{
			BMNFNJFCPHG.OnLanguageSwitch -= UpdateText;
            BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, UpdateText, false);
		}

		private void UpdateText()
		{
			if (string.IsNullOrEmpty(key))
			{
				return;
			}
			string value = OJEEJGGLNPC.Instance.GetText(key);
			if (text == null)
			{
				text = GetComponent<TextMeshProUGUI>();
			}
			if (text != null)
			{
				text.text = value;
				return;
			}
			if (oldText == null)
			{
				oldText = GetComponent<Text>();
			}
			if (oldText != null)
			{
				oldText.text = value;
			}
		}
	}
}
