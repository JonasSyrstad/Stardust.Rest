using System;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IErrorCategorizer
    {
        bool IsTransientError(Exception exception);
    }
}