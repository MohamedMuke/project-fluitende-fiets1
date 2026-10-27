using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FluitendeFietser
{
    public partial class BetaalWindow : Window
    {
        private readonly long teBetalenCent;
        private static readonly CultureInfo NL = CultureInfo.GetCultureInfo("nl-NL");
        private readonly int[] waarden = { 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };
        private readonly string[] namen = { "€ 200", "€ 100", "€ 50", "€ 20", "€ 10", "€ 5", "€ 2", "€ 1", "50 cent", "20 cent", "10 cent", "5 cent" };

        public BetaalWindow(decimal totaal)
        {
            InitializeComponent();
            teBetalenCent = (long)(totaal * 100m);
            txtTeBetalen.Text = "Te betalen: " + Geld(teBetalenCent);
            Bereken();
        }

        private static string Geld(long cent) { return (cent / 100m).ToString("C", NL); }

        private void Aantal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Aantal_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text) ||
                !((string)e.DataObject.GetData(DataFormats.Text)).All(char.IsDigit)) e.CancelCommand();
        }

        private void Aantal_TextChanged(object sender, TextChangedEventArgs e) { if (txtOntvangen != null) Bereken(); }
        private void Handmatig_TextChanged(object sender, TextChangedEventArgs e) { if (txtOntvangen != null) Bereken(); }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            WijzigAantal(sender as Button, 1);
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            WijzigAantal(sender as Button, -1);
        }

        private void WijzigAantal(Button knop, int verschil)
        {
            if (knop == null) return;
            string tag = knop.Tag as string;
            TextBox vak = pnlGeld.Children.OfType<StackPanel>()
                .SelectMany(rij => rij.Children.OfType<StackPanel>())
                .SelectMany(groep => groep.Children.OfType<TextBox>())
                .FirstOrDefault(t => (string)t.Tag == tag);
            if (vak == null) return;
            int huidig;
            if (!int.TryParse(vak.Text, out huidig) || huidig < 0) huidig = 0;
            if (verschil > 0 && huidig == int.MaxValue) return;
            vak.Text = Math.Max(0, huidig + verschil).ToString();
        }

        private void Bereken()
        {
            long ontvangen = 0;
            foreach (TextBox vak in pnlGeld.Children.OfType<StackPanel>()
                .SelectMany(rij => rij.Children.OfType<StackPanel>())
                .SelectMany(groep => groep.Children.OfType<TextBox>()))
            {
                int aantal;
                if (!int.TryParse(vak.Text, out aantal) || aantal < 0)
                {
                    txtVerschil.Text = "Vul geldige aantallen in.";
                    btnBevestigen.IsEnabled = false;
                    return;
                }
                try { checked { ontvangen += (long)aantal * int.Parse((string)vak.Tag); } }
                catch (OverflowException) { btnBevestigen.IsEnabled = false; txtVerschil.Text = "Bedrag is te groot."; return; }
            }
            decimal handmatig;
            if (!decimal.TryParse(txtHandmatig.Text, NumberStyles.Number, NL, out handmatig) || handmatig < 0 || decimal.Round(handmatig, 2) != handmatig)
            {
                txtVerschil.Text = "Vul een geldig handmatig bedrag in (maximaal 2 decimalen).";
                btnBevestigen.IsEnabled = false;
                return;
            }
            try { checked { ontvangen += (long)(handmatig * 100m); } }
            catch (OverflowException) { btnBevestigen.IsEnabled = false; txtVerschil.Text = "Bedrag is te groot."; return; }
            txtOntvangen.Text = Geld(ontvangen);
            long verschil = ontvangen - teBetalenCent;
            txtVerschil.Text = verschil >= 0 ? "Retour: " + Geld(verschil) : "Nog betalen: " + Geld(-verschil);
            btnBevestigen.IsEnabled = verschil >= 0;
            var tekst = new StringBuilder();
            if (verschil >= 0)
            {
                long rest = verschil;
                for (int i = 0; i < waarden.Length; i++)
                {
                    long aantal = rest / waarden[i];
                    if (aantal > 0) tekst.AppendLine(aantal + " × " + namen[i]);
                    rest %= waarden[i];
                }
                if (rest > 0) tekst.AppendLine("Niet exact uit te betalen: " + Geld(rest));
                if (verschil == 0) tekst.Append("Geen wisselgeld nodig.");
            }
            txtWisselgeld.Text = tekst.ToString();
        }

        private void Bevestigen_Click(object sender, RoutedEventArgs e)
        {
            Bereken();
            if (btnBevestigen.IsEnabled) DialogResult = true;
        }
        private void Annuleren_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    }
}
