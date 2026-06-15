namespace Helix3DSample
{
    public partial class Form1 : Form
    {
        MyEngine3D myEngine3D;

        public Form1()
        {
            InitializeComponent();
            myEngine3D = new MyEngine3D(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myEngine3D.StartViewPort();
            myEngine3D.SetMouseDrawing();
        }

        private void btnSaveToImage_Click(object sender, EventArgs e)
        {
            myEngine3D.ExportToImage("D:\\teste.png");
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            await myEngine3D.SyncWithDevice();
        }

        private async void btnSaveDraw_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog salvarDialog = new SaveFileDialog())
            {
                salvarDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                salvarDialog.Title = "Salvar arquivo como";
                salvarDialog.RestoreDirectory = true;
                salvarDialog.FileName = myEngine3D.GetCustomFilename("gpsdata_draw");

                if (salvarDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        myEngine3D.SaveDraw(salvarDialog.FileName);
                        MessageBox.Show("Arquivo salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao salvar arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnDrawClear_Click(object sender, EventArgs e)
        {
            await myEngine3D.Clear();
        }

        private async void btnOpenDraw_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog abrirDialog = new OpenFileDialog())
            {
                abrirDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                abrirDialog.Title = "Selecione o arquivo JSONL";
                abrirDialog.RestoreDirectory = true;

                if (abrirDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await myEngine3D.OpenDraw(abrirDialog.FileName);

                        MessageBox.Show("Arquivo aberto com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao abrir arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnOpenLog_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog abrirDialog = new OpenFileDialog())
            {
                abrirDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                abrirDialog.Title = "Selecione o arquivo JSONL";
                abrirDialog.RestoreDirectory = true;

                if (abrirDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await myEngine3D.OpenGpsLog(abrirDialog.FileName);

                        MessageBox.Show("Arquivo aberto com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao abrir arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnTelemetry_Click(object sender, EventArgs e)
        {
            await myEngine3D.ApplyLabels();
        }

        private async void btnLive_Click(object sender, EventArgs e)
        {
            await myEngine3D.Live();
        }
    }
}
