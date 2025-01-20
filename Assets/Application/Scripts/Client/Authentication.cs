using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Application.Scripts.Server;
using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Application.Scripts.Client
{
    public class Authentication : MonoBehaviour
    {
        [SerializeField] private UnityEvent onSignedUp;
        [SerializeField] private UnityEvent onSignedIn;
        [SerializeField] private UnityEvent onMoneySend;
        [SerializeField] private UnityEvent onBankAccountCreated;
        
        private const string ApiBaseUrl = "https://localhost:7113/api/Auth";
        
        private FirebaseAuth _auth;
        private SuccessLogiData _user;
        private FirebaseFirestore _db;
        
        private int _accountsUpdated = 0;
        
        public User User { get; private set; }

        public UnityEvent OnSignedUp => onSignedUp;
        public UnityEvent OnSignedIn => onSignedIn;
        public UnityEvent OnMoneySend => onMoneySend;
        public UnityEvent OnBankAccountCreated => onBankAccountCreated;
        
        private void Awake()
        {
            InitializeFirebase();
        }

        private void OnDestroy() {
            _auth.SignOut();
            // _auth.StateChanged -= AuthStateChanged;
            _auth = null;
        }
        
        public IEnumerator Register(string email, string password, int pin, int pesel, string firstName, string secondName)
        {
            var requestBody = new
            {
                Email = email,
                Password = password
            };
            string json = JsonConvert.SerializeObject(requestBody);

            UnityWebRequest request = new UnityWebRequest($"{ApiBaseUrl}/register", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseText = request.downloadHandler.text;
                Debug.Log($"Registration Success: {responseText}");

                _user = JsonConvert.DeserializeObject<SuccessLogiData>(responseText);
                
                CreateProfile(_user.UserId, pin, pesel, firstName, secondName);
                onSignedUp?.Invoke();
            }
            else
            {
                Debug.LogError($"Registration Failed: {request.error}");
            }
        }
        
        public IEnumerator Login(string email, string password)
        {
            var requestBody = new RegisterRequest
            {
                Email = email,
                Password = password
            };
            string json = JsonConvert.SerializeObject(requestBody);

            UnityWebRequest request = new UnityWebRequest($"{ApiBaseUrl}/login", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseText = request.downloadHandler.text;
                Debug.Log($"Login Success: {responseText}");
                
                _user = JsonConvert.DeserializeObject<SuccessLogiData>(responseText);
                
                GetLoggedInUser(() => onSignedIn?.Invoke());
            }
            else
            {
                Debug.LogError($"Login Failed: {request.error}");
            }
        }

        public IEnumerator AddData(object data)
        {
            var json = JsonUtility.ToJson(data);
            var request = new UnityWebRequest($"{ApiBaseUrl}/firestore/add", "POST");
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log($"Response: {request.downloadHandler.text}");
            else
                Debug.LogError($"Error: {request.error}");
        }

        public void SignUp(string email, string password, int pin, int pesel, string firstName, string secondName)
        {
            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
                if (task.IsCanceled || task.IsFaulted) {
                    Debug.Log("SignUp failed. Something went wrong...");
                    return;
                }

                // Firebase user has been created.
                var result = task.Result;
                CreateProfile(result.User.UserId, pin, pesel, firstName, secondName);
                onSignedUp?.Invoke();
            });
        }

        public void SignIn(string email, string password)
        {
            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
                if (task.IsCanceled || task.IsFaulted) {
                    Debug.Log("SignIn failed. Something went wrong...");
                }
            });
        }

        public void CreateAccount(string name, string currency)
        {
            var docRef = _db.Collection("Users").Document(User.FirstName + " " + User.SecondName);
            var account = new Server.Account
            {
                Name = name,
                Сurrency = currency,
                Amount = 5000,
                Number = new List<int>
                {
                    Random.Range(10, 99),
                    Random.Range(1000, 9999),
                    Random.Range(1000, 9999),
                    Random.Range(1000, 9999),
                    Random.Range(1000, 9999),
                    Random.Range(1000, 9999),
                    Random.Range(1000, 9999)
                }
            };

            docRef.UpdateAsync("Accounts", FieldValue.ArrayUnion(account))
                .ContinueWithOnMainThread(task =>
                {
                    GetLoggedInUser(() => onBankAccountCreated?.Invoke());
                    Debug.Log(task.IsCompleted ? "Account added successfully" : "Something went wrong");
                });
        }

        public void SendMoney(List<int> currentNumber, List<int> targetNumber, float moneyAmount, Action onSuccess)
        {
            _db.Collection("Users").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log("task.IsFaulted = true");
                    return;
                }
                
                var snapshot = task.Result;

                foreach (var document in snapshot.Documents)
                {
                    var user = document.ConvertTo<User>();

                    if (user.Accounts != null && user.Accounts.Count > 0)
                    {
                        foreach (var account in user.Accounts)
                        {
                            if (account.Number.SequenceEqual(currentNumber))
                            {
                                var oldAccount = new Server.Account
                                {
                                    Name = account.Name,
                                    Number = account.Number,
                                    Сurrency = account.Сurrency,
                                    Amount = account.Amount
                                };
                                
                                var newAccount = new Server.Account
                                {
                                    Name = account.Name,
                                    Number = account.Number,
                                    Сurrency = account.Сurrency,
                                    Amount = account.Amount -= moneyAmount
                                };
                                
                                UpdateUser(document.Reference, oldAccount, newAccount, onSuccess);
                            }
                            else if (account.Number.SequenceEqual(targetNumber))
                            {
                                var oldAccount = new Server.Account
                                {
                                    Name = account.Name,
                                    Number = account.Number,
                                    Сurrency = account.Сurrency,
                                    Amount = account.Amount
                                };
                                
                                var newAccount = new Server.Account
                                {
                                    Name = account.Name,
                                    Number = account.Number,
                                    Сurrency = account.Сurrency,
                                    Amount = account.Amount += moneyAmount
                                };
                                
                                UpdateUser(document.Reference, oldAccount, newAccount, onSuccess);
                            }
                        }
                    }
                }
            });
        }

        private void InitializeFirebase()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;
            // _auth.StateChanged += AuthStateChanged;
            //
            // AuthStateChanged(this, null);
        }

        // On state changed
        // private void AuthStateChanged(object sender, EventArgs eventArgs)
        // {
        //     if (_auth.CurrentUser == _user)
        //     {
        //         return;
        //     }
        //     
        //     var signedIn = _user != _auth.CurrentUser && _auth.CurrentUser != null && _auth.CurrentUser.IsValid();
        //     
        //     // Signed out
        //     if (!signedIn && _user != null) {
        //         Debug.Log("Signed out " + _user.UserId);
        //     }
        //     
        //     _user = _auth.CurrentUser;
        //     
        //     // Signed in
        //     if (signedIn) {
        //         Debug.Log("Signed in " + _user.UserId);
        //
        //         GetLoggedInUser(() => onSignedIn?.Invoke());
        //     }
        // }

        private void CreateProfile(string id, int pin, int pesel, string firstName, string secondName)
        {
            var docRef = _db.Collection("Users").Document(firstName + " " + secondName);
            var profile = new User
            {
                Id = id,
                Pin = pin,
                Pesel = pesel,
                FirstName = firstName,
                SecondName = secondName
            };
            
            profile.CreateAccount("Main", "PLN", 5000);

            docRef.SetAsync(profile).ContinueWithOnMainThread(task =>
            {
                if(task.IsCompletedSuccessfully)
                {
                    Debug.Log("Profile saved!");
                }
                else
                {
                    Debug.LogError($"Failed to save profile: {task.Exception}");
                } 
            });
        }

        private void UpdateUser(DocumentReference docRef, Server.Account oldAccount, Server.Account newAccount, Action onSuccess)
        {
            onSuccess += () => _accountsUpdated = 0;
            
            docRef.UpdateAsync("Accounts", FieldValue.ArrayRemove(oldAccount)).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    docRef.UpdateAsync("Accounts", FieldValue.ArrayUnion(newAccount)).ContinueWithOnMainThread(task2 =>
                    {
                        if (task2.IsCompletedSuccessfully)
                        {
                            _accountsUpdated += 1;

                            if (_accountsUpdated == 2)
                            {
                                onSuccess?.Invoke();
                                GetLoggedInUser(() => onMoneySend?.Invoke());
                            }
                            
                            Debug.Log("Account updated successfully");
                        }
                        else
                        {
                            Debug.LogError("Failed to update account: " + task2.Exception);
                        }
                    });
                }
                else
                {
                    Debug.LogError("Remove account was failed: " + task.Exception);
                }
            });
        }

        private void GetLoggedInUser(Action onSuccess)
        {
            print(_db);
            print(_user);
            print(_user.UserId);
            print(_user.Email);
            var query = _db.Collection("Users").WhereEqualTo("Id", _user.UserId);

            query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var snapshot = task.Result;

                    foreach (var document in snapshot.Documents)
                    {
                        User = document.ConvertTo<User>();
                        break;
                    }
                }
                
                onSuccess?.Invoke();
            });
        }
    }
    
    [Serializable]
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    
    [Serializable]
    public class SuccessLogiData
    {
        public string UserId { get; set; }
        public string Email { get; set; }
    }
}
