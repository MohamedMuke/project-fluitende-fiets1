var formulier = document.getElementById("contactForm");
var melding = document.getElementById("melding");

function UpdateClock() {
    var clockElement = document.getElementById("clock");
    var statusText = document.getElementById("statusText");

    if (!clockElement || !statusText) {
        return;
    }

    var now = new Date();
    var uren = now.getHours();
    var minuten = now.getMinutes();
    var seconden = now.getSeconds();

    if (minuten < 10) {
        minuten = "0" + minuten;
    }

    if (seconden < 10) {
        seconden = "0" + seconden;
    }

    clockElement.innerHTML = uren + ":" + minuten + ":" + seconden;

    if (uren >= 10 && uren < 18) {
        statusText.innerHTML = "We zijn geopend";
        statusText.style.color = "green";
    } else {
        statusText.innerHTML = "We zijn gesloten";
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
            melding.innerHTML = "Vul alle velden in.";
            melding.style.color = "red";
        } else {
            melding.innerHTML = "Bedankt " + naam + "! Je bericht is verstuurd.";
            melding.style.color = "green";
            formulier.reset();
        }
    };
}

let huidigeFotoSlide = 1;

function toonFotocarrousel(nummer) {
    let slides = document.getElementsByClassName("home-slide");
    let punten = document.getElementsByClassName("home-slide-punt");

    if (slides.length == 0) {
        return;
    }

    if (nummer > slides.length) {
        nummer = 1;
    }

    if (nummer < 1) {
        nummer = slides.length;
    }

    huidigeFotoSlide = nummer;

    for (let i = 0; i < slides.length; i++) {
        slides[i].style.display = "none";
    }

    for (let i = 0; i < punten.length; i++) {
        punten[i].className = punten[i].className.replace(" actief", "");
    }

    slides[huidigeFotoSlide - 1].style.display = "block";
    punten[huidigeFotoSlide - 1].className += " actief";
}

function veranderFotocarrousel(stap) {
    toonFotocarrousel(huidigeFotoSlide + stap);
}

function kiesFotocarrousel(nummer) {
    toonFotocarrousel(nummer);
}

toonFotocarrousel(huidigeFotoSlide);




function berekenTotaal() {
    var totaal = 0;
    var gazelle = document.getElementById("gazelle");
    var orange = document.getElementById("orange");
    var stella = document.getElementById("stella");
    var totaalprijs = document.getElementById("totaalprijs");

    if (!totaalprijs) {
        return;
    }

    if (gazelle && gazelle.checked) {
        totaal = totaal + 699;
    }

    if (orange && orange.checked) {
        totaal = totaal + 899;
    }

    if (stella && stella.checked) {
        totaal = totaal + 1499;
    }

    totaalprijs.innerHTML = "Totaalprijs: € " + totaal.toFixed(2).replace(".", ",");
}

function verwijderFiets(checkboxId, rijId) {
    var checkbox = document.getElementById(checkboxId);
    var rij = document.getElementById(rijId);

    if (checkbox) {
        checkbox.checked = false;
    }

    if (rij) {
        rij.style.display = "none";
    }

    berekenTotaal();
}

function maakWinkelmandjeLeeg() {
    var gazelle = document.getElementById("gazelle");
    var orange = document.getElementById("orange");
    var stella = document.getElementById("stella");
    var rijGazelle = document.getElementById("rij-gazelle");
    var rijOrange = document.getElementById("rij-orange");
    var rijStella = document.getElementById("rij-stella");

    if (gazelle) {
        gazelle.checked = false;
    }
    if (orange) {
        orange.checked = false;
    }
    if (stella) {
        stella.checked = false;
    }

    if (rijGazelle) {
        rijGazelle.style.display = "none";
    }
    if (rijOrange) {
        rijOrange.style.display = "none";
    }
    if (rijStella) {
        rijStella.style.display = "none";
    }

    berekenTotaal();
}

function afrekenen() {
    var totaalprijs = document.getElementById("totaalprijs");

    if (totaalprijs && totaalprijs.innerHTML == "Totaalprijs: € 0,00") {
        alert("Je winkelmandje is leeg.");
    } else {
        alert("Je kunt nu verder met betalen.");
    }
}
if (carouselFoto) {
    setInterval(function () {
        huidigeFoto = huidigeFoto + 1;

        if (huidigeFoto >= fotos.length) {
            huidigeFoto = 0;
        }

        carouselFoto.src = fotos[huidigeFoto];
    }, 3000);
}

