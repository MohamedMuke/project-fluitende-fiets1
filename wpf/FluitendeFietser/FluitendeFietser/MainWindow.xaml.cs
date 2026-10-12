using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FluitendeFietser
{
    public partial class MainWindow : Window
    {
        private readonly List<Artikel> artikelen = new List<Artikel>();
        private readonly List<Bestelregel> bestelling = new List<Bestelregel>();
        private readonly DispatcherTimer klokTimer = new DispatcherTimer();
        private readonly DispatcherTimer avgTimer = new DispatcherTimer();
        private int secondenOver = 60;
        private bool betalingAfgerond = false;

        public MainWindow()
        {
            InitializeComponent();

            btnBestelArtikel.Click += BtnBestelArtikel_Click;
            btnVerwijderBestelregel.Click += BtnVerwijderBestelregel_Click;
            btnVolgendeKlant.Click += BtnVolgendeKlant_Click;
            btnBetalingAfronden.Click += BtnBetalingAfronden_Click;
            btnSetTime.Click += BtnSetTime_Click;
            cmbFietsen.SelectionChanged += SelectieGewijzigd;
            cmbVerzekeringen.SelectionChanged += SelectieGewijzigd;
            cmbServices.SelectionChanged += SelectieGewijzigd;
            txtAantalDagen.TextChanged += AantalDagenGewijzigd;
            txtOntvangen.TextChanged += OntvangenGewijzigd;
            PreviewMouseDown += GebruikerActief;
            PreviewKeyDown += GebruikerActief;

            klokTimer.Interval = TimeSpan.FromSeconds(1);
            klokTimer.Tick += (s, e) => ToonKlok();
            klokTimer.Start();
            ToonKlok();

            avgTimer.Interval = TimeSpan.FromSeconds(1);
            avgTimer.Tick += AvgTimer_Tick;
            avgTimer.Start();
            ToonAvg();

            LaadArtikelen();
            ToonBestelling();
        }

        private void LaadArtikelen()
        {
            string pad = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "artikelen.csv");
            if (!File.Exists(pad))
            {
                MessageBox.Show("Bestand niet gevonden: " + pad + "\nVoeg artikelen.csv toe en zet 'Copy to Output Directory' op 'Copy if newer'.");
                return;
            }

            try
            {
                foreach (string regel in File.ReadAllLines(pad))
                {
                    if (string.IsNullOrWhiteSpace(regel) || regel.StartsWith("#")) continue;
                    string[] velden = regel.Split(';');
                    if (velden.Length != 3) continue;
                    decimal prijs;
                    if (!decimal.TryParse(velden[2].Trim(), NumberStyles.Number, CultureInfo.GetCultureInfo("nl-NL"), out prijs))
                        continue;
                    artikelen.Add(new Artikel
                    {
                        Categorie = velden[0].Trim(),
                        Naam = velden[1].Trim(),
                        PrijsPerDag = prijs
                    });
                }

                cmbFietsen.ItemsSource = artikelen.Where(a => a.Categorie.Equals("Fietsen", StringComparison.OrdinalIgnoreCase)).ToList();
                cmbVerzekeringen.ItemsSource = artikelen.Where(a => a.Categorie.Equals("Verzekeringen", StringComparison.OrdinalIgnoreCase)).ToList();
                cmbServices.ItemsSource = artikelen.Where(a => a.Categorie.Equals("Services", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kan artikelen niet inlezen: " + ex.Message);
            }
        }

        private List<Artikel> GeselecteerdeArtikelen()
        {
            var gekozen = new List<Artikel>();
            if (cmbFietsen.SelectedItem is Artikel fiets) gekozen.Add(fiets);
            if (cmbVerzekeringen.SelectedItem is Artikel verzekering) gekozen.Add(verzekering);
            if (cmbServices.SelectedItem is Artikel service) gekozen.Add(service);
            return gekozen;
        }

        private void SelectieGewijzigd(object sender, SelectionChangedEventArgs e)
        {
            // Andere ComboBoxen niet leegmaken: meerdere keuzes zijn toegestaan.
            ToonGeselecteerdePrijs();
        }

        private void AantalDagenGewijzigd(object sender, TextChangedEventArgs e)
        {
            ToonGeselecteerdePrijs();
        }

        private void ToonGeselecteerdePrijs()
        {
            if (txtGeselecteerdePrijs == null) return;
            int dagen;
            if (int.TryParse(txtAantalDagen.Text, out dagen) && dagen > 0)
                txtGeselecteerdePrijs.Text = Geld(GeselecteerdeArtikelen().Sum(a => a.PrijsPerDag) * dagen);
            else
                txtGeselecteerdePrijs.Text = Geld(0);
        }

        private void BtnBestelArtikel_Click(object sender, RoutedEventArgs e)
        {
            if (betalingAfgerond)
            {
                MessageBox.Show("Deze bestelling is al betaald. Kies 'Volgende klant'.");
                return;
            }

            List<Artikel> gekozen = GeselecteerdeArtikelen();
            if (gekozen.Count == 0)
            {
                MessageBox.Show("Kies eerst een fiets, verzekering of service.");
                return;
            }

            int dagen;
            if (!int.TryParse(txtAantalDagen.Text, out dagen) || dagen <= 0)
            {
                MessageBox.Show("Vul een geldig aantal dagen in (minimaal 1).");
                return;
            }

            foreach (Artikel artikel in gekozen)
                bestelling.Add(new Bestelregel { Artikel = artikel, Dagen = dagen });

            // Pas na bestellen wissen we de keuzes, klaar voor een nieuwe selectie.
            cmbFietsen.SelectedIndex = -1;
            cmbVerzekeringen.SelectedIndex = -1;
            cmbServices.SelectedIndex = -1;
            txtAantalDagen.Text = "1";
            ToonBestelling();
            ToonGeselecteerdePrijs();
        }

        private void BtnVerwijderBestelregel_Click(object sender, RoutedEventArgs e)
        {
            if (betalingAfgerond) { MessageBox.Show("De betaling is al afgerond."); return; }
            Bestelregel regel = lstBestelling.SelectedItem as Bestelregel;
            if (regel == null) { MessageBox.Show("Selecteer eerst een bestelregel."); return; }
            bestelling.Remove(regel);
            ToonBestelling();
        }

        private decimal Totaal()
        {
            return bestelling.Sum(r => r.Totaal);
        }

        private void ToonBestelling()
        {
            lstBestelling.ItemsSource = null;
            lstBestelling.ItemsSource = bestelling;
            txtTotaalBedrag.Text = Geld(Totaal());
            ToonRetour();
        }

        private void OntvangenGewijzigd(object sender, TextChangedEventArgs e)
        {
            ToonRetour();
        }

        private void ToonRetour()
        {
            if (txtRetour == null || txtOntvangen == null) return;
            decimal ontvangen;
            if (!decimal.TryParse(txtOntvangen.Text, NumberStyles.Number,
                CultureInfo.GetCultureInfo("nl-NL"), out ontvangen) || ontvangen < 0)
            {
                txtRetour.Text = "Ongeldig bedrag";
                return;
            }
            decimal verschil = ontvangen - Totaal();
            txtRetour.Text = verschil >= 0
                ? "Retour: " + Geld(verschil)
                : "Nog betalen: " + Geld(-verschil);
        }

        private void BtnBetalingAfronden_Click(object sender, RoutedEventArgs e)
        {
            if (betalingAfgerond) { MessageBox.Show("Deze bestelling is al betaald."); return; }
            if (bestelling.Count == 0) { MessageBox.Show("Er staan nog geen artikelen in de bestelling."); return; }

            decimal ontvangen;
            if (!decimal.TryParse(txtOntvangen.Text, NumberStyles.Number,
                CultureInfo.GetCultureInfo("nl-NL"), out ontvangen) || ontvangen < Totaal())
            {
                MessageBox.Show("Het ontvangen bedrag is ongeldig of onvoldoende.");
                return;
            }

            betalingAfgerond = true;
            MessageBox.Show("Betaling geslaagd!\nTotaal: " + Geld(Totaal()) +
                "\nOntvangen: " + Geld(ontvangen) +
                "\nWisselgeld: " + Geld(ontvangen - Totaal()));
        }

        private void BtnVolgendeKlant_Click(object sender, RoutedEventArgs e)
        {
            if (bestelling.Count > 0 && !betalingAfgerond &&
                MessageBox.Show("Onbetaalde bestelling wissen?", "Nieuwe klant",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            NieuweKlant();
        }

        private void NieuweKlant()
        {
            bestelling.Clear();
            betalingAfgerond = false;
            cmbFietsen.SelectedIndex = -1;
            cmbVerzekeringen.SelectedIndex = -1;
            cmbServices.SelectedIndex = -1;
            txtAantalDagen.Text = "1";
            txtOntvangen.Text = "0,00";
            ToonBestelling();
            ResetAvg();
        }

        private void ToonKlok()
        {
            txtKlok.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void BtnSetTime_Click(object sender, RoutedEventArgs e)
        {
            // De klok gebruikt de systeemtijd; deze knop ververst de weergave.
            ToonKlok();
            MessageBox.Show("De klok gebruikt de tijd van Windows.");
        }

        private void GebruikerActief(object sender, InputEventArgs e)
        {
            ResetAvg();
        }

        private void ResetAvg()
        {
            secondenOver = 60;
            ToonAvg();
        }

        private void ToonAvg()
        {
            prgAVG.Value = secondenOver;
            txtAVGTimer.Text = secondenOver + " seconden";
        }

        private void AvgTimer_Tick(object sender, EventArgs e)
        {
            secondenOver--;
            if (secondenOver <= 0)
            {
                // Demo: na inactiviteit wordt de huidige bestelling gewist.
                // Voor echte AVG-beveiliging zijn aanvullende maatregelen nodig.
                NieuweKlant();
            }
            ToonAvg();
        }

        private static string Geld(decimal bedrag)
        {
            return bedrag.ToString("C", CultureInfo.GetCultureInfo("nl-NL"));
        }
    }

    public class Artikel
    {
        public string Categorie { get; set; }
        public string Naam { get; set; }
        public decimal PrijsPerDag { get; set; }
        public override string ToString() { return Naam + " (" + PrijsPerDag.ToString("C", CultureInfo.GetCultureInfo("nl-NL")) + "/dag)"; }
    }

    public class Bestelregel
    {
        public Artikel Artikel { get; set; }
        public int Dagen { get; set; }
        public decimal Totaal { get { return Artikel.PrijsPerDag * Dagen; } }
        public override string ToString()
        {
            return Artikel.Naam + " | " + Artikel.PrijsPerDag.ToString("C", CultureInfo.GetCultureInfo("nl-NL")) +
                "/dag | " + Dagen + " dagen | " + Totaal.ToString("C", CultureInfo.GetCultureInfo("nl-NL"));
        }
    }
}
