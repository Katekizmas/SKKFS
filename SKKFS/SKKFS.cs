using SKKFS.Klases;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SKKFS
{
    public partial class SKKFS : Form
    {
        string A1, A2, A3, ATotal;
        //string _workingDirectory = "C:/Users/Paulius/Desktop/atvaizdai";
        string _workingDirectory = "E:/";
        FailuSistema _failuSistema = new FailuSistema();
        List<Failas> _failai = new List<Failas>();

        //Pradinės būsenos
        List<Failas> chosenFiles = new List<Failas>();
        string chosenFileToHide = "";

        public SKKFS()
        {
            InitializeComponent();
        }

        private void SKKFS_Load(object sender, EventArgs e)
        {
            List<Failas> _failais = new List<Failas>();
            //GetFileSystemData("2.vhd");
            //_failai = DeserializeFailasFromJson("Failai-Json.json");
            GetFileSystemData("SKKFS.vhd");
            _failai = DeserializeFailasFromJson("SKKFS.json");

            //ReadFilesDataFromFile("Failai.csv"); arba //GetFilesData(_failai, "2.vhd");
            //AssignSectors(_failai, "2.vhd");
            //CalculateClusters(_failai);
            //SaveListToJsonFile(_failai, "Failai-Json.json");
        }

        private void testas_Click(object sender, EventArgs e)
        {
            //ReadAndSaveFileUsingBinaryTest();
            InitiliazeInitialStatusForHiding();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitiliazeInitialStatusForDecoding();
        }

        public List<Failas> DeserializeFailasFromJson(string fileName)
        {
            using FileStream openStream = File.OpenRead($"{_workingDirectory}/{fileName}");
            return JsonSerializer.Deserialize<List<Failas>>(openStream);
        }

        public void GetFileSystemData(string fileName)
        {
            var process = new Process
            {
                StartInfo =
                    {
                         FileName = "fsstat",
                         WorkingDirectory = @$"{_workingDirectory}",
                         Arguments = $"-o 2048 {fileName}",
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         CreateNoWindow = true,
                    }
            };

            process.Start();

            string outputFileSystem = process.StandardOutput.ReadToEnd();

            string[] lines = outputFileSystem.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            _failuSistema.VolumeId = lines[4].Split(":")[1].Trim();
            _failuSistema.VolumeLabel = lines[6].Split(":")[1].Trim();
            _failuSistema.NextFreeSector = Int64.Parse(lines[8].Split(":")[1].Trim());
            _failuSistema.FreeSectorCount = Int64.Parse(lines[9].Split(":")[1].Trim());
            _failuSistema.ClusterAreaStart = Int64.Parse(lines[20].Split(":")[1].Split("-")[0].Trim());
            _failuSistema.ClusterAreaEnd = Int64.Parse(lines[20].Split(":")[1].Split("-")[1].Trim());
            _failuSistema.SectorSize = Int64.Parse(lines[28].Split(":")[1].Trim()); //29
            _failuSistema.ClusterSize = Int64.Parse(lines[29].Split(":")[1].Trim()); //30
            _failuSistema.SectorsAssignedPerCluster = _failuSistema.ClusterSize / _failuSistema.SectorSize;

            for (int i = 34; i < lines.Length; i++)
            {
                var matches = Regex.Matches(lines[i], "[0-9]+", RegexOptions.IgnoreCase)
                    .Cast<Match>().Select(x => x.Value).ToArray();
                _failuSistema.Clusters.Add(new Klasteris()
                {
                    Start = Int64.Parse(matches[0]),
                    End = Int64.Parse(matches[1]),
                    Length = Int64.Parse(matches[2]),
                    NextSectorNumber = lines[i].Split("->")[1].Trim()
                });
            }

            process.WaitForExit();
        }

        public void GetFilesData([In, Out] List<Failas> failai, string fullPath)
        {
            var process = new Process
            {
                StartInfo =
                    {
                         FileName = "fls",
                         WorkingDirectory = @$"{Path.GetDirectoryName(fullPath)}",
                         Arguments = $"-F -l -p -r -u -z UTC -o 2048 {Path.GetFileName(fullPath)}", //-F niekada nesibaigia...
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         CreateNoWindow = true,
                    }
            };

            var outputFiles = new StringBuilder();
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                outputFiles.AppendLine(eventArgs.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            //string outputFiles = process.StandardOutput.ReadToEnd();

            if (!process.WaitForExit(10000)) // 60s
            {
                process.Kill();
                richTextBox_paslepimasDengiamiFailai.Text = outputFiles.ToString();
            }

            var files = outputFiles.ToString().Split("\r\n");
            for(int i = 3; i < files.Length; i++) // Skip three first boot files
            {
                //file_type inode file_name mod_time acc_time chg_time cre_time size uid gid
                var file = files[i].Split("\t");
                if(file.Length > 5)
                    failai.Add(new Failas()
                    {
                        Name = file[1].Split("/").Last(),
                        FileExtension = $".{file[1].Split(".").Last()}",
                        LastAccessed = file[3].Substring(0, file[3].Length - 6), //
                        FileCreated = file[5].Substring(0, file[5].Length - 6),
                        LastModified = file[2].Substring(0, file[2].Length - 6), //
                        Size = Int64.Parse(file[6]), //
                        Address = Int64.Parse(Regex.Match(file[0], @"\d+").Value),//
                        FullPath = file[1],//
                    });
            }
        }

        public void ReadFilesDataFromFile(string fileName)
        {
            using (var reader = new StreamReader(@$"{_workingDirectory}/{fileName}"))
            {
                string headerLine = reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(';');
                    _failai.Add(new Failas(){
                        Name = values[0],
                        FileExtension = values[1],
                        LastAccessed = values[2],
                        FileCreated = values[3],
                        LastModified = values[4],
                        Size = Int64.Parse(values[5]),
                        Address = Int64.Parse(values[6]),
                        FullPath = values[7],
                    });
                }
                reader.Close();
            }
        }     
        
        public void AssignSectors([In, Out] List<Failas> failai, string fullPath)
        {
            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = failai.Count;
            progressBar1.Step = 1;
            progressBar1.Style = ProgressBarStyle.Continuous;

            for (int i = 0; i < failai.Count; i++)
            {
                progressBar1.Value = i;
                var process = new Process
                {
                    StartInfo =
                    {
                         FileName = "istat",
                         WorkingDirectory = @$"{Path.GetDirectoryName(fullPath)}",
                         Arguments = $"-o 2048 {Path.GetFileName(fullPath)} {failai[i].Address}",
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         CreateNoWindow = true,
                    }
                };

                process.Start();

                string outputClusters = DeleteLines(process.StandardOutput.ReadToEnd(), 24);

                string[] formatedClusters = outputClusters.Split(" ").Select(x => x.Replace("\n", "").Replace("\r", "")).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                failai[i].Sectors = Array.ConvertAll(formatedClusters, long.Parse);

                process.WaitForExit();
            }
        }

        public void CalculateClusters([In, Out] List<Failas> failai)
        {
            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = failai.Count;
            progressBar1.Step = 1;
            progressBar1.Style = ProgressBarStyle.Continuous;

            for (int i = 0; i < failai.Count; i++)
            {
                progressBar1.Value = i;
                var amountOfSectorsAssigned = failai[i].Sectors.Length;

                long clusterStart = failai[i].Sectors[0];

                for (long j = _failuSistema.SectorsAssignedPerCluster; j <= amountOfSectorsAssigned; j = j + _failuSistema.SectorsAssignedPerCluster)
                {
                    //TODO - Reikia pasižiūrėt ar tikrai paskutiniai sektoriai yra EOF kai persipyne failai
                    if(j != amountOfSectorsAssigned)
                    {
                        // Palygininam ar sekantis klasteris yra išeilės einantis ar ne
                        if (failai[i].Sectors[j - 1] != failai[i].Sectors[j] - 1)
                        {
                            var cluster = new Klasteris(clusterStart, failai[i].Sectors[j], failai[i].Sectors[j + 1].ToString());

                            failai[i].Clusters.Add(cluster);
                            clusterStart = failai[i].Sectors[j + 1];
                        }
                        else
                        {
                            //ClusterStart = failai[i].Sectors[j];
                        }
                    }
                    else
                    {
                        var cluster = new Klasteris(clusterStart, failai[i].Sectors[j - _failuSistema.SectorsAssignedPerCluster] + _failuSistema.SectorsAssignedPerCluster - 1, "EOF");

                        failai[i].Clusters.Add(cluster);
                        failai[i].Sectors = null;
                    }
                }
            }
        }

        public static string DeleteLines(string s, int linesToRemove)
        {
            return s.Split(Environment.NewLine.ToCharArray(), linesToRemove + 1).Skip(linesToRemove).FirstOrDefault();
        }

        public void SaveListToJsonFile<T>(List<T> list, string fileName)
        {
            var opt = new JsonSerializerOptions() { WriteIndented = true };

            string strJson = JsonSerializer.Serialize<IList<T>>(list, opt);

            File.WriteAllText($"{fileName}", strJson);
            //File.WriteAllText($"{_workingDirectory}/{fileName}", strJson);
        }

        public void ReadAndSaveFileUsingBinaryTest()
        {
            var failasIn = "test.txt";
            var failasOut = "test-out.txt";

            var binaryString = Helper.ReadFileToBinaryString(chosenFileToHide);

            richTextBox_paslepimasDengiamiFailai.Text = binaryString.Length.ToString();

            Helper.WriteBinaryStringToFile(_workingDirectory, failasOut, binaryString);
        }

        public void ChooseFileSystemFilesBySelectedFilesInDialog(RichTextBox textbox)
        {
            chosenFiles.Clear();
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            //openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";
            openFileDialog1.Multiselect = true;
            //openFileDialog1.Filter = "Failai|*.txt|Failai2|*.csv";e

            //DialogResult dr = openFileDialog1.ShowDialog();
            textbox.Text = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] selectedFiles = openFileDialog1.FileNames;
                if (selectedFiles.Length <= alpha.Length)
                {
                    progressBar1.Value = 0;
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = selectedFiles.Length - 1;
                    progressBar1.Step = 1;
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    //string[] selectedFiles = openFileDialog1.FileNames.Select(x => x.Substring(3).Replace("\\", "/")).ToArray();
                    richTextBox_paslepimasSlepiamiFailaiDuomenys.Text = "";
                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        progressBar1.Value = i;
                        textbox.AppendText($"Dengiantysis failas: > {selectedFiles[i]}\n");
                        var fileInformation = new FileInfo(selectedFiles[i]);   //Failo informacija
                        double maxFileCount = Math.Ceiling((double)fileInformation.Length / _failuSistema.SectorsAssignedPerCluster); // Apskaičiuojam kiek clusterių užima failas
                        var files = _failai.Where(x => x.Name[0].ToString().ToUpper() == alpha[i].ToString().ToUpper()).OrderBy(x => x.Size).ThenBy(x => x.Name).ToList();

                        for (int j = 0; j < files.Count; j++)
                        {
                            double clustersTaken = Math.Ceiling((double)files[j].Size / _failuSistema.SectorsAssignedPerCluster); // Apskaičiuojam kiek clusterių užima failas
                            if (maxFileCount - clustersTaken >= 0)
                            {
                                textbox.AppendText($"      > {files[j].FullPath} ({clustersTaken})\n");
                                maxFileCount = maxFileCount - clustersTaken;
                                chosenFiles.Add(files[j]);
                            }
                        }

                        //richTextBox_paslepimasSlepiamiFailaiDuomenys.AppendText($"Dengiantysis failas: > {selectedFiles[i]}, viso komponenčių: {chosenFiles.Count}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Tiek dengiamų failų pasirinkti negalima, maksimalus skaičius: " + alpha.Length);
                }
            }
        }
        public void ChooseFileToHide()
        {
            //openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";
            openFileDialog1.Multiselect = false;
            //openFileDialog1.Filter = "Failai|*.txt|Failai2|*.csv";e

            //DialogResult dr = openFileDialog1.ShowDialog();
            richTextBox_paslepimasSlepiamiFailai.Text = "";
            richTextBox_paslepimasSlepiamiFailaiDuomenys.Text = "";
            chosenFileToHide = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                chosenFileToHide = openFileDialog1.FileName;
                richTextBox_paslepimasSlepiamiFailai.AppendText(chosenFileToHide);
                foreach (string line in File.ReadAllLines(chosenFileToHide))
                {
                    richTextBox_paslepimasSlepiamiFailaiDuomenys.AppendText(line);
                }
            }
        }

        public void ChooseAndPrepareDiskImage()
        {
            //openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";
            //openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";//openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";
            openFileDialog1.Multiselect = false;

            var chosenDiskImage = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                richTextBox_pasirinktiDiskoAtvaizda.Text = openFileDialog1.FileName;
                //richTextBox_paslepimasSlepiamiFailai.AppendText(chosenFileToHide);
            }
        }
        public void InitiliazeInitialStatusForHiding()
        {
            int S = Int32.Parse(textBox_paslepimasSlaptasRaktas.Text);//2; //slapto rakto dalis, įvedamas

            if (S <= 0)
            {
                MessageBox.Show("S reikšmė turi būti didesnė už 0", "Klaidos pranešimas", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            //Slepiami duomenys
            string M = Helper.ReadFileToBinaryString(chosenFileToHide);
            int n = M.Length;


            //string F[] =;         // Dengiantys failai
            //long m = F.Length;    // Dengiančių failų skaičius
            //string D[] =;         // Failų komponentės
            //long i = D.Length;    // Failų komponenčių skaičius

            long C = _failuSistema.SectorsAssignedPerCluster;

            ////Check kad ilgis atitiktų p blokų ilgį
            ///
            string B0 = textBox_paslepimasVektorius.Text;//"00000000"; // Inicilizacijos vektorius, įvedamas

            for (int i = textBox_paslepimasVektorius.Text.Length; i < Int32.Parse(textBox_paslepimasIlgis.Text); i++)
            {
                B0 = "0" + B0;
            }

            //var N = // Tarpinių skaičiavimų masyvas, tiksliai nežinau kam, gal laisvi sektoriai, ar jau į kokius klasterius įrašyti tie paslėpti failai

            int p = Int32.Parse(textBox_paslepimasIlgis.Text);//8;// Įvedamas, slepiamų bitų ilgis - įvedamas nes persigalvoaju, neapsimoka apskaičiuot ten kažko

            M = B0 + M; // Čia reiktų B0 taisyklingo kad veiktų visada :)

            long[] B = M.Chunk(p).Select(x => Convert.ToInt64(new string(x), 2)).ToArray();

            if (chosenFiles.Count / S >= B.Length)
            {
                progressBar1.Value = 0;
                progressBar1.Minimum = 0;
                progressBar1.Maximum = chosenFiles.Count / S;
                progressBar1.Step = 1;
                progressBar1.Style = ProgressBarStyle.Continuous;
                richTextBox_paslepimasDengiamiFailai.Text = "";

                long[] N = new long[B.Length - 1]; long wot = (long)Math.Pow(2, p);
                for (int i = 0; i < (B.Length - 1); i++)
                {
                    //progressBar1.Value = i;
                    N[i] = Helper.Modulo(B[i + 1] - B[i], wot); // Apskaičiuojam kokia liekana turim gaut iš klasterio numerio
                    var tarpiniaiKlasteriai = _failuSistema.Clusters.Where(x => x.Start > _failuSistema.NextFreeSector).ToList(); // Sumažinti nereikalingą patikrinimą iš 1950 į 15 įrašų ir pnš
                    var fileSizeOnDiskInSectors = (long)Math.Ceiling((double)chosenFiles[i * S].Size / _failuSistema.ClusterSize) * _failuSistema.SectorsAssignedPerCluster;
                    for (long j = _failuSistema.NextFreeSector; j < _failuSistema.ClusterAreaEnd; j++) // Pasiimam laisvą sektorių
                    {
                        if (Helper.Modulo(j, wot) == N[i]) // Surandam sektoriaus / klasterio numerį į kurį talpinsim
                        {
                            bool canWriteIntoSector = true;
                            for (int k = 0; k < tarpiniaiKlasteriai.Count; k++) // Tikrinam ar užimtas
                            {
                                for (long l = j; l < j + fileSizeOnDiskInSectors; l = l + _failuSistema.SectorsAssignedPerCluster) // gal - 1
                                {
                                    if (
                                        l.IsWithin(tarpiniaiKlasteriai[k].Start, tarpiniaiKlasteriai[k].End)
                                        ||
                                        (l + _failuSistema.SectorsAssignedPerCluster - 1).IsWithin(tarpiniaiKlasteriai[k].Start, tarpiniaiKlasteriai[k].End)
                                        ) //Yra jau tas sektorius / klasteris naudojamas
                                    {
                                        //Failo pilnai negalima įrašyti į sektoriaus range, nes kažkuris yra užimtas
                                        canWriteIntoSector = false;
                                        break;
                                    }
                                }
                                if (!canWriteIntoSector)
                                {
                                    break;
                                }
                            }
                            if (canWriteIntoSector)
                            {
                                Klasteris newCluster = new Klasteris(j, j + fileSizeOnDiskInSectors - 1, "EOF");
                                _failuSistema.Clusters.Add(newCluster);

                                if ((newCluster.Start - _failuSistema.NextFreeSector) <= (fileSizeOnDiskInSectors - 1) || /*Čia nereik, bet greičiau veikia*/j != _failuSistema.NextFreeSector)
                                { // neveikia gerai, nes praleidzia.....
                                    _failuSistema.NextFreeSector = j + fileSizeOnDiskInSectors;
                                }
                                /*else
                                {
                                    for(int t = 0; t < tarpiniaiKlasteriai.Count; t++)
                                    {
                                        if()tarpiniaiKlasteriai[t].Start - _failuSistema.NextFreeSector
                                    }
                                    tarpiniaiKlasteriai = tarpiniaiKlasteriai.OrderBy(x => x.Start).Where(x => (x.Start - _failuSistema.NextFreeSector) <= (_failuSistema.SectorsAssignedPerCluster - 1)))
                                }*/
                                richTextBox_paslepimasDengiamiFailai.Text += $"{i + 1}. {chosenFiles[i * S].Name}, iš ({chosenFiles[i * S].Clusters.FirstOrDefault()}) į ({newCluster}) \n";

                                var index = _failai.FindIndex(x => x.Equals(chosenFiles[i * S]));
                                _failai[index].Clusters.Clear();
                                _failai[index].Clusters.Add(newCluster);
                                
                                break;
                            }

                        }
                    }
                }
                A1 = textBox_paslepimasIlgis.Text;
                A2 = textBox_paslepimasVektorius.Text;
                A3 = textBox_paslepimasSlaptasRaktas.Text;
                ATotal = richTextBox_paslepimasSlepiamiFailaiDuomenys.TextLength.ToString();
                //textBox_paslepimasIlgis.Text = "";
                //textBox_paslepimasVektorius.Text = "";
                //textBox_paslepimasSlaptasRaktas.Text = "";
                //richTextBox_paslepimasDengiamiFailai.Text = "";
                richTextBox_paslepimasSlepiamiFailai.Text = "";
                richTextBox_paslepimasSlepiamiFailaiDuomenys.Text = "";
                //SaveListToJsonFile(_failai, "C:/Users/Paulius/Desktop/atvaizdai/1/Failai-Json.json");
                MessageBox.Show("Duomenys sėkmingai paslėpti", "Duomenų paslėpimas", MessageBoxButtons.OK, MessageBoxIcon.Information);

            } else
            {
                MessageBox.Show($"Nepakanka komponenčių failų. Norint paslėpti reikės: {B.Length} arba daugiau, o komponenčių failų failų sistemoje yra: {chosenFiles.Count / S}.", "Paslėpti failo neįmanoma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        public void InitiliazeInitialStatusForDecoding()
        {
            var failasOut = "test-out.txt";

            int S = Int32.Parse(textBox_atkodavimasSlaptasRaktas.Text);//2; //slapto rakto dalis, įvedamas

            if (S <= 0)
            {
                MessageBox.Show("S reikšmė turi būti didesnė už 0", "Klaidos pranešimas", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //string F[] =;         // Dengiantys failai
            //long m = F.Length;    // Dengiančių failų skaičius
            //string D[] =;         // Failų komponentės
            //long i = D.Length;    // Failų komponenčių skaičius

            long C = _failuSistema.SectorsAssignedPerCluster;

            string B0 = textBox_atkodavimasVektorius.Text;//"00000000"; // Inicilizacijos vektorius, įvedamas

            for (int i = textBox_atkodavimasVektorius.Text.Length; i < Int32.Parse(textBox_atkodavimasIlgis.Text); i++)
            {
                B0 = "0" + B0;
            }


            //var N = // Tarpinių skaičiavimų masyvas, tiksliai nežinau kam, gal laisvi sektoriai, ar jau į kokius klasterius įrašyti tie paslėpti failai

            int p = Int32.Parse(textBox_atkodavimasIlgis.Text);//8;// Įvedamas, slepiamų bitų ilgis - įvedamas nes persigalvoaju, neapsimoka apskaičiuot ten kažko

            long[] N = new long[chosenFiles.Count]; long wot = (long)Math.Pow(2, p);

            long[] B = new long[chosenFiles.Count + 1];
            B[0] = Convert.ToInt64(B0, 2);

            string binaryString = "";

            //S < (B.Length - 1); i = i + S
            for (int i = 0; i < (chosenFiles.Count - 1) / S; i++)
            {
                N[i] = Helper.Modulo(chosenFiles[i * S].Clusters[0].Start, wot);
                B[i + 1] = Helper.Modulo(N[i] + B[i], wot);
                binaryString += Convert.ToString(Helper.Modulo(N[i] + B[i], wot), 2).PadLeft(p, '0');
            }

/*            for (int i = 0; i < B.Length - 1; i++)
            {
                B[i + 1] = Helper.Modulo(N[i] + B[i], wot); // Cj praleidzia viena
                binaryString += Convert.ToString(Helper.Modulo(N[i] + B[i], wot), 2).PadLeft(p, '0');
            }*/

            Helper.WriteBinaryStringToFile(_workingDirectory, failasOut, binaryString);
            richTextBox_atkodavimasAtkoduotiDuomenys.Text = "";
            foreach (string line in File.ReadAllLines($"{_workingDirectory}/{failasOut}"))
            {
                richTextBox_atkodavimasAtkoduotiDuomenys.AppendText(line);
            }
            if (textBox_atkodavimasIlgis.Text == A1 && textBox_atkodavimasVektorius.Text == A2 && textBox_atkodavimasSlaptasRaktas.Text == A3)
                richTextBox_atkodavimasAtkoduotiDuomenys.Text = richTextBox_atkodavimasAtkoduotiDuomenys.Text.Substring(0, Convert.ToInt32(ATotal));
        }

        private void button_paslepimasSlepiamiFailai_Click(object sender, EventArgs e)
        {
            ChooseFileToHide();
        }

        private void button_paslepimasDengFailai_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            ChooseFileSystemFilesBySelectedFilesInDialog(richTextBox_paslepimasDengiamiFailai);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds,ts.Milliseconds / 10);
            //richTextBox_paslepimasSlepiamiFailaiDuomenys.AppendText(elapsedTime);
        }

        private void button_paslepti_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            InitiliazeInitialStatusForHiding();
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            //richTextBox_paslepimasSlepiamiFailaiDuomenys.Text = (elapsedTime);
        }

        private void button_atkodavimasDengFailai_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            ChooseFileSystemFilesBySelectedFilesInDialog(richTextBox_atkodavimasDengiamiFailai);
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            //richTextBox_atkodavimasAtkoduotiDuomenys.Text = (elapsedTime);
        }

        private void button_atkoduoti_Click(object sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            InitiliazeInitialStatusForDecoding();
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value. 
            string elapsedTime = String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            //richTextBox_atkodavimasAtkoduotiDuomenys.Text = (elapsedTime);
        }

        private void btnPasirinktiDiskoAtvaizda_Click(object sender, EventArgs e)
        {
            ChooseAndPrepareDiskImage();
        }

        private void btnNuskaitytiDiskoAtvaizda_Click(object sender, EventArgs e)
        {
            if(String.IsNullOrWhiteSpace(richTextBox_pasirinktiDiskoAtvaizda.Text))
            {
                MessageBox.Show("Nepasirinktas disko atvaizdas", "Klaidos pranešimas", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                GetFilesData(_failai, richTextBox_pasirinktiDiskoAtvaizda.Text);
                //ReadFilesDataFromFile("Failai.csv"); arba ^
                AssignSectors(_failai, richTextBox_pasirinktiDiskoAtvaizda.Text);
                CalculateClusters(_failai);
                SaveListToJsonFile(_failai, $"{Path.GetFileName(richTextBox_pasirinktiDiskoAtvaizda.Text).Split('.')[0]}.json");
                MessageBox.Show("Disko atvaizdas sėkmingai nuskaitytas", "Disko atvaizdo nuskaitymas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}