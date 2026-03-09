// Connectivity monitoring module for Privestio
// Tracks online/offline status and notifies Blazor via JS interop

let dotNetReference = null;

function isOnline() {
    return navigator.onLine;
}

function initialize(dotNetRef) {
    dotNetReference = dotNetRef;

    window.addEventListener('online', () => {
        if (dotNetReference) {
            dotNetReference.invokeMethodAsync('OnConnectivityChanged', true);
        }
    });

    window.addEventListener('offline', () => {
        if (dotNetReference) {
            dotNetReference.invokeMethodAsync('OnConnectivityChanged', false);
        }
    });

    return navigator.onLine;
}

function dispose() {
    dotNetReference = null;
}

window.connectivityFunctions = {
    isOnline,
    initialize,
    dispose
};
