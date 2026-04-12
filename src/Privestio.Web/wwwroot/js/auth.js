const AUTH_KEYS = new Set(['privestio_token', 'privestio_refresh_token']);

let authDotNetRef = null;
let authStorageHandler = null;

function initialize(dotNetRef) {
    authDotNetRef = dotNetRef;

    if (authStorageHandler === null) {
        authStorageHandler = (event) => {
            if (!authDotNetRef || !AUTH_KEYS.has(event.key)) {
                return;
            }

            authDotNetRef.invokeMethodAsync('OnStorageChanged');
        };
    }

    window.addEventListener('storage', authStorageHandler);
}

function dispose() {
    if (authStorageHandler !== null) {
        window.removeEventListener('storage', authStorageHandler);
    }

    authDotNetRef = null;
}

window.authFunctions = {
    initialize,
    dispose
};
