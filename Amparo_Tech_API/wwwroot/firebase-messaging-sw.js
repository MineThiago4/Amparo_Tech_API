// Service worker for Firebase Messaging (background notifications)
// Replace firebaseConfig below or inject at build time.
importScripts('https://www.gstatic.com/firebasejs/9.23.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.23.0/firebase-messaging-compat.js');

const firebaseConfig = {
  // paste minimal config here or serve via template
  apiKey: "REPLACE_ME",
  authDomain: "REPLACE_ME",
  projectId: "REPLACE_ME",
  storageBucket: "REPLACE_ME",
  messagingSenderId: "REPLACE_ME",
  appId: "REPLACE_ME",
};

firebase.initializeApp(firebaseConfig);
const messaging = firebase.messaging();

// Background message handler
messaging.onBackgroundMessage(function(payload) {
  try {
    const notificationTitle = payload.notification?.title ?? 'Nova notificação';
    const notificationOptions = {
      body: payload.notification?.body ?? '',
      data: payload.data ?? {}
    };
    self.registration.showNotification(notificationTitle, notificationOptions);
  } catch (e) { console.error(e); }
});

self.addEventListener('notificationclick', function(event) {
  event.notification.close();
  const url = event.notification?.data?.link || '/';
  event.waitUntil(clients.matchAll({ type: 'window' }).then(windowClients => {
    for (let client of windowClients) {
      if (client.url === url && 'focus' in client) return client.focus();
    }
    if (clients.openWindow) return clients.openWindow(url);
  }));
});
