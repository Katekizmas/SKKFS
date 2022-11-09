namespace SKKFS
{
    partial class SKKFS
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
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.testas = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBox1.Location = new System.Drawing.Point(12, 12);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(2040, 414);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // testas
            // 
            this.testas.Location = new System.Drawing.Point(240, 432);
            this.testas.Name = "testas";
            this.testas.Size = new System.Drawing.Size(572, 98);
            this.testas.TabIndex = 1;
            this.testas.Text = "Testas";
            this.testas.UseVisualStyleBackColor = true;
            this.testas.Click += new System.EventHandler(this.testas_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(884, 432);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(201, 98);
            this.button1.TabIndex = 2;
            this.button1.Text = "Pasirinkti failą";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SKKFS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2064, 562);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.testas);
            this.Controls.Add(this.richTextBox1);
            this.Name = "SKKFS";
            this.Text = "Slaptas kanalas klasterinėje failų struktūroje";
            this.Load += new System.EventHandler(this.SKKFS_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private RichTextBox richTextBox1;
        private Button testas;
        private OpenFileDialog openFileDialog1;
        private Button button1;
    }
}