using UnityEngine;
using UnityEngine.UI;

namespace SweatyChair.UI
{

	/// <summary>
	/// A button to open redeem code input box.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class RedeemCodeButton : MonoBehaviour
	{

		[Tooltip("If thie button is diffen to work around some app store requirement.")]
		[SerializeField] private bool _isHidden = true;

		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			if (_isHidden)
				RedeemCodeManager.CheckShow();
			else
				RedeemCodeManager.Show();
		}

	}

}