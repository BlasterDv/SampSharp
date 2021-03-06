﻿using System;

namespace SampSharp.Core.Natives.NativeObjects
{
    public interface INativeObjectProxyFactory
    {
        /// <summary>
        ///     Creates a proxy instance of the specified <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type to create a proxy of.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The proxy instance.</returns>
        object CreateInstance(Type type, params object[] arguments);
    }
}