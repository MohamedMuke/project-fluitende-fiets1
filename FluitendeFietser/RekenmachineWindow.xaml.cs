
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FluitendeFietser
{
    public partial class RekenmachineWindow : Window
    {
        private string invoer = "0";
        private bool resultaatGetoond = false;

        private static readonly CultureInfo NL =
            CultureInfo.GetCultureInfo("nl-NL");

        public RekenmachineWindow()
        {
            InitializeComponent();
            ToonScherm();
        }

        private void ToonScherm()
        {
            txtScherm.Text = invoer;
        }

        // Cijfers invoeren
        private void Cijfer_Click(object sender, RoutedEventArgs e)
        {
            string cijfer = ((Button)sender).Content.ToString();

            if (resultaatGetoond || invoer == "Fout")
            {
                invoer = "0";
                txtBerekening.Text = "";
                resultaatGetoond = false;
            }

            if (invoer == "0")
                invoer = cijfer;
            else
                invoer += cijfer;

            ToonScherm();
        }

        // Kommagetal invoeren
        private void Komma_Click(object sender, RoutedEventArgs e)
        {
            if (resultaatGetoond || invoer == "Fout")
            {
                invoer = "0";
                txtBerekening.Text = "";
                resultaatGetoond = false;
            }

            int start = invoer.Length - 1;

            while (start >= 0 &&
                   !IsOperator(invoer[start]))
            {
                start--;
            }

            string laatsteGetal = invoer.Substring(start + 1);

            if (!laatsteGetal.Contains(","))
            {
                if (laatsteGetal.Length == 0)
                    invoer += "0";

                invoer += ",";
            }

            ToonScherm();
        }

        // Operator toevoegen
        private void Operator_Click(object sender, RoutedEventArgs e)
        {
            string op = ((Button)sender).Content.ToString();

            if (invoer == "Fout")
                return;

            resultaatGetoond = false;

            char laatste = invoer[invoer.Length - 1];

            // Bestaande operator vervangen
            if (IsOperator(laatste))
            {
                invoer = invoer.Substring(0, invoer.Length - 1);
            }

            invoer += op;
            ToonScherm();
        }

        private bool IsOperator(char teken)
        {
            return teken == '+' ||
                   teken == '−' ||
                   teken == '×' ||
                   teken == '÷';
        }

        // Positief / negatief wisselen
        private void Negatief_Click(object sender, RoutedEventArgs e)
        {
            if (invoer == "Fout")
                return;

            if (resultaatGetoond)
            {
                resultaatGetoond = false;
                txtBerekening.Text = "";
            }

            int start = invoer.Length - 1;

            while (start >= 0 &&
                   !IsOperator(invoer[start]))
            {
                start--;
            }

            string laatsteGetal = invoer.Substring(start + 1);

            if (laatsteGetal.Length == 0)
                return;

            // Een negatief getal staat tussen haakjes.
            // Bijvoorbeeld 5 + (-3)
            if (start >= 0 &&
                invoer[start] == '−' &&
                (start == 0 || IsOperator(invoer[start - 1])))
            {
                invoer = invoer.Remove(start, 1);
            }
            else
            {
                invoer = invoer.Insert(start + 1, "−");
            }

            ToonScherm();
        }

        // Alles wissen
        private void Wissen_Click(object sender, RoutedEventArgs e)
        {
            invoer = "0";
            txtBerekening.Text = "";
            resultaatGetoond = false;
            ToonScherm();
        }

        // Laatste teken verwijderen
        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (resultaatGetoond || invoer == "Fout")
            {
                invoer = "0";
                txtBerekening.Text = "";
                resultaatGetoond = false;
            }
            else if (invoer.Length > 1)
            {
                invoer = invoer.Substring(0, invoer.Length - 1);
            }
            else
            {
                invoer = "0";
            }

            ToonScherm();
        }

        // Som uitrekenen
        private void IsGelijk_Click(object sender, RoutedEventArgs e)
        {
            if (invoer == "Fout")
                return;

            string volledigeSom = invoer;

            try
            {
                var parser = new SomParser(invoer);
                decimal uitkomst = parser.Bereken();

                txtBerekening.Text = volledigeSom + " =";
                invoer = uitkomst.ToString("0.############################", NL);
                resultaatGetoond = true;
            }
            catch
            {
                txtBerekening.Text = volledigeSom;
                invoer = "Fout";
                resultaatGetoond = true;
            }

            ToonScherm();
        }

        private void Sluiten_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Verwerkt de volledige som met de juiste rekenvolgorde
        private class SomParser
        {
            private readonly string tekst;
            private int positie;

            public SomParser(string som)
            {
                tekst = som.Replace(" ", "");
                positie = 0;
            }

            public decimal Bereken()
            {
                decimal waarde = LeesOptellen();

                if (positie != tekst.Length)
                    throw new FormatException("Ongeldige som.");

                return waarde;
            }

            private decimal LeesOptellen()
            {
                decimal waarde = LeesVermenigvuldigen();

                while (positie < tekst.Length)
                {
                    char op = tekst[positie];

                    if (op != '+' && op != '−')
                        break;

                    positie++;

                    decimal rechts = LeesVermenigvuldigen();

                    if (op == '+')
                        waarde += rechts;
                    else
                        waarde -= rechts;
                }

                return waarde;
            }

            private decimal LeesVermenigvuldigen()
            {
                decimal waarde = LeesGetal();

                while (positie < tekst.Length)
                {
                    char op = tekst[positie];

                    if (op != '×' && op != '÷')
                        break;

                    positie++;

                    decimal rechts = LeesGetal();

                    if (op == '×')
                    {
                        waarde *= rechts;
                    }
                    else
                    {
                        if (rechts == 0)
                            throw new DivideByZeroException();

                        waarde /= rechts;
                    }
                }

                return waarde;
            }

            private decimal LeesGetal()
            {
                bool negatief = false;

                while (positie < tekst.Length &&
                       (tekst[positie] == '−' ||
                        tekst[positie] == '+'))
                {
                    if (tekst[positie] == '−')
                        negatief = !negatief;

                    positie++;
                }

                int begin = positie;

                while (positie < tekst.Length &&
                       (char.IsDigit(tekst[positie]) ||
                        tekst[positie] == ','))
                {
                    positie++;
                }

                if (begin == positie)
                    throw new FormatException("Getal ontbreekt.");

                string getalTekst =
                    tekst.Substring(begin, positie - begin);

                decimal getal = decimal.Parse(
                    getalTekst,
                    NumberStyles.Number,
                    NL);

                return negatief ? -getal : getal;
            }
        }
    }
}
