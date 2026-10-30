using Levio.SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.Identity.Domain.Exceptions
{
    public class UnauthorizedDomainException : DomainException
    {
        public UnauthorizedDomainException(string message)
        : base(message)
        {
        }
    }
}
