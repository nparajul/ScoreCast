import { initializeApp } from "https://www.gstatic.com/firebasejs/11.6.0/firebase-app.js";
import { getAuth, signInWithEmailAndPassword, createUserWithEmailAndPassword,
         signOut, onAuthStateChanged, GoogleAuthProvider, signInWithPopup,
         updateProfile, getIdToken } from "https://www.gstatic.com/firebasejs/11.6.0/firebase-auth.js";

let auth = null;
let currentUser = null;
let dotNetRef = null;

window.firebaseAuth = {
    init(config, objRef) {
        const app = initializeApp(config);
        auth = getAuth(app);
        dotNetRef = objRef;
        onAuthStateChanged(auth, user => {
            currentUser = user;
            dotNetRef?.invokeMethodAsync("OnAuthStateChanged", user ? {
                uid: user.uid,
                email: user.email,
                displayName: user.displayName
            } : null);
        });
    },

    async signInWithEmail(email, password) {
        try {
            const result = await signInWithEmailAndPassword(auth, email, password);
            return { success: true, uid: result.user.uid };
        } catch (e) {
            return { success: false, error: mapError(e.code) };
        }
    },

    async registerWithEmail(email, password, displayName) {
        try {
            const result = await createUserWithEmailAndPassword(auth, email, password);
            if (displayName) await updateProfile(result.user, { displayName });
            return { success: true, uid: result.user.uid };
        } catch (e) {
            return { success: false, error: mapError(e.code) };
        }
    },

    async signInWithGoogle() {
        try {
            const result = await signInWithPopup(auth, new GoogleAuthProvider());
            return { success: true, uid: result.user.uid };
        } catch (e) {
            return { success: false, error: mapError(e.code) };
        }
    },

    async signOut() {
        await signOut(auth);
    },

    async getIdToken() {
        if (!currentUser) return null;
        return await getIdToken(currentUser, false);
    }
};

function mapError(code) {
    switch (code) {
        case "auth/invalid-credential": return "Invalid email or password";
        case "auth/user-not-found": return "No account found with this email";
        case "auth/wrong-password": return "Invalid email or password";
        case "auth/email-already-in-use": return "An account with this email already exists";
        case "auth/weak-password": return "Password is too weak";
        case "auth/too-many-requests": return "Too many attempts. Please try again later";
        case "auth/popup-closed-by-user": return "Sign-in cancelled";
        default: return code;
    }
}
