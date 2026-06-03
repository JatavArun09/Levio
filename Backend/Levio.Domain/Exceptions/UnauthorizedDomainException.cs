using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Domain.Exceptions
{
    public class UnauthorizedDomainException : DomainException
    {
        public UnauthorizedDomainException(string message)
        : base(message)
        {
        }
    }
}
