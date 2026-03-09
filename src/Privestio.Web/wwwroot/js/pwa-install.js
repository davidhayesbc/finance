// PWA install prompt module for Privestio
// Captures the beforeinstallprompt event and provides install functionality

let deferredPrompt = null;
let isInstalled = false;

window.addEventListener('beforeinstallprompt', (e) => {
    // Prevent the default browser install prompt
    e.preventDefault();
    deferredPrompt = e;
});

window.addEventListener('appinstalled', () => {
    deferredPrompt = null;
    isInstalled = true;
});

function isInstallAvailable() {
    return deferredPrompt !== null && !isInstalled;
}

async function promptInstall() {
    if (!deferredPrompt) {
        return false;
    }

    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    deferredPrompt = null;

    return outcome === 'accepted';
}

window.pwaInstallFunctions = {
    isInstallAvailable,
    promptInstall
};
