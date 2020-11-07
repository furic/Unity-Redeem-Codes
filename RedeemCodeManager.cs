using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SweatyChair;
using SweatyChair.UI;

/// <summary>
/// A manage to validate the redeem code in game, used for compensating players or rewarding them in events.
/// </summary>
public static class RedeemCodeManager
{

	// The character count in a redeem code, this should be matched with server
	private const int NUM_CHAR_REDEEM_CODE = 12;

	private const string PREFS_USED_CODES = "RedeemCodeUsedCodes";

	private static string _enteredCode;

	private static float _lastCheckShowTime;
	private static int _checkShownCount;

	public static void CheckShow()
	{
		if (Time.unscaledTime < _lastCheckShowTime + 1)
			_checkShownCount++;
		else
			_checkShownCount = 1;
		_lastCheckShowTime = Time.unscaledTime;

		if (_checkShownCount >= 3) {
			Show();
			_checkShownCount = 0;
		}
	}

	public static void Show()
	{
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
		new Message {
			format = MessageFormat.Input,
			title = LocalizeUtils.Get("Redeem"),
			content = LocalizeUtils.Get("Code:"),
			inputPlaceholderString = LocalizeUtils.Get("Enter Your Redeem Code"),
			inputConfirmCallback = OnRedeemCodeConfirm,
			inputValidationData = new InputFieldValidationData(NUM_CHAR_REDEEM_CODE, InputField.CharacterValidation.Alphanumeric)
		}.Show();
#endif
	}

#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL

	public static void OnRedeemCodeConfirm(string code)
	{
#if UNITY_IOS || UNITY_ANDROID
		Handheld.StartActivityIndicator();
#endif
		_enteredCode = code.ToUpper();
		ServerManager.Get("redeem/" + code, OnRedeemCodeSucceed, OnRedeemCodeReturnFailed);
	}

#if UNITY_IOS || UNITY_ANDROID

	private static void OnRedeemCodeSucceed(Hashtable ht)
	{
		Handheld.StopActivityIndicator();

		ArrayList rewardsAl = ht["rewards"] as ArrayList;

		if (rewardsAl.Count <= 0) { // Find redeem code but no reward? somthing wrong in backend
			new Message {
				title = LocalizeUtils.Get("Redeem Failed"),
				content = LocalizeUtils.Get("No redeem code found."),
			}.Show();
			return;
		}

		if (ht.ContainsKey("reusable") && (ht["reusable"] as string) == "1") { // If code is reusable, use local PlayerPrefs to check if the code is used before
			List<string> usedCodes = new List<string>(PlayerPrefsX.GetStringArray(PREFS_USED_CODES));
			if (usedCodes.Contains(_enteredCode)) {
				new Message {
					title = LocalizeUtils.Get("Redeem Failed"),
					content = LocalizeUtils.Get("Redeem code has already redeemed."),
				}.Show();
				return;
			} else {
				usedCodes.Add(_enteredCode);
				PlayerPrefsX.SetStringArray(PREFS_USED_CODES, usedCodes.ToArray());
			}
		}

		//List<Item> items = new List<Item>();

		foreach (var rewardObj in rewardsAl) {

			Hashtable rewardHt = rewardObj as Hashtable;
			DebugUtils.Log(rewardHt);
			ItemType type = (ItemType)DataUtils.GetInt(rewardHt["type"] as string);
			int amount = DataUtils.GetInt(rewardHt["amount"] as string);
			int id = DataUtils.GetInt(rewardHt["id"] as string);

			// TODO 201011: Backward compatiable NH only: convert money to coin
			if (id == 0) id = 1;

			SerializableItem item = new SerializableItem(id, type, amount);

			item.Obtain();
		}

		// Analytics
		var dict = new Dictionary<string, object> {
			{"code", _enteredCode },
		};
		UnityEngine.Analytics.AnalyticsEvent.Custom("redeem_code", dict);

		//new Reward(items).Claim(LocalizeUtils.Get("Redeem Succeed"));
	}

	private static void OnRedeemCodeReturnFailed(string e)
	{
		new Message {
			title = LocalizeUtils.Get("Redeem Failed"),
			content = LocalizeUtils.Get(e),
		}.Show();
		Handheld.StopActivityIndicator();
	}

#endif

#if UNITY_EDITOR

	[UnityEditor.MenuItem("Debug/Redeem Codes/Reset All Used Redeem Codes")]
	private static void ResetAllUsedRedeemCodes()
	{
		PlayerPrefs.DeleteKey(PREFS_USED_CODES);
	}
#endif

#endif

}