using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Domain.Exceptions
{
    public class LeaveBalanceException: DomainException
    {
        public LeaveBalanceException(string message) : base(message)
        {
        }
    }
}
