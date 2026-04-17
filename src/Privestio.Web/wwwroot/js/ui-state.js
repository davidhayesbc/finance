const scrollStatePrefix = "privestio-scroll:";
let lastContextMenuPoint = null;

window.addEventListener("contextmenu", (event) => {
    lastContextMenuPoint = {
        x: event.clientX,
        y: event.clientY
    };
}, true);

function getScrollStateKey(key) {
    return `${scrollStatePrefix}${key}`;
}

function saveScrollState(key, focusId) {
    const payload = {
        x: window.scrollX,
        y: window.scrollY,
        focusId: focusId || null
    };

    sessionStorage.setItem(getScrollStateKey(key), JSON.stringify(payload));
    return true;
}

function restoreScrollState(key) {
    const raw = sessionStorage.getItem(getScrollStateKey(key));
    if (!raw) {
        return false;
    }

    sessionStorage.removeItem(getScrollStateKey(key));

    try {
        const state = JSON.parse(raw);
        window.requestAnimationFrame(() => {
            window.scrollTo({ left: state.x || 0, top: state.y || 0, behavior: "auto" });

            if (state.focusId) {
                focusElementById(state.focusId);
            }
        });

        return true;
    }
    catch {
        return false;
    }
}

function focusElementById(elementId) {
    if (!elementId) {
        return false;
    }

    window.requestAnimationFrame(() => {
        const element = document.getElementById(elementId);
        if (element instanceof HTMLElement) {
            element.focus({ preventScroll: true });
        }
    });

    return true;
}

function getLastContextMenuPoint() {
    return lastContextMenuPoint;
}

window.uiState = {
    saveScrollState,
    restoreScrollState,
    focusElementById,
    getLastContextMenuPoint
};
