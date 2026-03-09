// IndexedDB module for Privestio offline storage
// Provides CRUD operations and sync queue management

const DB_NAME = 'privestio-offline-db';
const DB_VERSION = 1;
const STORE_ACCOUNTS = 'accounts';
const STORE_RECENT_TRANSACTIONS = 'recentTransactions';
const STORE_SYNC_QUEUE = 'syncQueue';

let dbInstance = null;

function openDatabase() {
    return new Promise((resolve, reject) => {
        if (dbInstance) {
            resolve(dbInstance);
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;

            if (!db.objectStoreNames.contains(STORE_ACCOUNTS)) {
                db.createObjectStore(STORE_ACCOUNTS, { keyPath: 'id' });
            }

            if (!db.objectStoreNames.contains(STORE_RECENT_TRANSACTIONS)) {
                db.createObjectStore(STORE_RECENT_TRANSACTIONS, { keyPath: 'id' });
            }

            if (!db.objectStoreNames.contains(STORE_SYNC_QUEUE)) {
                db.createObjectStore(STORE_SYNC_QUEUE, { keyPath: 'operationId', autoIncrement: true });
            }
        };

        request.onsuccess = (event) => {
            dbInstance = event.target.result;
            resolve(dbInstance);
        };

        request.onerror = (event) => {
            console.error('IndexedDB open error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function putItem(storeName, item) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.put(item);

        request.onsuccess = () => resolve(true);
        request.onerror = (event) => {
            console.error('IndexedDB put error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function getItem(storeName, key) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.get(key);

        request.onsuccess = () => resolve(request.result || null);
        request.onerror = (event) => {
            console.error('IndexedDB get error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function getAllItems(storeName) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.getAll();

        request.onsuccess = () => resolve(request.result || []);
        request.onerror = (event) => {
            console.error('IndexedDB getAll error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function deleteItem(storeName, key) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.delete(key);

        request.onsuccess = () => resolve(true);
        request.onerror = (event) => {
            console.error('IndexedDB delete error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function clearStore(storeName) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.clear();

        request.onsuccess = () => resolve(true);
        request.onerror = (event) => {
            console.error('IndexedDB clear error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function addToSyncQueue(operation) {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE_SYNC_QUEUE, 'readwrite');
        const store = tx.objectStore(STORE_SYNC_QUEUE);
        const entry = {
            ...operation,
            timestamp: new Date().toISOString()
        };
        const request = store.add(entry);

        request.onsuccess = () => resolve(request.result);
        request.onerror = (event) => {
            console.error('IndexedDB addToSyncQueue error:', event.target.error);
            reject(event.target.error);
        };
    });
}

async function getSyncQueue() {
    return await getAllItems(STORE_SYNC_QUEUE);
}

async function clearSyncQueue() {
    return await clearStore(STORE_SYNC_QUEUE);
}

// Expose functions globally for Blazor JS interop
window.indexedDbFunctions = {
    openDatabase,
    putItem,
    getItem,
    getAllItems,
    deleteItem,
    clearStore,
    addToSyncQueue,
    getSyncQueue,
    clearSyncQueue
};
