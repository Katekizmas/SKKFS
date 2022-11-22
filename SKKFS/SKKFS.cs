﻿using SKKFS.Klases;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SKKFS
{
    public partial class SKKFS : Form
    {
        string _workingDirectory = "C:/Users/Paulius/Desktop/atvaizdai";
        FailuSistema _failuSistema = new FailuSistema();
        List<Failas> _failai = new List<Failas>();
        public SKKFS()
        {
            InitializeComponent();
        }

        private void SKKFS_Load(object sender, EventArgs e)
        {
            List<Failas> _failais = new List<Failas>();
            GetFileSystemData("2.vhd");
            _failai = DeserializeFailasFromJson("Failai-Json.json");
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
            _failuSistema.SectorSize = Int64.Parse(lines[29].Split(":")[1].Trim());
            _failuSistema.ClusterSize = Int64.Parse(lines[30].Split(":")[1].Trim());
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

        public void GetFilesData([In, Out] List<Failas> failai, string fileName)
        {
            var process = new Process
            {
                StartInfo =
                    {
                         FileName = "fls",
                         WorkingDirectory = @$"{_workingDirectory}",
                         Arguments = $"-F -l -p -r -u -z UTC -o 2048 {fileName}", //-F niekada nesibaigia...
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
                richTextBox1.Text = outputFiles.ToString();
            }

            var files = outputFiles.ToString().Split("\r\n");
            for(int i = 3; i < files.Length; i++) // Skip three first boot files
            {
                //file_type inode file_name mod_time acc_time chg_time cre_time size uid gid
                var file = files[i].Split("\t");
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
        
        public void AssignSectors([In, Out] List<Failas> failai, string fileName)
        {
            for(int i = 0; i < failai.Count; i++)
            {
                var process = new Process
                {
                    StartInfo =
                    {
                         FileName = "istat",
                         WorkingDirectory = @$"{_workingDirectory}",
                         Arguments = $"-o 2048 {fileName} {failai[i].Address}",
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
            for (int i = 0; i < failai.Count; i++)
            {
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

            File.WriteAllText($"{_workingDirectory}/{fileName}", strJson);
        }

        public void ReadAndSaveFileUsingBinaryTest()
        {
            var failasIn = "test.txt";
            var failasOut = "test-out.txt";

            var binaryString = Helper.ReadFileToBinaryString(_workingDirectory, failasIn);

            richTextBox1.Text = binaryString.Length.ToString();

            Helper.WriteBinaryStringToFile(_workingDirectory, failasOut, binaryString);
        }

        public List<Failas> ChooseFileSystemFilesBySelectedFilesInDialog()
        {
            var chosenFiles = new List<Failas>();
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            //openFileDialog1.InitialDirectory = "E:\\Dep1\\auqitobpirfqzp";
            openFileDialog1.Multiselect = true;
            //openFileDialog1.Filter = "Failai|*.txt|Failai2|*.csv";e

            //DialogResult dr = openFileDialog1.ShowDialog();
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (chosenFiles.Count <= alpha.Length)
                {
                    //string[] selectedFiles = openFileDialog1.FileNames.Select(x => x.Substring(3).Replace("\\", "/")).ToArray();
                    string[] selectedFiles = openFileDialog1.FileNames;
                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        var fileInformation = new FileInfo(selectedFiles[i]);   //Failo informacija
                        double maxFileCount = Math.Ceiling((double)fileInformation.Length / _failuSistema.SectorsAssignedPerCluster); // Apskaičiuojam kiek clusterių užima failas
                        var files = _failai.Where(x => x.Name[0].ToString().ToUpper() == alpha[i].ToString().ToUpper()).OrderBy(x => x.Size).ThenBy(x => x.Name).ToList();

                        for (int j = 0; j < files.Count; j++)
                        {
                            double clustersTaken = Math.Ceiling((double)files[j].Size / _failuSistema.SectorsAssignedPerCluster); // Apskaičiuojam kiek clusterių užima failas
                            if (maxFileCount - clustersTaken >= 0)
                            {
                                richTextBox1.AppendText($"> {files[j].FullPath} ({clustersTaken})\n");
                                maxFileCount = maxFileCount - clustersTaken;
                                chosenFiles.Add(files[j]);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Tiek dengiamų failų pasirinkti negalima, maksimalus skaičius: " + alpha.Length);
                }
            }

            return chosenFiles;
        }

        public void InitiliazeInitialStatusForHiding()
        {
            var failasIn = "test.txt";
            //Čia reikia file read

            //Slepiami duomenys
            string M = Helper.ReadFileToBinaryString(_workingDirectory, failasIn);
            //richTextBox1.Text += M;
            int n = M.Length;


            //string F[] =;         // Dengiantys failai
            //long m = F.Length;    // Dengiančių failų skaičius
            //string D[] =;         // Failų komponentės
            //long i = D.Length;    // Failų komponenčių skaičius

            long C = _failuSistema.SectorsAssignedPerCluster;

            int S = 2; //slapto rakto dalis, įvedamas

            string B0 = "00000000"; // Inicilizacijos vektorius, įvedamas

            //var N = // Tarpinių skaičiavimų masyvas, tiksliai nežinau kam, gal laisvi sektoriai, ar jau į kokius klasterius įrašyti tie paslėpti failai

            int p = 8;// Įvedamas, slepiamų bitų ilgis - įvedamas nes persigalvoaju, neapsimoka apskaičiuot ten kažko

            M = B0 + M; // Čia reiktų B0 taisyklingo kad veiktų visada :)

            long[] B = M.Chunk(p).Select(x => Convert.ToInt64(new string(x), 2)).ToArray();

            var chosenFiles = ChooseFileSystemFilesBySelectedFilesInDialog();

            if (chosenFiles.Count >= B.Length)
            {

                long[] N = new long[B.Length - 1]; long wot = (long)Math.Pow(2, p);
                for (int i = 0; i < B.Length - 1; i++)
                {
                    N[i] = Helper.Modulo(B[i + 1] - B[i], wot); // Apskaičiuojam kokia liekana turim gaut iš klasterio numerio
                    var tarpiniaiKlasteriai = _failuSistema.Clusters.Where(x => x.Start > _failuSistema.NextFreeSector).ToList(); // Sumažinti nereikalingą patikrinimą iš 1950 į 15 įrašų ir pnš
                    var fileSizeOnDiskInSectors = (long)Math.Ceiling((double)chosenFiles[i].Size / _failuSistema.ClusterSize) * _failuSistema.SectorsAssignedPerCluster;
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

                                richTextBox1.Text += $"- Failas: {chosenFiles[i].Name}, iš ({chosenFiles[i].Clusters.FirstOrDefault().ToString()}), į ({newCluster.ToString()}) \n";

                                var index = _failai.FindIndex(x => x.Equals(chosenFiles[i]));
                                _failai[index].Clusters.Clear();
                                _failai[index].Clusters.Add(newCluster);
                                
                                break;
                            }

                        }
                    }
                }
                var asd = "";
            } else
            {
                MessageBox.Show($"Nepakanka komponenčių failų. Norint paslėpti reikės: {B.Length} arba daugiau, o komponenčių failų failų sistemoje yra: {chosenFiles.Count}.", "Paslėpti failo neįmanoma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        public void InitiliazeInitialStatusForDecoding()
        {
            var failasOut = "test-out.txt";


            //string F[] =;         // Dengiantys failai
            //long m = F.Length;    // Dengiančių failų skaičius
            //string D[] =;         // Failų komponentės
            //long i = D.Length;    // Failų komponenčių skaičius

            long C = _failuSistema.SectorsAssignedPerCluster;

            int S = 2; //slapto rakto dalis, įvedamas

            string B0 = "00000000"; // Inicilizacijos vektorius, įvedamas

            //var N = // Tarpinių skaičiavimų masyvas, tiksliai nežinau kam, gal laisvi sektoriai, ar jau į kokius klasterius įrašyti tie paslėpti failai

            int p = 8;// Įvedamas, slepiamų bitų ilgis - įvedamas nes persigalvoaju, neapsimoka apskaičiuot ten kažko

            var chosenFiles = ChooseFileSystemFilesBySelectedFilesInDialog();

            long[] N = new long[chosenFiles.Count]; long wot = (long)Math.Pow(2, p);

            long[] B = new long[chosenFiles.Count + 1];
            B[0] = Convert.ToInt64(B0, 2);

            string binaryString = "";
            for (int i = 0; i < chosenFiles.Count - 1; i++)
            {
                N[i] = Helper.Modulo(chosenFiles[i].Clusters[0].Start, wot);
                B[i + 1] = Helper.Modulo(N[i] + B[i], wot);
                binaryString += Convert.ToString(Helper.Modulo(N[i] + B[i], wot), 2).PadLeft(p, '0');
            }

/*            for (int i = 0; i < B.Length - 1; i++)
            {
                B[i + 1] = Helper.Modulo(N[i] + B[i], wot); // Cj praleidzia viena
                binaryString += Convert.ToString(Helper.Modulo(N[i] + B[i], wot), 2).PadLeft(p, '0');
            }*/



            richTextBox1.Text = binaryString;

            Helper.WriteBinaryStringToFile(_workingDirectory, failasOut, binaryString);
        }
    }
}