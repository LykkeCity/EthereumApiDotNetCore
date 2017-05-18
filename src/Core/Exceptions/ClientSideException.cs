﻿using System;

namespace Core.Exceptions
{

    public class ClientSideException : Exception
    {
        public ExceptionType ExceptionType { get; private set; }

        public ClientSideException(ExceptionType exceptionType, string message) : base(message)
        {
            ExceptionType = exceptionType;
        }
    }
}
