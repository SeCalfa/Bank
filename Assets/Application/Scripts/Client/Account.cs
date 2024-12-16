using UnityEngine;
using TMPro;

namespace Application.Scripts.Client
{
    public class Account : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI accountNameText;
        [SerializeField] private TextMeshProUGUI numberText;
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI currencyText;

        private App _app;
        private Application.Scripts.Server.Account _account;

        public void Init(string accountName, string number, string balance, string currency, App app, Application.Scripts.Server.Account account)
        {
            accountNameText.text = accountName;
            numberText.text = number;
            balanceText.text = balance;
            currencyText.text = currency;

            _app = app;
            _account = account;
        }

        public void FillSelectedAccountData()
        {
            _app.FillSelectedAccountData(_account);
        }
    }
}
