import { DotNet } from '@microsoft/dotnet-js-interop';
let manager: DotNet.DotNetObject | undefined;

// These are the public APIs at Blazor.rootComponents.*
export const RootComponentsFunctions = {
    async add(toElement: Element, componentIdentifier: FunctionStringCallback): Promise<DynamicRootComponent> {
        // TODO: Figure out how to describe the insertion location
        const componentId = await getRequiredManager().invokeMethodAsync<number>(
            'AddRootComponent', componentIdentifier, '#test');
        return new DynamicRootComponent(componentId);
    }
};

class DynamicRootComponent {
    private _componentId: number | null;

    constructor(componentId: number) {
        this._componentId = componentId;
    }

    setParameters(parameters: any) {
        return getRequiredManager().invokeMethodAsync('RenderRootComponentAsync', this._componentId, parameters);
    }

    async dispose() {
        if (this._componentId !== null) {
            await getRequiredManager().invokeMethodAsync('RemoveRootComponent', this._componentId);
            this._componentId = null; // Ensure it can't be used again
        }
    }
}

// Called by the framework
export function setDynamicRootComponentManager(instance: DotNet.DotNetObject) {
    if (manager) {
        // This will only happen in very nonstandard cases where someone has multiple hosts.
        // It's up to the developer to ensure that only one of them enables dynamic root components.
        throw new Error('Dynamic root components have already been enabled.');
    }

    manager = instance;
}

function getRequiredManager(): DotNet.DotNetObject {
    if (!manager) {
        throw new Error('Dynamic root components have not been enabled in this application.');
    }

    return manager;
}
