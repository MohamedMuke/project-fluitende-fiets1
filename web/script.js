var formulier = document.getElementById("contactForm");
var melding = document.getElementById("melding");


function UpdateClock() {
    // Controleer of de klok op deze pagina bestaat; zo niet, stop direct 
    const clockElement = document.getElementById("clock"); 
    const statusText = document.getElementById("statusText"); 
    if (!clockElement || !statusText) return;
    const now = new Date();

    let hours = now.getHours();
    const meridiem = hours >= 12 ? "PM" : "AM";

    hours = hours % 12 || 12;
    hours = hours.toString().padStart(2, "0");

    const minutes = now.getMinutes().toString().padStart(2, "0");
    const seconds = now.getSeconds().toString().padStart(2, "0");

    const timeString = hours + ":" + minutes + ":" + seconds + " " + meridiem;

    document.getElementById("clock").textContent = timeString;

    //const statusText = document.getElementById("statusText");

    if (now.getHours() >= 10 && now.getHours() < 18) {
        statusText.textContent = "We zijn geopend";
        statusText.style.color = "green";
    } else {
        statusText.textContent = "We zijn gesloten";
        statusText.style.color = "red";
    }
}

UpdateClock();
setInterval(UpdateClock, 1000);


if (formulier) {
    formulier.onsubmit = function (event) {
        event.preventDefault();

        var naam = document.getElementById("naam").value.trim();
        var email = document.getElementById("email").value.trim();
        var onderwerp = document.getElementById("onderwerp").value.trim();
        var bericht = document.getElementById("bericht").value.trim();

        if (naam == "" || email == "" || onderwerp == "" || bericht == "") {
            melding.innerText = "Vul alle velden in.";
            melding.style.color = "red";
        }
        else {
            melding.innerText = "Bedankt " + naam + "! Je bericht is verstuurd.";
            melding.style.color = "green";

            formulier.reset();
        }
    };

}

