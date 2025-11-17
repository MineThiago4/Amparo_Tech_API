// Example snippet to initialize Firebase messaging and register token
import { initializeApp } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-app.js';
import { getMessaging, getToken, onMessage } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-messaging.js';

export async function initFirebaseAndRegisterToken(firebaseConfig, vapidKey, apiBase, jwt) {
  const app = initializeApp(firebaseConfig);
  const messaging = getMessaging(app);

  try {
    const token = await getToken(messaging, { vapidKey });
    if (token) {
      // send to API
      await fetch(`${apiBase}/api/devicetokens`, {
        method: 'POST', headers: { 'Content-Type':'application/json', 'Authorization': `Bearer ${jwt}` },
        body: JSON.stringify({ token, platform: 'web' })
      });
    }
  } catch (err) {
    console.error('Unable to get permission to notify.', err);
  }

  onMessage(messaging, (payload) => {
    console.log('Message received. ', payload);
    // show in-app toast
  });
}
