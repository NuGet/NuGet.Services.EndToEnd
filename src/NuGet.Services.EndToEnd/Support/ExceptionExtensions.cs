// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    public static class ExceptionExtensions
    {
        public static bool HasTypeOrInnerType<T>(this Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is T)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}
