// נוצר בגרסה 1 מהתבנית, ולא שונה.
// הקוד של חלון החיבור מחדש: מגיב לניתוק מהשרת ומנסה להתחבר שוב.

// חיבור הפעולות לאירועים: שינוי מצב החיבור, ולחיצה על הכפתורים שבחלון
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
        reconnectModal.close();
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // ניסיון החיבור מחזיר, בצורה אסינכרונית:
        // - אמת: החיבור הצליח
        // - שקר: הגענו לשרת, אבל הוא דחה את החיבור (למשל, הוא כבר לא מכיר את החיבור הקודם)
        // - חריגה: לא הצלחנו להגיע לשרת בכלל
        const successful = await Blazor.reconnect();
        if (!successful) {
            // הגענו לשרת, אבל החיבור הקודם כבר לא קיים.
            // טוענים את הדף מחדש, כדי שהמשתמש יוכל להמשיך לעבוד כמה שיותר מהר
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // נזרקה חריגה: השרת לא זמין כרגע
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
