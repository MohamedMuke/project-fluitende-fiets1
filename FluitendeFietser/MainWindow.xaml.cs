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
        private TimeSpan klokVerschil = TimeSpan.Zero;



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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (grdResponsive == null ||
                pnlArtikel == null ||
                pnlBestelling == null)
                return;

            bool smalScherm = ActualWidth < 1050;

            if (smalScherm)
            {
                // Smal scherm: onderdelen onder elkaar.
                grdResponsive.ColumnDefinitions[0].Width =
                    new GridLength(1, GridUnitType.Star);

                grdResponsive.ColumnDefinitions[1].Width =
                    new GridLength(0);

                Grid.SetColumn(pnlArtikel, 0);
                Grid.SetRow(pnlArtikel, 0);

                Grid.SetColumn(pnlBestelling, 0);
                Grid.SetRow(pnlBestelling, 1);

                pnlArtikel.Margin = new Thickness(0, 0, 0, 12);
                pnlBestelling.Margin = new Thickness(0);
            }
            else
            {
                // Desktop: 40% links, 60% rechts.
                grdResponsive.ColumnDefinitions[0].Width =
                    new GridLength(2, GridUnitType.Star);

                grdResponsive.ColumnDefinitions[1].Width =
                    new GridLength(3, GridUnitType.Star);

                Grid.SetColumn(pnlArtikel, 0);
                Grid.SetRow(pnlArtikel, 0);

                Grid.SetColumn(pnlBestelling, 1);
                Grid.SetRow(pnlBestelling, 0);

                pnlArtikel.Margin = new Thickness(0, 0, 6, 0);
                pnlBestelling.Margin = new Thickness(6, 0, 0, 0);
            }
        }



        private void btnRekenmachine_Click(object sender, RoutedEventArgs e)
        {
            RekenmachineWindow rekenmachine = new RekenmachineWindow();
            rekenmachine.Owner = this;
            rekenmachine.ShowDialog();
        }

        private void LaadArtikelen()
        {
            string pad = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "prijzen_fietsen_verzekeringen_services.csv");

            if (!File.Exists(pad))
            {
                MessageBox.Show("CSV-bestand niet gevonden: " + pad);
                return;
            }

            try
            {
                artikelen.Clear();

                foreach (string regel in File.ReadAllLines(pad))
                {
                    string schoon = regel.Trim().Trim('"');

                    if (string.IsNullOrWhiteSpace(schoon))
                        continue;

                    string[] velden = schoon.Split(';');

                    if (velden.Length != 3)
                        continue;

                    string categorie = velden[0].Trim();
                    string naam = velden[1].Trim();

                    string prijsTekst = velden[2]
                        .Replace("€", "")
                        .Replace("/ dag", "")
                        .Replace("/dag", "")
                        .Trim()
                        .Replace(",", ".");

                    decimal prijs;

                    if (!decimal.TryParse(
                        prijsTekst,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out prijs))
                        continue;

                    artikelen.Add(new Artikel
                    {
                        Categorie = categorie,
                        Naam = naam,
                        PrijsPerDag = prijs
                    });
                }

                cmbFietsen.ItemsSource = artikelen
                    .Where(a => a.Categorie.Equals(
                        "fietsen",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                cmbVerzekeringen.ItemsSource = artikelen
                    .Where(a => a.Categorie.Equals(
                        "verzekeringen",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                cmbServices.ItemsSource = artikelen
                    .Where(a => a.Categorie.Equals(
                        "services",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij laden: " + ex.Message);
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
            if (sender is ComboBox gekozen && gekozen.SelectedItem != null)
            {
                if (gekozen != cmbFietsen) cmbFietsen.SelectedIndex = -1;
                if (gekozen != cmbVerzekeringen) cmbVerzekeringen.SelectedIndex = -1;
                if (gekozen != cmbServices) cmbServices.SelectedIndex = -1;
            }
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

        }

        private void BtnBetalingAfronden_Click(object sender, RoutedEventArgs e)
        {
            if (betalingAfgerond)
            {
                MessageBox.Show("Deze bestelling is al betaald.");
                return;
            }
            if (bestelling.Count == 0)
            {
                MessageBox.Show("Voeg eerst een artikel toe aan de bestelling.");
                return;
            }

            var betaalVenster = new BetaalWindow(Totaal()) { Owner = this };
            if (betaalVenster.ShowDialog() == true)
            {
                betalingAfgerond = true;
                MessageBox.Show("Betaling afgerond. Klik op 'Volgende klant'.");
            }
        }

        private void BtnVolgendeKlant_Click(object sender, RoutedEventArgs e)
        {
            if (bestelling.Count == 0)
            {
                MessageBox.Show("Er is geen bestelling om af te sluiten.");
                return;
            }
            if (!betalingAfgerond &&
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
            ToonBestelling();
            ResetAvg();
        }

        private void ToonKlok()
        {
            txtKlok.Text = DateTime.Now.Add(klokVerschil).ToString("HH:mm:ss");
        }

        private void BtnSetTime_Click(object sender, RoutedEventArgs e)
        {
            // Een klein invoervenster zonder extra XAML-bestand.
            Window invoerVenster = new Window
            {
                Title = "Klok instellen",
                Width = 350,
                Height = 205,
                MinWidth = 300,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            StackPanel inhoud = new StackPanel { Margin = new Thickness(20) };
            inhoud.Children.Add(new TextBlock
            {
                Text = "Voer de gewenste tijd in (uu:mm:ss):",
                Margin = new Thickness(0, 0, 0, 10)
            });

            TextBox tijdInvoer = new TextBox
            {
                Text = DateTime.Now.Add(klokVerschil).ToString("HH:mm:ss"),
                FontSize = 20,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 14)
            };
            inhoud.Children.Add(tijdInvoer);

            StackPanel knoppen = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button annuleren = new Button
            {
                Content = "Annuleren",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            annuleren.Click += (s, args) => invoerVenster.DialogResult = false;

            Button opslaan = new Button
            {
                Content = "Instellen",
                Width = 90,
                Height = 32
            };
            opslaan.Click += (s, args) =>
            {
                DateTime gewensteTijd;
                bool geldig = DateTime.TryParseExact(
                    tijdInvoer.Text.Trim(),
                    new[] { "HH:mm:ss", "HH:mm" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out gewensteTijd);

                if (!geldig)
                {
                    MessageBox.Show("Vul een geldige tijd in, bijvoorbeeld 14:30:00.",
                        "Ongeldige tijd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime nu = DateTime.Now;
                DateTime gewensteDatumTijd = nu.Date.Add(gewensteTijd.TimeOfDay);
                klokVerschil = gewensteDatumTijd - nu;
                ToonKlok();
                invoerVenster.DialogResult = true;
            };

            knoppen.Children.Add(annuleren);
            knoppen.Children.Add(opslaan);
            inhoud.Children.Add(knoppen);
            invoerVenster.Content = inhoud;
            invoerVenster.Loaded += (s, args) =>
            {
                tijdInvoer.Focus();
                tijdInvoer.SelectAll();
            };
            invoerVenster.ShowDialog();
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
                klokTimer.Stop();
                avgTimer.Stop();
                Application.Current.Shutdown();
                return;
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
