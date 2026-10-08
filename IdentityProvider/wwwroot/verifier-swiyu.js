
const verificationId = document.getElementById('verificationId');
const form = document.getElementById("pollSwiyu");
const buttonPollSwiyu = document.getElementById("buttonPollSwiyu");

if (verificationId != null) {

    let checkStatus = setInterval(function () {
        if (verificationId) {
            if (form) {
                form.submit();
            }
            clearInterval(checkStatus)
        }
    }, 1500);
}