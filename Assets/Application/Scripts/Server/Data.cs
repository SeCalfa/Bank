using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;
using Random = UnityEngine.Random;

namespace Application.Scripts.Server
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public int Pin { get; set; }

        [FirestoreProperty]
        public string FirstName { get; set; }
        [FirestoreProperty]
        public string SecondName { get; set; }
        [FirestoreProperty]
        public int Pesel { get; set; }

        [FirestoreProperty]
        public List<Account> Accounts { get; set; } = new();

        public void CreateAccount(string name, string currency, float amount)
        {
            Accounts.Add(new Account
            {
                Name = name,
                Сurrency = currency,
                Amount = amount,
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
            });
        }
    }

    [FirestoreData]
    public class Account
    {
        [FirestoreProperty]
        public string Name { get; set; }
        [FirestoreProperty]
        public List<int> Number { get; set; }
        
        [FirestoreProperty]
        public string Сurrency { get; set; }
        [FirestoreProperty]
        public float Amount { get; set; }
        
        [FirestoreProperty]
        public List<AccountHistory> History { get; set; } = new();
    }
    
    [FirestoreData]
    public class AccountHistory
    {
        [FirestoreProperty]
        public string TransactionType { get; set; }
        [FirestoreProperty]
        public float Amount { get; set; }
        
        [FirestoreProperty]
        public string TargetAccountName { get; set; }
        [FirestoreProperty]
        public List<int> TargetAccountNumber { get; set; }
        
        [FirestoreProperty]
        public DateTime Date { get; set; }
    }
}
