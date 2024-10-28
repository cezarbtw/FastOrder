using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastOrder
{
    public partial class Form1 : Form
    {
        private DatabaseConnection dbConnection = new DatabaseConnection();
        private List<(byte[] Imagem, DateTime DataUpload, int Tamanho, int Id)> imageList = new List<(byte[], DateTime, int, int)>();
        private int currentIndex = 0;
        private int currentPage = 0;
        private int itemsPerPage = 100;
        private SortingCriteria currentCriteria = SortingCriteria.DataUpload;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadImages();
            RefreshImageButtons();
            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            int totalPages = (int)Math.Ceiling((double)imageList.Count / itemsPerPage);
            label5.Text = $"Página {currentPage + 1} de {totalPages}";
        }

        // Carrega os metadados das imagens
        private async Task LoadImages()
        {
            imageList.Clear();
            flowLayoutPanel1.Controls.Clear();

            await Task.Run(() =>
            {
                using (SqlConnection connection = dbConnection.OpenConnection())
                {
                    string sql = "SELECT Id, DataUpload, Tamanho FROM Imagens";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            DateTime dataUpload = reader.GetDateTime(1);
                            int tamanho = reader.GetInt32(2);

                            imageList.Add((null, dataUpload, tamanho, id));
                        }
                    }
                }
            });
        }

        // Carrega a imagem correspondente pelo ID no PictureBox
        private async Task LoadImageById(int id)
        {
            await Task.Run(() =>
            {
                using (SqlConnection connection = dbConnection.OpenConnection())
                {
                    string sql = "SELECT Imagem, DataUpload, Tamanho FROM Imagens WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            byte[] imageData = (byte[])reader["Imagem"];
                            DateTime dataUpload = reader.GetDateTime(1);
                            int tamanho = reader.GetInt32(2);

                            this.Invoke(new Action(() =>
                            {
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    pictureBox1.Image = Image.FromStream(ms);
                                }

                                label3.Text = $"ID: {id}\nTamanho: {tamanho} bytes\nData de Upload: {dataUpload:dd/MM/yyyy HH:mm:ss}";
                            }));
                        }
                    }
                }
            });
        }


        // Evento de clique no botão de imagem no FlowLayoutPanel
        private async void ImageButton_Click(object sender, EventArgs e)
        {
            if (sender is Button clickedButton)
            {
                int imageId = (int)clickedButton.Tag;

                // Atualiza o currentIndex com base no ID da imagem clicada no FlowLayoutPanel
                currentIndex = imageList.FindIndex(item => item.Id == imageId);

                await LoadImageById(imageId);  // Carrega a imagem no PictureBox
            }
        }

        // Paginação (navegação entre imagens no PictureBox)
        private async void Previous_Click(object sender, EventArgs e)
        {
            if (imageList.Count > 0)
            {
                // Continua a partir da imagem selecionada no FlowLayoutPanel
                currentIndex = (currentIndex - 1 + imageList.Count) % imageList.Count;
                int imageId = imageList[currentIndex].Id;
                await LoadImageById(imageId);  // Carrega a imagem anterior
            }
        }

        private async void Next_Click(object sender, EventArgs e)
        {
            if (imageList.Count > 0)
            {
                // Continua a partir da imagem selecionada no FlowLayoutPanel
                currentIndex = (currentIndex + 1) % imageList.Count;
                int imageId = imageList[currentIndex].Id;
                await LoadImageById(imageId);  // Carrega a próxima imagem
            }
        }


        // Paginação dos botões no FlowLayoutPanel

        private void button1_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)imageList.Count / itemsPerPage);
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                RefreshImageButtons();
                UpdatePageLabel();
            }
        }

        // Botão de página anterior
        private void button2_Click(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                RefreshImageButtons();
                UpdatePageLabel();
            }
        }

        // Inserção de uma nova imagem no banco de dados
        private void Inserir_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Selecione uma Imagem"
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    byte[] imageData = File.ReadAllBytes(filePath);
                    InsertImageIntoDatabase(imageData);
                }
            }
        }

        private async void InsertImageIntoDatabase(byte[] imageData)
        {
            int insertedImageId;

            using (SqlConnection connection = dbConnection.OpenConnection())
            {
                string sql = "INSERT INTO Imagens (Imagem, DataUpload, Tamanho) OUTPUT INSERTED.Id VALUES (@Imagem, @DataUpload, @Tamanho)";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Imagem", imageData);
                    command.Parameters.AddWithValue("@DataUpload", DateTime.Now);
                    command.Parameters.AddWithValue("@Tamanho", imageData.Length);

                    // Executa o comando e captura o ID da imagem inserida
                    insertedImageId = (int)command.ExecuteScalar();
                }
            }

            MessageBox.Show("Imagem inserida com sucesso!");

            // Carregar a imagem recém-inserida no PictureBox
            await LoadImageById(insertedImageId);

            // Atualiza os metadados e o FlowLayoutPanel
            await ReloadMetadataAfterInsert();
        }

        private async Task ReloadMetadataAfterInsert()
        {
            imageList.Clear();
            flowLayoutPanel1.Controls.Clear();
            await LoadImages();
            currentPage = (imageList.Count - 1) / itemsPerPage;
            RefreshImageButtons();
        }

        // Métodos de ordenação
        private void BubbleSort_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < imageList.Count - 1; i++)
            {
                for (int j = 0; j < imageList.Count - i - 1; j++)
                {
                    bool shouldSwap = false;

                    switch (currentCriteria)
                    {
                        case SortingCriteria.DataUpload:
                            shouldSwap = imageList[j].Item2 > imageList[j + 1].Item2;
                            break;
                        case SortingCriteria.Tamanho:
                            shouldSwap = imageList[j].Item3 > imageList[j + 1].Item3;
                            break;
                        case SortingCriteria.Id:
                            shouldSwap = imageList[j].Item4 > imageList[j + 1].Item4;
                            break;
                    }

                    if (shouldSwap)
                    {
                        var temp = imageList[j];
                        imageList[j] = imageList[j + 1];
                        imageList[j + 1] = temp;
                    }
                }
            }

            stopwatch.Stop();

            TimeSpan elapsed = stopwatch.Elapsed;

            string tempoFormatado = string.Format("{0}m {1}s {2}ms",
    (int)elapsed.TotalMinutes,  // Minutos totais
    elapsed.Seconds,             // Segundos
    elapsed.Milliseconds);       // Milissegundos

            RefreshImageButtons();
            MessageBox.Show($"Ordenação por bolha completa em {tempoFormatado}.");
        }

        private void InsertionSort_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Algoritmo de ordenação por inserção
            for (int i = 1; i < imageList.Count; i++)
            {
                var key = imageList[i];
                int j = i - 1;

                // Lógica de ordenação com comparação diretamente no laço
                while (j >= 0)
                {
                    bool shouldSwap = false;

                    // Verifica o critério de ordenação selecionado
                    switch (currentCriteria)
                    {
                        case SortingCriteria.DataUpload:
                            shouldSwap = key.DataUpload < imageList[j].DataUpload;  // Ordena por DataUpload
                            break;
                        case SortingCriteria.Tamanho:
                            shouldSwap = key.Tamanho < imageList[j].Tamanho;  // Ordena por Tamanho
                            break;
                        case SortingCriteria.Id:
                            shouldSwap = key.Id < imageList[j].Id;  // Ordena por Id
                            break;
                    }

                    // Se não for necessário fazer troca, interrompe o laço
                    if (!shouldSwap)
                        break;

                    imageList[j + 1] = imageList[j];
                    j--;
                }

                imageList[j + 1] = key;
            }

            stopwatch.Stop();

            TimeSpan elapsed = stopwatch.Elapsed;

            string tempoFormatado = string.Format("{0}m {1}s {2}ms",
     (int)elapsed.TotalMinutes,  // Minutos totais
     elapsed.Seconds,             // Segundos
     elapsed.Milliseconds);       // Milissegundos


            // Atualiza os botões e exibe o tempo de execução
            RefreshImageButtons();
            MessageBox.Show($"Ordenação por inserção completa em {tempoFormatado}.");
        }



        private void QuickSort_Click(object sender, EventArgs e)
        {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();



            int i = 0; // Inicializa i
            int j = imageList.Count - 1; // Inicializa j como o último índice da lista

            // Verifica se a lista contém pelo menos dois elementos
            if (imageList.Count < 2)
            {
                MessageBox.Show("A lista deve ter pelo menos dois elementos para ordenar.");
                return;
            }

            // Verifica se i é menor que j antes de começar a ordenar
            if (i < j)
            {
                var key = imageList[j];  // Definindo o pivô como o último elemento do intervalo
                int partitionIndex = i - 1;

                for (int k = i; k < j; k++)
                {
                    bool shouldSwap = false;

                    // Escolhe o critério de ordenação
                    switch (currentCriteria)
                    {
                        case SortingCriteria.DataUpload:
                            shouldSwap = imageList[k].DataUpload < key.DataUpload;
                            break;
                        case SortingCriteria.Tamanho:
                            shouldSwap = imageList[k].Tamanho < key.Tamanho;
                            break;
                        case SortingCriteria.Id:
                            shouldSwap = imageList[k].Id < key.Id;
                            break;
                    }

                    if (shouldSwap)
                    {
                        partitionIndex++;

                        // Troca usando uma variável temporária
                        if (partitionIndex < imageList.Count && k < imageList.Count)
                        {
                            var tempSwap = imageList[partitionIndex];
                            imageList[partitionIndex] = imageList[k];
                            imageList[k] = tempSwap;
                        }
                    }
                }


                // Atualiza j e executa a lógica semelhante ao código fornecido
                j--;
                while (j > partitionIndex) // Mova todos os elementos para a direita
                {
                    if (j + 1 < imageList.Count) // Verifica se o índice está dentro do intervalo
                    {
                        imageList[j + 1] = imageList[j]; // Desloca o elemento para a direita
                    }
                    j--;
                }


                if (partitionIndex + 1 < imageList.Count) // Verifica se o índice está dentro do intervalo
                {
                    imageList[partitionIndex + 1] = key; // Coloca o pivô na posição correta
                }

                // Para fins de medição de tempo
                stopwatch.Stop();
                TimeSpan elapsed = stopwatch.Elapsed;

                string tempoFormatado = string.Format("{0}m {1}s {2}ms",
                    (int)elapsed.TotalMinutes,  // Minutos totais
                    elapsed.Seconds,             // Segundos
                    elapsed.Milliseconds);       // Milissegundos

                // Atualiza os botões e exibe o tempo de execução
                RefreshImageButtons();
                MessageBox.Show($"Ordenação por Ordenação rápida completa em {tempoFormatado}.");
            }
        }





        // Atualiza os botões após ordenação ou navegação de página
        private void RefreshImageButtons()
        {
            flowLayoutPanel1.Controls.Clear();

            // Calcula o intervalo de botões para a página atual
            int startIndex = currentPage * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, imageList.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                var item = imageList[i];
                Button imageButton = new Button
                {
                    Text = $"ID: {item.Id}\n Tamanho: {item.Tamanho}\n Data: {item.DataUpload}",
                    Width = 120,
                    Height = 70,
                    Tag = item.Id
                };
                imageButton.Click += ImageButton_Click;
                flowLayoutPanel1.Controls.Add(imageButton);
            }
        }

        // Radio buttons para selecionar o critério de ordenação
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                currentCriteria = SortingCriteria.DataUpload;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                currentCriteria = SortingCriteria.Tamanho;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                currentCriteria = SortingCriteria.Id;
        }

        public enum SortingCriteria
        {
            DataUpload,
            Tamanho,
            Id
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
