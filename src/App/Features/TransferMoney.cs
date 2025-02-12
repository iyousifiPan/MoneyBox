using System;
using App.DataAccess;
using App.Domain;
using App.Domain.Services;

namespace App.Features
{
    public class TransferMoney
    {
        private readonly IAccountRepository _accountRepository;
        private readonly INotificationService _notificationService;

        public TransferMoney(IAccountRepository accountRepository, INotificationService notificationService)
        {
            this._accountRepository = accountRepository;
            this._notificationService = notificationService;
        }

        public void Execute(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            var from = this._accountRepository.GetAccountById(fromAccountId);
            var to = this._accountRepository.GetAccountById(toAccountId);

            var fromBalance = from.Balance - amount;
            switch (fromBalance)
            {
                case < 0m:
                    throw new InvalidOperationException("Insufficient funds to make transfer");
                case < 500m:
                    this._notificationService.NotifyFundsLow(from.User.Email);
                    break;
            }

            var paidIn = to.PaidIn + amount;
            if (paidIn > Account.PayInLimit)
            {
                throw new InvalidOperationException("Account pay in limit reached");
            }

            if (Account.PayInLimit - paidIn < 500m)
            {
                this._notificationService.NotifyApproachingPayInLimit(to.User.Email);
            }

            from.Balance = from.Balance - amount;
            from.Withdrawn = from.Withdrawn - amount;

            to.Balance = to.Balance + amount;
            to.PaidIn = to.PaidIn + amount;

            this._accountRepository.Update(from);
            this._accountRepository.Update(to);
        }
    }
}
