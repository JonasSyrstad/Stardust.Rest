using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    public interface IExceptionToResponseHandler
    {
        bool OverrideDefault { get; }
        IActionResult ConvertToErrorResponse(Exception exception, HttpResponse result, ControllerBase serviceWrapperBase);
    }
}
