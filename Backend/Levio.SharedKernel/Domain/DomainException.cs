using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.SharedKernel.Domain
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }
}
