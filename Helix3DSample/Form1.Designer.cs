namespace Helix3DSample
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSaveToImage = new Button();
            btnSincronizar = new Button();
            panel1 = new Panel();
            btnOpenDraw = new Button();
            btnSaveDraw = new Button();
            btnDrawClear = new Button();
            panel2 = new Panel();
            btnTelemetry = new Button();
            btnOpenLog = new Button();
            btnLive = new Button();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // btnSaveToImage
            // 
            btnSaveToImage.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSaveToImage.Location = new Point(1013, 339);
            btnSaveToImage.Name = "btnSaveToImage";
            btnSaveToImage.Size = new Size(101, 23);
            btnSaveToImage.TabIndex = 2;
            btnSaveToImage.Text = "Save To Image";
            btnSaveToImage.UseVisualStyleBackColor = true;
            btnSaveToImage.Click += btnSaveToImage_Click;
            // 
            // btnSincronizar
            // 
            btnSincronizar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSincronizar.Location = new Point(92, 12);
            btnSincronizar.Name = "btnSincronizar";
            btnSincronizar.Size = new Size(75, 23);
            btnSincronizar.TabIndex = 3;
            btnSincronizar.Text = "Sincronizar";
            btnSincronizar.UseVisualStyleBackColor = true;
            btnSincronizar.Click += btnSincronizar_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnOpenDraw);
            panel1.Controls.Add(btnSaveDraw);
            panel1.Controls.Add(btnDrawClear);
            panel1.Location = new Point(34, 23);
            panel1.Name = "panel1";
            panel1.Size = new Size(447, 46);
            panel1.TabIndex = 4;
            // 
            // btnOpenDraw
            // 
            btnOpenDraw.Location = new Point(16, 10);
            btnOpenDraw.Name = "btnOpenDraw";
            btnOpenDraw.Size = new Size(128, 23);
            btnOpenDraw.TabIndex = 2;
            btnOpenDraw.Text = "Abrir desenho";
            btnOpenDraw.UseVisualStyleBackColor = true;
            btnOpenDraw.Click += btnOpenDraw_Click;
            // 
            // btnSaveDraw
            // 
            btnSaveDraw.Location = new Point(150, 10);
            btnSaveDraw.Name = "btnSaveDraw";
            btnSaveDraw.Size = new Size(128, 23);
            btnSaveDraw.TabIndex = 0;
            btnSaveDraw.Text = "Salvar desenho";
            btnSaveDraw.UseVisualStyleBackColor = true;
            btnSaveDraw.Click += btnSaveDraw_Click;
            // 
            // btnDrawClear
            // 
            btnDrawClear.Location = new Point(284, 10);
            btnDrawClear.Name = "btnDrawClear";
            btnDrawClear.Size = new Size(101, 23);
            btnDrawClear.TabIndex = 1;
            btnDrawClear.Text = "Limpar";
            btnDrawClear.UseVisualStyleBackColor = true;
            btnDrawClear.Click += btnDrawClear_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(btnLive);
            panel2.Controls.Add(btnTelemetry);
            panel2.Controls.Add(btnOpenLog);
            panel2.Controls.Add(btnSincronizar);
            panel2.Location = new Point(570, 23);
            panel2.Name = "panel2";
            panel2.Size = new Size(342, 46);
            panel2.TabIndex = 5;
            // 
            // btnTelemetry
            // 
            btnTelemetry.Location = new Point(171, 12);
            btnTelemetry.Name = "btnTelemetry";
            btnTelemetry.Size = new Size(75, 23);
            btnTelemetry.TabIndex = 5;
            btnTelemetry.Text = "Telemetria";
            btnTelemetry.UseVisualStyleBackColor = true;
            btnTelemetry.Click += btnTelemetry_Click;
            // 
            // btnOpenLog
            // 
            btnOpenLog.Location = new Point(13, 12);
            btnOpenLog.Name = "btnOpenLog";
            btnOpenLog.Size = new Size(75, 23);
            btnOpenLog.TabIndex = 4;
            btnOpenLog.Text = "Abrir Log";
            btnOpenLog.UseVisualStyleBackColor = true;
            btnOpenLog.Click += btnOpenLog_Click;
            // 
            // btnLive
            // 
            btnLive.Location = new Point(252, 12);
            btnLive.Name = "btnLive";
            btnLive.Size = new Size(75, 23);
            btnLive.TabIndex = 6;
            btnLive.Text = "Live";
            btnLive.UseVisualStyleBackColor = true;
            btnLive.Click += btnLive_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1128, 403);
            Controls.Add(panel2);
            Controls.Add(btnSaveToImage);
            Controls.Add(panel1);
            Margin = new Padding(2);
            Name = "Form1";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button btnSaveToImage;
        private Button btnSincronizar;
        private Panel panel1;
        private Button btnSaveDraw;
        private Button btnDrawClear;
        private Button btnOpenDraw;
        private Panel panel2;
        private Button btnOpenLog;
        private Button btnTelemetry;
        private Button btnLive;
    }
}
