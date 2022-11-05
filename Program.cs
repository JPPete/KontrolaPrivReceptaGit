using System.Net.Mail;
using System.Net;
using System.Xml.Linq;

namespace KontrolaPrivRecepta
{
    /// <summary>
    /// 
    /// </summary>


    internal class Program
    {
        static void Main(string[] args)
        {
            //da dobijemo vreme za log
            DateTime now = DateTime.Now;

            //pretraga svih kontrola na 43ci
            string[] kontrole = Directory.GetDirectories(@"\\LOKACIJAKONTROLA"
                        , "Kontrola*", SearchOption.TopDirectoryOnly);

            //info za slanje maila
            string body = "Izvrseno je:\n";

            //ZA TEST zamenjeno "C:" u "\\172.16.130.74"

            //Putanja gde prebacujemo racune koje smo pronasli da su losi
            string pronadjeniRacuni = @"C:\Lilly\KontrolaKolicina\PrivatniRecepti\PronadjeniRacuni\" + now.ToString("dd.MM.yyyy");

            //OVDE UPISUJES GDE SE CUVA RACUN
            string ispravljeniRacuni = @"C:\Lilly\KontrolaKolicina\PrivatniRecepti\IspravljeniRacuni\" + now.ToString("dd.MM.yyyy");

            //Pravljenje foldera za logovanje
            string dirLogFile = @"C:\Lilly\KontrolaKolicina\PrivatniRecepti\LogFiles\" + now.ToString("dd.MM.yyyy");


            #region "Pravljenje fodera za skladistenje (Log,Ispravljeni,Prazni,Pronadjeni)"
            if (!Directory.Exists(dirLogFile))
            {
                Directory.CreateDirectory(dirLogFile);
            }
            else//ovo je ako se program pokrene vise puta na dan (da se doda na folder i sat min sek)
            {
                dirLogFile += "_" + now.ToString("HHmmss");
                Directory.CreateDirectory(dirLogFile);

            }

            if (!Directory.Exists(pronadjeniRacuni))
            {
                Directory.CreateDirectory(pronadjeniRacuni);
            }
            else//ovo je ako se program pokrene vise puta na dan (da se doda na folder i sat min sek)
            {
                pronadjeniRacuni += "_" + now.ToString("HHmmss");
                Directory.CreateDirectory(pronadjeniRacuni);

            }


            if (!Directory.Exists(ispravljeniRacuni))
            {
                Directory.CreateDirectory(ispravljeniRacuni);
            }
            else//ovo je ako se program pokrene vise puta na dan (da se doda na folder i sat min sek)
            {
                ispravljeniRacuni += "_" + now.ToString("HHmmss");
                Directory.CreateDirectory(ispravljeniRacuni);

            }
            #endregion

            List<string> napakeKontrole = new List<string>();
            //prolazak kroz array kontrole i dodaje se u listu sa nastavkom do Napake
            foreach (string s in kontrole)
            {
                napakeKontrole.Add(s + @"\Queue\Napake");
            }//foreach napakeKontrole

            //da izbrojimo ispravljen broj artiakala
            int count = 0;
            //Za proveru da li smo nasli neki racun
            List<string> losiRacuni = new List<string>();

            //prolazak kroz listu da nadjemo putanje do Napake
            foreach (string kontrola in napakeKontrole)
            {
                //UZIMANJE FILEOVA IZ FOLDERA
                //Uzima sve filepath iz foldera Napake, koji pocinju sa "PAR" a zarvsavaju se sa ".xml"
                //i stavlja ih u array
                string[] filePaths = Directory.GetFiles(kontrola
                        , "PAR*.xml", SearchOption.TopDirectoryOnly);

                //ako nema racuna 
                if (filePaths == null || filePaths.Length == 0)
                {
                    using (StreamWriter _log = new StreamWriter(dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "SveOkLog.txt"
                        , true))
                    {
                        _log.WriteLine(now.ToString("G") + "\t"
                                + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                        _log.WriteLine(now.ToString("G") + "\t"
                            + $"Nema racuna u {kontrola}");

                        _log.WriteLine(now.ToString("G") + "\t"
                                    + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    }
                    continue;
                }

                //prolazak kroz listu filePaths (xmlFile je putanja sa nazivom racuna)
                foreach (string xmlFile in filePaths)
                {
                    //Ovde uzimamo putanju do file-a i onda koristimo Substring
                    //da nadjemo naziv file-a. Ovo planiram da koristim kada budem sacuvao file
                    string nazivRacuna = xmlFile.Substring(xmlFile.LastIndexOf('\\') + 1);

                    //ovde kreiramo objekat klase PodaciRacuna odakle mozemo da izvucemo vise informacija
                    PodaciRacuna infoRacun = new PodaciRacuna(nazivRacuna);

                    try // ovaj try koristimo da izbegnemo Exception gde postoje racuni bez vrednosti u njima
                    {
                        //Ucitava xmlFile u program
                        XDocument xDoc = XDocument.Load(xmlFile);

                        //Nalazi Node POSTAVKA
                        var postavke = xDoc.Descendants("POSTAVKA");
                        //Nalazi Node PRIVATNI_RECEPT
                        var privRecepti = xDoc.Descendants("PRIVATNI_RECEPT");

                        //Proverava da li ima privatni recept na racunu
                        if (privRecepti != null)
                        {
                            //prolazimo kroz privatne recepte
                            foreach (var privRecept in privRecepti)
                            {
                                //Uzima atribut KOLICINA iz priv receta i pokusava da ga konvertuje u double
                                double kolicina;
                                bool res = double.TryParse(privRecept.Attribute("KOLICINA").Value, out kolicina);
                                //ako je konverzija uspesna
                                if (res)
                                {
                                    //Ako je kolicina pogresna
                                    if (kolicina > 9999)
                                    {

                                        //da izbrojimo ispravljene artikle
                                        count++;
                                        if (!losiRacuni.Contains(nazivRacuna))
                                        {
                                            losiRacuni.Add(nazivRacuna);
                                        }

                                        //prolazak kroz POSTAVKE da nadjemo kolicinu i iznos
                                        foreach (var postavka in postavke)
                                        {
                                            //ako je barkod u privReceptu isti kao barkod u stavci
                                            if (postavka.Attribute("EAN").Value == privRecept.Attribute("IZDANO_ZDRAVILO").Value)
                                            {
                                                //              TODO
                                                //      Napisi log koji pise sta se ovde radi
                                                using (StreamWriter log = new(dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "PromeneLog.txt", true))
                                                {
                                                    log.WriteLine("\n" + now.ToString("G") + "\t"
                                                    + $"Kontrola je: {kontrola}");

                                                    log.WriteLine(now.ToString("G") + "\t"
                                                        + $"Naziv racuna: {nazivRacuna}");

                                                    log.WriteLine(now.ToString("G") + "\t" + $"Broj racuna: {infoRacun.BrRacuna} | Datum i vreme: {infoRacun.DatumRacuna} {infoRacun.VremeRacuna} | " +
                                                        $"Poslovnica: {infoRacun.Poslovnica} | Blagajna: {infoRacun.Blagajna}");

                                                    log.WriteLine(now.ToString("G") + "\t" + $"Pozicija: {privRecept.Attribute("POZICIJA").Value}");
                                                    log.WriteLine(now.ToString("G") + "\t" + $"Artikal: {postavka.Attribute("ARTIKEL").Value}");
                                                    log.WriteLine(now.ToString("G") + "\t" + $"EAN: {postavka.Attribute("EAN").Value}");
                                                    log.WriteLine(now.ToString("G") + "\t" + $"EAN u Priv. Receptu: {privRecept.Attribute("IZDANO_ZDRAVILO").Value}");

                                                    //kolicina u stavci
                                                    log.WriteLine(now.ToString("G") + "\t" + $"Kolicina u artiklu: {postavka.Attribute("KOLICINA").Value}");
                                                    log.WriteLine(now.ToString("G") + "\t" + $"ZNESEK u artiklu: {postavka.Attribute("ZNESEK").Value}");
                                                    //kolicina u priv receptu
                                                    log.WriteLine(now.ToString("G") + "\t" + $"Kolicina u priv. receptu: {privRecept.Attribute("KOLICINA").Value}");
                                                    log.WriteLine(now.ToString("G") + "\t" + $"ZNESEK_PLACILA u receptu: {privRecept.Attribute("ZNESEK_PLACILA").Value}");

                                                    //uzmi kolicinu iz stavke i postavi je u kolicinu za privatni recept
                                                    privRecept.Attribute("KOLICINA").Value = postavka.Attribute("KOLICINA").Value;
                                                    log.WriteLine(now.ToString("G") + "\t!!!\t" + $"Kolicina je promenjena na: {privRecept.Attribute("KOLICINA").Value}" + "\t!!!");

                                                    //uzmi znesek (iznos) iz stavke i stavi u iznos privatnog recepta (znesek_placila)
                                                    privRecept.Attribute("ZNESEK_PLACILA").Value = postavka.Attribute("ZNESEK").Value;
                                                    log.WriteLine(now.ToString("G") + "\t!!!\t" + $"ZNESEK_PLACILA je promenjen na: {privRecept.Attribute("ZNESEK_PLACILA").Value}" + "\t!!!\n");


                                                }//streamwriter

                                            }//if ean == izdano_zdravilo




                                        }//foreach postavke


                                        //sacuvaj file na putanju
                                        xDoc.Save(ispravljeniRacuni + "\\" + nazivRacuna);

                                        //Prebaci racun u PronadjeniRacuni
                                        if (!File.Exists(pronadjeniRacuni + "\\" + nazivRacuna))
                                        {
                                            File.Move(xmlFile, pronadjeniRacuni + "\\" + nazivRacuna, true);
                                        }



                                    }// if kolicina veca 9999 manja -9999

                                }// if res == true
                                else // konverzija nje uspela
                                {
                                    using (StreamWriter _log = new StreamWriter(dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "PromeneLog.txt", true))
                                    {
                                        _log.WriteLine("\n");

                                        _log.WriteLine(now.ToString("G") + "\t"
                                            + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                                        _log.WriteLine(now.ToString("G") + "\t"
                                            + $"Racun: {nazivRacuna} je prazan");

                                        _log.WriteLine(now.ToString("G") + "\t"
                                            + "PARSIRANJE KOLICINE NIJE USPELO");

                                        _log.WriteLine(now.ToString("G") + "\t"
                                            + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");


                                        _log.WriteLine("\n");
                                    }

                                }//else konverzija nije uspela


                            }//foreach privRecepti


                        }//if privRecepti != null
                      

                    }//try
                    catch (System.Xml.XmlException)
                    {

                        using (StreamWriter _log = new StreamWriter(dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "PromeneLog.txt", true))
                        {

                            _log.WriteLine("\n");

                            _log.WriteLine(now.ToString("G") + "\t"
                                + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                            _log.WriteLine(now.ToString("G") + "\t"
                                + $"Racun: {nazivRacuna} je prazan");

                            _log.WriteLine(now.ToString("G") + "\t"
                                        + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                            _log.WriteLine("\n");
                        }

                        continue;
                    }//catch System.Xml.XmlException



                }//foreach filePaths(prolazak kroz racune)




            }//foreach napakeKontrole

            //ako se nadju losi racuni salji mail
            if (losiRacuni.Count() != 0)
            {
                body += $"Broj losih privatnih recepata je : {losiRacuni.Count()}\nBroj ispravljenih stavki je: {count}\nPogledaj attachments za vise informacija." +
                    $"\nIspravljeni racuni se nalaze u (location) {ispravljeniRacuni}" + $"\n\n\n\nIzvrseno " + now.ToString("dd.MM.yyyy HH:mm:ss");
                //string att1 = dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "Log.txt";
                string att1 = dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "PromeneLog.txt";

                // ZA TEST MENJAM U MOJ EMAIL, ZA PROGRAM TREBA STAVITI it@llly.rs
                MailAddress to = new MailAddress("EMAIL@lilly.rs");

                SendEmail(to, body, att1);

                //waits for the mail to be send before closing the program
                Thread.Sleep(3000);
            }
            else // ako se ne nadju losi racuni salji drugaciji mail
            {
                body += "Nije pronadjen ni jedan privatan recept, koji ima barkod prokucan u kolicini" +
                    $"\n\n\n\nIzvrseno " + now.ToString("dd.MM.yyyy HH:mm:ss");
                using (StreamWriter _log = new StreamWriter(dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "SveOkLog.txt", true))
                {
                    _log.WriteLine(now.ToString("G") + "\t"
                                + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                    _log.WriteLine(now.ToString("G") + "\t"
                        + $"Nema priv recepta sa barkodom u kolicini");

                    _log.WriteLine(now.ToString("G") + "\t"
                                + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                }
                string att1 = dirLogFile + "\\" + now.ToString("dd_MM_yyyy") + "_" + "SveOkLog.txt";
                MailAddress to = new MailAddress("EMAIL@lilly.rs");

                SendEmail(to, body, att1);

                //waits for the mail to be send before closing the program
                Thread.Sleep(1000);
            }





        }//main method



        //Method za slanje maila
        static void SendEmail(MailAddress to, string b, string att1)
        {
            MailAddress from = new MailAddress("EMAIL@lilly.rs");
            //MailAddress to = new MailAddress("petar.jovancic@lilly.rs");
            //MailAddress too = to;

            var smtpClinet = new SmtpClient("MAILSERVER")
            {
                Port = 587,
                Credentials = new NetworkCredential("EMAIL@lilly.rs", "PASSWORD"),
                EnableSsl = true,

            };


            using (MailMessage message = new MailMessage(from, to)
            {
                Subject = "Izvestaj : KONTROLA PRIV. RECEPTA",
                Body = b
            })
            {

                message.Attachments.Add(new Attachment(att1));
                //message.Attachments.Add(new Attachment(att2));
                smtpClinet.Send(message);

                Thread.Sleep(1000);

                smtpClinet.Dispose();

            }

        }


    }//Class Program
}//namespace