﻿using System;

namespace Netly.Interfaces
{
    public interface IToHttpServer
    {
        /// <summary>
        /// Open Server Connection
        /// </summary>
        /// <param name="host">Server Uri</param>
        void Open(Uri host);


        /// <summary>
        /// Close Server Connection
        /// </summary>
        void Close();
    }
}