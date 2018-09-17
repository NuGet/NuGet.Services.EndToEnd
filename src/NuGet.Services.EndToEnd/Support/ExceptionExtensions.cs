// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.Services.EndToEnd.Support
{
    public static class ExceptionExtensions
    {
        public static bool HasTypeOrInnerType<T>(this Exception ex) where T : Exception
        {
            return ex.HasTypeOrInnerType<T>(out var typedException);
        }

        public static bool HasTypeOrInnerType<T>(this Exception ex, out T typedException) where T : Exception
        {
            var current = ex;
            while (current != null)
            {
                if (current is T)
                {
                    typedException = (T)current;
                    return true;
                }

                current = current.InnerException;
            }

            typedException = null;
            return false;
        }
    }
}
