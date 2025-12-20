const gatewayBase = window.location.origin.replace(/:\d+$/, ':8080');
const logEl = document.getElementById('log');
const ordersEl = document.getElementById('orders');
const balanceEl = document.getElementById('balance');

let connection;
let currentUserId;
let lastAccountId;

const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function generateGuid() {
    return (crypto?.randomUUID?.() ??
        'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        })).toLowerCase();
}

function log(message) {
    const time = new Date().toISOString();
    logEl.textContent += `[${time}] ${message}\n`;
    logEl.scrollTop = logEl.scrollHeight;
}

function ensureUserId() {
    const input = document.getElementById('userId');
    let value = input.value.trim();
    if (!value) {
        value = generateGuid();
        input.value = value;
        log(`Generated userId: ${value}`);
    }
    if (!guidRegex.test(value)) {
        alert('Введите корректный GUID (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)');
        throw new Error('invalid guid');
    }
    currentUserId = value;
    return value;
}

async function connectWs() {
    const userId = ensureUserId();
    if (connection && connection.state === 'Connected') {
        log('Already connected');
        return;
    }

    if (!connection) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl(`${gatewayBase}/hubs/orders`)
        .withAutomaticReconnect()
        .build();

    connection.on('OrderStatusChanged', payload => {
        log(`Order ${payload.id} -> ${payload.status}`);
    });

    connection.onreconnected(() => log('Reconnected to SignalR'));
    connection.onclose(() => log('SignalR connection closed'));
    }

    if (connection.state !== 'Connected') {
    await connection.start();
    log(`SignalR connected for user ${userId}`);
    }
}

async function joinOrder(orderId) {
    if (!connection || connection.state !== 'Connected') {
        await connectWs();
    }
    await connection.invoke('JoinOrder', orderId);
    log(`Subscribed to order ${orderId}`);
}

async function createAccount() {
    const userId = ensureUserId();
    const response = await fetch(`${gatewayBase}/api/accounts`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId })
    });
    if (!response.ok) {
        if (response.status === 409) {
            const data = await response.json().catch(() => null);
            if (data) {
                lastAccountId = data.accountId;
                balanceEl.textContent = `Balance: ${data.balance}`;
                log(`Account already exists ${data.accountId}, balance ${data.balance}`);
                return;
            }
        }
        const text = await response.text();
        log(`Account create failed: ${response.status} ${text}`);
        return;
    }
    const data = await response.json();
    lastAccountId = data.accountId;
    balanceEl.textContent = `Balance: ${data.balance}`;
    log(`Account created ${data.accountId}, balance ${data.balance}`);
}

async function topUp() {
    const userId = ensureUserId();
    if (!lastAccountId) {
        const balance = await getBalanceByUser(userId);
        if (!balance) {
            alert('Create account first');
            return;
        }
        lastAccountId = balance.accountId;
    }
    const amount = Number(document.getElementById('topupAmount').value);
    if (!Number.isFinite(amount) || amount <= 0) {
        alert('Top up amount must be > 0');
        return;
    }
    const response = await fetch(`${gatewayBase}/api/accounts/${lastAccountId}/topup`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount })
    });
    if (!response.ok) {
        const text = await response.text();
        log(`Top up failed: ${response.status} ${text}`);
        return;
    }
    const data = await response.json();
    balanceEl.textContent = `Balance: ${data.balance}`;
    log(`Top up success. Balance ${data.balance}`);
}

async function getBalanceByUser(userId) {
    const response = await fetch(`${gatewayBase}/api/accounts/by-user/${userId}/balance`);
    if (response.status !== 200) {
        return null;
    }
    return response.json();
}

async function getBalance() {
    const userId = ensureUserId();
    const data = await getBalanceByUser(userId);
    if (!data) {
        alert('Account not found');
        return;
    }
    lastAccountId = data.accountId;
    balanceEl.textContent = `Balance: ${data.balance}`;
    log(`Balance: ${data.balance}`);
}

async function createOrder() {
    const userId = ensureUserId();
    await connectWs();
    const amount = Number(document.getElementById('orderAmount').value);
    if (!Number.isFinite(amount) || amount <= 0) {
        alert('Order amount must be > 0');
        return;
    }
    const description = document.getElementById('orderDescription').value;
    const response = await fetch(`${gatewayBase}/api/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, amount, description })
    });
    if (!response.ok) {
        const text = await response.text();
        log(`Order create failed: ${response.status} ${text}`);
        return;
    }
    const data = await response.json();
    log(`Order created ${data.id} status ${data.status}`);
    await joinOrder(data.id);
}

async function loadOrders() {
    const userId = ensureUserId();
    await connectWs();
    const response = await fetch(`${gatewayBase}/api/orders?userId=${userId}`);
    if (!response.ok) {
        const text = await response.text();
        log(`Load orders failed: ${response.status} ${text}`);
        return;
    }
    const list = await response.json();
    const statusText = status => {
        if (typeof status === 'string') return status;
        switch (status) {
            case 0: return 'New';
            case 1: return 'Finished';
            case 2: return 'Cancelled';
            default: return status;
        }
    };
    ordersEl.innerHTML = list.map(o => `<div>#${o.id} — ${statusText(o.status)} — ${o.amount}</div>`).join('');
    log(`Loaded ${list.length} orders`);
    for (const order of list) {
        await joinOrder(order.id);
    }
}

const wrap = fn => async () => {
    try {
        await fn();
    } catch (err) {
        log(`Error: ${err?.message ?? err}`);
        console.error(err);
    }
};

window.addEventListener('error', e => {
    log(`Global error: ${e.message}`);
});
window.addEventListener('unhandledrejection', e => {
    log(`Unhandled rejection: ${e.reason}`);
});

document.getElementById('connectWs').addEventListener('click', wrap(connectWs));
document.getElementById('createAccount').addEventListener('click', wrap(createAccount));
document.getElementById('topup').addEventListener('click', wrap(topUp));
document.getElementById('getBalance').addEventListener('click', wrap(getBalance));
document.getElementById('createOrder').addEventListener('click', wrap(createOrder));
document.getElementById('loadOrders').addEventListener('click', wrap(loadOrders));

// Сгенерировать удобный userId по умолчанию
document.getElementById('userId').value = generateGuid();

