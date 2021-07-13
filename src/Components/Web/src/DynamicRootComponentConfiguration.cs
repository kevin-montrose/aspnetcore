// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Configures options for allowing JavaScript to add root components dynamically.
    /// </summary>
    public class DynamicRootComponentConfiguration
    {
        internal readonly List<(Type ComponentType, string Identifier)> AllowedComponents = new();

        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        [DynamicDependency(nameof(DynamicRootComponentInterop.AddRootComponent), typeof(DynamicRootComponentInterop))]
        public void Register<[DynamicallyAccessedMembers(Component)] TComponent>(string name) where TComponent : IComponent
        {
            AllowedComponents.Add((typeof(TComponent), name));
        }
    }
}
