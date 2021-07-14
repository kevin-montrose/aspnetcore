// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Infrastructure
{
    // This type is a way of exposing some of the renderer methods to JS interop without
    // otherwise changing the public API surface.

    /// <summary>
    /// Contains methods called by interop. Intended for framework use only, not supported for use in application
    /// code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DynamicRootComponentInterop : IDisposable
    {
        private const int MaxParameters = 100;
        private readonly DotNetObjectReference<DynamicRootComponentInterop> _selfReference;
        private readonly IJSRuntime _jsRuntime;
        private readonly Func<Type, string, int> _addRootComponent;
        private readonly Func<int, ParameterView, Task> _renderRootComponentAsync;
        private readonly Action<int> _removeRootComponent;
        private readonly Dictionary<string, Type> _allowedComponentTypes;

        internal DynamicRootComponentInterop(
            DefaultDynamicRootComponentConfiguration configurationBuilder,
            IJSRuntime jsRuntime,
            Func<Type, string, int> addRootComponent,
            Func<int, ParameterView, Task> renderRootComponentAsync,
            Action<int> removeRootComponent)
        {
            _selfReference = DotNetObjectReference.Create(this);
            _jsRuntime = jsRuntime;
            _addRootComponent = addRootComponent;
            _removeRootComponent = removeRootComponent;
            _renderRootComponentAsync = renderRootComponentAsync;

            // Snapshot the config to ensure it's not mutated later
            _allowedComponentTypes = new(configurationBuilder.AllowedComponentsByIdentifier, StringComparer.Ordinal);
        }

        internal async ValueTask InitializeAsync()
        {
            if (_allowedComponentTypes.Count > 0)
            {
                await _jsRuntime.InvokeVoidAsync(
                    "Blazor._internal.setDynamicRootComponentManager",
                    _selfReference);
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public int AddRootComponent(string componentIdentifier, string domElementSelector)
        {
            if (!_allowedComponentTypes.TryGetValue(componentIdentifier, out var componentType))
            {
                throw new ArgumentException($"There is no registered dynamic root component with identifier '{componentIdentifier}'.");
            }

            return _addRootComponent(componentType, domElementSelector);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public Task RenderRootComponentAsync(int componentId, int parameterCount, JsonElement parameters)
        {
            // In case the client misreports the number of parameters, impose bounds so we know the amount
            // of work done is limited to a fixed, low amount.
            if (parameterCount < 0 || parameterCount > MaxParameters)
            {
                throw new ArgumentOutOfRangeException($"{nameof(parameterCount)} must be between 0 and {MaxParameters}.");
            }

            // TODO: Use the [Parameter] attributes on the component type to create a precached
            // JsonElement->ParameterView parser that respects the declared parameter types, not
            // the actual types in the JSON data.
            var parameterViewBuilder = new ParameterViewBuilder(parameterCount);
            foreach (var jsonElement in parameters.EnumerateObject())
            {
                switch (jsonElement.Value.ValueKind)
                {
                    case JsonValueKind.Number:
                        parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetInt32());
                        break;
                    case JsonValueKind.String:
                        parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetString());
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        parameterViewBuilder.Add(jsonElement.Name, jsonElement.Value.GetBoolean());
                        break;
                }
            }

            return _renderRootComponentAsync(componentId, parameterViewBuilder.ToParameterView());
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void RemoveRootComponent(int componentId)
            => _removeRootComponent(componentId);

        /// <inheritdoc />
        public void Dispose()
            => _selfReference.Dispose();
    }
}
