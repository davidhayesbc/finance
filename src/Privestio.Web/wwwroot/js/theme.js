const THEME_STORAGE_KEY = 'privestio-theme';

let themeDotNetRef = null;
let themeMediaQuery = null;
let themeMediaHandler = null;

function getStoredPreference() {
    return localStorage.getItem(THEME_STORAGE_KEY) || 'system';
}

function resolveTheme(preference) {
    if (preference === 'light' || preference === 'dark') {
        return preference;
    }

    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function applyTheme(preference) {
    const resolvedTheme = resolveTheme(preference);
    document.documentElement.dataset.theme = resolvedTheme;
    document.documentElement.style.colorScheme = resolvedTheme;

    return {
        preference,
        resolvedTheme
    };
}

function notifyThemeChanged() {
    if (!themeDotNetRef) {
        return;
    }

    themeDotNetRef.invokeMethodAsync('OnThemeChanged', applyTheme(getStoredPreference()));
}

function initialize(dotNetRef) {
    themeDotNetRef = dotNetRef;
    themeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    if (themeMediaHandler === null) {
        themeMediaHandler = () => {
            if (getStoredPreference() === 'system') {
                notifyThemeChanged();
            }
        };
    }

    themeMediaQuery.addEventListener('change', themeMediaHandler);
    return applyTheme(getStoredPreference());
}

function setPreference(preference) {
    const normalizedPreference = ['system', 'light', 'dark'].includes(preference)
        ? preference
        : 'system';

    localStorage.setItem(THEME_STORAGE_KEY, normalizedPreference);
    const state = applyTheme(normalizedPreference);
    notifyThemeChanged();
    return state;
}

function dispose() {
    if (themeMediaQuery && themeMediaHandler) {
        themeMediaQuery.removeEventListener('change', themeMediaHandler);
    }

    themeDotNetRef = null;
}

window.themeFunctions = {
    initialize,
    setPreference,
    dispose
};
