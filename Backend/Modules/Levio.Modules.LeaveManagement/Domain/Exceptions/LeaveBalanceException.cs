using Levio.SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.LeaveManagement.Domain.Exceptions
{
    public class LeaveBalanceException: DomainException
    {
        public LeaveBalanceException(string message) : base(message)
        {
        }
    }
}
