using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Application.Scripts.Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Application.Scripts.Client
{
    public class App : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject account;
        
        [Header("App")]
        [SerializeField] private Transform accountsScrollBar;
        
        [Space]
        [Header("Pages")]
        [SerializeField] private GameObject homePageOut;
        [SerializeField] private GameObject registerPage;
        [SerializeField] private GameObject loginPage;
        [SerializeField] private GameObject homePageInPage;
        [SerializeField] private GameObject createAccountPage;
        [SerializeField] private GameObject accountPage;
        [SerializeField] private GameObject sendMoneyPage;

        [Header("Register page")]
        [SerializeField] private TMP_InputField loginUpField;
        [SerializeField] private TMP_InputField passwordUpField;
        [SerializeField] private TMP_InputField pinUpField;
        [SerializeField] private TMP_InputField peselField;
        [SerializeField] private TMP_InputField firstNameField;
        [SerializeField] private TMP_InputField secondNameField;
        
        [Header("Login page")]
        [SerializeField] private TMP_InputField loginInField;
        [SerializeField] private TMP_InputField passwordInField;
        
        [Header("Home page")]
        [SerializeField] private TextMeshProUGUI greetingField;
        
        [Header("Create account page")]
        [SerializeField] private TMP_InputField createAccountNameField;
        [SerializeField] private TMP_Dropdown createAccountCurrency;
        
        [Header("Account page")]
        [SerializeField] private TextMeshProUGUI accountName;
        [SerializeField] private TextMeshProUGUI accountNumber;
        [SerializeField] private TextMeshProUGUI accountBalance;
        [SerializeField] private TextMeshProUGUI accountCurrency;
        
        [Header("Send money page")]
        [SerializeField] private TMP_InputField sendMoneyAmount;
        [SerializeField] private TMP_InputField sendMoneyAccountNumber;
        [SerializeField] private TextMeshProUGUI sendMoneyCurrency;
        [SerializeField] private TMP_InputField sendMoneyPin;

        private Authentication _authentication;

        private GameObject _hoveredPage;

        private string _login;
        private string _password;
        private int _pin;
        private int _pesel;
        private string _firstName;
        private string _secondName;

        private Application.Scripts.Server.Account _selectedAccountData;
        
        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            _authentication = GetComponent<Authentication>();
            
            _authentication.OnSignedIn.AddListener(HomePageDataFill);
            _authentication.OnBankAccountCreated.AddListener(HomePageDataFill);
            _authentication.OnMoneySend.AddListener(HomePageDataFill);
            
            _authentication.OnSignedIn.AddListener(ClearLoginPage);
            _authentication.OnBankAccountCreated.AddListener(ClearRegisterPage);
            _authentication.OnMoneySend.AddListener(ClearSendMoneyPage);

            CloseAllPages();
            homePageOut.SetActive(true);
        }

        private void HomePageDataFill()
        {
            var accounts = accountsScrollBar.GetComponentsInChildren<Account>();
            foreach (var a in accounts)
            {
                Destroy(a.gameObject);
            }
            
            greetingField.text = $"Hello, {_authentication.User.FirstName}";

            var sortedAccount = _authentication.User.Accounts.OrderByDescending(item => item.Name);

            foreach (var acc in sortedAccount)
            {
                var a = Instantiate(account, accountsScrollBar);
                a.GetComponent<Account>().Init(acc.Name, string.Join(" ", acc.Number), acc.Amount.ToString(CultureInfo.InvariantCulture), acc.Сurrency, this, acc);
                a.GetComponent<Button>().onClick.AddListener(delegate { OpenPage(accountPage);});
            }
        }

        private void CloseAllPages()
        {
            homePageOut.SetActive(false);
            registerPage.SetActive(false);
            loginPage.SetActive(false);
            homePageInPage.SetActive(false);
            createAccountPage.SetActive(false);
            accountPage.SetActive(false);
            sendMoneyPage.SetActive(false);
        }

        public void OpenPage(GameObject page)
        {
            Debug.Log("Open Page");
            CloseAllPages();
            
            page.SetActive(true);
        }

        public void OpenPageAsHover(GameObject page)
        {
            _hoveredPage = page;
            _hoveredPage.SetActive(true);
        }

        public void CloseHoverPage()
        {
            if (_hoveredPage == null)
            {
                return;
            }
            
            _hoveredPage.SetActive(false);
            _hoveredPage = null;
        }

        public void SignUp()
        {
            _login = loginUpField.text;
            _password = passwordUpField.text;
            _pin = int.Parse(pinUpField.text);
            _pesel = int.Parse(peselField.text);
            _firstName = firstNameField.text;
            _secondName = secondNameField.text;
            
            _authentication.SignUp(
                _login,
                _password,
                _pin,
                _pesel,
                _firstName,
                _secondName);
        }

        public void SignIn()
        {
            _login = loginInField.text;
            _password = passwordInField.text;
            
            _authentication.SignIn(
                _login,
                _password);
        }

        public void CreateBankAccount()
        {
            var name = createAccountNameField.text;
            var currency = createAccountCurrency.options[createAccountCurrency.value].text;
            
            _authentication.CreateAccount(name, currency);
            createAccountNameField.text = "";
            
            ClearCreateAccountPage();
        }

        public void FillSelectedAccountData(Application.Scripts.Server.Account acc)
        {
            accountName.text = acc.Name;
            accountNumber.text = string.Join(" ", acc.Number);
            accountBalance.text = acc.Amount.ToString(CultureInfo.InvariantCulture);
            accountCurrency.text = acc.Сurrency;

            sendMoneyCurrency.text = acc.Сurrency;
            
            _selectedAccountData = acc;
        }

        public void SendMoney()
        {
            var targetAccountNumber = sendMoneyAccountNumber.text;
            float.TryParse(sendMoneyAmount.text, out var moneyAmount);
            int.TryParse(sendMoneyPin.text, out var pin);

            if (targetAccountNumber.Length != 26)
            {
                Debug.Log("Target account number incorrect");
                return;
            }

            if (_selectedAccountData.Amount < moneyAmount)
            {
                Debug.Log("Not enough money on balance");
                return;
            }

            if (_authentication.User.Pin != pin)
            {
                Debug.Log("Incorrect PIN");
                return;
            }

            var targetNumber = new List<int>
            {
                int.Parse(targetAccountNumber[..2]),
                int.Parse(targetAccountNumber[2..6]),
                int.Parse(targetAccountNumber[6..10]),
                int.Parse(targetAccountNumber[10..14]),
                int.Parse(targetAccountNumber[14..18]),
                int.Parse(targetAccountNumber[18..22]),
                int.Parse(targetAccountNumber[22..26])
            };
            
            _authentication.SendMoney(
                _selectedAccountData.Number,
                targetNumber,
                moneyAmount, 
                () => OpenPage(homePageInPage));
        }

        private void ClearRegisterPage()
        {
            loginUpField.text = string.Empty;
            passwordUpField.text = string.Empty;
            pinUpField.text = string.Empty;
            peselField.text = string.Empty;
            firstNameField.text = string.Empty;
            secondNameField.text = string.Empty;
        }
        
        private void ClearLoginPage()
        {
            loginInField.text = string.Empty;
            passwordInField.text = string.Empty;
        }
        
        private void ClearCreateAccountPage()
        {
            createAccountNameField.text = string.Empty;
            createAccountCurrency.value = 0;
        }
        
        private void ClearSendMoneyPage()
        {
            sendMoneyAmount.text = string.Empty;
            sendMoneyAccountNumber.text = string.Empty;
            sendMoneyPin.text = string.Empty;
        }
    }
}
